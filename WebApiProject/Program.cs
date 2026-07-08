using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebApiProject.Data;
using WebApiProject.Exceptions;
using WebApiProject.Extensions;
using WebApiProject.Interfaces;
using WebApiProject.Models;

var builder = WebApplication.CreateBuilder(args);

var connection = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

// DB Context Configuration
builder.Services.AddDbContext<WebApiDbContext>(options =>
    options.UseMySql(
        connection,
        ServerVersion.AutoDetect(connection)
    )
);

// User Identity Configuration
builder.Services.AddIdentity<User, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 6;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._";
    })
    .AddEntityFrameworkStores<WebApiDbContext>()
    .AddDefaultTokenProviders();

// Cookie settings to prevent redirecting to login page for API requests
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = 403;
        return Task.CompletedTask;
    };
});

//JWT Authentication Configuration

// Clear default claims mapping
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); 
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "JwtBearer";
        options.DefaultChallengeScheme = "JwtBearer";
    })
    .AddJwtBearer("JwtBearer", options => {
        options.MapInboundClaims = false; // Prevents automatic mapping of claims
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = JwtRegisteredClaimNames.Name,
            RoleClaimType = "role" // Use the custom "role" claim type
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti);
                if(jti == null)
                {
                    context.Fail("Token tidak valid");
                    return;
                }

                var blacklist = context.HttpContext.RequestServices.GetRequiredService<ITokenServices>();
                if(await blacklist.IsTokenRevokedAsync(jti.Value))
                {
                    context.Fail("Token telah dicabut");
                }
            }
        };
});

builder.Services.AddMemoryCache();
builder.Services.AddAuthorization();
builder.Services.AddApplicationServices(); 
builder.Services.AddControllers();

var app = builder.Build();

//Exception Handling
app.UseExceptionHandler( errorApp =>
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        var (status, message) = exception switch
        {
            AppExceptions ex => (ex.StatusCode, ex.Message),
            _ => (500, "Internal Server Error")
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { message = message });
    })
);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
