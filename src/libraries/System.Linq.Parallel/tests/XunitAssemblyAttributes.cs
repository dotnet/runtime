// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

// Tests could run slower if PLINQ itself is run in parallel, especially as concurrent
// queries compete for thread pool resources.  Further, the ETW tests must not be run
// concurrently with any other tests.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true, MaxParallelThreads = 1)]
