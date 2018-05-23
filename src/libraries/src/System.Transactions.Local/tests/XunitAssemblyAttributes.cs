// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

// Currently has a bunch of blocking in tests waiting for other operations that post back to the current
// synchronization context.  With parallelism enabled, that causes deadlocks, especially on single core
// machines but possibly on higher core counts as well.  Until all of that can be cleaned up, disable
// parallelization of the tests.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true, MaxParallelThreads = 1)]
