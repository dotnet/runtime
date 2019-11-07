// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

// We don't want to run the tests in parallel so we don't collide store state.
// If we add the identity based constructors we could potentially
// create unique identities for every test to allow every test to have
// it's own store.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true)]
