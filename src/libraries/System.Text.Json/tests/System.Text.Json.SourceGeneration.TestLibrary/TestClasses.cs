// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace System.Text.Json.SourceGeneration.Tests.NETStandard
{
    public class MyPoco
    {
        public string Value { get; set; }
    }

    public class ClassFromOtherAssemblyWithNonPublicMembers
    {
        public int PublicValue { get; set; } = 1;
        internal int InternalValue { get; set; } = 2;
        private int PrivateValue { get; set; } = 4;
        protected int ProtectedValue { get; set; } = 8;
        private protected int PrivateProtectedValue { get; set; } = 16;
        internal protected int InternalProtectedValue { get; set; } = 32;
    }

    [JsonSerializable(typeof(MyPoco))]
    public partial class NETStandardSerializerContext : JsonSerializerContext
    {
    }
}
