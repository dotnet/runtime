// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

// There can only be one EventSource per AppDomain, and when an event is raised through that EventSource,
// all existing listeners that enabled that EventSource will receive the event.
// This makes running EventSourceLogger tests in parallel difficult. We mark this assembly
// with CollectionBehavior.CollectionPerAssembly to ensure that all tests in this assembly are executed serially.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
