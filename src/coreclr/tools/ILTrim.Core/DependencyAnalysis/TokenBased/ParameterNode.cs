// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;

using Internal.TypeSystem.Ecma;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents an method parameter.
    /// </summary>
    public sealed class ParameterNode : TokenBasedNode
    {
        public ParameterNode(EcmaModule module, ParameterHandle handle)
            : base(module, handle)
        {
        }

        private ParameterHandle Handle => (ParameterHandle)_handle;

        public override void AddStaticDependencies(DependencySink<NodeFactory> sink, NodeFactory context)
        {
            DependencySink<NodeFactory> dependencies = sink;

            Parameter parameter = _module.MetadataReader.GetParameter(Handle);
            CustomAttributeNode.AddDependenciesDueToCustomAttributes(dependencies, context, _module, parameter.GetCustomAttributes());

        }

        public override string ToString()
        {
            MetadataReader reader = _module.MetadataReader;
            return reader.GetString(reader.GetParameter(Handle).Name);
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;
            Parameter parameter = reader.GetParameter(Handle);

            var builder = writeContext.MetadataBuilder;
            return builder.AddParameter(
                parameter.Attributes,
                builder.GetOrAddString(reader.GetString(parameter.Name)),
                parameter.SequenceNumber);
        }
    }
}
