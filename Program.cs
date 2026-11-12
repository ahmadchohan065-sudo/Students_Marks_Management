using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;                       // CHANGED (replaces the Swagger usings)
using MarksApi.Common;
using MarksApi.Data;
using MarksApi.Data.Entities;
using MarksApi.Endpoints;
using MarksApi.Services;
using MarksApi.SmokeTests;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration ----
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwt = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key))
    throw new InvalidOperationException("Jwt:Key is not configured. Set it with user-secrets or an environment variable.");
builder.Services.Configure<JwtOptions>(jwtSection);
builder.Services.Configure<MarkingOptions>(builder.Configuration.GetSection("Marking"));

// ---- Database ----
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Database"))
       .UseSnakeCaseNamingConvention());

// ---- Dependency injection ----
builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IReportService, ReportService>();

// ---- Errors: problem details everywhere ----
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ---- Authentication and authorization ----
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = jwt.Issuer,
            ValidateAudience = true, ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };
    });
builder.Services.AddAuthorization(o => o.AddPolicy("TeacherOnly", p => p.RequireRole("Teacher")));

// ---- CORS: only origins from configuration ----
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

// ---- Health check and API documentation ----
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();
builder.Services.AddOpenApi(o => o.AddDocumentTransformer<BearerSecurityTransformer>());   // CHANGED

var app = builder.Build();

// ---- Middleware pipeline (order matters) ----
app.UseExceptionHandler();      // unexpected exceptions -> clean 500
app.UseStatusCodePages();       // empty 401/403/404 -> problem details body
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi().AllowAnonymous();            // CHANGED: JSON description at /openapi/v1.json
app.MapScalarApiReference();                  // CHANGED: interactive docs page

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (ctx, report) =>
        await ctx.Response.WriteAsJsonAsync(new { status = report.Status.ToString() })
}).AllowAnonymous();

app.MapAuthEndpoints();
app.MapStudentEndpoints();
app.MapCourseEndpoints();
app.MapEnrollmentEndpoints();
app.MapReportEndpoints();

// ---- Demo data, development only ----
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await DbSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), app.Configuration);
}

// ---- Smoke test: only when asked ----
var smoke = args.Contains("--smoke-test") || app.Configuration.GetValue<bool>("SMOKE_TEST");
if (smoke)
{
    await app.StartAsync();
    var baseUrl = app.Urls.FirstOrDefault(u => u.StartsWith("https")) ?? app.Urls.First();
    var exitCode = await new SmokeTestRunner(baseUrl, app.Configuration).RunAsync();
    await app.StopAsync();
    return exitCode;
}

await app.RunAsync();
return 0;