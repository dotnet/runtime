// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

// Process tests can conflict with each other, as they modify ambient state 
// like the console code page and environment variables
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
