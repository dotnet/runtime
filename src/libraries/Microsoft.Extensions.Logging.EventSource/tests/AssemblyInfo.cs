// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

// There can only be one EventSource per AppDomain, and when an event is raised through that EventSource,
// all existing listeners that enabled that EventSource will receive the event.
// This makes running EventSourceLogger tests in parallel difficult. We mark this assembly
// with CollectionBehavior.CollectionPerAssembly to ensure that all tests in this assembly are executed serially.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
