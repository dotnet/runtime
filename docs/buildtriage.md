## Build Triage Rotation

The responsibility of this role is triaging our rolling / official builds, filing issues to track broken tests, submitting changes to dotnet/runtime to work around issues (disabling a test, undoing a PR that broke the build). 

In some cases this will require working with core-eng team when the issues are in Helix / Azure / Arcade. This person will also attend CI Council with the infra manager to provide updates on our reliability status.

This directly impacts developer productivity and the need to promptly fix such breaks can span across time zones. Hence it will be the collective responsibility of the Scout pool to investigate such breaks. 

This role will work on a rotation basis. There are six people in the role and each rotation will last for a calendar month.

## Tracking Build Failures
All the CI failures can be tracked through the CI Council dashboards i.e.  [Public](https://dev.azure.com/dnceng/public/_dashboards/dashboard/40ac4990-3498-4b3a-85dd-2ffde961d672), [Internal](https://dev.azure.com/dnceng/internal/_dashboards/dashboard/e1bb572d-a2b0-488f-a58a-54c73a547f0d).
We have different dashboards for public(Rolling & PR Builds) and internal builds. 

Tests are not run during the internal builds. Publishing and signing steps are run only during  internal builds. Rolling builds runs tests for the full matrix. 

Contact @chcosta if you are having any trouble accessing the dashboards.

## Ongoing Issues

All the issues causing the builds to fail should be marked with ```blocking-clean-ci``` label.

It helps in tracking issues effectively. There are some issues that are reproduced more often than others and should be fixed with a higher priority in the next rotation. Such issues are mentioned in the following table.

| Issue | Comments  | 
|-------|-----------|
| https://github.com/dotnet/runtime/issues/35101| |
| https://github.com/dotnet/runtime/issues/41078| |
| https://github.com/dotnet/runtime/issues/35916| |
| https://github.com/dotnet/runtime/issues/41494| |


## Some helpful resources
- [runfo Website](https://runfo.azurewebsites.net/)
- [runfo Documentation](https://github.com/jaredpar/devops-util/tree/master/runfo)
- [Internal Build Defination](https://dev.azure.com/dnceng/internal/_build?definitionId=679)
- [Public Build Defination](https://dev.azure.com/dnceng/public/_build?definitionId=686)

## Build Rotation for upcoming months

| Month | Alias  | 
|-------|-----------|
| September |  @directhex  |
| Octorber  | @jkoritzinsky |
| November  | @aik-jahoda  |
| December  | @akoeplinger   |

