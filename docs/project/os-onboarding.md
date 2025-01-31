# OS Onboarding Guide

Adding support for new operating systems (largely just new versions) is a frequent need. This guide describes how we do that, including policies we use.

References:

- [.NET OS Support Tracking](https://github.com/dotnet/core/issues/9638)
- [Prereq container image lifecycle](https://github.com/dotnet/dotnet-buildtools-prereqs-docker/blob/main/lifecycle.md)

## Context

In most cases, we find that new OSes _may_  uncover problems in dotnet/runtime and once resolved don't affect up-stack components or apps. This is because nearly all the APIs that touch native code (networking, cryptography) and deal with standard formats (time zones, ASN.1) are in dotnet/runtime. In many cases, we only see test breaks.

Our testing philosophy is based on risk and past experience. The effective test matrix is huge, the product of OSes \* supported versions \* architectures.  We try to make smart choices to skip testing most of the matrix while retaining much of the practical coverage. We also know where we tend to get bitten most when we don't pay sufficient attention. For example, our bug risk across Linux, macOS, and Windows is not uniform.

## Approach

New OSes should be added/tested first in `main`. If changes are required, we should prove them out first in `main` before committing to shipping them in a servicing release. However, it isn't always necessary to backport test coverage.

There are two reasons (beyond known product breaks) to add a new OS reference to a release branch:

- Add coverage due to practice or known risk
- Update a reference to an EOL OS version

If those reasons don't apply, then we can often skip backporting new coverage.

In the case that a .NET version will be EOL in <6 months, then new coverage can typically be skipped.

## End-of-life

We will often  maintain our level of coverage when a new OS comes available by replacing an older one. This ends up being an effective stratgegy to remediating EOL OSes, ahead of time.

In some cases, we're required to test an OS version until the end of its life and will need to take specific action to remediate the reference.

For whatever the reason, we should update references to EOL OSes if we have them.

## Mechanics

Most of our testing is done in container images. New images need to be created for each new version in the [dotnet/dotnet-buildtools-prereqs-docker](https://github.com/dotnet/dotnet-buildtools-prereqs-docker) repo. The repo is self-service and largely self-explanatory. One typically creates a new image using the pattern demonstrated by the previous version.

These images are referenced in our pipeline files:

- https://github.com/dotnet/runtime/blob/main/eng/pipelines/coreclr/templates/helix-queues-setup.yml
- https://github.com/dotnet/runtime/blob/main/eng/pipelines/libraries/helix.yml

Those files are for the `main` branch. The same files should be located in the same location in release branches.

Example PRs:

- https://github.com/dotnet/runtime/pull/111768
- https://github.com/dotnet/runtime/pull/111504
- https://github.com/dotnet/runtime/pull/110492
