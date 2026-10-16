#!/usr/bin/env bash
# Frontend API handoff, including the cart endpoints.
# Copy individual examples and replace placeholders before using them.
# This file is a reference, not an automated end-to-end test.
# Run the API with: cd MyMvcApp && dotnet run --launch-profile http

BASE_URL='http://localhost:5015'
USER_TOKEN='PASTE_LOGIN_DATA_TOKEN'
ADMIN_TOKEN='PASTE_ADMIN_LOGIN_DATA_TOKEN'
REFRESH_TOKEN='PASTE_LOGIN_DATA_REFRESH_TOKEN'
PRODUCT_ID='REPLACE_WITH_PRODUCT_GUID'
TARGET_USER_ID='REPLACE_WITH_USER_GUID'
RESET_COOKIE_JAR='./password-reset-cookies.txt'

# Login response: data.token, data.refreshToken, data.id, data.role.
# Refresh response: token, refreshToken at the root; store both new values.
# Register creates a User. Admin-only calls require an existing Admin token.
# JSON names are camelCase. DTOs bind from JSON even without explicit [FromBody].
# CancellationToken is server supplied; never send it in the JSON body.

# 1. Register — public; success: 200.
# userName and phoneNumber: max 10 characters.
# Password: 8-128 characters, uppercase/lowercase/digit/special, no whitespace.
curl -i -X POST "$BASE_URL/api/Auth/Register" \
  -H 'Content-Type: application/json' \
  --data-raw '{"userName":"rahul","email":"rahul@example.com","phoneNumber":"9876543210","password":"Test@1234"}'

# 2. Login — public; success: 200; wrong credentials currently return 404.
# Copy data.token to USER_TOKEN and data.refreshToken to REFRESH_TOKEN.
curl -i -X POST "$BASE_URL/api/Auth/Login" \
  -H 'Content-Type: application/json' \
  --data-raw '{"email":"rahul@example.com","password":"Test@1234"}'

# 3. Refresh token — public; success: 200; invalid/expired token: 401.
# This rotates the refresh token. Replace saved tokens with response.token and
# response.refreshToken before subsequent requests.
curl -i -X POST "$BASE_URL/api/Auth/Refresh-Token" \
  -H 'Content-Type: application/json' \
  --data-raw "{\"refreshToken\":\"$REFRESH_TOKEN\"}"

# 4. Current user's profile — Admin or User; success: 200.
curl -i "$BASE_URL/api/Auth/GetUserProfile" \
  -H "Authorization: Bearer $USER_TOKEN"

# 5. Update current user's profile — Admin or User; success: 200.
# Send both fields: omitted fields default to empty strings in the current DTO.
curl -i -X POST "$BASE_URL/api/Auth/Update-Profile" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -H 'Content-Type: application/json' \
  --data-raw '{"userName":"rahulnew","phoneNumber":"9876543211"}'

# 6. Upload profile image — Admin or User; success: 200.
# Replace the local file path. JPEG, PNG or WebP; maximum 5 MB.
# Let curl/browser set multipart Content-Type and its boundary.
curl -i -X POST "$BASE_URL/api/Auth/Upload-Profile-Image" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -F 'ProfileImage=@/absolute/path/profile.jpg;type=image/jpeg'

# 7. Forgot password — public; success: 200; unknown email: 404.
# Save the reset-session cookie. Use the SAME cookie jar for steps 7, 8 and 9.
curl -i -X POST "$BASE_URL/api/Auth/Forgot-Password" \
  -c "$RESET_COOKIE_JAR" \
  -H 'Content-Type: application/json' \
  --data-raw '{"email":"rahul@example.com"}'

# 8. Verify OTP — public; success: 200; invalid/expired OTP: 400.
# Replace 123456 with the emailed six-digit code. OTP is a JSON string.
# Save the rotated cookie returned by this call.
curl -i -X POST "$BASE_URL/api/Auth/verify-otp" \
  -b "$RESET_COOKIE_JAR" -c "$RESET_COOKIE_JAR" \
  -H 'Content-Type: application/json' \
  --data-raw '{"otp":"123456"}'

# 9. Reset password — public, requires verified reset cookie; success: 200.
# No email or OTP in this request. Reset invalidates the saved refresh token.
curl -i -X POST "$BASE_URL/api/Auth/Reset-Password" \
  -b "$RESET_COOKIE_JAR" -c "$RESET_COOKIE_JAR" \
  -H 'Content-Type: application/json' \
  --data-raw '{"password":"Reset@1234"}'

# 10. Change password — Admin or User; success: 200.
# Use the account's actual current password. This invalidates its refresh token.
curl -i -X POST "$BASE_URL/api/Auth/Change-Password" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -H 'Content-Type: application/json' \
  --data-raw '{"currentPassword":"Reset@1234","newPassword":"Changed@1234"}'

# 11. Add product — Admin only; success: 200.
# Save response.data.id as PRODUCT_ID. Price must be zero or greater.
curl -i -X POST "$BASE_URL/api/Product/Add-Product" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H 'Content-Type: application/json' \
  --data-raw '{"productName":"Keyboard","price":1499.50,"desc":"Wireless keyboard"}'

# 12. List/filter products — Admin or User; success: 200.
# All query parameters are optional. Defaults: page=1, pageSize=10.
# page >= 1; pageSize 1-100; price filters >= 0; minPrice <= maxPrice.
# Response: data.items, data.page, data.pageSize, data.totalRecords,
# data.totalPages, data.hasPreviousPage, data.hasNextPage.
curl -i --get "$BASE_URL/api/Product/GetAll-Product" \
  -H "Authorization: Bearer $USER_TOKEN" \
  --data-urlencode 'page=1' \
  --data-urlencode 'pageSize=10' \
  --data-urlencode 'search=keyboard' \
  --data-urlencode 'minPrice=100' \
  --data-urlencode 'maxPrice=5000'

# 13. Get one product — Admin or User; success: 200; absent: 404.
curl -i "$BASE_URL/api/Product/GetProductById/$PRODUCT_ID" \
  -H "Authorization: Bearer $USER_TOKEN"

# 14. Update product — Admin only; success: 200; absent: 404.
# Only include fields to update; omitted/null fields retain existing values.
curl -i -X PUT "$BASE_URL/api/Product/Update-ProductById/$PRODUCT_ID" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H 'Content-Type: application/json' \
  --data-raw '{"productName":"Mechanical Keyboard","price":1999.99,"desc":"Updated description"}'

# 15. Delete product — Admin only; success: 200; absent: 404.
curl -i -X DELETE "$BASE_URL/api/Product/DeleteProduct/$PRODUCT_ID" \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# 16. List all users — Admin only; success: 200.
# Response data is an array of { id, userName, roleName }.
curl -i "$BASE_URL/api/RoleAssign/GetAllUsers" \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# 17. Assign a role — Admin only; success: 200; absent user: 404.
# userId is the target user's GUID; roleName is exactly Admin or User.
curl -i -X POST "$BASE_URL/api/RoleAssign/Role-Assign" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H 'Content-Type: application/json' \
  --data-raw "{\"userId\":\"$TARGET_USER_ID\",\"roleName\":\"Admin\"}"

# 18. Add a product to the current user's cart — Admin or User; success: 200.
# Alias: POST /api/Cart/Add. Quantity defaults to 1 when omitted.
# Same product adds to the existing quantity. Do not send price/name/userId:
# product details are read from the database and the owner comes from the JWT.
curl -i -X POST "$BASE_URL/api/Cart/Add-To-Cart" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -H 'Content-Type: application/json' \
  --data-raw "{\"productId\":\"$PRODUCT_ID\",\"quantity\":1}"

# 19. Read the current user's cart — Admin or User; success: 200.
# data is an array with id (cart item), productId, productName, price, desc,
# quantity, totalPrice, userId, createdAt, updatedAt. An empty cart returns [].
# Prices/names/descriptions reflect the current product record.
curl -i "$BASE_URL/api/Cart" \
  -H "Authorization: Bearer $USER_TOKEN"

# Frontend integration notes:
# - For the three reset requests, fetch needs credentials: "include";
#   Axios needs withCredentials: true. The cookie is HttpOnly: JS cannot read it.
# - Keep the same hostname throughout (localhost with localhost). Development
#   CORS allows localhost/127.0.0.1 on ports 5173 and 4173, with credentials.
# - Reset sessions expire after 5 minutes and allow 5 failed OTP attempts.
# - Protected calls use Authorization: Bearer <token>; missing/invalid JWT: 401;
#   insufficient role: 403. Do not send CancellationToken as request data.
# - API error format: {"message":"...","success":false,"data":null}.
#   The status code is on the HTTP response, not in the JSON body.
# - Success response shapes differ: login uses data, refresh returns a bare DTO;
#   the product-list message field is currently spelled "messsage" in code.
# - Swagger (Development): http://localhost:5015/swagger
