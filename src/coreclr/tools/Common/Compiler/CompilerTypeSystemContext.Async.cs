// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public partial class CompilerTypeSystemContext
    {
        public MethodDesc GetAsyncVariantMethod(MethodDesc taskReturningMethod)
        {
            Debug.Assert(taskReturningMethod.Signature.ReturnsTaskOrValueTask());
            MethodDesc asyncMetadataMethodDef = taskReturningMethod.GetTypicalMethodDefinition();
            MethodDesc result = _asyncVariantImplHashtable.GetOrCreateValue((EcmaMethod)asyncMetadataMethodDef);

            if (asyncMetadataMethodDef != taskReturningMethod)
            {
                TypeDesc owningType = taskReturningMethod.OwningType;
                if (owningType.HasInstantiation)
                    result = GetMethodForInstantiatedType(result, (InstantiatedType)owningType);

                if (taskReturningMethod.HasInstantiation)
                    result = GetInstantiatedMethod(result, taskReturningMethod.Instantiation);
            }

            return result;
        }

        private sealed class AsyncVariantImplHashtable : LockFreeReaderHashtable<EcmaMethod, AsyncMethodVariant>
        {
            protected override int GetKeyHashCode(EcmaMethod key) => key.GetHashCode();
            protected override int GetValueHashCode(AsyncMethodVariant value) => value.Target.GetHashCode();
            protected override bool CompareKeyToValue(EcmaMethod key, AsyncMethodVariant value) => key == value.Target;
            protected override bool CompareValueToValue(AsyncMethodVariant value1, AsyncMethodVariant value2)
                => value1.Target == value2.Target;
            protected override AsyncMethodVariant CreateValueFromKey(EcmaMethod key) => new AsyncMethodVariant(key);
        }
        private AsyncVariantImplHashtable _asyncVariantImplHashtable = new AsyncVariantImplHashtable();

        public MetadataType GetContinuationType(GCPointerMap pointerMap)
        {
            return _continuationTypeHashtable.GetOrCreateValue(pointerMap);
        }

        private sealed class ContinuationTypeHashtable : LockFreeReaderHashtable<GCPointerMap, ContinuationType>
        {
            private readonly CompilerTypeSystemContext _parent;
            private MetadataType _continuationType;

            public ContinuationTypeHashtable(CompilerTypeSystemContext parent)
                => _parent = parent;

            protected override int GetKeyHashCode(GCPointerMap key) => key.GetHashCode();
            protected override int GetValueHashCode(ContinuationType value) => value.PointerMap.GetHashCode();
            protected override bool CompareKeyToValue(GCPointerMap key, ContinuationType value) => key.Equals(value.PointerMap);
            protected override bool CompareValueToValue(ContinuationType value1, ContinuationType value2)
                => value1.PointerMap.Equals(value2.PointerMap);
            protected override ContinuationType CreateValueFromKey(GCPointerMap key)
            {
                if (_continuationType == null)
                    _continuationType = _parent.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "Continuation"u8);
                return new ContinuationType(_continuationType, key);
            }
        }
        private ContinuationTypeHashtable _continuationTypeHashtable;

        /// <summary>
        /// An async continuation type. The code generator will request this to store local state
        /// through an async suspension/resumption. We only identify these using a <see cref="GCPointerMap"/>
        /// since that's all the code generator cares about - size of the type, and where the GC pointers are.
        /// </summary>
        private sealed class ContinuationType : MetadataType
        {
            private readonly MetadataType _continuationType;
            private FieldDesc[] _fields;
            public GCPointerMap PointerMap { get; }

            public override DefType[] ExplicitlyImplementedInterfaces => [];
            public override ReadOnlySpan<byte> Name => Encoding.UTF8.GetBytes(DiagnosticName);
            public override ReadOnlySpan<byte> Namespace => [];

            // The layout of the type is "sequential-in-spirit", but since there are GC pointers,
            // the standard layout algorithm wouldn't respect that. We have a custom layout algorithm.
            // The following layout-related properties are meaningless.
            public override bool IsExplicitLayout => false;
            public override bool IsSequentialLayout => false;
            public override bool IsExtendedLayout => false;
            public override bool IsAutoLayout => false;
            public override ClassLayoutMetadata GetClassLayout() => default;

            public override bool IsBeforeFieldInit => false;
            public override ModuleDesc Module => _continuationType.Module;
            public override MetadataType BaseType => _continuationType;
            public override bool IsSealed => true;
            public override bool IsAbstract => false;
            public override MetadataType ContainingType => null;
            public override PInvokeStringFormat PInvokeStringFormat => default;
            public override string DiagnosticName => $"ContinuationType_{PointerMap}";
            public override string DiagnosticNamespace => "";
            protected override int ClassCode => 0x528741a;
            public override TypeSystemContext Context => _continuationType.Context;

            public ContinuationType(MetadataType continuationType, GCPointerMap pointerMap)
                => (_continuationType, PointerMap) = (continuationType, pointerMap);

            public override bool HasCustomAttribute(string attributeNamespace, string attributeName) => false;
            public override IEnumerable<MetadataType> GetNestedTypes() => [];
            public override MetadataType GetNestedType(string name) => null;
            protected override MethodImplRecord[] ComputeVirtualMethodImplsForType() => [];
            public override MethodImplRecord[] FindMethodsImplWithMatchingDeclName(ReadOnlySpan<byte> name) => [];

            protected override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
            {
                GCPointerMap otherPointerMap = ((ContinuationType)other).PointerMap;
                return PointerMap.CompareTo(otherPointerMap);
            }

            public override int GetHashCode() => PointerMap.GetHashCode();

            protected override TypeFlags ComputeTypeFlags(TypeFlags mask)
            {
                TypeFlags flags = 0;

                if ((mask & TypeFlags.HasGenericVarianceComputed) != 0)
                {
                    flags |= TypeFlags.HasGenericVarianceComputed;
                }

                if ((mask & TypeFlags.CategoryMask) != 0)
                {
                    flags |= TypeFlags.Class;
                }

                flags |= TypeFlags.HasFinalizerComputed;
                flags |= TypeFlags.AttributeCacheComputed;

                return flags;
            }

            private void InitializeFields()
            {
                FieldDesc[] fields = new FieldDesc[PointerMap.Size];

                for (int i = 0; i < PointerMap.Size; i++)
                    fields[i] = new ContinuationField(this, i);

                Interlocked.CompareExchange(ref _fields, fields, null);
            }
            public override IEnumerable<FieldDesc> GetFields()
            {
                if (_fields == null)
                {
                    InitializeFields();
                }
                return _fields;
            }

            /// <summary>
            /// A field on a continuation type. The type of the field is determined by consulting the
            /// associated GC pointer map and it's either `object` or `nint`.
            /// </summary>
            private sealed class ContinuationField : FieldDesc
            {
                private readonly ContinuationType _owningType;
                private readonly int _index;

                public ContinuationField(ContinuationType owningType, int index)
                    => (_owningType, _index) = (owningType, index);

                public override MetadataType OwningType => _owningType;
                public override TypeDesc FieldType => Context.GetWellKnownType(_owningType.PointerMap[_index] ? WellKnownType.Object : WellKnownType.IntPtr);
                public override bool HasEmbeddedSignatureData => false;
                public override bool IsStatic => false;
                public override bool IsInitOnly => false;
                public override bool IsThreadStatic => false;
                public override bool HasRva => false;
                public override bool IsLiteral => false;
                public override TypeSystemContext Context => _owningType.Context;
                public override EmbeddedSignatureData[] GetEmbeddedSignatureData() => null;
                public override bool HasCustomAttribute(string attributeNamespace, string attributeName) => false;

                protected override int ClassCode => 0xc761a66;
                protected override int CompareToImpl(FieldDesc other, TypeSystemComparer comparer)
                {
                    var otherField = (ContinuationField)other;
                    int result = _index.CompareTo(otherField._index);
                    if (result != 0)
                        return result;

                    return comparer.Compare(_owningType, otherField._owningType);
                }
            }
        }

        /// <summary>
        /// Layout algorithm that lays out continuation types. It ensures the type has the layout
        /// that the code generator requested (the GC pointers are where we need them).
        /// </summary>
        private sealed class ContinuationTypeFieldLayoutAlgorithm : FieldLayoutAlgorithm
        {
            public override bool ComputeContainsGCPointers(DefType type)
            {
                // ContainsGCPointers because the base already has some.
                Debug.Assert(type.BaseType.ContainsGCPointers);
                return true;
            }

            public override ComputedInstanceFieldLayout ComputeInstanceLayout(DefType type, InstanceLayoutKind layoutKind)
            {
                var continuationType = (ContinuationType)type;
                var continuationBaseType = (EcmaType)continuationType.BaseType;
                Debug.Assert(continuationBaseType.Name.SequenceEqual("Continuation"u8));

                LayoutInt pointerSize = continuationType.Context.Target.LayoutPointerSize;

                LayoutInt dataOffset = continuationBaseType.InstanceByteCountUnaligned;

#if DEBUG
                // Validate we match the expected DataOffset value.
                // DataOffset is a const int32 field in the Continuation class.
                EcmaField dataOffsetField = continuationBaseType.GetField("DataOffset"u8);
                Debug.Assert(dataOffsetField.IsLiteral);

                var reader = dataOffsetField.MetadataReader;
                var constant = reader.GetConstant(reader.GetFieldDefinition(dataOffsetField.Handle).GetDefaultValue());
                Debug.Assert(constant.TypeCode == System.Reflection.Metadata.ConstantTypeCode.Int32);
                int expectedDataOffset = reader.GetBlobReader(constant.Value).ReadInt32()
                    + pointerSize.AsInt /* + MethodTable field */;
                Debug.Assert(dataOffset.AsInt == expectedDataOffset);
#endif
                FieldAndOffset[] offsets = new FieldAndOffset[continuationType.PointerMap.Size];
                int i = 0;

                foreach (FieldDesc field in type.GetFields())
                {
                    Debug.Assert(field.FieldType.GetElementSize() == pointerSize);
                    offsets[i++] = new FieldAndOffset(field, dataOffset);
                    dataOffset += pointerSize;
                }

                return new ComputedInstanceFieldLayout
                {
                    FieldSize = pointerSize,
                    FieldAlignment = pointerSize,
                    ByteCountAlignment = pointerSize,
                    ByteCountUnaligned = dataOffset,
                    Offsets = offsets,
                    IsAutoLayoutOrHasAutoLayoutFields = false,
                    IsInt128OrHasInt128Fields = false,
                    IsVectorTOrHasVectorTFields = false,
                    LayoutAbiStable = false,
                };
            }

            public override bool ComputeContainsByRefs(DefType type) => throw new NotImplementedException();
            public override bool ComputeIsUnsafeValueType(DefType type) => throw new NotImplementedException();
            public override ComputedStaticFieldLayout ComputeStaticFieldLayout(DefType type, StaticLayoutKind layoutKind) => default;
            public override ValueTypeShapeCharacteristics ComputeValueTypeShapeCharacteristics(DefType type) => throw new NotImplementedException();
        }
    }
}
