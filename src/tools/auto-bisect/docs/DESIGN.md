# Auto Bisect Tool - Design Document

## Overview

The Auto Bisect Tool (`auto-bisect`) is a command-line utility designed to identify the root cause of test regressions in outer-loop CI pipelines. It automates the bisection process by:

1. Comparing test failures between two Azure DevOps build runs
2. Allowing users to select a specific failing test to isolate
3. Performing binary search (bisect) or ternary search (trisect) across commits
4. Managing long-running pipeline jobs with full resume capability

## Goals

- **Efficiency**: Minimize the number of pipeline runs needed to find the regression commit
- **Resumability**: Handle interruptions gracefully; persist state to allow resuming
- **Usability**: Provide clear feedback and interactive selection of tests to investigate
- **Flexibility**: Support both bisect (2-way) and trisect (3-way) search strategies

## Non-Goals

- Automatic root cause analysis beyond identifying the commit
- Integration with other CI systems (only Azure DevOps)
- Parallel bisection of multiple tests (future consideration)

---

## Architecture

### High-Level Components

```
┌─────────────────────────────────────────────────────────────────┐
│                        CLI Interface                            │
│  (Commands: start, status, resume, abort, list-failures)        │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Bisect Orchestrator                        │
│  (State machine managing the bisection workflow)                │
└─────────────────────────────────────────────────────────────────┘
                               │
          ┌────────────────────┼────────────────────┐
          ▼                    ▼                    ▼
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│  AzDO Client     │  │  State Manager   │  │  Git Integration │
│  (REST API)      │  │  (Persistence)   │  │  (Commit Graph)  │
└──────────────────┘  └──────────────────┘  └──────────────────┘
```

### Component Responsibilities

#### 1. CLI Interface
- Parse command-line arguments
- Display interactive prompts for test selection
- Show progress and status updates
- Handle user interrupts (Ctrl+C) gracefully

#### 2. Bisect Orchestrator
- Manage the bisection state machine
- Decide which commit(s) to test next
- Coordinate between AzDO API and state persistence
- Implement bisect and trisect algorithms

#### 3. AzDO Client
- Authenticate with Azure DevOps REST API
- Fetch build information and test results
- Queue new pipeline runs at specific commits
- Poll for build completion status

#### 4. State Manager
- Persist bisection state to disk (JSON)
- Enable resume after interruption
- Track tested commits and their results
- Maintain audit trail of operations

#### 5. Git Integration
- Enumerate commits between two points
- Validate commit SHAs
- Calculate midpoints for bisection

---

## Data Models

### BisectSession
```csharp
public class BisectSession
{
    public string Id { get; set; }                    // Unique session identifier
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Initial parameters
    public string GoodBuildId { get; set; }           // AzDO build ID where test passed
    public string BadBuildId { get; set; }            // AzDO build ID where test failed
    public string GoodCommit { get; set; }            // Git SHA of good build
    public string BadCommit { get; set; }             // Git SHA of bad build
    public string TargetTest { get; set; }            // Fully qualified test name

    // Pipeline configuration
    public string Organization { get; set; }
    public string Project { get; set; }
    public int PipelineId { get; set; }
    public string TargetBranch { get; set; }

    // Current state
    public BisectState State { get; set; }
    public List<string> RemainingCommits { get; set; }
    public List<TestedCommit> TestedCommits { get; set; }
    public List<PendingBuild> PendingBuilds { get; set; }

    // Result
    public string? RegressionCommit { get; set; }
}

public enum BisectState
{
    Initializing,
    SelectingTest,
    WaitingForBuilds,
    Analyzing,
    Completed,
    Failed,
    Aborted
}

public class TestedCommit
{
    public string Sha { get; set; }
    public string BuildId { get; set; }
    public TestResult Result { get; set; }           // Passed, Failed, Skipped, Error
    public DateTime TestedAt { get; set; }
}

public class PendingBuild
{
    public string Sha { get; set; }
    public string BuildId { get; set; }
    public DateTime QueuedAt { get; set; }
    public BuildStatus Status { get; set; }
}
```

### TestFailureDiff
```csharp
public class TestFailureDiff
{
    public List<string> NewFailures { get; set; }     // Failed in bad, passed in good
    public List<string> FixedTests { get; set; }      // Passed in bad, failed in good
    public List<string> ConsistentFailures { get; set; } // Failed in both
}
```

---

## Workflow

### 1. Starting a Bisect Session

```
User runs: auto-bisect start --good <build-id> --bad <build-id>

1. Fetch test results from both builds via AzDO API
2. Compute diff of failing tests
3. Display new failures to user
4. User selects test to investigate
5. Fetch commit SHAs for both builds
6. Enumerate commits between good and bad
7. Create and persist BisectSession
8. Queue first bisect build(s)
```

### 2. Bisection Algorithm

#### Binary Search (Bisect)
```
Given: commits [good, ..., bad] where good passes and bad fails

while remaining_commits.length > 1:
    mid = remaining_commits[length / 2]
    result = run_test_at_commit(mid)

    if result == FAIL:
        bad = mid
        remaining_commits = commits[good..mid]
    else:
        good = mid
        remaining_commits = commits[mid..bad]

regression_commit = bad
```

#### Ternary Search (Trisect)
```
Queues two builds simultaneously to reduce wall-clock time at cost of more builds.

while remaining_commits.length > 1:
    third = remaining_commits.length / 3
    mid1 = remaining_commits[third]
    mid2 = remaining_commits[2 * third]

    results = run_tests_at_commits([mid1, mid2])  // parallel

    if mid1 == FAIL:
        remaining_commits = commits[good..mid1]
    else if mid2 == PASS:
        remaining_commits = commits[mid2..bad]
    else:
        remaining_commits = commits[mid1..mid2]
```

### 3. Resuming a Session

```
User runs: auto-bisect resume [session-id]

1. Load BisectSession from disk
2. Check status of any pending builds
3. If builds completed, analyze results and update state
4. If more commits to test, queue next build(s)
5. If narrowed to single commit, report result
```

### 4. Polling Strategy

Since builds can take hours:
- Initial poll: 5 minutes after queue
- Subsequent polls: exponential backoff up to 15 minutes
- User can manually trigger status check with `auto-bisect status`
- Consider webhook integration for instant notification (future)

---

## CLI Commands

### `auto-bisect start`
```
Usage: auto-bisect start --good <build-id> --bad <build-id> [options]

Options:
  --good <build-id>       Build ID where test was passing (required)
  --bad <build-id>        Build ID where test is failing (required)
  --organization <org>    Azure DevOps organization (or from config)
  --project <project>     Azure DevOps project (or from config)
  --test <test-name>      Pre-select test (skip interactive selection)
  --strategy <bisect|trisect>  Search strategy (default: bisect)
  --dry-run               Show what would be done without queuing builds
```

### `auto-bisect status`
```
Usage: auto-bisect status [session-id]

Shows current state of active or specified session:
  - Commits tested and results
  - Pending builds and their status
  - Remaining commits to test
  - Estimated time to completion
```

### `auto-bisect resume`
```
Usage: auto-bisect resume [session-id]

Resumes a bisect session:
  - Checks pending build status
  - Queues next builds if needed
  - Reports if regression found
```

### `auto-bisect abort`
```
Usage: auto-bisect abort [session-id]

Aborts an active session:
  - Cancels any pending builds
  - Marks session as aborted
  - Preserves history for reference
```

### `auto-bisect list`
```
Usage: auto-bisect list [--all]

Lists bisect sessions:
  - Active sessions (default)
  - All sessions including completed (--all)
```

---

## State Persistence

### File Structure
```
~/.auto-bisect/
├── config.json           # Default organization, project, PAT reference
├── sessions/
│   ├── <session-id>.json # Individual session state
│   └── ...
└── cache/
    └── builds/           # Cached build/test data (optional)
```

### Session File Format
```json
{
  "id": "bisect-2024-01-15-abc123",
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T14:45:00Z",
  "goodBuildId": "12345",
  "badBuildId": "12350",
  "goodCommit": "abc123...",
  "badCommit": "def456...",
  "targetTest": "System.Net.Tests.HttpClientTest.GetAsync_Timeout",
  "organization": "dnceng",
  "project": "internal",
  "pipelineId": 1234,
  "state": "WaitingForBuilds",
  "remainingCommits": ["commit1", "commit2", "commit3"],
  "testedCommits": [
    {
      "sha": "abc123...",
      "buildId": "12345",
      "result": "Passed",
      "testedAt": "2024-01-15T10:30:00Z"
    }
  ],
  "pendingBuilds": [
    {
      "sha": "xyz789...",
      "buildId": "12355",
      "queuedAt": "2024-01-15T14:00:00Z",
      "status": "InProgress"
    }
  ]
}
```

---

## Authentication

### Azure DevOps PAT
- Stored securely (environment variable `AUTO_BISECT_PAT` or credential manager)
- Required scopes: `Build (Read & Execute)`, `Test Management (Read)`
- Never persisted in session files

### Configuration Priority
1. Command-line arguments
2. Environment variables
3. Config file (`~/.auto-bisect/config.json`)

---

## Error Handling

### Recoverable Errors
- Network timeouts: Retry with exponential backoff
- Build failures (infrastructure): Mark commit as "Error", don't count in bisection
- Rate limiting: Respect `Retry-After` headers

### Non-Recoverable Errors
- Invalid build IDs: Fail with clear error message
- Authentication failures: Prompt for new PAT
- No commits between builds: Report and exit

### Graceful Shutdown
- Catch SIGINT/SIGTERM
- Persist current state before exit
- Don't cancel in-flight builds (they'll be picked up on resume)

---

## Future Enhancements

1. **Webhook Integration**: Receive build completion notifications instead of polling
2. **Parallel Test Isolation**: Bisect multiple tests simultaneously
3. **Smart Scheduling**: Queue builds during off-peak hours
4. **Blame Integration**: Automatically identify commit author and create issue
5. **Flaky Test Detection**: Detect intermittent failures during bisection
6. **Pipeline Templates**: Support different pipeline configurations per test type
7. **Result Caching**: Reuse results from existing builds at same commits

---

## Dependencies

- **Azure.Identity**: Azure authentication
- **Microsoft.TeamFoundation.Build.WebApi**: AzDO build API
- **Microsoft.VisualStudio.Services.TestManagement.WebApi**: Test result API
- **LibGit2Sharp**: Git operations (optional, can shell out to git)
- **System.Text.Json**: State serialization
- **Spectre.Console**: Rich CLI output and prompts
