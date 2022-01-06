// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace System.Text.Json.SourceGeneration.Tests.NETStandard
{
    public class MyPoco
    {
        public string Value { get; set; }
    }

    [JsonSerializable(typeof(MyPoco))]
    public partial class NETStandardSerializerContext : JsonSerializerContext
    {
    }
}
