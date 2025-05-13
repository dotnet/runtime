# OS Support Matrix

.NET is a cross-platform product, requiring good coverage across [operating system and distro families](https://github.com/dotnet/core/edit/main/os-lifecycle-policy.md).

This document describes our coverage intent. It is a higher-level description than our pipelines.

Pipelines:

- [Runtime](https://github.com/dotnet/runtime/blob/main/eng/pipelines/coreclr/templates/helix-queues-setup.yml)
- [Libraries](https://github.com/dotnet/runtime/blob/main/eng/pipelines/libraries/helix-queues-setup.yml)

## Run types

We rely on multiple levels of testing to provide good coverage at reasonable cost. Our testing uses OSes both as a vehicle to test .NET itself and to validate discover distro-specific breakage. This is why we test multiple OSes for each run type.

- Inner loop -- Baseline set of tests that validate correct functional behavior, for PRs and branch builds.
- Outer loop -- A much larger set of tests that validate expected edge case behavior, for ([on-demand builds)](https://github.com/dotnet/runtime/pull/115415#issuecomment-2864759316).
- Extra platforms -- Additional OSes that are run in a rolling build that can target either inner our outer loop tests.

The libraries pipeline defines these run types. The runtime pipeline has only the inner loop run type.

The remainder of the document defines the tiers we apply to each operating system family. The tiers will be adapted over time, as needed. Architecture is another aspect of coverage. It isn't covered in this document.

## Linux Tiers

The following tiers apply for Linux. 

- Tier 1: Azure Linux, Debian, Ubuntu (in inner and outer loop)
- Tier 2: Alpine and CentOS Stream (in inner loop)
- Tier 3: Fedora and OpenSUSE (in extra platforms)

## Windows Tiers

The following tiers apply for windows.

- Tier 1: Windows Server 2016, Windows Server 2022, Windows Server 2025, Windows 11

Note: "Windows 10" references in pipelines are really Windows Server 2016.
