using MarksApi.Common;
using MarksApi.Dtos;
using MarksApi.Services;

namespace MarksApi.Endpoints;

public static class CourseEndpoints
{
    public static void MapCourseEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/courses").WithTags("Courses").RequireAuthorization();

        g.MapGet("/", async ([AsParameters] PageQuery q, ICourseService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(q, ct)));

        g.MapGet("/{id:int}", async (int id, ICourseService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        g.MapPost("/", async (CourseRequest body, ICourseService svc, CancellationToken ct) =>
            {
                var created = await svc.CreateAsync(body, ct);
                return Results.Created($"/api/courses/{created.Id}", created);
            })
            .AddEndpointFilter<ValidationFilter<CourseRequest>>()
            .RequireAuthorization("TeacherOnly");

        g.MapPut("/{id:int}", async (int id, CourseRequest body, ICourseService svc, CancellationToken ct) =>
                Results.Ok(await svc.UpdateAsync(id, body, ct)))
            .AddEndpointFilter<ValidationFilter<CourseRequest>>()
            .RequireAuthorization("TeacherOnly");

        g.MapDelete("/{id:int}", async (int id, ICourseService svc, CancellationToken ct) =>
            {
                await svc.DeleteAsync(id, ct);
                return Results.NoContent();
            })
            .RequireAuthorization("TeacherOnly");
    }
}