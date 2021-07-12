// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;

namespace System.Text.Json.SourceGeneration.Tests
{
    public partial class CollectionTests_Metadata : CollectionTests
    {
        public CollectionTests_Metadata()
            : this(new JsonSerializerWrapperForString_SourceGen(CollectionTestsContext_Metadata.Default, (options) => new CollectionTestsContext_Metadata(options)))
        {
        }

        protected CollectionTests_Metadata(Serialization.Tests.JsonSerializerWrapperForString serializerWrapper)
            : base(serializerWrapper)
        {
        }

        [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
        [JsonSerializable(typeof(ConcurrentDictionary<string, string>))]
        //[JsonSerializable(typeof())]
        //[JsonSerializable(typeof())]
        //[JsonSerializable(typeof())]
        internal sealed partial class CollectionTestsContext_Metadata : JsonSerializerContext
        {
        }
    }

    //public partial class CollectionTests_Default : CollectionTests_Metadata
    //{
    //    public CollectionTests_Default()
    //        : base(new JsonSerializerWrapperForString_SourceGen(CollectionTestsContext_Default.Default, (options) => new CollectionTestsContext_Default(options)))
    //    {
    //    }

    //    internal sealed partial class CollectionTestsContext_Default : JsonSerializerContext
    //    {
    //    }
    //}
}
