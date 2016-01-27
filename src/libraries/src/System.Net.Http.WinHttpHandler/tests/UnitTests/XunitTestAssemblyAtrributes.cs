// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using Xunit;

// The WinHttpHandler unit tests need to have parallelism turned off between test classes since they rely on
// a mock network with simulated failures controlled by singleton static classes (TestControl, TestServer).
// This attribute will put all test classes into a single collection. Default Xunit behavior is to run tests
// within a single collection in series and not in parallel.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
