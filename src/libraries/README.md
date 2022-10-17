# .NET Libraries

This folder contains the source and tests for the .NET Libraries. Different libraries are owned by different team members; refer to the [Areas](../../docs/area-owners.md#areas) list for lead and owner information.

## Contribution Bar

Some libraries are under more active development than others. Depending on the library's status, expectations for issues and pull requests can vary. Check the library's folder for a `README.md` that declares the contribution bar for that library which consists of  a **Primary bar** and optional **Secondary bars**. Regardless of a library's contribution bar, refer to the [DOs and DON'Ts](../../CONTRIBUTING.md#dos-and-donts) and [Suggested Workflow](../../CONTRIBUTING.md#suggested-workflow) in our contribution guidelines before submitting a pull request.

### Assumed bars (unless a library says otherwise)
- **We take security fixes**
- **We take test changes**
- **We don't take style-only changes**

### Primary bar
- **We take new features, new APIs and performance changes**
  - This is the most encompassing category and takes on the most risk 
  - PRs for both features and fixes will be considered when aligned with current efforts
    - For new APIs, please follow the [API Review Process](../../docs/project/api-review-process.md)
  - PRs for performance gains are welcome. Update [benchmarks](https://github.com/dotnet/performance) as appropriate.
  - Refactoring changes are welcome
- **We only take fixes to maintain or improve quality**
  - New features and APIs are **not** normally considered but there are exceptions such as when responding to runtime changes or language additions
  - The library is likely mature and feature-complete
  - PRs for performance are considered if they are lower-risk or high-impact
  - Refactoring changes are considered if there is a clear benefit
- **We only take lower-risk or high-impact fixes to maintain or improve quality**
  - New features and APIs are **not** considered
  - We don't take potentially destabilizing fixes or test changes unless there is a clear need
- **We only take fixes that unblock critical issues**
  - New features and APIs are **not** considered
  - This may include infrastructure changes and other housekeeping

### Secondary bars
- **We take PRs that target this library for new [source code analyzers](../../docs/project/analyzers.md)**
- **We don't take refactorting changes due to new language features**

## Deployment

Some libraries are included in the .NET SDK as part of the runtime's [shared framework](https://learn.microsoft.com/en-us/dotnet/standard/glossary#shared-framework). Other libraries are deployed as out-of-band (OOB) NuGet packages and need to be installed separately.

For more information, see the [Runtime libraries overview](https://learn.microsoft.com/en-us/dotnet/standard/runtime-libraries-overview).
