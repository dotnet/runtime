// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;

namespace System.Text.Json.SourceGeneration.Tests
{
    public sealed partial class UnmappedMemberHandlingTests_Metadata_String : UnmappedMemberHandlingTests
    {
        public UnmappedMemberHandlingTests_Metadata_String()
            : base(new StringSerializerWrapper(UnmappedMemberHandlingTestsContext.Default))
        {
        }

        [JsonSerializable(typeof(PocoWithoutAnnotations))]
        [JsonSerializable(typeof(PocoWithSkipAnnotation))]
        [JsonSerializable(typeof(PocoWithDisallowAnnotation))]
        [JsonSerializable(typeof(PocoInheritingDisallowAnnotation))]
        [JsonSerializable(typeof(ClassWithExtensionData))]
        [JsonSerializable(typeof(ClassWithExtensionDataAndDisallowHandling))]
        public partial class UnmappedMemberHandlingTestsContext : JsonSerializerContext
        { }
    }

    public sealed class UnmappedMemberHandlingTests_Metadata_Async : UnmappedMemberHandlingTests
    {
        public UnmappedMemberHandlingTests_Metadata_Async()
            : base(new AsyncStreamSerializerWrapper(UnmappedMemberHandlingTests_Metadata_String.UnmappedMemberHandlingTestsContext.Default))
        {
        }
    }
}
