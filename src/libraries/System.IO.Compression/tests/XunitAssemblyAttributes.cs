// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

// For testing purposes, we force compression/decompression to use a particular mode, via a static field.
// Since that static field then ends up affecting how all instances do compression/decompression, we want
// to avoid parallelism so that one test doesn't affect another.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true, MaxParallelThreads = 1)]

