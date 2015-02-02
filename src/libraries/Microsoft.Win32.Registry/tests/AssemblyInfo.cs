// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

// Registry tests can conflict with each other due to accessing the same keys/values in the registry
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]