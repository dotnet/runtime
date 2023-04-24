// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true, MaxParallelThreads = 1)]

[assembly: SkipOnPlatform(TestPlatforms.Browser, "System.Configuration.ConfigurationManager is not supported on Browser")]
