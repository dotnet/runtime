// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

// Console tests can conflict with each other due to accessing the reading and writing to the console at the same time.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
