// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization.Tests;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    public partial class RequiredKeywordTests_SourceGen : RequiredKeywordTests
    {
        public RequiredKeywordTests_SourceGen()
            : base(new StringSerializerWrapper(RequiredKeywordTestsContext.Default))
        {
        }

        [JsonSerializable(typeof(InheritedPersonWithRequiredMembers))]
        [JsonSerializable(typeof(InheritedPersonWithRequiredMembersWithAdditionalRequiredMembers))]
        [JsonSerializable(typeof(InheritedPersonWithRequiredMembersSetsRequiredMembers))]
        [JsonSerializable(typeof(PersonWithRequiredMembers))]
        [JsonSerializable(typeof(PersonWithRequiredMembersAndSmallParametrizedCtor))]
        [JsonSerializable(typeof(PersonWithRequiredMembersAndLargeParametrizedCtor))]
        [JsonSerializable(typeof(PersonWithRequiredMembersAndSetsRequiredMembers))]
        [JsonSerializable(typeof(PersonWithRequiredMembersAndSmallParametrizedCtorAndSetsRequiredMembers))]
        [JsonSerializable(typeof(PersonWithRequiredMembersAndLargeParametrizedCtorAndSetsRequiredMembers))]
        [JsonSerializable(typeof(ClassWithInitOnlyRequiredProperty))]
        [JsonSerializable(typeof(ClassWithRequiredField))]
        [JsonSerializable(typeof(ClassWithRequiredExtensionDataProperty))]
        [JsonSerializable(typeof(ClassWithRequiredKeywordAndJsonRequiredCustomAttribute))]
        [JsonSerializable(typeof(ClassWithCustomRequiredPropertyName))]
        [JsonSerializable(typeof(DerivedClassWithRequiredInitOnlyProperty))]
        internal sealed partial class RequiredKeywordTestsContext : JsonSerializerContext
        {
        }
    }
}
