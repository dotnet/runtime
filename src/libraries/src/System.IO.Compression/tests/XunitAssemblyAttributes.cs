// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

// For testing purposes, we force compression/decompression to use a particular mode, via a static field.
// Since that static field then ends up affecting how all instances do compression/decompression, we want
// to avoid parallelism so that one test doesn't affect another.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true, MaxParallelThreads = 1)]

