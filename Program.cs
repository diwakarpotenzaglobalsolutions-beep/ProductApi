using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyMvcApp.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MyMvcApp.Services;
using System.Security.AccessControl;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MyMvcApp.Middleware;
using MyMvcApp.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

 
builder.Services.AddControllersWithViews()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
            new BadRequestObjectResult(ApiErrorResponse.Create(
                StatusCodes.Status400BadRequest, "Request validation failed."));
        options.SuppressMapClientErrors = true;
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.AllowTrailingCommas = true;
    });

// Swagger services
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddDbContext<MySqlDbContext>(options =>options.UseMySQL(builder.Configuration.GetConnectionString("MySqlConnection") ?? throw new InvalidOperationException("MySqlConnection is not configured.")));
builder.Services.AddScoped<MyDbContext>(services => services.GetRequiredService<MySqlDbContext>());
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthServices,AuthServices>();
builder.Services.AddScoped<ProductServices>();
builder.Services.AddScoped<ICartServices, CartServices>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:4173",
                "http://127.0.0.1:4173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["AppSettings:Issuer"],
        ValidAudience = builder.Configuration["AppSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["AppSettings:Token"]!)),
        RoleClaimType = ClaimTypes.Role
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var identity = context.Principal?.Identity as ClaimsIdentity;
            if (identity is null || !Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                context.Fail("Invalid user identity.");
                return;
            }
            var db = context.HttpContext.RequestServices.GetRequiredService<MyDbContext>();
            var user = await db.users.AsNoTracking().FirstOrDefaultAsync(user => user.ID == userId, context.HttpContext.RequestAborted);
            if (user is null)
            {
                context.Fail("User no longer exists.");
                return;
            }
            // Read the current role so a demoted admin loses access immediately,
            // even when an earlier access token has not expired yet.
            foreach (var claim in identity.FindAll(identity.RoleClaimType).ToList())
                identity.RemoveClaim(claim);
            identity.AddClaim(new Claim(identity.RoleClaimType,
                string.IsNullOrWhiteSpace(user.Roles) ? "User" : user.Roles));
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole("Admin", "User")
        .Build();
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "MyMvcApp",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Login se mila token paste karo. Bearer word mat likho."
    });

    options.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseStatusCodePages(async statusContext =>
{
    var context = statusContext.HttpContext;
    await context.Response.WriteAsJsonAsync(
        ApiErrorResponse.Create(context.Response.StatusCode), cancellationToken: context.RequestAborted);
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "MyMvcApp v1");
        options.RoutePrefix = "swagger";
        options.EnablePersistAuthorization();
    });
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseCors("Frontend");
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
