// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.NET.Build.Tasks;

internal abstract class InvocationPathMap
{
    // Mappings must remain stable and round-trip every declared path for the invocation's lifetime.
    internal abstract string Map(string physicalPath);
    internal abstract string Resolve(string identity);
}
