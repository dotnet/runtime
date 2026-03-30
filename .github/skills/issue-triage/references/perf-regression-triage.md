# Performance Regression Triage

Triage-specific guidance for assessing and recommending action on performance
regressions in dotnet/runtime. Referenced from the main
[SKILL.md](../SKILL.md) during Step 5.

For detailed investigation methodology (benchmarking, bisection, bot usage),
use the `performance-investigation` skill. This document covers only the
triage-specific assessment and recommendation criteria.

## Sources of Performance Regressions

A performance regression is a report that something got measurably slower (or
uses more memory/allocations) compared to a previous .NET version or a recent
commit. These reports come from several sources:

- **Automated bot issues** -- filed by `performanceautofiler[bot]` with
  baseline/compare commits, regression tables, and repro commands.
- **Customer reports** -- a user reports that an operation became slower after
  upgrading to a new .NET version, often with sample code or a reproduction
  project.
- **Cross-release regressions** -- a regression observed between two stable
  releases (e.g., .NET 9 → .NET 10) without a specific commit range.

## Investigation

The investigation goal is to validate that the regression is real and, if
possible, bisect to the exact commit that introduced it.

Use the `performance-investigation` skill (Workflow 2: Regression Investigation)
for the full methodology, which includes:

- Feasibility checks for local vs. bot-based investigation
- Building dotnet/runtime at specific commits and using CoreRun
- Comparing good/bad commits with BenchmarkDotNet
- Git bisect workflow for finding the culprit commit
- Using @EgorBot and @MihuBot for remote validation

## Performance-Specific Assessment

When assessing a performance regression in Step 6, consider:

- **Severity** -- What is the Test/Base ratio? >2× is severe; 1.1–1.2× may be
  noise or acceptable.
- **Breadth** -- How many benchmarks regressed? A single narrow benchmark vs.
  hundreds of benchmarks suggests different root causes.
- **Affected component** -- Is it JIT (codegen), GC, a specific library, or
  cross-cutting?
- **User impact** -- Does the regressing benchmark represent a real-world hot
  path, or is it a synthetic microbenchmark?
- **Trade-off** -- Was the regression an intentional trade-off for correctness,
  security, or another dimension? Check the PR description of the culprit
  commit.

## Performance-Specific Recommendation Criteria

### KEEP

- Regression is confirmed with statistical significance
- Regression is not an intentional trade-off documented in the culprit PR
- Bisect identified a specific culprit commit (include it in the report)

### CLOSE

- Regression is not reproducible locally and the automated data appears noisy
- Regression was an intentional, documented trade-off
- Regression has already been fixed in a subsequent commit

### NEEDS INFO

- Regression is marginal (Test/Base <1.05) and could be noise -- request a
  re-run on the performance infrastructure
- Environment-specific regression that cannot be reproduced locally -- note the
  environment mismatch
