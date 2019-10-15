// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

// Some tests launch processes.  If other tests that run concurrently create inheritable pipes and the launched
// process inherits it, then the lifetime of those pipes will be extended beyond when the test expects, leading
// to failures due to naming conflicts and the like.  On Unix this can happen even for non-inheritable pipes, as they're
// made non-inheritable via CLOEXEC, but there's still a small window between the process being forked and that
// forked process calling exec where the handle remains open in the other process.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true, MaxParallelThreads = 1)]
