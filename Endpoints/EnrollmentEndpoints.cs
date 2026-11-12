using MarksApi.Common;
using MarksApi.Dtos;
using MarksApi.Services;

namespace MarksApi.Endpoints;

public static class EnrollmentEndpoints
{
    public static void MapEnrollmentEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api").WithTags("Enrollments and Marks").RequireAuthorization("TeacherOnly");

        g.MapPost("/courses/{courseId:int}/enrollments",
                async (int courseId, EnrollRequest body, IEnrollmentService svc, CancellationToken ct) =>
                {
                    var created = await svc.EnrollAsync(courseId, body, ct);
                    return Results.Created($"/api/courses/{courseId}/enrollments", created);
                })
            .AddEndpointFilter<ValidationFilter<EnrollRequest>>();

        g.MapGet("/courses/{courseId:int}/enrollments",
            async (int courseId, [AsParameters] PageQuery q, IEnrollmentService svc, CancellationToken ct) =>
                Results.Ok(await svc.ListForCourseAsync(courseId, q, ct)));

        g.MapPut("/enrollments/{id:int}/mark",
                async (int id, MarkRequest body, IEnrollmentService svc, CancellationToken ct) =>
                    Results.Ok(await svc.SetMarkAsync(id, body, ct)))
            .AddEndpointFilter<ValidationFilter<MarkRequest>>();

        g.MapDelete("/enrollments/{id:int}", async (int id, IEnrollmentService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        });
    }
}