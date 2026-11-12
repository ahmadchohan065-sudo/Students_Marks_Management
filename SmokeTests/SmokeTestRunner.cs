using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;

namespace MarksApi.SmokeTests;

// Calls every endpoint of the RUNNING API (real HTTP, real PostgreSQL). Started only with --smoke-test.
public class SmokeTestRunner
{
    private const string Prefix = "SMOKE-";
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private int _passed, _failed;
    private string _teacherToken = "";
    private readonly List<int> _studentIds = new(), _courseIds = new(), _enrollmentIds = new();

    public SmokeTestRunner(string baseUrl, IConfiguration config)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _config = config;
    }

    public async Task<int> RunAsync()
    {
        Console.WriteLine($"Smoke test against {_http.BaseAddress}");
        try { await RunChecksAsync(); }
        catch (Exception ex)
        {
            _failed++;
            Console.WriteLine($"FAIL smoke test stopped early: {ex.Message}");
        }
        finally { await CleanUpAsync(); }

        Console.WriteLine($"SUMMARY: {_passed} passed, {_failed} failed");
        return _failed == 0 ? 0 : 1;
    }

    private async Task RunChecksAsync()
    {
        // credentials come from configuration (user-secrets / environment), never from source code
        var tUser = _config["Smoke:TeacherUsername"] ?? throw new InvalidOperationException("Smoke:TeacherUsername missing");
        var tPass = _config["Smoke:TeacherPassword"] ?? throw new InvalidOperationException("Smoke:TeacherPassword missing");
        var sUser = _config["Smoke:StudentUsername"] ?? throw new InvalidOperationException("Smoke:StudentUsername missing");
        var sPass = _config["Smoke:StudentPassword"] ?? throw new InvalidOperationException("Smoke:StudentPassword missing");

        // ---- Auth ----
        var login = await Check(HttpMethod.Post, "/api/auth/login", HttpStatusCode.OK, new { username = tUser, password = tPass });
        _teacherToken = login!.Value.GetProperty("token").GetString()!;
        await Check(HttpMethod.Post, "/api/auth/login", HttpStatusCode.Unauthorized, new { username = tUser, password = "wrong-password" });
        await Check(HttpMethod.Post, "/api/auth/login", HttpStatusCode.BadRequest, new { username = "", password = "" });
        var sLogin = await Check(HttpMethod.Post, "/api/auth/login", HttpStatusCode.OK, new { username = sUser, password = sPass });
        var studentToken = sLogin!.Value.GetProperty("token").GetString()!;
        var ownStudentId = int.Parse(new JsonWebTokenHandler().ReadJsonWebToken(studentToken).GetClaim("studentId").Value);

        // ---- Health ----
        await Check(HttpMethod.Get, "/health", HttpStatusCode.OK);

        // ---- Students ----
        await Check(HttpMethod.Get, "/api/students", HttpStatusCode.Unauthorized);
        await Check(HttpMethod.Get, "/api/students", HttpStatusCode.Forbidden, token: studentToken);
        await Check(HttpMethod.Get, "/api/students?page=1&pageSize=5", HttpStatusCode.OK, token: _teacherToken);

        var s1 = await Check(HttpMethod.Post, "/api/students", HttpStatusCode.Created,
            new { registrationNo = Prefix + "001", fullName = Prefix + "Student One", email = "smoke-one@example.com" }, _teacherToken);
        var s1Id = s1!.Value.GetProperty("id").GetInt32(); _studentIds.Add(s1Id);
        var s2 = await Check(HttpMethod.Post, "/api/students", HttpStatusCode.Created,
            new { registrationNo = Prefix + "002", fullName = Prefix + "Student Two", email = "smoke-two@example.com" }, _teacherToken);
        var s2Id = s2!.Value.GetProperty("id").GetInt32(); _studentIds.Add(s2Id);

        await Check(HttpMethod.Post, "/api/students", HttpStatusCode.BadRequest,
            new { registrationNo = Prefix + "003", fullName = "", email = "x@example.com" }, _teacherToken);
        await Check(HttpMethod.Post, "/api/students", HttpStatusCode.Conflict,
            new { registrationNo = Prefix + "001", fullName = "Duplicate", email = "smoke-dup@example.com" }, _teacherToken);
        await Check(HttpMethod.Post, "/api/students", HttpStatusCode.Unauthorized, new { registrationNo = "A", fullName = "A", email = "a@example.com" });
        await Check(HttpMethod.Post, "/api/students", HttpStatusCode.Forbidden,
            new { registrationNo = "A", fullName = "A", email = "a@example.com" }, studentToken);

        await Check(HttpMethod.Get, $"/api/students/{s1Id}", HttpStatusCode.OK, token: _teacherToken);
        await Check(HttpMethod.Get, "/api/students/999999", HttpStatusCode.NotFound, token: _teacherToken);
        await Check(HttpMethod.Get, $"/api/students/{s1Id}", HttpStatusCode.Forbidden, token: studentToken);   // another student's record
        await Check(HttpMethod.Get, $"/api/students/{ownStudentId}", HttpStatusCode.OK, token: studentToken);   // own record
        await Check(HttpMethod.Get, $"/api/students/{s1Id}", HttpStatusCode.Unauthorized);

        await Check(HttpMethod.Put, $"/api/students/{s1Id}", HttpStatusCode.OK,
            new { registrationNo = Prefix + "001", fullName = Prefix + "Student One Updated", email = "smoke-one@example.com" }, _teacherToken);
        await Check(HttpMethod.Put, $"/api/students/{s1Id}", HttpStatusCode.BadRequest,
            new { registrationNo = Prefix + "001", fullName = "", email = "smoke-one@example.com" }, _teacherToken);
        await Check(HttpMethod.Put, "/api/students/999999", HttpStatusCode.NotFound,
            new { registrationNo = Prefix + "009", fullName = "Nobody", email = "nobody@example.com" }, _teacherToken);
        await Check(HttpMethod.Put, $"/api/students/{s1Id}", HttpStatusCode.Conflict,
            new { registrationNo = Prefix + "002", fullName = "Clash", email = "smoke-one@example.com" }, _teacherToken);
        await Check(HttpMethod.Put, $"/api/students/{s1Id}", HttpStatusCode.Unauthorized, new { });
        await Check(HttpMethod.Put, $"/api/students/{s1Id}", HttpStatusCode.Forbidden,
            new { registrationNo = "A", fullName = "A", email = "a@example.com" }, studentToken);

        // ---- Courses ----
        await Check(HttpMethod.Get, "/api/courses", HttpStatusCode.Unauthorized);
        await Check(HttpMethod.Get, "/api/courses", HttpStatusCode.OK, token: studentToken);

        var c1 = await Check(HttpMethod.Post, "/api/courses", HttpStatusCode.Created,
            new { code = "SMK101", title = Prefix + "Course One", creditHours = 3 }, _teacherToken);
        var c1Id = c1!.Value.GetProperty("id").GetInt32(); _courseIds.Add(c1Id);
        var c2 = await Check(HttpMethod.Post, "/api/courses", HttpStatusCode.Created,
            new { code = "SMK102", title = Prefix + "Course Two", creditHours = 2 }, _teacherToken);
        var c2Id = c2!.Value.GetProperty("id").GetInt32(); _courseIds.Add(c2Id);

        await Check(HttpMethod.Post, "/api/courses", HttpStatusCode.BadRequest, new { code = "SMK103", title = "Bad", creditHours = 5 }, _teacherToken);
        await Check(HttpMethod.Post, "/api/courses", HttpStatusCode.Conflict, new { code = "SMK101", title = "Dup", creditHours = 1 }, _teacherToken);
        await Check(HttpMethod.Post, "/api/courses", HttpStatusCode.Unauthorized, new { code = "X", title = "X", creditHours = 1 });
        await Check(HttpMethod.Post, "/api/courses", HttpStatusCode.Forbidden, new { code = "X", title = "X", creditHours = 1 }, studentToken);

        await Check(HttpMethod.Get, $"/api/courses/{c1Id}", HttpStatusCode.OK, token: _teacherToken);
        await Check(HttpMethod.Get, "/api/courses/999999", HttpStatusCode.NotFound, token: _teacherToken);
        await Check(HttpMethod.Get, $"/api/courses/{c1Id}", HttpStatusCode.Unauthorized);

        await Check(HttpMethod.Put, $"/api/courses/{c1Id}", HttpStatusCode.OK, new { code = "SMK101", title = Prefix + "Course One Updated", creditHours = 3 }, _teacherToken);
        await Check(HttpMethod.Put, $"/api/courses/{c1Id}", HttpStatusCode.BadRequest, new { code = "SMK101", title = "", creditHours = 3 }, _teacherToken);
        await Check(HttpMethod.Put, "/api/courses/999999", HttpStatusCode.NotFound, new { code = "SMK199", title = "None", creditHours = 1 }, _teacherToken);
        await Check(HttpMethod.Put, $"/api/courses/{c1Id}", HttpStatusCode.Conflict, new { code = "SMK102", title = "Clash", creditHours = 3 }, _teacherToken);
        await Check(HttpMethod.Put, $"/api/courses/{c1Id}", HttpStatusCode.Unauthorized, new { });
        await Check(HttpMethod.Put, $"/api/courses/{c1Id}", HttpStatusCode.Forbidden, new { code = "X", title = "X", creditHours = 1 }, studentToken);

        // ---- Enrollments and marks ----
        var e1 = await Check(HttpMethod.Post, $"/api/courses/{c1Id}/enrollments", HttpStatusCode.Created, new { studentId = s1Id }, _teacherToken);
        var e1Id = e1!.Value.GetProperty("id").GetInt32(); _enrollmentIds.Add(e1Id);
        var e2 = await Check(HttpMethod.Post, $"/api/courses/{c1Id}/enrollments", HttpStatusCode.Created, new { studentId = s2Id }, _teacherToken);
        var e2Id = e2!.Value.GetProperty("id").GetInt32(); _enrollmentIds.Add(e2Id);
        var e3 = await Check(HttpMethod.Post, $"/api/courses/{c2Id}/enrollments", HttpStatusCode.Created, new { studentId = s1Id }, _teacherToken);
        _enrollmentIds.Add(e3!.Value.GetProperty("id").GetInt32());

        await Check(HttpMethod.Post, $"/api/courses/{c1Id}/enrollments", HttpStatusCode.Conflict, new { studentId = s1Id }, _teacherToken);
        await Check(HttpMethod.Post, $"/api/courses/{c1Id}/enrollments", HttpStatusCode.BadRequest, new { studentId = 0 }, _teacherToken);
        await Check(HttpMethod.Post, "/api/courses/999999/enrollments", HttpStatusCode.NotFound, new { studentId = s1Id }, _teacherToken);
        await Check(HttpMethod.Post, $"/api/courses/{c1Id}/enrollments", HttpStatusCode.NotFound, new { studentId = 999999 }, _teacherToken);
        await Check(HttpMethod.Post, $"/api/courses/{c1Id}/enrollments", HttpStatusCode.Unauthorized, new { studentId = s1Id });
        await Check(HttpMethod.Post, $"/api/courses/{c1Id}/enrollments", HttpStatusCode.Forbidden, new { studentId = s1Id }, studentToken);

        await Check(HttpMethod.Get, $"/api/courses/{c1Id}/enrollments", HttpStatusCode.OK, token: _teacherToken);
        await Check(HttpMethod.Get, "/api/courses/999999/enrollments", HttpStatusCode.NotFound, token: _teacherToken);
        await Check(HttpMethod.Get, $"/api/courses/{c1Id}/enrollments", HttpStatusCode.Unauthorized);
        await Check(HttpMethod.Get, $"/api/courses/{c1Id}/enrollments", HttpStatusCode.Forbidden, token: studentToken);

        await Check(HttpMethod.Put, $"/api/enrollments/{e1Id}/mark", HttpStatusCode.OK, new { mark = 80 }, _teacherToken);
        await Check(HttpMethod.Put, $"/api/enrollments/{e2Id}/mark", HttpStatusCode.OK, new { mark = 70 }, _teacherToken);
        await Check(HttpMethod.Put, $"/api/enrollments/{e1Id}/mark", HttpStatusCode.BadRequest, new { mark = 101 }, _teacherToken);
        await Check(HttpMethod.Put, $"/api/enrollments/{e1Id}/mark", HttpStatusCode.BadRequest, new { mark = -1 }, _teacherToken);
        await Check(HttpMethod.Put, "/api/enrollments/999999/mark", HttpStatusCode.NotFound, new { mark = 50 }, _teacherToken);
        await Check(HttpMethod.Put, $"/api/enrollments/{e1Id}/mark", HttpStatusCode.Unauthorized, new { mark = 50 });
        await Check(HttpMethod.Put, $"/api/enrollments/{e1Id}/mark", HttpStatusCode.Forbidden, new { mark = 50 }, studentToken);

        // ---- Reports ----
        await Check(HttpMethod.Get, $"/api/courses/{c1Id}/stats", HttpStatusCode.OK, token: _teacherToken);
        await Check(HttpMethod.Get, "/api/courses/999999/stats", HttpStatusCode.NotFound, token: _teacherToken);
        await Check(HttpMethod.Get, $"/api/courses/{c1Id}/stats", HttpStatusCode.Unauthorized);
        await Check(HttpMethod.Get, $"/api/courses/{c1Id}/stats", HttpStatusCode.Forbidden, token: studentToken);

        await Check(HttpMethod.Get, $"/api/students/{s1Id}/transcript", HttpStatusCode.OK, token: _teacherToken);
        await Check(HttpMethod.Get, $"/api/students/{ownStudentId}/transcript", HttpStatusCode.OK, token: studentToken);
        await Check(HttpMethod.Get, "/api/students/999999/transcript", HttpStatusCode.NotFound, token: _teacherToken);
        await Check(HttpMethod.Get, $"/api/students/{s1Id}/transcript", HttpStatusCode.Forbidden, token: studentToken);
        await Check(HttpMethod.Get, $"/api/students/{s1Id}/transcript", HttpStatusCode.Unauthorized);

        // ---- Deletes (including the blocked ones) ----
        await Check(HttpMethod.Delete, $"/api/students/{s1Id}", HttpStatusCode.Conflict, token: _teacherToken);   // has enrollments
        await Check(HttpMethod.Delete, $"/api/courses/{c1Id}", HttpStatusCode.Conflict, token: _teacherToken);    // has enrollments
        await Check(HttpMethod.Delete, $"/api/enrollments/{e1Id}", HttpStatusCode.Forbidden, token: studentToken);
        await Check(HttpMethod.Delete, $"/api/enrollments/{e1Id}", HttpStatusCode.Unauthorized);
        await Check(HttpMethod.Delete, "/api/enrollments/999999", HttpStatusCode.NotFound, token: _teacherToken);
        await Check(HttpMethod.Delete, $"/api/enrollments/{e1Id}", HttpStatusCode.NoContent, token: _teacherToken);
        await Check(HttpMethod.Delete, $"/api/students/{s2Id}", HttpStatusCode.Conflict, token: _teacherToken);   // e2 still exists
        await Check(HttpMethod.Delete, $"/api/enrollments/{e2Id}", HttpStatusCode.NoContent, token: _teacherToken);
        await Check(HttpMethod.Delete, $"/api/students/{s2Id}", HttpStatusCode.NoContent, token: _teacherToken);
        await Check(HttpMethod.Delete, $"/api/students/{s2Id}", HttpStatusCode.NotFound, token: _teacherToken);
        await Check(HttpMethod.Delete, $"/api/students/{s2Id}", HttpStatusCode.Unauthorized);
        await Check(HttpMethod.Delete, $"/api/students/{s2Id}", HttpStatusCode.Forbidden, token: studentToken);
        await Check(HttpMethod.Delete, $"/api/courses/{c2Id}", HttpStatusCode.Conflict, token: _teacherToken);    // e3 still exists
        await Check(HttpMethod.Delete, $"/api/courses/{c1Id}", HttpStatusCode.NoContent, token: _teacherToken);
        await Check(HttpMethod.Delete, $"/api/courses/{c1Id}", HttpStatusCode.NotFound, token: _teacherToken);
        await Check(HttpMethod.Delete, $"/api/courses/{c1Id}", HttpStatusCode.Unauthorized);
        await Check(HttpMethod.Delete, $"/api/courses/{c1Id}", HttpStatusCode.Forbidden, token: studentToken);
        // Remaining data (s1, c2, e3) is removed by CleanUpAsync below.
    }

    // One helper = one request + one PASS/FAIL line.
    private async Task<JsonElement?> Check(HttpMethod method, string path, HttpStatusCode expected,
        object? body = null, string? token = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _http.SendAsync(request);
        var ok = response.StatusCode == expected;
        if (ok) _passed++; else _failed++;
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {method.Method,-6} {path,-45} expected {(int)expected} actual {(int)response.StatusCode}");

        var text = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(text)) return null;
        try { return JsonDocument.Parse(text).RootElement.Clone(); } catch { return null; }
    }

    // Delete everything the run created, in the right order. Errors are ignored (item may already be gone).
    private async Task CleanUpAsync()
    {
        if (string.IsNullOrEmpty(_teacherToken)) return;
        async Task Del(string path)
        {
            var req = new HttpRequestMessage(HttpMethod.Delete, path);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _teacherToken);
            try { await _http.SendAsync(req); } catch { /* ignore */ }
        }
        foreach (var id in _enrollmentIds) await Del($"/api/enrollments/{id}");
        foreach (var id in _courseIds) await Del($"/api/courses/{id}");
        foreach (var id in _studentIds) await Del($"/api/students/{id}");
        Console.WriteLine("Cleanup done: all SMOKE- data removed.");
    }
}