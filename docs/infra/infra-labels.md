
# Infra labels

The following labels have special meaning for dotnet/runtime infrastructure, usually to indicate the health of testing and building the repository.

- blocking-outerloop – Blocking any outerloop build, meaning a build not normally run on PRs that is queued on a time schedule.
- blocking-clean-ci – “Blocking our core PR or CI builds”
- blocking-clean-ci-optional – “Blocking optional legs in PRs”
