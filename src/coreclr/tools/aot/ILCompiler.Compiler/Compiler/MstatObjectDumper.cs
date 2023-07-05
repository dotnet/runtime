// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

using Internal.TypeSystem;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;
using AssemblyName = System.Reflection.AssemblyName;

namespace ILCompiler
{
    public class MstatObjectDumper : ObjectDumper
    {
        private const int VersionMajor = 2;
        private const int VersionMinor = 0;

        private readonly string _fileName;
        private readonly MstatEmitter _emitter;

        private readonly InstructionEncoder _types = new InstructionEncoder(new BlobBuilder());

        private readonly BlobBuilder _mangledNames = new BlobBuilder();

        private List<(MethodDesc Method, string MangledName, int Size, int GcInfoSize)> _methods = new();
        private Dictionary<MethodDesc, int> _methodEhInfo = new();
        private Dictionary<string, int> _blobs = new();

        public MstatObjectDumper(string fileName, TypeSystemContext context)
        {
            _fileName = fileName;
            var asmName = new AssemblyName(Path.GetFileName(fileName));
            asmName.Version = new Version(VersionMajor, VersionMinor);
            _emitter = new MstatEmitter(asmName, context);
            _emitter.AllowUseOfAddGlobalMethod();
        }

        internal override void Begin()
        {
        }

        protected override void DumpObjectNode(NodeFactory factory, ObjectNode node, ObjectData objectData)
        {
            switch (node)
            {
                case EETypeNode eeType:
                    _types.OpCode(ILOpCode.Ldtoken);
                    _types.Token(_emitter.EmitMetadataHandleForTypeSystemEntity(eeType.Type));
                    _types.LoadConstantI4(objectData.Data.Length);
                    _types.LoadConstantI4(AppendMangledName(DependencyNodeCore<NodeFactory>.GetNodeName(node, factory)));
                    break;
                case IMethodBodyNode methodBody:
                    var codeInfo = (INodeWithCodeInfo)node;
                    _methods.Add((
                        methodBody.Method,
                        DependencyNodeCore<NodeFactory>.GetNodeName(node, factory),
                        objectData.Data.Length,
                        codeInfo.GCInfo.Length));
                    break;
                case MethodExceptionHandlingInfoNode ehInfoNode:
                    _methodEhInfo.Add(ehInfoNode.Method, objectData.Data.Length);
                    break;
                default:
                    string nodeName = GetObjectNodeName(node);
                    if (!_blobs.TryGetValue(nodeName, out int size))
                        size = 0;
                    _blobs[nodeName] = size + objectData.Data.Length;
                    break;
            }
        }

        private int AppendMangledName(string mangledName)
        {
            int index = _mangledNames.Count;
            _mangledNames.WriteSerializedString(mangledName);
            return index;
        }

        internal override void End()
        {
            var methods = new InstructionEncoder(new BlobBuilder());
            foreach (var m in _methods)
            {
                methods.OpCode(ILOpCode.Ldtoken);
                methods.Token(_emitter.EmitMetadataHandleForTypeSystemEntity(m.Method));
                methods.LoadConstantI4(m.Size);
                methods.LoadConstantI4(m.GcInfoSize);
                methods.LoadConstantI4(_methodEhInfo.GetValueOrDefault(m.Method));
                methods.LoadConstantI4(AppendMangledName(m.MangledName));
            }

            var blobs = new InstructionEncoder(new BlobBuilder());
            foreach (var b in _blobs)
            {
                blobs.LoadString(_emitter.GetUserStringHandle(b.Key));
                blobs.LoadConstantI4(b.Value);
            }

            _emitter.AddGlobalMethod("Methods", methods, 0);
            _emitter.AddGlobalMethod("Types", _types, 0);
            _emitter.AddGlobalMethod("Blobs", blobs, 0);

            _emitter.AddPESection(".names", _mangledNames);

            using (var fs = File.Create(_fileName))
                _emitter.SerializeToStream(fs);
        }

        private sealed class MstatEmitter : TypeSystemMetadataEmitter
        {
            private readonly List<(string Name, BlobBuilder Content)> _customSections = new();

            public MstatEmitter(AssemblyName assemblyName, TypeSystemContext context, AssemblyFlags flags = default(AssemblyFlags), byte[] publicKeyArray = null)
                : base(assemblyName, context, flags, publicKeyArray)
            {
            }

            public void AddPESection(string name, BlobBuilder content)
            {
                _customSections.Add((name, content));
            }

            protected override ManagedPEBuilder CreateManagedPEBuilder(BlobBuilder ilBuilder)
            {
                return new MstatPEBuilder(this, ilBuilder);
            }

            private sealed class MstatPEBuilder : ManagedPEBuilder
            {
                private readonly MstatEmitter _emitter;

                public MstatPEBuilder(
                    MstatEmitter emitter,
                    BlobBuilder ilStream)
                    : base(PEHeaderBuilder.CreateLibraryHeader(), new MetadataRootBuilder(emitter.Builder), ilStream, deterministicIdProvider: content => s_contentId)
                {
                    _emitter = emitter;
                }

                protected override ImmutableArray<Section> CreateSections()
                {
                    ImmutableArray<Section> result = base.CreateSections();
                    return result.AddRange(_emitter._customSections.Select(s => new Section(s.Name, SectionCharacteristics.MemRead)));
                }

                protected override BlobBuilder SerializeSection(string name, SectionLocation location)
                {
                    foreach (var section in _emitter._customSections)
                    {
                        if (section.Name == name)
                            return section.Content;
                    }

                    return base.SerializeSection(name, location);
                }
            }
        }
    }
}
