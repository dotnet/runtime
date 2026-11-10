// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

// The OleDB tests use the ACE driver, which has issues with concurrency. 
[assembly: CollectionBehavior(DisableTestParallelization = true)]
