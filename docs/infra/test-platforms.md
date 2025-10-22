
# Platform Testing Organization

## Overview

.NET ships on many platforms. For our purposes, a platform consists of a combination of:

* Architectures (x86, arm64, et al.)
* OSes, including libc (Windows, Linux, Linux-musl, Browser, et al.)
* OS flavors (Windows Client, Windows Server, Windows Nano, Azure Linux, Ubuntu, Debian, Alpine, Chrome Browser, Mozilla Browser, et al.)
* Crypto stack (OpenSSL, Android, SymCrypt, et al.)

Each of the above combinations is considered a single "platform". New versions are not considered new platforms, but different versions of the same platform. Only if a new version modifies one of the above elements would it be considered a new platform.

Testing all versions of all platforms all the time is too expensive. This document defines the testing policy for each branch of the product.

## Policy

### Versions

We want to mix and match platform versions and .NET versions to produce good platform coverage without too much cost. This means we want to catch breaks on each platform as quickly as possible, and prioritize catching the type of platform breaks that are most likely to affect the specific version being tested.

* `main` - PRs run on the *latest supported* platform.
* `servicing` - PRs run on the *oldest supported* platform.

The above policy only applies to PRs. Scheduled or incidental runs can be queued against other platform definitions, if deemed necessary.

- [ ] **Open question** Should certain area paths trigger additional version testing?

## Details

### Platform version

There are two platform versions we want to test on, depending on the .NET support lifecycle:

1. Latest supported
2. Oldest supported

We assume that all supported platform versions in between have sufficient coverage based on the latest and the oldest. We currently have no defined strategy for pre-release versions.

### .NET lifecycle

We regularly maintain three .NET versions:

1. `main`, i.e. the next release.
2. The previous release.
3. The release before the previous release.

The last two versions (which have already been released) are called "servicing" releases. During the Release Candidate (RC1, RC2, GA) phase of the next release, its release branches are treated the same as "servicing" releases.

Because the policy differs between main and servicing, whenever the main branch is snapped to a servicing release, the PR configuration needs to change. In particular, the following tasks have to be done:

1. [ ] Determine the **oldest** supported version of each test platform. The policy to determine this version is currently out of scope of this document. Refer to https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
2. [ ] Change PR definitions in CI to use oldest versions.
3. [ ] Stabilize CI for oldest OS versions.
