
# Platform Testing Organization

## Overview

.NET ships on many platforms. Testing all platforms all the time is too expensive. This document defines the testing policy for each branch of the product.

## Policy

We want to mix and match platform versions and .NET versions to produce good platform coverage without too much a cost. This means we want to catch breaks on each platform as quickly as possible, and prioritize catching the type of platform breaks that are most likely to affect the specific version being tested.

* `main` - PRs run on the *latest supported* platform.
* `servicing` - PRs run on the *oldest supported* platform.

The above policy only applies to PRs. Scheduled or incidental runs can be queued against other platform definitions, if deemed necessary.

## Details

### Platform version

There are two platform versions we want to test on, depending on the .NET support lifecycle:

1. Latest supported
2. Oldest supported

We assume that all supported platform versions in between have sufficient coverage based on the latest and the oldest. We currently have no defined strategy for pre-release versions.

### .NET lifecycle

We currently regularly maintain three .NET versions:

1. `main`, i.e. the next release
2. The previous release
3. The release before the previous release

The last two versions (which have already been released) are called "servicing" releases.
