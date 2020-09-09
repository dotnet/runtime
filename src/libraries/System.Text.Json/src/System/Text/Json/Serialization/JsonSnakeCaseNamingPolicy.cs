// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json
{
    internal class JsonSnakeCaseNamingPolicy : JsonSeparatedCaseNamingPolicy
    {
        public override string ConvertName(string name) => ToSeparatedCase(name, '_');
    }
}
