// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Contains all GC static fields for a particular MethodTable.
    /// Fields that have preinitialized data are pointer reloc pointing to frozen objects.
    /// Other fields are initialized with 0.
    /// We simply memcpy these over the GC static MethodTable object.
    /// </summary>
    public class GCStaticsPreInitDataNode : ObjectNode, ISymbolDefinitionNode
    {
        private TypePreinit.PreinitializationInfo _preinitializationInfo;

        public GCStaticsPreInitDataNode(TypePreinit.PreinitializationInfo preinitializationInfo)
        {
            Debug.Assert(!preinitializationInfo.Type.IsCanonicalSubtype(CanonicalFormKind.Specific));
            _preinitializationInfo = preinitializationInfo;
        }

        protected override string GetName(NodeFactory factory) => GetMangledName(_preinitializationInfo.Type, factory.NameMangler);

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(GetMangledName(_preinitializationInfo.Type, nameMangler));
        }

        public int Offset => 0;
        public MetadataType Type => _preinitializationInfo.Type;

        public static string GetMangledName(TypeDesc type, NameMangler nameMangler)
        {
            return nameMangler.NodeMangler.GCStatics(type) + "__PreInitData";
        }

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectNodeSection Section
        {
            get
            {
                if (Type.Context.Target.IsWindows)
                    return ObjectNodeSection.ReadOnlyDataSection;
                else
                    return ObjectNodeSection.DataSection;
            }
        }
        public override bool IsShareable => EETypeNode.IsTypeNodeShareable(_preinitializationInfo.Type);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);

            MetadataType type = _preinitializationInfo.Type;

            builder.RequireInitialAlignment(factory.Target.PointerSize);

            // GC static fields don't begin at offset 0, need to subtract that.
            int initialOffset = CompilerMetadataFieldLayoutAlgorithm.GetGCStaticFieldOffset(factory.TypeSystemContext).AsInt;

            foreach (FieldDesc field in type.GetFields())
            {
                if (!field.IsStatic || field.HasRva || field.IsLiteral || field.IsThreadStatic || !field.HasGCStaticBase)
                    continue;
                
                int padding = field.Offset.AsInt - initialOffset - builder.CountBytes;
                Debug.Assert(padding >= 0);
                builder.EmitZeros(padding);

                TypePreinit.ISerializableValue val = _preinitializationInfo.GetFieldValue(field);
                int currentOffset = builder.CountBytes;
                if (val != null)
                    val.WriteFieldData(ref builder, field, factory);
                else
                    builder.EmitZeroPointer();
                Debug.Assert(builder.CountBytes - currentOffset == field.FieldType.GetElementSize().AsInt);
            }

            int pad = _preinitializationInfo.Type.GCStaticFieldSize.AsInt - builder.CountBytes - initialOffset;
            Debug.Assert(pad >= 0);
            builder.EmitZeros(pad);

            builder.AddSymbol(this);

            return builder.ToObjectData();
        }

        public override int ClassCode => 1148300665;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_preinitializationInfo.Type, ((GCStaticsPreInitDataNode)other)._preinitializationInfo.Type);
        }
    }
}
