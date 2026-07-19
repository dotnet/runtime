// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using ILVerify;

namespace ILVerification.Tests
{
    [JsonSourceGenerationOptions(WriteIndented = false)]
    [JsonSerializable(typeof(List<VerifierError>))]
    internal partial class JsonContext : JsonSerializerContext
    {
    }
}
