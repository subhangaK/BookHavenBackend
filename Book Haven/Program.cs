using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Book_Haven;
using Book_Haven.Entities;
using Book_Haven.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ApplicationDbContext>(p => p.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<User, Roles>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
               .AllowAnyMethod() // Includes OPTIONS for preflight
               .WithHeaders("Authorization", "Content-Type") // Explicitly allow Authorization
               .AllowCredentials(); // Allow credentials (e.g., JWT token)
    });
});

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Log incoming requests
app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
    await next.Invoke();
    Console.WriteLine($"Response: {context.Response.StatusCode}");
});

// Apply CORS before authentication and authorization
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();
app.UseStaticFiles();

// Force HTTPS in development
app.Urls.Clear();
app.Urls.Add("https://localhost:7189");

app.Run();