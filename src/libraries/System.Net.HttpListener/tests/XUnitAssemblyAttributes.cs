// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

// [ActiveIssue("https://github.com/dotnet/runtime/issues/21870")]: Disabling parallel execution of HttpListener tests
// until all of the hangs can be addressed
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true, MaxParallelThreads = 1)]
