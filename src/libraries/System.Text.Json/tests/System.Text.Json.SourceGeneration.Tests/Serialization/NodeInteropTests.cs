// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;
using System.Text.Json.Serialization.Tests.Schemas.OrderPayload;

namespace System.Text.Json.SourceGeneration.Tests
{
    public sealed partial class NodeInteropTests_Metadata : NodeInteropTests
    {
        public NodeInteropTests_Metadata()
            : base(new StringSerializerWrapper(NodeInteropTestsContext_Metadata.Default, (options) => new NodeInteropTestsContext_Metadata(options)))
        {
        }

        [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
        [JsonSerializable(typeof(List<Order>))]
        [JsonSerializable(typeof(JsonArray))]
        [JsonSerializable(typeof(Poco))]
        [JsonSerializable(typeof(string))]
        internal sealed partial class NodeInteropTestsContext_Metadata : JsonSerializerContext
        {
        }
    }

    public sealed partial class NodeInteropTests_Default : NodeInteropTests
    {
        public NodeInteropTests_Default()
            : base(new StringSerializerWrapper(NodeInteropTestsContext_Default.Default, (options) => new NodeInteropTestsContext_Default(options)))
        {
        }

        [JsonSerializable(typeof(List<Order>))]
        [JsonSerializable(typeof(JsonArray))]
        [JsonSerializable(typeof(Poco))]
        [JsonSerializable(typeof(string))]
        internal sealed partial class NodeInteropTestsContext_Default : JsonSerializerContext
        {
        }
    }
}
