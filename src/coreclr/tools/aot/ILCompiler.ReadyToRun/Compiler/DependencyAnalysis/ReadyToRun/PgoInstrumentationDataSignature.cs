// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.JitInterface;
using Internal.ReadyToRunConstants;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public sealed class PgoInstrumentationDataSignature : Signature
    {
        private readonly MethodWithToken _method;
        private readonly PgoInstrumentationSchemaTableNode _table;
        private readonly PgoInstrumentationSchemaTableNode.Entry _schema;

        public PgoInstrumentationDataSignature(MethodWithToken method,
            PgoInstrumentationSchemaTableNode table, PgoInstrumentationSchemaTableNode.Entry schema)
        {
            _method = method;
            _table = table;
            _schema = schema;
            CompilerTypeSystemContext context = (CompilerTypeSystemContext)method.Method.Context;
            context.EnsureLoadableMethod(method.Method);
            context.EnsureLoadableType(method.OwningType);
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, Array.Empty<ISymbolDefinitionNode>());

            ObjectDataSignatureBuilder builder = new(factory, relocsOnly);
            builder.AddSymbol(this);
            SignatureContext context = builder.EmitFixup(factory, ReadyToRunFixupKind.PgoInstrumentationData,
                _method.Token.Module, factory.SignatureContext);
            builder.EmitMethodSignature(_method, enforceDefEncoding: false, enforceOwningType: false,
                context, isInstantiatingStub: false);
            builder.EmitUInt((uint)_table.GetIndex(_schema));
            return builder.ToObjectData();
        }

        public override int ClassCode => 150063500;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            PgoInstrumentationDataSignature signature = (PgoInstrumentationDataSignature)other;
            int result = _method.CompareTo(signature._method, comparer);
            return result != 0 ? result : StringComparer.Ordinal.Compare(_schema.Key, signature._schema.Key);
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("PgoInstrumentationData("u8);
            _method.AppendMangledName(nameMangler, sb);
            sb.Append(": "u8);
            sb.Append(_schema.Name);
            sb.Append(")"u8);
        }
    }
}
