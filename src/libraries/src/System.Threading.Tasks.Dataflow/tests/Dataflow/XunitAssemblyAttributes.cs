// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

// Disabling parallelization until xunit provides a more fine-grained mechanism for allowing
// certain tests to not run in parallel with any others, while still allowing for those others
// to run in parallel with each other.  This is required for the ETW tests, which interact with
// global state and thus can't run concurrently with any other tests.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true, MaxParallelThreads = 1)]
