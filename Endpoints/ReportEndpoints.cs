using System.Security.Claims;
using MarksApi.Common;
using MarksApi.Services;

namespace MarksApi.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api").WithTags("Reports").RequireAuthorization();

        g.MapGet("/courses/{courseId:int}/stats", async (int courseId, IReportService svc, CancellationToken ct) =>
                Results.Ok(await svc.CourseStatsAsync(courseId, ct)))
            .RequireAuthorization("TeacherOnly");

        g.MapGet("/students/{id:int}/transcript",
            async (int id, ClaimsPrincipal user, IReportService svc, CancellationToken ct) =>
                Results.Ok(await svc.TranscriptAsync(id, user.ToCurrentUser(), ct)));
    }
}