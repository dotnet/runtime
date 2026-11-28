# Auto-Bisect Tool Design

## Overview

Auto-bisect is a command-line tool that automatically identifies the first commit that introduced a test failure in Azure DevOps builds. Given a known good build and a known bad build, it bisects the commit range to find the exact commit that caused the regression.

## Architecture

### High-Level Design

The tool integrates three key systems:
1. **Azure DevOps API** - For accessing build information, test results, and queuing new builds
2. **Local Git repository** - For retrieving commit history and metadata
3. **Bisection algorithm** - For efficiently narrowing down the search space

### User Interaction Flow

#### 1. Initial Setup
The user provides:
- Azure DevOps organization and project
- A known good build ID (test passes)
- A known bad build ID (test fails)
- The exact name of the test to track

The tool authenticates using a Personal Access Token (PAT) from the `AZDO_PAT` environment variable or `--pat` flag. **Note: This is only available to Microsoft employees.**

#### 2. Bisection Process

In **auto-queue mode** (default), the tool automatically:
- Searches for existing builds at the target commit
- Queues a new build if none exists
- Polls the build until completion
- Checks the test result and updates the search range

In **manual mode**, the tool:
- Only uses existing builds
- Prompts the user to manually queue builds for untested commits
- Waits for user confirmation before proceeding

#### 3. Result Presentation
Once complete, the tool displays:
- The exact commit SHA that introduced the failure
- Commit metadata (author, date, message)
- Summary of all builds tested during bisection

### Supporting Commands

Beyond the main `bisect` command, the tool provides utilities for exploration:

- **`diff`** - Compares two builds to show which tests newly failed, useful for finding the right test to bisect
- **`tests`** - Lists all failed tests in a specific build
- **`build`** - Shows detailed information about a single build
- **`queued`** - Displays status of queued builds for monitoring progress

## Key Design Decisions

### Azure DevOps Integration
Uses Azure DevOps REST API with PAT authentication. Requires minimal permissions: read access to builds and tests, plus execute permission to queue builds in auto-queue mode.

### Local Git Dependency
Relies on a local Git clone to retrieve commit history rather than querying Azure DevOps. This provides accurate chronological ordering and avoids API limitations, but requires the user to have the repository checked out locally.

### Stateless and resumable
The tool doesn't maintain its own persistent state between runs, but it reuses existing build results from Azure DevOps. If a bisection is interrupted and restarted with the same good/bad builds, it will find and use previously queued builds rather than queuing duplicates. This makes the bisection process resumable without explicit state management.

## Usage Examples

### Typical Workflow
```bash
# Set up authentication
export AZDO_PAT=<your-token>

# First, find which tests are failing
dotnet run -- diff \
  -o dnceng-public -p public \
  --good 12345 --bad 12350

# Then bisect to find the culprit commit
dotnet run -- bisect \
  -o dnceng-public -p public \
  --good 12345 --bad 12350 \
  --test "MyNamespace.MyTestClass.MyFailingTest"
```

### Manual Mode (Use Existing Builds Only)
```bash
dotnet run -- bisect \
  -o dnceng-public -p public \
  --good 12345 --bad 12350 \
  --test "MyNamespace.MyTestClass.MyFailingTest" \
  --manual
```

## Limitations

- Requires a local Git repository clone at the commit range being tested
- Needs pre-existing good and bad builds as starting points
- Test name must match exactly as reported in Azure DevOps test results
- Azure DevOps specific—does not support other CI/CD platforms

## Trade-offs

**Build Result Reuse**: The tool leverages Azure DevOps as implicit state storage by reusing existing builds. This means bisections are naturally resumable but also means you can't easily "reset" a bisection without manually deleting builds.

**Auto-queue vs. Manual**: Auto-queue mode is faster and more convenient but consumes CI resources. Manual mode gives users full control over when builds run, useful for resource-constrained scenarios or when builds are expensive.
