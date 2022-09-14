// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.Text;
using Internal.TypeSystem;

using ILCompiler.DependencyAnalysis;

using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;
using AssemblyName = System.Reflection.AssemblyName;
using System.Collections.Generic;

namespace ILCompiler
{
    public class MstatObjectDumper : ObjectDumper
    {
        private const int VersionMajor = 1;
        private const int VersionMinor = 1;

        private readonly string _fileName;
        private readonly TypeSystemMetadataEmitter _emitter;

        private readonly InstructionEncoder _types = new InstructionEncoder(new BlobBuilder());

        private Dictionary<MethodDesc, (string MangledName, int Size, int GcInfoSize)> _methods = new();
        private Dictionary<MethodDesc, int> _methodEhInfo = new();
        private Dictionary<string, int> _blobs = new();

        private Utf8StringBuilder _utf8StringBuilder = new Utf8StringBuilder();

        public MstatObjectDumper(string fileName, TypeSystemContext context)
        {
            _fileName = fileName;
            var asmName = new AssemblyName(Path.GetFileName(fileName));
            asmName.Version = new Version(VersionMajor, VersionMinor);
            _emitter = new TypeSystemMetadataEmitter(asmName, context);
            _emitter.AllowUseOfAddGlobalMethod();
        }

        internal override void Begin()
        {
        }

        protected override void DumpObjectNode(NameMangler mangler, ObjectNode node, ObjectData objectData)
        {
            string mangledName = null;
            if (node is ISymbolNode symbol)
            {
                _utf8StringBuilder.Clear();
                symbol.AppendMangledName(mangler, _utf8StringBuilder);
                mangledName = _utf8StringBuilder.ToString();
            }

            switch (node)
            {
                case EETypeNode eeType:
                    SerializeSimpleEntry(_types, eeType.Type, mangledName, objectData);
                    break;
                case IMethodBodyNode methodBody:
                    var codeInfo = (INodeWithCodeInfo)node;
                    _methods.Add(methodBody.Method, (mangledName, objectData.Data.Length, codeInfo.GCInfo.Length));
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

        private void SerializeSimpleEntry(InstructionEncoder encoder, TypeSystemEntity entity, string mangledName, ObjectData blob)
        {
            encoder.OpCode(ILOpCode.Ldtoken);
            encoder.Token(_emitter.EmitMetadataHandleForTypeSystemEntity(entity));
            // Would like to do this but mangled names are very long and go over the 16 MB string limit quickly.
            // encoder.LoadString(_emitter.GetUserStringHandle(mangledName));
            encoder.LoadConstantI4(blob.Data.Length);
        }

        internal override void End()
        {
            var methods = new InstructionEncoder(new BlobBuilder());
            foreach (var m in _methods)
            {
                methods.OpCode(ILOpCode.Ldtoken);
                methods.Token(_emitter.EmitMetadataHandleForTypeSystemEntity(m.Key));
                // Would like to do this but mangled names are very long and go over the 16 MB string limit quickly.
                // methods.LoadString(_emitter.GetUserStringHandle(m.Value.MangledName));
                methods.LoadConstantI4(m.Value.Size);
                methods.LoadConstantI4(m.Value.GcInfoSize);
                methods.LoadConstantI4(_methodEhInfo.GetValueOrDefault(m.Key));
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

            using (var fs = File.OpenWrite(_fileName))
                _emitter.SerializeToStream(fs);
        }
    }
}
