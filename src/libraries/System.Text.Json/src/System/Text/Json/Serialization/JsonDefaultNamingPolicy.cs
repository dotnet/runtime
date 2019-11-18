// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json
{
    internal class JsonDefaultNamingPolicy : JsonNamingPolicy
    {
        [return: NotNullIfNotNull("name")]
        public override string? ConvertName(string? name) => name;
    }
}
