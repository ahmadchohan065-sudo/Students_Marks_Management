using System.ComponentModel.DataAnnotations;

namespace MarksApi.Common;

// Runs before the endpoint. If the body breaks the [Required]/[Range]... rules, answer 400.
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var body = ctx.Arguments.OfType<T>().FirstOrDefault();
        if (body is null)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = new[] { "Request body is required." }
            });

        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(body, new ValidationContext(body), results, validateAllProperties: true))
        {
            var errors = results
                .SelectMany(r => (r.MemberNames.Any() ? r.MemberNames : new[] { "body" })
                    .Select(m => (Key: ToCamel(m), Message: r.ErrorMessage ?? "Invalid value.")))
                .GroupBy(x => x.Key)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Message).ToArray());
            return Results.ValidationProblem(errors);   // 400 + problem details + "errors" object
        }

        return await next(ctx);
    }

    private static string ToCamel(string s) => char.ToLowerInvariant(s[0]) + s[1..];
}