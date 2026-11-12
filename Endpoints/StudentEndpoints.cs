using System.Security.Claims;
using MarksApi.Common;
using MarksApi.Dtos;
using MarksApi.Services;

namespace MarksApi.Endpoints;

public static class StudentEndpoints
{
    public static void MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/students").WithTags("Students").RequireAuthorization();

        g.MapGet("/", async ([AsParameters] PageQuery q, IStudentService svc, CancellationToken ct) =>
                Results.Ok(await svc.ListAsync(q, ct)))
            .RequireAuthorization("TeacherOnly");

        g.MapGet("/{id:int}", async (int id, ClaimsPrincipal user, IStudentService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, user.ToCurrentUser(), ct)));

        g.MapPost("/", async (StudentRequest body, IStudentService svc, CancellationToken ct) =>
            {
                var created = await svc.CreateAsync(body, ct);
                return Results.Created($"/api/students/{created.Id}", created);
            })
            .AddEndpointFilter<ValidationFilter<StudentRequest>>()
            .RequireAuthorization("TeacherOnly");

        g.MapPut("/{id:int}", async (int id, StudentRequest body, IStudentService svc, CancellationToken ct) =>
                Results.Ok(await svc.UpdateAsync(id, body, ct)))
            .AddEndpointFilter<ValidationFilter<StudentRequest>>()
            .RequireAuthorization("TeacherOnly");

        g.MapDelete("/{id:int}", async (int id, IStudentService svc, CancellationToken ct) =>
            {
                await svc.DeleteAsync(id, ct);
                return Results.NoContent();
            })
            .RequireAuthorization("TeacherOnly");
    }
}