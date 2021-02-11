// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

// The unit tests need to have parallelism turned off since they rely on fake implemented with singletons.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
