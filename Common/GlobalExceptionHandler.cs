using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MarksApi.Common;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetails;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetails)
    {
        _logger = logger;
        _problemDetails = problemDetails;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception ex, CancellationToken ct)
    {
        if (ex is OperationCanceledException) return true;   // client closed the connection

        ProblemDetails problem;
        if (ex is AppException app)
        {
            _logger.LogWarning("Request failed with {Status}: {Title}", app.StatusCode, app.Title);
            problem = new ProblemDetails { Status = app.StatusCode, Title = app.Title, Detail = app.Message };
        }
        else
        {
            _logger.LogError(ex, "Unhandled exception");   // full details go to the log only
            problem = new ProblemDetails
            {
                Status = 500,
                Title = "An unexpected error occurred.",
                Detail = "Please try again later."        // no stack trace for the client
            };
        }

        context.Response.StatusCode = problem.Status!.Value;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem
        });
    }
}