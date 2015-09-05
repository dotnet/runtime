// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

// Process tests can conflict with each other, as they modify ambient state 
// like the console code page and environment variables
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
