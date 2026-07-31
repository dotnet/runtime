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

    public closed class ReferencedClosedShape
    {
    }

    public sealed class ReferencedClosedCircle : ReferencedClosedShape
    {
    }

    public sealed class ReferencedClosedSquare : ReferencedClosedShape
    {
    }

    [JsonSerializable(typeof(MyPoco))]
    public partial class NETStandardSerializerContext : JsonSerializerContext
    {
    }
}

namespace System.Runtime.CompilerServices
{
    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute : System.Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName)
        {
            FeatureName = featureName;
        }

        public string FeatureName { get; }

        public bool IsOptional { get; set; }
    }

    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class IsClosedTypeAttribute : System.Attribute
    {
        public System.Type[] DerivedTypes { get; set; }
    }
}
