# .NET Libraries

This folder contains the source and tests for the .NET Libraries. Different libraries are owned by different team members; refer to the [Areas](../../docs/area-owners.md#areas) list for lead and owner information.

## Development Statuses

Some libraries are under more active development than others. Depending on the library's status, expectations for issues and pull requests can vary. Check the library's folder for a `README.md` that declares the status for that library. Regardless of a library's status, refer to the [DOs and DON'Ts](../../CONTRIBUTING.md#dos-and-donts) and [Suggested Workflow](../../CONTRIBUTING.md#suggested-workflow) in our contribution guidelines before submitting a pull request.

- **Active**
  - Under active development by the team
  - Issues will be considered for fix or addition to the backlog
  - PRs for both features and fixes will be considered when aligned with current efforts
- **Inactive**
  - Under minimal development; quality is maintained
  - Issues will be considered for fix or addition to the backlog
  - PRs for both features and fixes will be considered
- **Legacy**
  - Not under development; maintained for compatibility
  - Issues are likely to be closed without fixes
  - PRs are unlikely to be accepted

## Deployment

Some libraries are included in the .NET SDK as part of the runtime's [shared framework](https://learn.microsoft.com/en-us/dotnet/standard/glossary#shared-framework). Other libraries are deployed as out-of-band (OOB) NuGet packages and need to be installed separately.

For more information, see the [Runtime libraries overview](https://learn.microsoft.com/en-us/dotnet/standard/runtime-libraries-overview).
