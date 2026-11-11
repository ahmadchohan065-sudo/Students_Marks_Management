using Microsoft.EntityFrameworkCore;
using MarksApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Database"))
       .UseSnakeCaseNamingConvention());

var app = builder.Build();
app.MapGet("/", () => "MarksApi is running");
app.Run();