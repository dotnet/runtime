// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

using ILCompiler.DependencyAnalysisFramework;

using ILTrim.DependencyAnalysis;

using Debug = System.Diagnostics.Debug;

namespace ILTrim
{
    /// <summary>
    /// Responsible for writing a single module into the specified output file.
    /// </summary>
    public class ModuleWriter
    {
        private readonly NodeFactory _factory;
        private readonly EcmaModule _module;
        private readonly List<TokenBasedNode> _tokensToWrite;

        private ModuleWriter(NodeFactory factory, EcmaModule module, List<TokenBasedNode> tokensToWrite)
            => (_factory, _module, _tokensToWrite) = (factory, module, tokensToWrite);

        public void Save(Stream outputStream)
        {
            // Sort the tokens so that tokens start from the lowest source token (this is the emission order)
            _tokensToWrite.Sort();

            // Ask each of the output nodes to assign their tokens in the output.
            var tokenMapBuilder = new TokenMap.Builder(_module.MetadataReader);
            foreach (TokenBasedNode node in _tokensToWrite)
            {
                node.BuildTokens(tokenMapBuilder);
            }

            // Ask each node to write itself out.
            TokenMap tokenMap = tokenMapBuilder.ToTokenMap();
            ModuleWritingContext context = new ModuleWritingContext(_factory, tokenMap);
            foreach (TokenBasedNode node in _tokensToWrite)
            {
                node.Write(context);
            }

            // Map any other things
            MethodDefinitionHandle sourceEntryPoint = default;
            CorHeader corHeader = _module.PEReader.PEHeaders.CorHeader;
            Debug.Assert((corHeader.Flags & CorFlags.NativeEntryPoint) == 0);
            if (corHeader.EntryPointTokenOrRelativeVirtualAddress != 0)
            {
                sourceEntryPoint = (MethodDefinitionHandle)MetadataTokens.Handle(corHeader.EntryPointTokenOrRelativeVirtualAddress);
            }

            // Serialize to the output PE file
            // TODO: instead of the default header, copy flags from the source module
            var headerBuilder = PEHeaderBuilder.CreateExecutableHeader();
            var mdRootBuilder = new MetadataRootBuilder(context.MetadataBuilder);
            var peBuilder = new ManagedPEBuilder(
                headerBuilder,
                mdRootBuilder,
                context.MethodBodyEncoder.Builder,
                entryPoint: (MethodDefinitionHandle)tokenMap.MapToken(sourceEntryPoint));

            var o = new BlobBuilder();
            peBuilder.Serialize(o);

            o.WriteContentTo(outputStream);
        }

        public static ModuleWriter[] CreateWriters(NodeFactory factory, ImmutableArray<DependencyNodeCore<NodeFactory>> markedNodeList)
        {
            // Go over all marked vertices and make a list of vertices for each module
            var moduleToTokenList = new Dictionary<EcmaModule, List<TokenBasedNode>>();
            foreach (DependencyNodeCore<NodeFactory> node in markedNodeList)
            {
                if (node is not TokenBasedNode tokenNode)
                    continue;

                if (!moduleToTokenList.TryGetValue(tokenNode.Module, out List<TokenBasedNode> list))
                {
                    list = new List<TokenBasedNode>();
                    moduleToTokenList.Add(tokenNode.Module, list);
                }

                list.Add(tokenNode);
            }

            // Create a ModuleWriter for each of the output modules.
            ModuleWriter[] result = new ModuleWriter[moduleToTokenList.Count];
            int i = 0;
            foreach (KeyValuePair<EcmaModule, List<TokenBasedNode>> moduleAndTokens in moduleToTokenList)
            {
                result[i++] = new ModuleWriter(factory, moduleAndTokens.Key, moduleAndTokens.Value);
            }

            return result;
        }
    }
}
