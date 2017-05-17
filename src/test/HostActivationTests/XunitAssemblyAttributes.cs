// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

// Test cases in this assembly cannot be run in parallel.
// Some test cases modify global state and will interfere with others running at the same time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]