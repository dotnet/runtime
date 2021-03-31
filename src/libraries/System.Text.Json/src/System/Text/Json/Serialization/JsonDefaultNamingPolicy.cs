// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json
{
    internal sealed class JsonDefaultNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name) => name;
    }
}
