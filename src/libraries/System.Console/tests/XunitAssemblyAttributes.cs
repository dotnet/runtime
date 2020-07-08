// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

// Console tests can conflict with each other due to accessing the reading and writing to the console at the same time.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
