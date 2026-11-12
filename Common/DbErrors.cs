using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MarksApi.Common;

public static class DbErrors
{
    // PostgreSQL error 23505 = a unique index was violated
    public static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    // PostgreSQL error 23503 = a foreign key rule was violated
    public static bool IsForeignKeyViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation };
}