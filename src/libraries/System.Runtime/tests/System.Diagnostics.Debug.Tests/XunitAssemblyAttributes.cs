// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

// Debug tests can conflict with each other since they all share the same output logger (due to the design of Debug).
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
