// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

// Some tests launch processes.  If other tests that run concurrently create inheritable maps and the launched
// process inherits it, then the lifetime of those maps will be extended beyond when the test expects, leading
// to failures due to naming conflicts and the like.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true, MaxParallelThreads = 1)]
