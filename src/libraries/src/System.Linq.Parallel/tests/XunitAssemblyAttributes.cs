// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

// Tests could run slower if PLINQ itself is run in parallel, especially as concurrent
// queries compete for thread pool resources
[assembly: CollectionBehavior(DisableTestParallelization = true)]

