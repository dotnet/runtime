// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// An async continuation type. The code generator will request this to store local state
    /// through an async suspension/resumption. We only identify these using a <see cref="GCPointerMap"/>
    /// since that's all the code generator cares about - size of the type, and where the GC pointers are.
    /// </summary>
    public sealed partial class AsyncContinuationType : MetadataType
    {
        private readonly MetadataType _continuationBaseType;
        public GCPointerMap PointerMap { get; }

        public override DefType[] ExplicitlyImplementedInterfaces => [];
        public override ReadOnlySpan<byte> Name => Encoding.UTF8.GetBytes(DiagnosticName);
        public override ReadOnlySpan<byte> Namespace => [];

        // We don't lay these out using MetadataType metadata.
        // Autolayout (which we'd get due to GC pointers) would likely not match what codegen expects.
        public override bool IsExplicitLayout => throw new NotImplementedException();
        public override bool IsSequentialLayout => throw new NotImplementedException();
        public override bool IsExtendedLayout => throw new NotImplementedException();
        public override bool IsAutoLayout => throw new NotImplementedException();
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

        public AsyncContinuationType(MetadataType continuationBaseType, GCPointerMap pointerMap)
            => (_continuationBaseType, PointerMap) = (continuationBaseType, pointerMap);

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
}
