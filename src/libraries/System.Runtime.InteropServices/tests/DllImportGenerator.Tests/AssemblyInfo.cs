// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

// We build the libraries tests in CI once per target OS+Arch+Configuration, but we share it between runtime test runs.
// As a result, we need to exclude the Mono run here since we build the tests once for CoreCLR and Mono for desktop test runs.
// We should switch this to another mechanism in the future so we don't submit a work item of this assembly that skips every test
// for Mono-on-Desktop-Platforms test runs.
[assembly:ActiveIssue("https://github.com/dotnet/runtime/issues/59815", TestRuntimes.Mono)]
