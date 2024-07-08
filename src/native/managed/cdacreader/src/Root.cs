// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.DataContractReader;

internal static class Root
{
    // https://github.com/dotnet/runtime/issues/101205
    public static JsonDerivedTypeAttribute[] R1 = new JsonDerivedTypeAttribute[] { null! };
}
