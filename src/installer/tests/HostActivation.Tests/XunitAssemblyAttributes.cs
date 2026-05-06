// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

// Test cases in this assembly cannot be run in parallel.
// Some test cases modify global state and will interfere with others running at the same time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
