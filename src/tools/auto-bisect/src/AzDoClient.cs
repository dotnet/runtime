using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AutoBisect;

/// <summary>
/// Interface for Azure DevOps API client operations.
/// </summary>
public interface IAzDoClient
{
    /// <summary>
    /// Gets build information by build ID.
    /// </summary>
    Task<Build?> GetBuildAsync(int buildId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed test results for a specific build.
    /// </summary>
    IAsyncEnumerable<TestResult> GetFailedTestsAsync(
        int buildId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Checks if a specific test has failed in a build (even if the build is still running).
    /// </summary>
    Task<bool> HasTestFailedAsync(
        int buildId,
        string testName,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Finds completed builds for a specific commit and pipeline definition.
    /// </summary>
    Task<IReadOnlyList<Build>> FindBuildsAsync(
        string commitSha,
        int? definitionId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Queues a new build for a specific commit.
    /// </summary>
    Task<Build> QueueBuildAsync(
        int definitionId,
        string commitSha,
        string? sourceBranch = null,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// Azure DevOps REST API client.
/// </summary>
public class AzDoClient : IAzDoClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _organization;
    private readonly string _project;

    public AzDoClient(string organization, string project, string personalAccessToken)
        : this(organization, project, personalAccessToken, null) { }

    internal AzDoClient(
        string organization,
        string project,
        string personalAccessToken,
        HttpClient? httpClient
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organization);
        ArgumentException.ThrowIfNullOrWhiteSpace(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(personalAccessToken);

        _organization = organization;
        _project = project;

        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri($"https://dev.azure.com/{_organization}/{_project}/");

        var credentials = Convert.ToBase64String(
            System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}")
        );
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            credentials
        );
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json")
        );
    }

    public async Task<Build?> GetBuildAsync(
        int buildId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.GetAsync(
            $"_apis/build/builds/{buildId}?api-version=7.1",
            cancellationToken
        );

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync(
            AzDoJsonContext.Default.Build,
            cancellationToken
        );
    }

    public async IAsyncEnumerable<TestResult> GetFailedTestsAsync(
        int buildId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var continuationToken = (string?)null;

        do
        {
            var url = $"_apis/test/runs?buildUri=vstfs:///Build/Build/{buildId}&api-version=7.1";
            url += continuationToken != null ? $"&continuationToken={continuationToken}" : "";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var runsResponse = await response.Content.ReadFromJsonAsync(
                AzDoJsonContext.Default.TestRunsResponse,
                cancellationToken
            );
            if (runsResponse?.Value is not { } runs)
            {
                break;
            }

            foreach (var run in runs)
            {
                await foreach (var testResult in GetFailedTestsForRunAsync(run.Id, cancellationToken))
                {
                    yield return testResult;
                }
            }

            continuationToken = response.Headers.TryGetValues(
                "x-ms-continuationtoken",
                out var tokens
            )
                ? tokens.FirstOrDefault()
                : null;
        } while (continuationToken != null);
    }

    public async Task<bool> HasTestFailedAsync(
        int buildId,
        string testName,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var url = $"_apis/test/runs?buildUri=vstfs:///Build/Build/{buildId}&api-version=7.1";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var runsResponse = await response.Content.ReadFromJsonAsync(
                AzDoJsonContext.Default.TestRunsResponse,
                cancellationToken
            );

            if (runsResponse?.Value is not { } runs)
            {
                return false;
            }

            // Check each test run for the specific failure
            foreach (var run in runs)
            {
                await foreach (var testResult in GetFailedTestsForRunAsync(run.Id, cancellationToken))
                {
                    if (testResult.FullyQualifiedName.Equals(testName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch
        {
            // If we can't check, assume not failed yet
            return false;
        }
    }

    private async IAsyncEnumerable<TestResult> GetFailedTestsForRunAsync(
        int runId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        int skip = 0;
        const int top = 1000;

        while (true)
        {
            var response = await _httpClient.GetAsync(
                $"_apis/test/runs/{runId}/results?api-version=7.1&$top={top}&$skip={skip}&outcomes=Failed",
                cancellationToken
            );
            response.EnsureSuccessStatusCode();

            var resultsResponse = await response.Content.ReadFromJsonAsync(
                AzDoJsonContext.Default.TestResultsResponse,
                cancellationToken
            );
            if (resultsResponse?.Value is not { Count: > 0 } results)
            {
                break;
            }

            foreach (var result in results)
            {
                yield return result;
            }

            if (resultsResponse.Value.Count < top)
            {
                break;
            }

            skip += top;
        }
    }

    public async Task<IReadOnlyList<Build>> FindBuildsAsync(
        string commitSha,
        int? definitionId = null,
        CancellationToken cancellationToken = default
    )
    {
        var allBuilds = new List<Build>();

        // We need to query both completed and in-progress builds separately
        // because the API defaults to completed builds and may not return in-progress ones
        var statusFilters = new[] { "completed", "inProgress", "notStarted" };

        foreach (var statusFilter in statusFilters)
        {
            var url =
                $"_apis/build/builds?api-version=7.1&$top=500&queryOrder=queueTimeDescending&statusFilter={statusFilter}";

            if (definitionId.HasValue)
            {
                url += $"&definitions={definitionId.Value}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var buildsResponse = await response.Content.ReadFromJsonAsync(
                AzDoJsonContext.Default.BuildsResponse,
                cancellationToken
            );

            if (buildsResponse?.Value is { } builds)
            {
                allBuilds.AddRange(builds);
            }
        }

        // Filter by commit SHA (case-insensitive prefix match to handle short SHAs)
        return allBuilds
            .Where(b =>
                b.SourceVersion != null
                && (
                    b.SourceVersion.StartsWith(commitSha, StringComparison.OrdinalIgnoreCase)
                    || commitSha.StartsWith(b.SourceVersion, StringComparison.OrdinalIgnoreCase)
                )
            )
            .ToList();
    }

    public async Task<IReadOnlyList<Build>> GetActiveBuildsAsync(
        int definitionId,
        CancellationToken cancellationToken = default
    )
    {
        // Query specifically for in-progress and not-started builds
        var url =
            $"_apis/build/builds?api-version=7.1&definitions={definitionId}&statusFilter=inProgress,notStarted&queryOrder=queueTimeDescending&$top=50";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var buildsResponse = await response.Content.ReadFromJsonAsync(
            AzDoJsonContext.Default.BuildsResponse,
            cancellationToken
        );

        return buildsResponse?.Value ?? [];
    }

    public async Task<IReadOnlyList<Build>> GetRecentBuildsAsync(
        int definitionId,
        int top = 10,
        CancellationToken cancellationToken = default
    )
    {
        // Query completed builds, ordered by finish time descending
        var url =
            $"_apis/build/builds?api-version=7.1&definitions={definitionId}&statusFilter=completed&queryOrder=finishTimeDescending&$top={top}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var buildsResponse = await response.Content.ReadFromJsonAsync(
            AzDoJsonContext.Default.BuildsResponse,
            cancellationToken
        );

        return buildsResponse?.Value ?? [];
    }

    public async Task<Build> QueueBuildAsync(
        int definitionId,
        string commitSha,
        string? sourceBranch = null,
        CancellationToken cancellationToken = default
    )
    {
        var request = new QueueBuildRequest
        {
            Definition = new BuildDefinitionReference { Id = definitionId },
            SourceVersion = commitSha,
            SourceBranch = sourceBranch,
        };

        var response = await _httpClient.PostAsJsonAsync(
            "_apis/build/builds?api-version=7.1",
            request,
            AzDoJsonContext.Default.QueueBuildRequest,
            cancellationToken
        );

        response.EnsureSuccessStatusCode();

        var build = await response.Content.ReadFromJsonAsync(
            AzDoJsonContext.Default.Build,
            cancellationToken
        );
        return build
            ?? throw new InvalidOperationException("Failed to queue build - no response received");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

// Response wrapper types for AzDO API
internal class TestRunsResponse
{
    public List<TestRun>? Value { get; set; }
    public int Count { get; set; }
}

internal class TestRun
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

internal class TestResultsResponse
{
    public List<TestResult>? Value { get; set; }
    public int Count { get; set; }
}

internal class BuildsResponse
{
    public List<Build>? Value { get; set; }
    public int Count { get; set; }
}

internal class QueueBuildRequest
{
    public BuildDefinitionReference? Definition { get; set; }
    public string? SourceVersion { get; set; }
    public string? SourceBranch { get; set; }
}

internal class BuildDefinitionReference
{
    public int Id { get; set; }
}

/// <summary>
/// Source-generated JSON serializer context for AOT compatibility.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    Converters = [
        typeof(JsonStringEnumConverter<BuildStatus>),
        typeof(JsonStringEnumConverter<BuildResult>),
        typeof(JsonStringEnumConverter<TestOutcome>),
    ]
)]
[JsonSerializable(typeof(Build))]
[JsonSerializable(typeof(TestRunsResponse))]
[JsonSerializable(typeof(TestResultsResponse))]
[JsonSerializable(typeof(BuildsResponse))]
[JsonSerializable(typeof(QueueBuildRequest))]
internal partial class AzDoJsonContext : JsonSerializerContext { }
