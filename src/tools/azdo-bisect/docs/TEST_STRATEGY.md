# Azure DevOps Bisect Tool - Test Strategy Document

## Overview

This document outlines the testing strategy for the `azdo-bisect` tool. Given the tool's reliance on external services (Azure DevOps API) and long-running operations, the testing approach emphasizes:

1. **Unit tests** for core logic with mocked dependencies
2. **Integration tests** with recorded HTTP responses

---

## Test Categories

### 1. Unit Tests

Unit tests focus on isolated components with all external dependencies mocked.

#### 1.1 Bisect Algorithm Tests

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| `Bisect_SingleCommit_ReturnsImmediately` | Only one commit between good/bad | Returns that commit as regression |
| `Bisect_TwoCommits_SelectsCorrect` | Two commits, second is bad | Returns second commit |
| `Bisect_MultipleCommits_FindsMiddle` | 7 commits, commit 4 is bad | Finds commit 4 in 3 iterations |
| `Bisect_AllPass_ReportsBadCommit` | All tested commits pass | Returns original bad commit |
| `Bisect_AllFail_ReportsFirstFailure` | All tested commits fail | Returns commit after good |
| `Trisect_SelectsCorrectPartition` | Trisect with 9 commits | Divides into thirds correctly |
| `Trisect_BothFail_SelectsFirstThird` | Both midpoints fail | Narrows to first third |
| `Trisect_BothPass_SelectsLastThird` | Both midpoints pass | Narrows to last third |
| `Trisect_MixedResults_SelectsMiddle` | First passes, second fails | Narrows to middle third |

#### 1.2 Test Diff Logic Tests

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| `Diff_IdenticalResults_EmptyNewFailures` | Same failures in both | Empty new failures list |
| `Diff_NewFailure_DetectedCorrectly` | Test fails only in bad build | Listed as new failure |
| `Diff_FixedTest_DetectedCorrectly` | Test passes only in bad build | Listed as fixed test |
| `Diff_MultipleChanges_CategorizedCorrectly` | Mix of new, fixed, consistent | All categorized properly |
| `Diff_CaseInsensitive_HandlesCorrectly` | Test names differ by case | Treated as same test |
| `Diff_EmptyResults_HandledGracefully` | No test results in build | Returns empty diff |

#### 1.3 State Management Tests

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| `State_Serialization_RoundTrip` | Serialize then deserialize | Identical object |
| `State_MissingFields_DefaultsApplied` | Old format session file | Loads with defaults |
| `State_CorruptedFile_ThrowsUseful` | Invalid JSON | Clear error message |
| `State_ConcurrentAccess_Handled` | Two processes same session | File locking prevents corruption |
| `State_Resume_RestoresCorrectly` | Load after interrupt | Continues from saved point |

#### 1.4 CLI Parsing Tests

| Test Case | Description | Expected Result |
|-----------|-------------|-----------------|
| `CLI_Start_RequiredArgs` | Missing --good or --bad | Error with usage hint |
| `CLI_Start_ValidArgs` | All required args provided | Session created |
| `CLI_Status_NoSession` | No active sessions | Helpful message |
| `CLI_Resume_InvalidId` | Non-existent session ID | Error with available sessions |
| `CLI_Abort_ConfirmationRequired` | Abort without --force | Prompts for confirmation |

### 2. Integration Tests

Integration tests use recorded HTTP responses (via WireMock or similar) to test AzDO API interactions without hitting real endpoints.

#### 2.1 AzDO Client Tests

| Test Case | Description | Setup |
|-----------|-------------|-------|
| `AzDO_GetBuild_ReturnsDetails` | Fetch build by ID | Record real build response |
| `AzDO_GetBuild_NotFound_HandlesError` | Invalid build ID | Record 404 response |
| `AzDO_GetTestResults_ParsesCorrectly` | Fetch test results | Record test run response |
| `AzDO_GetTestResults_Pagination` | >1000 test results | Record paginated responses |
| `AzDO_QueueBuild_CreatesRun` | Queue new pipeline run | Record queue response |
| `AzDO_QueueBuild_AtCommit_SetsCorrectly` | Specify source version | Verify request payload |
| `AzDO_GetBuildStatus_Transitions` | Poll build progress | Record status changes |
| `AzDO_RateLimited_RetriesCorrectly` | 429 response | Verify backoff behavior |
| `AzDO_AuthFailure_ClearMessage` | 401 response | User-friendly auth error |

#### 2.2 Git Integration Tests

| Test Case | Description | Setup |
|-----------|-------------|-------|
| `Git_EnumerateCommits_Linear` | Simple linear history | Test repo with linear commits |
| `Git_EnumerateCommits_WithMerges` | History with merge commits | Test repo with merges |
| `Git_EnumerateCommits_InvalidSha` | Non-existent commit | Clear error message |
| `Git_EnumerateCommits_Empty` | Same commit for good/bad | Returns empty list |
| `Git_Midpoint_OddCount` | 5 commits | Returns index 2 |
| `Git_Midpoint_EvenCount` | 6 commits | Returns index 3 |

---

## Test Infrastructure

### Mock Services

```csharp
public interface IAzDoClient
{
    Task<Build> GetBuildAsync(string buildId);
    Task<TestResults> GetTestResultsAsync(string buildId);
    Task<Build> QueueBuildAsync(QueueBuildRequest request);
}

// For testing
public class MockAzDoClient : IAzDoClient
{
    public Dictionary<string, Build> Builds { get; set; }
    public Dictionary<string, TestResults> TestResults { get; set; }
    public List<QueueBuildRequest> QueuedBuilds { get; } = new();

    // ... implementation that returns from dictionaries
}
```

### Test Fixtures

```csharp
public class BisectTestFixture
{
    public MockAzDoClient AzDoClient { get; }
    public MemoryStateManager StateManager { get; }
    public TestGitRepository GitRepo { get; }

    public void SetupLinearHistory(int commitCount, int regressionAt)
    {
        // Creates commits and configures mock to fail tests after regressionAt
    }
}
```

### HTTP Recording

For integration tests, record real API responses:

```bash
# Record mode
AZDO_BISECT_RECORD_MODE=true dotnet test --filter Category=Integration

# Playback mode (default)
dotnet test --filter Category=Integration
```

Recordings stored in `/test/recordings/` as sanitized JSON files.

---

## Test Data

### Sample Test Results

```json
{
  "goodBuild": {
    "id": "12345",
    "commit": "abc123",
    "testResults": [
      { "name": "TestA", "outcome": "Passed" },
      { "name": "TestB", "outcome": "Passed" },
      { "name": "TestC", "outcome": "Failed" }
    ]
  },
  "badBuild": {
    "id": "12350",
    "commit": "def456",
    "testResults": [
      { "name": "TestA", "outcome": "Passed" },
      { "name": "TestB", "outcome": "Failed" },
      { "name": "TestC", "outcome": "Failed" }
    ]
  },
  "expectedDiff": {
    "newFailures": ["TestB"],
    "fixedTests": [],
    "consistentFailures": ["TestC"]
  }
}
```

### Commit History Scenarios

```
Linear (7 commits):
good -> c1 -> c2 -> c3 -> c4 -> c5 -> bad
                    ^ regression here

With merge:
good -> c1 -> c2 ---------> c5 -> bad
          \-> c3 -> c4 -/
              ^ regression here
```

---

## Coverage Goals

| Component | Line Coverage | Branch Coverage |
|-----------|--------------|-----------------|
| Bisect Algorithm | 95% | 90% |
| State Management | 90% | 85% |
| CLI Commands | 85% | 80% |
| AzDO Client | 80% | 75% |
| Overall | 85% | 80% |

### Critical Paths (100% Coverage Required)

- Bisect/trisect decision logic
- State serialization/deserialization
- Session resume logic
- Commit enumeration

---

## Continuous Integration

### PR Validation

```yaml
trigger:
  - main

jobs:
  - job: UnitTests
    steps:
      - task: DotNetCoreCLI@2
        inputs:
          command: test
          arguments: '--filter Category=Unit'

  - job: IntegrationTests
    steps:
      - task: DotNetCoreCLI@2
        inputs:
          command: test
          arguments: '--filter Category=Integration'
```

---

## Test Utilities

### Assertion Helpers

```csharp
public static class BisectAssertions
{
    public static void AssertFoundRegression(BisectSession session, string expectedCommit)
    {
        Assert.Equal(BisectState.Completed, session.State);
        Assert.Equal(expectedCommit, session.RegressionCommit);
    }

    public static void AssertCorrectPartition(
        IEnumerable<string> remaining,
        string expectedStart,
        string expectedEnd)
    {
        var list = remaining.ToList();
        Assert.Equal(expectedStart, list.First());
        Assert.Equal(expectedEnd, list.Last());
    }
}
```

### Test Timeout Configuration

```csharp
// Unit tests: 5 seconds max
[Fact]
[Trait("Category", "Unit")]
[Timeout(5000)]
public void Bisect_QuickOperation() { }

// Integration tests: 30 seconds max
[Fact]
[Trait("Category", "Integration")]
[Timeout(30000)]
public async Task AzDO_FetchResults() { }
```

---

## Known Testing Challenges

### 1. Time-Dependent Logic
- **Challenge**: Polling intervals, timeouts
- **Solution**: Inject `ITimeProvider` interface, use `FakeTimeProvider` in tests

### 2. Git Operations
- **Challenge**: Need real git repos for commit enumeration
- **Solution**: Create temporary repos in tests, or use LibGit2Sharp's in-memory repos

### 3. Long-Running Builds
- **Challenge**: Real builds take hours
- **Solution**: Mock AzDO responses in integration tests; use recorded HTTP responses

### 4. Flaky External Services
- **Challenge**: AzDO API can be slow/flaky
- **Solution**: Record/playback for integration; retry logic in production

### 5. State File Concurrency
- **Challenge**: Multiple processes accessing same session
- **Solution**: File locking; test with parallel test execution disabled for state tests

---

## Appendix: Test File Organization

```
/test/
├── AzdoBisect.Tests/
│   ├── Unit/
│   │   ├── BisectAlgorithmTests.cs
│   │   ├── TrisectAlgorithmTests.cs
│   │   ├── TestDiffTests.cs
│   │   ├── StateManagerTests.cs
│   │   └── CliParserTests.cs
│   ├── Integration/
│   │   ├── AzDoClientTests.cs
│   │   ├── GitIntegrationTests.cs
│   │   └── Recordings/
│   │       ├── GetBuild_Success.json
│   │       ├── GetTestResults_Paginated.json
│   │       └── ...
│   ├── Fixtures/
│   │   ├── BisectTestFixture.cs
│   │   ├── MockAzDoClient.cs
│   │   └── TestGitRepository.cs
│   └── TestData/
│       ├── SampleSessions/
│       └── SampleTestResults/
└── AzdoBisect.Tests.csproj
```
