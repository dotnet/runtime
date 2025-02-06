# Onboarding Guide for New Operating System Versions

Adding support for new operating systems versions is a frequent need. This guide describes how we do that, including policies we use.

[Porting .NET to a new operating system or architecture](../design/coreclr/botr/guide-for-porting.md) is a related task. Some of these patterns apply, but the overall task will be much larger.

References:

- [.NET OS Support Tracking](https://github.com/dotnet/core/issues/9638)
- [.NET Support](https://github.com/dotnet/core/blob/main/support.md)
- [Prereq container image lifecycle](https://github.com/dotnet/dotnet-buildtools-prereqs-docker/blob/main/lifecycle.md)

Internal links:

- https://dev.azure.com/dnceng/internal/_wiki/wikis/DNCEng%20Services%20Wiki/940/Support-for-Linux-Distros
- https://dev.azure.com/dnceng/internal/_wiki/wikis/DNCEng%20Services%20Wiki/933/Support-for-Apple-Operating-Systems-(macOS-iOS-and-tvOS)
- https://dev.azure.com/dnceng/internal/_wiki/wikis/DNCEng%20Services%20Wiki/939/Support-for-Windows-Operating-Systems

## Context

In most cases, we find that new OS versions _may_  uncover problems in dotnet/runtime and once resolved don't affect up-stack components or apps. A key design point of our runtime is to be a quite complete cross-platform and -architecture abstraction, so resolving OS compatibility breaks for higher-level code is our enduring intent.

Nearly all the APIs that touch native code (networking, cryptography) and deal with standard formats (time zones, ASN.1) are in dotnet/runtime. In many cases, we only see test breaks when we onboard a new OS, often from code that tests edge cases.

## Approach

Our rule is that we declare support for a new OS (for all supported .NET versions) after it is validated in dotnet/runtime `main`. We  only hold support on additional testing in special cases (which are uncommon).

Our testing philosophy is based on percieved risk and past experience. The effective test matrix is huge, the product of OSes \* supported versions \* architectures.  We try to make smart choices to skip testing most of the matrix while retaining much of the practical coverage. We also know where we tend to get bitten most when we don't pay sufficient attention. For example, our bug risk across Linux, macOS, and Windows is not uniform.

We  use pragmatism and efficiency to drive our decision making. All things being equal, we'll choose the lowest cost approach.

## Testing

Testing is the bread-and-butter of OS onboarding, particularly for a mature runtime like ours. New OS support always needs some form of test enablement.

Linux and some Windows testing is done in container images. This approach enables us to test many and regularly changing OS versions in a fixed/limited VM environment. The container image creation/update process is self-service.

We also have VMs (Linux and Windows) and raw metal hardware (Apple) for more direct testing. This is the primary model for Apple and Windows OSes. The VMs are and Apple hardware are relatively slow moving and require [support from dnceng](https://github.com/dotnet/dnceng).

### Adding coverage

New OS coverage should be added/tested first in `main`. If changes are required, we should prove them out first in `main` before committing to shipping them in a servicing release, if necessary.

There are multiple reasons to add a new OS reference to a release branch:

- Known product breaks that require validation and regression testing.
- Past experience suggests that coverage is required to protect against risk.
- OS version is or [will soon go EOL](https://github.com/dotnet/runtime/issues/111818#issuecomment-2613642202) and should be replaced by a newer version.

For example, we frequently need to backport Alpine updates to release branches to avoid EOL references but less commonly for Ubuntu, given the vast difference in support length.

A good strategy is to keep `main` at the bleeding edge of new OS versions. That way those references have a decent chance of never needing remediation once they end up in release branches. Being _active_ in `main` enables being _lazy_ in `release/`.

### Updating or removing coverage

We will often replace an older OS version with a new one, when it comes available. This approach is an effective strategy of maintaining the same level of coverage and of remediating EOL OSes ahead of time. For the most part, we don't need to care about a specific version. We just want coverage for the OS, like Alpine.

We should remediate any EOL OS references in our codebase. They don't serve any benefit and come with some risk.

In the case that a .NET version will be EOL in <6 months, new coverage can typically be skipped. We may even be able to skip remediating EOL OS references. We often opt to stop updating [Supported OSes](https://github.com/dotnet/core/blob/main/os-lifecycle-policy.md) late in support period for related reasons. A lazy approach is often the best approach late in the game. Don't upset what's working.

## Building

Our [build methodology](https://github.com/dotnet/runtime/blob/main/docs/project/linux-build-methodology.md) is oriented around cross-compiling, enabling us to target an old OS version and run on newer ones. It is uncommon for us to need to make changes to the build to address new OS versions, however, there are [rare cases where we need to make adjustments](https://github.com/dotnet/runtime/issues/101944).

We use both containers and VMs for building, depending on the OS.

Our primary concern is ensuring that we are using [supported operating systems and tools for our build](https://github.com/dotnet/runtime/tree/main/docs/workflow/requirements).

Our Linux build containers are based on Azure Linux. We [typically need to update them](https://github.com/dotnet/runtime/issues/112191) with a new version of Azure Linux once per release. We do not update the toolset, however. That's fixed, per release. 

## Prereqs containers

New images need to be created for each new OS version in the [dotnet/dotnet-buildtools-prereqs-docker](https://github.com/dotnet/dotnet-buildtools-prereqs-docker) repo.

The repo is self-service and largely self-explanatory. One typically creates a new image using the pattern demonstrated by the previous version. Look at commits and [blame](https://github.com/dotnet/dotnet-buildtools-prereqs-docker/blame/776324ff16d38e22fd9f06c9842ec338a4b98489/src/alpine/3.20/helix/Dockerfile) to find people who are best suited to help.

Installing/building the Helix client can be quite involved, particularly for Arm platforms. Don't struggle with that. Just ask for help.

Test container images are referenced in our pipeline files:

- https://github.com/dotnet/runtime/blob/main/eng/pipelines/coreclr/templates/helix-queues-setup.yml
- https://github.com/dotnet/runtime/blob/main/eng/pipelines/libraries/helix.yml
- https://github.com/dotnet/runtime/blob/main/eng/pipelines/common/templates/pipeline-with-resources.yml

Those files are for the `main` branch. The same files should be located in the same location in release branches.

Example PRs:

- <https://github.com/dotnet/runtime/pull/111768>
- <https://github.com/dotnet/runtime/pull/111504>
- <https://github.com/dotnet/runtime/pull/110492>
- <https://github.com/dotnet/dotnet-buildtools-prereqs-docker/pull/1282>
- <https://github.com/dotnet/dotnet-buildtools-prereqs-docker/pull/1314>

### VMs

VMs and raw metal environments are used for Linux, macOS, and Windows. They need to be [requested from dnceng](https://github.com/dotnet/dnceng/issues/4307). The turnaround can be long so put in your request before you need it.

- Linux: All Linux VMs are moving to Azure Linux as a container host.
- macOS: Raw metal hosts for all forms of Apple testing.
- Windows: Windows Client and Server VMs
