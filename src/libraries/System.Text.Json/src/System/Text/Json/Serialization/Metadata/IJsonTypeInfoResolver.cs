// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Metadata
{
    internal interface IJsonTypeInfoResolver
    {
        JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options);
    }
}
