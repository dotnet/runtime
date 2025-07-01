# How to service a library

This document provides the steps that need to be followed after modifying a library in a servicing branch.

Servicing branches represent shipped versions of .NET, and their name is in the format `release/X.0-staging`. Examples:

- `release/9.0-staging`
- `release/8.0-staging`

IMPORTANT: Starting with .NET 9, you no longer need to edit a NuGet package's csproj to enable building and bump the version.
Keep in mind that we still need package authoring in .NET 8 and older versions.

## Test your changes

Develop and test your change as normal.  For packages, you may want to test them outside the repo infrastructure. To do so, execute the following steps:

1. From a clean copy of your branch, run `build.cmd/sh libs -pack`

2. Check in `artifacts\bin\packages\Debug` for the existence of your package, with the appropriate package version.

3. Try installing the built package in a test application, testing that your changes to the library are present & working as expected.
   To install your package add your local packages folder as a feed source in VS or your nuget.config and then add a PackageReference to the specific version of the package you built then try using the APIs.

## Approval Process

All the servicing change must go through an approval process. You have two ways to submit your PR:

- By manually creating your PR using [this template](https://raw.githubusercontent.com/dotnet/runtime/main/.github/PULL_REQUEST_TEMPLATE/servicing_pull_request_template.md).
- Or by asking the bot to automatically create the servicing PR for you using a merged `main` PR as source. This method requires typing an AzDO backport command as a comment of your merged PR using the format `/backport to release/X.0-staging`. Examples:

  - `/backport to release/9.0-staging`
  - `/backport to release/8.0-staging`

For all cases, you must:

- Fill out the template of the PR description.
- Bring it to the attention of the [engineering lead responsible for the area](/docs/area-owners.md).
- If the fix is a product change, the area owner will:
  - Add the `Servicing-consider` label.
  - Ask the area owner to champion your PR in the .NET Tactics meeting to request merge approval.
  - If the change is approved, they will replace the `Servicing-consider` label by `Servicing-approved` and sign-off the PR.
- If the fix is a test-only or infra-only change, the area owner will:
  - Review the PR and sign-off if they approve it.
  - Add the `Servicing-approved` label.

The area owner can then merge the PR once the CI looks good (it's either green or the failures are investigated and determined to be unrelated to the PR).

**Note**: Applying the `Servicing-approved` label ensures the `check-service-labels` CI job passes, which is a mandatory requirement for merging a PR in a servicing branch.