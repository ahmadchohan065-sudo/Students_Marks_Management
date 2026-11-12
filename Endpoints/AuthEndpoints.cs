using MarksApi.Common;
using MarksApi.Dtos;
using MarksApi.Services;

namespace MarksApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (LoginRequest body, IAuthService auth, CancellationToken ct) =>
                Results.Ok(await auth.LoginAsync(body, ct)))
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .AllowAnonymous()
            .WithTags("Auth");
    }
}