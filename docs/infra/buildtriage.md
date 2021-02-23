## Build Triage Rotation

The responsibility of this role is triaging our rolling / official builds, filing issues to track broken tests, submitting changes to dotnet/runtime to work around issues (disabling a test, undoing a PR that broke the build).

In some cases this will require working with core-eng team when the issues are in Helix / Azure / Arcade. This person will also attend CI Council with the infra manager to provide updates on our reliability status.

This directly impacts developer productivity and the need to promptly fix such breaks can span across time zones. Hence it will be the collective responsibility of the Scout pool to investigate such breaks.

This role will work on a rotation basis. There are six people in the role and each rotation will last for a calendar month.

## Prerequisites
Please make sure that you are part of the following groups before you start the rotation:
- Runtime Infrastructure GitHub group: https://github.com/orgs/dotnet/teams/runtime-infrastructure
- Internal distribution list: runtimerepo-infra
- Internal teams channel: dotnet/runtime repo -> Infrastructure Team

Unfortunately, the teams channel's members need to be listed individually. Ping @ViktorHofer if you need access.

## Tracking Build Failures
All the CI failures can be tracked through the CI Council dashboards i.e. [Public](https://dev.azure.com/dnceng/public/_dashboards/dashboard/40ac4990-3498-4b3a-85dd-2ffde961d672), [Internal](https://dev.azure.com/dnceng/internal/_dashboards/dashboard/e1bb572d-a2b0-488f-a58a-54c73a547f0d).
We have different dashboards for public (Rolling & PR Builds) and internal builds.

In addition to the dashboards, official build failure notifications are sent to the internal runtime infrastructure email alias.

For each of these mail notifications, a matching issue should exist (either in the dotnet/runtime repository or in dotnet/core-eng or dotnet/arcade). The person triaging build failures should reply to the email with a link to the issue to let everyone know it is triaged. This guarantees that we are following-up on infrastructure issues immediately. If a build failure's cause isn't trivial to identify, consider looping in dnceng.

Tests are not run during the internal builds. Publishing and signing steps are run only during internal builds. Rolling builds run tests for the full matrix.

Any consistently failing test where the fix is not in pipeline should be promptly disabled on the CI. Don't leave tests failing in CI while you investigate; it's too disruptive for the rest of the team.

For new issues, try to provide a [runfo](https://runfo.azurewebsites.net/) search which will make it easy to isolate repeated instances of that failure.

Contact @chcosta if you are having any trouble accessing the dashboards.
Contact @Chrisboh if you don't have the calendar invite for the CI Council meeting.
Contact @jaredpar if you are having any trouble with runfo, site or utility.

## Ongoing Issues

All the issues causing the builds to fail should be marked with [`blocking-clean-ci`](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+label%3Ablocking-clean-ci) label.
Any issues causing build breaks in the official build should be marked with [`blocking-clean-official`](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+label%3Ablocking-clean-official).
It helps in tracking issues effectively.

The main meta-bug linking to currently tracked issues is [here](https://github.com/dotnet/runtime/issues/702).

## Some helpful resources
- [runfo Website](https://runfo.azurewebsites.net/)
- [runfo command-line util](https://github.com/jaredpar/devops-util)
- [runfo Documentation](https://github.com/jaredpar/devops-util/tree/master/runfo)
- [Internal Build Definition](https://dev.azure.com/dnceng/internal/_build?definitionId=679)
- [Public Build Definition](https://dev.azure.com/dnceng/public/_build?definitionId=686)
- [Runtime dependency status](https://maestro-prod.westus2.cloudapp.azure.com/1296/https:%2F%2Fgithub.com%2Fdotnet%2Fruntime/latest/graph)

## Build Rotation for upcoming months

| Month | Alias  |
|-------|-----------|
| September 2020 | @directhex |
| October 2020   | @jkoritzinsky |
| November 2020  | @aik-jahoda |
| December 2020  | @akoeplinger |
| January 2021   | @hoyosjs |
| February 2021  | @anipik |
| March 2021  | @directhex |
