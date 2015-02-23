// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

// Debug tests can conflict with each other since they all share the same output logger (due to the design of Debug).
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
