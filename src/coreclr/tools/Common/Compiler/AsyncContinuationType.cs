// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    /// <summary>
    /// An async continuation type. The code generator will request this to store local state
    /// through an async suspension/resumption. We only identify these using a <see cref="GCPointerMap"/> (and owning method for R2R for LoaderAllocator purposes),
    /// since that's all the code generator cares about - size of the type, and where the GC pointers are.
    /// </summary>
    public sealed partial class AsyncContinuationType : MetadataType
    {
        private readonly MetadataType _continuationBaseType;
        public GCPointerMap PointerMap { get; }

#if READYTORUN
        // CoreCLR R2R needs to know the owning method to associate the type with the right LoaderAllocator
        public MethodDesc OwningMethod { get; }
#endif

        public override DefType[] ExplicitlyImplementedInterfaces => [];
        public override ReadOnlySpan<byte> Name => Encoding.UTF8.GetBytes(DiagnosticName);
        public override ReadOnlySpan<byte> Namespace => [];

        // We don't lay these out using MetadataType metadata.
        // Autolayout (which we'd get due to GC pointers) would likely not match what codegen expects.
        public override bool IsExplicitLayout => true;
        public override bool IsSequentialLayout => false;
        public override bool IsExtendedLayout => throw new NotImplementedException();
        public override bool IsAutoLayout => false;
        public override ClassLayoutMetadata GetClassLayout() => throw new NotImplementedException();

        public override bool IsBeforeFieldInit => false;
        public override ModuleDesc Module => _continuationBaseType.Module;
        public override MetadataType BaseType => _continuationBaseType;
        public override bool IsSealed => true;
        public override bool IsAbstract => false;
        public override MetadataType ContainingType => null;
        public override PInvokeStringFormat PInvokeStringFormat => default;
        public override string DiagnosticName => $"ContinuationType_{PointerMap}";
        public override string DiagnosticNamespace => "";
        protected override int ClassCode => 0x528741a;
        public override TypeSystemContext Context => _continuationBaseType.Context;

#if READYTORUN
        public AsyncContinuationType(MetadataType continuationBaseType, GCPointerMap pointerMap, MethodDesc owningMethod)
            => (_continuationBaseType, PointerMap, OwningMethod) = (continuationBaseType, pointerMap, owningMethod);
#else
        public AsyncContinuationType(MetadataType continuationBaseType, GCPointerMap pointerMap)
            => (_continuationBaseType, PointerMap) = (continuationBaseType, pointerMap);
#endif

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName) => false;
        public override IEnumerable<MetadataType> GetNestedTypes() => [];
        public override MetadataType GetNestedType(ReadOnlySpan<byte> name) => null;
        protected override MethodImplRecord[] ComputeVirtualMethodImplsForType() => [];
        public override MethodImplRecord[] FindMethodsImplWithMatchingDeclName(ReadOnlySpan<byte> name) => [];

        protected override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
        {
            Debug.Assert(_continuationBaseType == ((AsyncContinuationType)other)._continuationBaseType);
            GCPointerMap otherPointerMap = ((AsyncContinuationType)other).PointerMap;
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
    }

    internal sealed class AsyncContinuationLayoutAlgorithm : FieldLayoutAlgorithm
    {
        public override bool ComputeContainsByRefs(DefType type)
        {
            ValidateType(type);
            return false;
        }

        public override bool ComputeContainsGCPointers(DefType type)
        {
            AsyncContinuationType act = (AsyncContinuationType)type;
            foreach (var bit in act.PointerMap)
            {
                if (bit)
                {
                    return true;
                }
            }

            return false;
        }

        public override ComputedInstanceFieldLayout ComputeInstanceLayout(DefType type, InstanceLayoutKind layoutKind)
        {
            AsyncContinuationType act = (AsyncContinuationType)type;
            return new ComputedInstanceFieldLayout()
            {
                IsAutoLayoutOrHasAutoLayoutFields = false,
                IsInt128OrHasInt128Fields = false,
                IsVectorTOrHasVectorTFields = false,
                LayoutAbiStable = true,
                FieldSize = new LayoutInt(act.PointerMap.Size * act.Context.Target.PointerSize),
                FieldAlignment = new LayoutInt(act.Context.Target.PointerSize),
                ByteCountAlignment = new LayoutInt(act.Context.Target.PointerSize),
                ByteCountUnaligned = new LayoutInt(act.PointerMap.Size * act.Context.Target.PointerSize),
                Offsets = [],
            };
        }

        public override bool ComputeIsUnsafeValueType(DefType type)
        {
            ValidateType(type);
            return false;
        }

        private static void ValidateType(DefType type)
        {
            if (type is not AsyncContinuationType)
                throw new InvalidOperationException();
        }

        public override ComputedStaticFieldLayout ComputeStaticFieldLayout(DefType type, StaticLayoutKind layoutKind)
        {
            ValidateType(type);
            return default;
        }

        public override ValueTypeShapeCharacteristics ComputeValueTypeShapeCharacteristics(DefType type)
        {
            ValidateType(type);
            return default;
        }
    }
}
