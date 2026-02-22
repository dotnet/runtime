# CI and Pull Requests

Guides for working with the dotnet/runtime CI system and contributing changes.

## Contributing

| Guide | Description |
|-------|-------------|
| [PR Guide](pr-guide.md) | How to create and manage pull requests |
| [Contribution Guidelines](/CONTRIBUTING.md) | Repository contribution policies |

## CI System

| Guide | Description |
|-------|-------------|
| [Pipelines Overview](pipelines-overview.md) | How the CI pipelines are structured |
| [Failure Analysis](failure-analysis.md) | How to investigate CI failures |
| [Triaging Failures](triaging-failures.md) | Process for handling flaky tests |
| [CoreCLR CI Health](coreclr-ci-health.md) | CoreCLR-specific CI status |

## Test Management

| Guide | Description |
|-------|-------------|
| [Disabling Tests](disabling-tests.md) | How to temporarily disable flaky tests |

## Quick Reference

### Before Submitting a PR

1. Build locally: `./build.sh clr+libs`
2. Run relevant tests
3. Review [contribution guidelines](/CONTRIBUTING.md)
4. Read the [PR guide](pr-guide.md)

### When CI Fails

1. Check if it's a known issue in [failure analysis](failure-analysis.md)
2. Look at the build logs
3. Try to reproduce locally
4. If flaky, see [triaging failures](triaging-failures.md)

### Understanding CI Results

- ✅ Green check: All tests passed
- ❌ Red X: Build or test failure
- 🟡 Yellow dot: Running or pending

Check the PR's "Checks" tab for details.
