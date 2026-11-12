namespace MarksApi.Dtos;

public record PageQuery(int? Page, int? PageSize, string? Search, string? Sort)
{
    public int SafePage => Page is null or < 1 ? 1 : Page.Value;
    public int SafePageSize => PageSize is null or < 1 ? 10 : Math.Min(PageSize.Value, 50);
    public string? SafeSearch => string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);