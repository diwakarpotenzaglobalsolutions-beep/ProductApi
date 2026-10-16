The API uses only the local MySQL database `jwt_auth`
(`127.0.0.1:3306`). Set `ConnectionStrings:MySqlConnection` in .NET user secrets
for Development, or `ConnectionStrings__MySqlConnection` for deployment.
The checked-in connection string contains no password.

MySQL migrations live in `Migrations/MySql` and use `MySqlDbContext`.
To add a migration, run:

```sh
dotnet ef migrations add YourChange --context MySqlDbContext --output-dir Migrations/MySql
```

SQLite is no longer supported by the API. The old database file and migration
source files are retained as historical records. Keep using `MySqlDbContext`
for migrations so the existing MySQL migration history remains intact.
The optional `DatabaseTransfer` tool has its own SQLite dependency solely for
reading an old database during a one-time import.

To copy existing SQLite users/products into an empty MySQL database, run from
`MyMvcApp` after applying the MySQL migration:

```sh
dotnet run --project ../tools/DatabaseTransfer -- users.db
```

The transfer reads SQLite without modifying it, preserves valid IDs and password hashes,
assigns new GUIDs to legacy rows with invalid IDs,
verifies every copied field, and commits the MySQL rows in one transaction.
It refuses to overwrite an occupied target. Dates are normalized to MySQL's
microsecond precision; uninitialized dates become the expired date 1970-01-01.

Run the API from this directory:

```sh
dotnet ef database update --context MySqlDbContext
dotnet run --launch-profile http
```

Email delivery uses Gmail SMTP (`smtp.gmail.com`, port 587) and the sender
configured in `appsettings.json`. The password is intentionally empty in that
file. For Development, store a Gmail App Password in .NET user secrets:

```sh
dotnet user-secrets set "EmailSettings:Password" "YOUR_APP_PASSWORD"
dotnet run --launch-profile http
```

The project already has a `UserSecretsId`. User secrets are loaded automatically
in Development. Gmail SMTP password authentication requires an App Password
rather than your normal account password.

Create the App Password at https://myaccount.google.com/apppasswords after enabling
2-Step Verification. Replace the stored secret using the command above and restart
the API. A normal account password will not work with this Gmail SMTP setup.
Email configuration and SMTP failures return HTTP 503 with an email-specific
message. Failed email delivery does not persist a new reset OTP.

For deployment, configure environment variables before starting the API:

```sh
export EmailSettings__DeliveryMethod=Network
export EmailSettings__SenderEmail='your-sender@gmail.com'
export EmailSettings__SmtpHost='smtp.gmail.com'
export EmailSettings__SmtpPort=587
export EmailSettings__Password='your-smtp-app-password'
export PASSWORD_RESET_OTP_SECRET='your-long-random-secret'
dotnet run --launch-profile http
```

For local email pickup, set `EmailSettings__DeliveryMethod=SpecifiedPickupDirectory`
and `EmailSettings__PickupDirectory` to an absolute directory outside `wwwroot`.
Messages will be saved as `.eml` files in that directory instead of delivered.

Keep credentials out of source files. OTP hashing uses `PASSWORD_RESET_OTP_SECRET`
or `PasswordReset__OtpSecret` when configured, otherwise the existing JWT signing
secret with a password-reset purpose prefix. Keep the secret stable across restarts.

Reset flow (use the same browser or cookie jar for all three requests):

1. POST `api/Auth/Forgot-Password` with `{"email":"registered@example.com"}`.
2. POST `api/Auth/verify-otp` with `{"otp":"123456"}` (use the emailed code).
3. POST `api/Auth/Reset-Password` with `{"password":"NewPassword123!"}`.

Forgot-Password sets an HttpOnly, SameSite=Strict cookie identifying a temporary
reset session. Only its hash is stored in the database. Verify-OTP rotates that
cookie and consumes the OTP; Reset-Password consumes the verified session and
clears the cookie. A new Forgot-Password request invalidates the previous session
for that account. Sessions expire five minutes after Forgot-Password and allow
five failed OTP verification attempts. Resetting invalidates the refresh token.
HTTPS requests use a Secure cookie.

Swagger on this API's origin handles the cookie automatically. For a separate
frontend, use `credentials: "include"` with fetch or `withCredentials: true` with
Axios. Use the same hostname for frontend and API (for example, both `localhost`)
so the SameSite cookie can be sent; the allowed development origins support CORS
credentials.

For curl, preserve cookies between requests:

```sh
curl -c /tmp/reset-cookies.txt -b /tmp/reset-cookies.txt \
  -H 'Content-Type: application/json' \
  -d '{"email":"registered@example.com"}' \
  http://localhost:5015/api/Auth/Forgot-Password
curl -c /tmp/reset-cookies.txt -b /tmp/reset-cookies.txt \
  -H 'Content-Type: application/json' \
  -d '{"otp":"123456"}' \
  http://localhost:5015/api/Auth/verify-otp
curl -c /tmp/reset-cookies.txt -b /tmp/reset-cookies.txt \
  -H 'Content-Type: application/json' \
  -d '{"password":"NewPassword123!"}' \
  http://localhost:5015/api/Auth/Reset-Password
```

To change a password while signed in, POST `api/Auth/Change-Password` with the
login JWT in the `Authorization: Bearer <token>` header and this JSON body:

```json
{"currentPassword":"OldPassword123!","newPassword":"NewPassword123!"}
```

A successful change saves the new password hash, updates `UpdatedAt`, and invalidates the
refresh token and any outstanding password-reset session. Issued access JWTs
keep their existing expiration time.

Run the reset and change-password integration checks with `python3 tests/password_reset_smoke.py`.
It delegates to the MySQL integration suite, using a temporary schema and local mail directory.

HTTP responses use these status codes:

| Status | Meaning |
| --- | --- |
| 200 | Successful registration, product/cart addition, read, login, update, delete, or password operation |
| 400 | Invalid fields, malformed JSON, invalid OTP/reset session, or incorrect current password |
| 401 | Missing/invalid JWT or invalid refresh token |
| 403 | Authenticated caller lacks the required role |
| 404 | Route, user, or product not found; incorrect login credentials |
| 405 | Unsupported HTTP method; `Allow` lists supported methods |
| 409 | Registration email already exists |
| 413 | Profile image exceeds 5 MB or the server rejects an oversized request |
| 415 | Unsupported request content type or profile image type |
| 500 | Unexpected internal failure; exception details stay in server logs |
| 503 | Password-reset email delivery unavailable |

All API error responses contain only `message`, `success: false`, and `data: null`.
The HTTP response status carries the status code.
Empty framework error responses, including 429 if a rate limiter is added,
use the same JSON format. No rate limiter is configured by this change.
Existing successful response bodies retain their formats.

Run `dotnet build` followed by `python3 tests/password_reset_smoke.py` to check
these responses along with the password flows using an isolated temporary MySQL schema.

Run the MySQL integration checks from `MyMvcApp` with:

```sh
dotnet run --project ../tools/MySqlSmoke
```

This uses the configured server credentials, creates a unique temporary schema,
checks the API with local email pickup, and drops only that temporary schema.
It covers MySQL migrations, login/roles, product decimal CRUD and filtering,
password reset/change and token revocation.

Controller cancellation tokens are bound to the aborted HTTP request and passed
through services to EF Core queries/writes, profile uploads, and SMTP sending.
The global middleware handles request cancellation without writing a server-error
response. The MySQL smoke suite also checks cancelled service calls and cancellation
while SMTP is waiting for a server greeting, without sending real email.

The cart accepts product IDs instead of copied product details. Send an authenticated
`POST /api/Cart/Add-To-Cart` (also available as `POST /api/Cart/Add`):

```json
{"productId":"PRODUCT_GUID","quantity":1}
```

Quantity defaults to 1. The owner is taken from the JWT, and product name, price,
and description are read from the products table. Repeated additions increase
quantity on the same cart item, including concurrent requests. Invalid/empty IDs
or quantities below 1 return 400; a nonexistent product returns 404.

`GET /api/Cart` returns only the signed-in user's cart in `data`, with each item's
`id`, `productId`, `productName`, `price`, `desc`, `quantity`, `totalPrice`, `userId`,
`createdAt` and `updatedAt`. An empty cart returns `data: []`. Prices and details
reflect the current product record for linked rows. Existing rows without a product
ID return their saved details and `productId: null`. Deleting a product removes its
linked cart rows.

The existing `CartDB` entity and `cartDBs` table store the cart. Product details are
copied from the database on addition, alongside `ProductId` and `Quantity`.
The `UseExistingCartTable` migration preserves existing rows, gives them quantity 1,
and transfers rows from the temporary `cartItems` table before dropping that table.
It adds a product foreign key and a unique `(UserId, ProductId)` index to `cartDBs`.
Apply the migration before running the updated API:

```sh
dotnet ef database update --context MySqlDbContext
```

Cart cURL examples are in `docs/api-curl-examples.sh`. The MySQL smoke suite checks
cart validation, ownership, current product details, repeated/concurrent additions,
and product deletion using its disposable schema.
