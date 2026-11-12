namespace MarksApi.Common;

// Base class: any "expected" failure carries an HTTP status, a title and a detail.
public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public string Title { get; }

    protected AppException(int statusCode, string title, string detail) : base(detail)
    {
        StatusCode = statusCode;
        Title = title;
    }
}

public class NotFoundException(string title, string detail) : AppException(404, title, detail);
public class ConflictException(string title, string detail) : AppException(409, title, detail);
public class ForbiddenException(string title, string detail) : AppException(403, title, detail);
public class UnauthorizedException(string title, string detail) : AppException(401, title, detail);