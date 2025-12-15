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

namespace AzdoBisect;

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
    Task<IReadOnlyList<TestResult>> GetFailedTestsAsync(int buildId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Azure DevOps REST API client.
/// </summary>
public class AzDoClient : IAzDoClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _organization;
    private readonly string _project;
    private readonly JsonSerializerOptions _jsonOptions;

    public AzDoClient(string organization, string project, string personalAccessToken)
        : this(organization, project, personalAccessToken, null)
    {
    }

    internal AzDoClient(string organization, string project, string personalAccessToken, HttpClient? httpClient)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organization);
        ArgumentException.ThrowIfNullOrWhiteSpace(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(personalAccessToken);

        _organization = organization;
        _project = project;

        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri($"https://dev.azure.com/{_organization}/{_project}/");

        var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{personalAccessToken}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    public async Task<Build?> GetBuildAsync(int buildId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"_apis/build/builds/{buildId}?api-version=7.1",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Build>(_jsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<TestResult>> GetFailedTestsAsync(int buildId, CancellationToken cancellationToken = default)
    {
        var results = new List<TestResult>();
        var continuationToken = (string?)null;

        do
        {
            var url = $"_apis/test/runs?buildUri=vstfs:///Build/Build/{buildId}&api-version=7.1";
            if (continuationToken != null)
            {
                url += $"&continuationToken={continuationToken}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var runsResponse = await response.Content.ReadFromJsonAsync<TestRunsResponse>(_jsonOptions, cancellationToken);
            if (runsResponse?.Value == null)
            {
                break;
            }

            foreach (var run in runsResponse.Value)
            {
                var testResults = await GetFailedTestsForRunAsync(run.Id, cancellationToken);
                results.AddRange(testResults);
            }

            continuationToken = response.Headers.TryGetValues("x-ms-continuationtoken", out var tokens)
                ? tokens.FirstOrDefault()
                : null;

        } while (continuationToken != null);

        return results;
    }

    private async Task<IReadOnlyList<TestResult>> GetFailedTestsForRunAsync(int runId, CancellationToken cancellationToken)
    {
        var results = new List<TestResult>();
        int skip = 0;
        const int top = 1000;

        while (true)
        {
            var response = await _httpClient.GetAsync(
                $"_apis/test/runs/{runId}/results?api-version=7.1&$top={top}&$skip={skip}&outcomes=Failed",
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var resultsResponse = await response.Content.ReadFromJsonAsync<TestResultsResponse>(_jsonOptions, cancellationToken);
            if (resultsResponse?.Value == null || resultsResponse.Value.Count == 0)
            {
                break;
            }

            results.AddRange(resultsResponse.Value);

            if (resultsResponse.Value.Count < top)
            {
                break;
            }

            skip += top;
        }

        return results;
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
