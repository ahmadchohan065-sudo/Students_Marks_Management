using System.Security.Claims;

namespace MarksApi.Common;

public record CurrentUser(int UserId, string Role, int? StudentId)
{
    public bool IsTeacher => Role == "Teacher";
}

public static class ClaimsExtensions
{
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal)
    {
        var id = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = principal.FindFirstValue(ClaimTypes.Role)!;
        var studentId = principal.FindFirstValue("studentId");
        return new CurrentUser(id, role, studentId is null ? null : int.Parse(studentId));
    }
}