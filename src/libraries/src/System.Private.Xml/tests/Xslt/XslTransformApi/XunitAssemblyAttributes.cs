// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

// For testing purposes, we keep the output of the Transform in a file.
// Since the content of the file ends up affecting the result of each test,
// we want to avoid parallelism so that one test doesn't affect another.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true, MaxParallelThreads = 1)]
