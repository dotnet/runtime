# Onboarding Guide for New Operating System Versions

Adding support for new operating systems versions is a frequent need. This guide describes how we do that, including the policies we use.

> Being _active_ in `main` enables being _lazy_ in `release/`.

This witticism is the underlying philosophy of our approach. By actively maintaining OS versions in our active coding branch, we get the double benefit of bleeding-edge coverage and (in many cases) can avoid EOL remediation cost in `release/` branches. Spending time on avoidable work is a failure of planning.

> Users are best served when we act _quickly_ not _exhaustively_.

This double meaning is instructing us to be boldly pragmatic. Each new OS release brings a certain risk of breakage. The risk is far from uniform across the various repos and components that we maintain. Users are best served when we've developed 80% confidence and to leave the remaining (potential) 20% to bug reports. Exhaustive testing serves no one. We've also found that our users do a great job finding corner cases and enthusiastically participate in the process by opening issues in the appropriate repo.

Continuing with the idea of pragmatism, if you only read this far, you've got the basic idea. The rest of the doc describes more context and mechanics.

References:

- [New Operating System Version Onboarding Guide](https://github.com/dotnet/dnceng/blob/main/Documentation/ProjectDocs/OS%20Onboarding/Guidance.md)
- [.NET OS Support Tracking](https://github.com/dotnet/core/issues/9638)

## Context

In most cases, we find that new OS versions _may_  uncover problems in dotnet/runtime, but don't affect up-stack components or apps once resolved. A key design point of our runtime is to be a quite complete cross-platform and -architecture abstraction, so resolving OS compatibility breaks for higher-level code is an enduring intent.

Nearly all the APIs that touch native code (networking, cryptography) and deal with standard formats (time zones, ASN.1) are in dotnet/runtime. In many cases, we only see test breaks when we onboard a new OS, often from code that tests edge cases.

## Approach

Our rule is that we declare support (for all [supported .NET releases](https://github.com/dotnet/core/blob/main/releases.md)) for a new OS version after it is validated in dotnet/runtime `main`. We will only hold support on additional testing in special cases (which are uncommon).

We aim to have "day of" support for about half the OSes we support, including Azure Linux, Ubuntu LTS, and Windows. This means we need to perform ahead-of-time signoff on [non-final builds](https://github.com/dotnet/runtime/pull/111768#issuecomment-2617229139).

Our testing philosophy is based on perceived risk and past experience. The effective test matrix is huge, the product of OSes \* supported versions \* architectures.  We try to make smart choices to **skip testing most of the matrix** while retaining much of the **practical coverage**. We also know where we tend to get bitten most when we don't pay sufficient attention. For example, our bug risk across Linux, macOS, and Windows is not uniform.

We  use pragmatism and efficiency to drive our decision making. All things being equal, we'll choose the lowest cost approach.

## Testing

Testing is the bread and butter of OS onboarding, particularly for a mature runtime like ours. New OS support always needs some form of test enablement.

Linux, Wasm, and some Windows testing is done in container images. This approach enables us to test many and regularly changing OS versions in a fixed/limited VM environment. The container image creation/update process is self-service (discussed later).

We use VMs (Linux and Windows) and raw metal hardware (Android and Apple) in cases where containers are not practical or where direct testing is desired. This is the primary model for Apple and Windows OSes. The VMs and mobile/Apple hardware are relatively slow to change and require support from dnceng (discussed later).

### Adding coverage

New OS coverage should be added/tested first in `main`. If changes are required, we should prove them out first in `main` before committing to shipping them in a servicing release, if necessary.

There are multiple reasons to add a new OS reference in a release branch:

- Known product (as opposed to test) breaks that require validation and regression testing.
- Past experience suggests that coverage is required to protect against risk.
- OS version is or [will soon go EOL](https://github.com/dotnet/runtime/issues/111818#issuecomment-2613642202) and should be replaced by a newer version.

For example, we frequently need to backport Alpine updates to release branches to avoid EOL references but less commonly for Ubuntu, given the vast difference in support length.

A good strategy is to keep `main` at the bleeding edge of new OS versions. That way those references have a decent chance of never needing remediation once they end up in release branches.

### Updating or removing coverage

We will often replace an older OS version with a new one, when it comes available. This approach is an effective strategy of maintaining the same level of coverage and of remediating EOL OSes ahead of time. For the most part, we don't need to care about a specific version. We just want coverage for the OS, like Alpine.

We should remediate any EOL OS references in our codebase. They don't serve any benefit and come with some risk. They are also likely to result in compliance tickets (that come with a deadline) that we want to avoid.

In the case that a .NET version will be EOL in <6 (and certainly <3) months, new coverage can typically be skipped. We may even be able to skip remediating EOL OS references. We often opt to stop updating [supported OSes](https://github.com/dotnet/core/blob/main/os-lifecycle-policy.md) late in support period for related reasons. A lazy approach is often the best approach late in the game. Don't upset what's working.

## Building

Our [build methodology](https://github.com/dotnet/runtime/blob/main/docs/project/linux-build-methodology.md) is oriented around cross-compiling, enabling us to target an old OS version and run on newer ones. It is uncommon for us to need to make changes to the build to address new OS versions, however, there are [rare cases where we need to make adjustments](https://github.com/dotnet/runtime/issues/101944).

We use both containers and VMs for building, depending on the OS. If we test in a container, we likely build in a container. Same for VMs.

Our primary concern is ensuring that we are using [supported operating systems and tools for our build](https://github.com/dotnet/runtime/tree/main/docs/workflow/requirements).

Our Linux build containers are based on Azure Linux. We [typically need to update them](https://github.com/dotnet/runtime/issues/112191) with a new version of Azure Linux once per release. We do not update the toolset, however. That's fixed, per release.

For Apple, we likely need to make an adjustment at each macOS or iOS release to account for an Xcode version no longer being supported.

## Environments

We rely on a set of standard environments for building and testing. These environments are managed with a "config as code" paradigm in our source code. This approach delivers CI reliability -- which we greatly value -- but also comes with the tedious cost of needing to regularly update various reference strings in several files. Updating version strings almost always requires sorting through build breaks, which is a reminder of why we value our approach.

We may use multiple environments for building and testing a given OS.

### Containers

New container images need to be created for each new OS version in the [dotnet/dotnet-buildtools-prereqs-docker](https://github.com/dotnet/dotnet-buildtools-prereqs-docker) repo. These are used for building and testing Android, Linux, Wasm, and Windows OSes/targets.

The repo is self-service and largely self-explanatory. One typically creates a new image using the pattern demonstrated by the previous version. Look at commits and [blame](https://github.com/dotnet/dotnet-buildtools-prereqs-docker/blame/776324ff16d38e22fd9f06c9842ec338a4b98489/src/alpine/3.20/helix/Dockerfile) to find people who are best suited to help.

Installing/building the Helix client can be quite involved, particularly for Arm platforms. Don't struggle with that. Just ask for help.

Container images are referenced in our pipeline files:

- [eng/pipelines/coreclr/templates/helix-queues-setup.yml](https://github.com/dotnet/runtime/blob/main/eng/pipelines/coreclr/templates/helix-queues-setup.yml)
- [eng/pipelines/libraries/helix.yml](https://github.com/dotnet/runtime/blob/main/eng/pipelines/libraries/helix.yml)
- [eng/pipelines/common/templates/pipeline-with-resources.yml](https://github.com/dotnet/runtime/blob/main/eng/pipelines/common/templates/pipeline-with-resources.yml)

Notes:

- The first two links are for testing and the last for building.
- The links are for the `main` branch. Release branches should have the same layout.

Example PRs:

- [dotnet/runtime #111768](https://github.com/dotnet/runtime/pull/111768)
- [dotnet/runtime #111504](https://github.com/dotnet/runtime/pull/111504)
- [dotnet/runtime #110492](https://github.com/dotnet/runtime/pull/110492)
- [dotnet/dotnet-buildtools-prereqs-docker #1282](https://github.com/dotnet/dotnet-buildtools-prereqs-docker/pull/1282)
- [dotnet/dotnet-buildtools-prereqs-docker #1314](https://github.com/dotnet/dotnet-buildtools-prereqs-docker/pull/1314)

### VMs

VMs and raw metal environments are used for Android, Apple, Linux, and Windows OSes. They need to be [requested from dnceng](https://github.com/dotnet/dnceng/issues/4307). The turnaround can be long so put in your request before you need it.

- Android: Raw metal hosts for some forms of Android testing.
- Apple: Raw metal hosts for all forms of Apple testing.
- Linux: All Linux VMs are moving to Azure Linux as a container host.
- Windows: Windows Client and Server VMs

### Other

Other environments are typically use a custom process.

- [Browser Wasm](https://github.com/dotnet/runtime/pull/112066)

## Porting

[Porting .NET to a new operating system or architecture](../design/coreclr/botr/guide-for-porting.md) is a related task. The same patterns likely apply, but the overall task is much larger in scope.
