// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.TypeSystem;

namespace Internal.Runtime
{
    internal static class EETypeBuilderHelpers
    {
        private static EETypeElementType ComputeEETypeElementType(TypeDesc type)
        {
            // Enums are represented as their underlying type
            type = type.UnderlyingType;

            if (type.IsWellKnownType(WellKnownType.Array))
            {
                // SystemArray is a special EETypeElementType that doesn't exist in TypeFlags
                return EETypeElementType.SystemArray;
            }
            else
            {
                // The rest of TypeFlags should be directly castable to EETypeElementType.
                // Spot check the enums match.
                Debug.Assert((int)TypeFlags.Void == (int)EETypeElementType.Void);
                Debug.Assert((int)TypeFlags.IntPtr == (int)EETypeElementType.IntPtr);
                Debug.Assert((int)TypeFlags.Single == (int)EETypeElementType.Single);
                Debug.Assert((int)TypeFlags.UInt32 == (int)EETypeElementType.UInt32);
                Debug.Assert((int)TypeFlags.Pointer == (int)EETypeElementType.Pointer);
                Debug.Assert((int)TypeFlags.Array == (int)EETypeElementType.Array);

                EETypeElementType elementType = (EETypeElementType)type.Category;

                // Would be surprising to get these here though.
                Debug.Assert(elementType != EETypeElementType.SystemArray);
                Debug.Assert(elementType <= EETypeElementType.FunctionPointer);

                return elementType;

            }
        }

        public static uint ComputeFlags(TypeDesc type)
        {
            uint flags;
            if (type.IsParameterizedType)
                flags = (uint)EETypeKind.ParameterizedEEType;
            else if (type.IsFunctionPointer)
                flags = (uint)EETypeKind.FunctionPointerEEType;
            else
                flags = (uint)EETypeKind.CanonicalEEType;

            // 5 bits near the top of flags are used to convey enum underlying type, primitive type, or mark the type as being System.Array
            EETypeElementType elementType = ComputeEETypeElementType(type);
            flags |= ((uint)elementType << (byte)EETypeFlags.ElementTypeShift);

            if (type.IsArray || type.IsString)
            {
                flags |= (uint)EETypeFlags.HasComponentSizeFlag;
            }

            if (type.HasVariance)
            {
                flags |= (uint)EETypeFlags.GenericVarianceFlag;
            }

            if (type.IsGenericDefinition)
            {
                flags |= (uint)EETypeKind.GenericTypeDefEEType;

                // Generic type definition EETypes don't set the other flags.
                return flags;
            }

            if (type.HasFinalizer)
            {
                flags |= (uint)EETypeFlags.HasFinalizerFlag;
            }

            if (type.IsDefType
                && !type.IsCanonicalSubtype(CanonicalFormKind.Universal)
                && ((DefType)type).ContainsGCPointers)
            {
                flags |= (uint)EETypeFlags.HasPointersFlag;
            }
            else if (type.IsArray && !type.IsCanonicalSubtype(CanonicalFormKind.Universal))
            {
                var arrayElementType = ((ArrayType)type).ElementType;
                if ((arrayElementType.IsValueType && ((DefType)arrayElementType).ContainsGCPointers) || arrayElementType.IsGCPointer)
                {
                    flags |= (uint)EETypeFlags.HasPointersFlag;
                }
            }

            if (type.HasInstantiation)
            {
                flags |= (uint)EETypeFlags.IsGenericFlag;
            }

            return flags;
        }

        public static int GetMinimumObjectSize(TypeSystemContext typeSystemContext)
            => typeSystemContext.Target.PointerSize * 3;

        public static int ComputeBaseSize(TypeDesc type)
        {
            int pointerSize = type.Context.Target.PointerSize;
            int objectSize;

            if (type.IsInterface)
            {
                // Interfaces don't live on the GC heap. Don't bother computing a number.
                // Zero compresses better than any useless number we would come up with.
                return 0;
            }
            else if (type.IsDefType)
            {
                LayoutInt instanceByteCount = ((DefType)type).InstanceByteCount;

                if (instanceByteCount.IsIndeterminate)
                {
                    // Some value must be put in, but the specific value doesn't matter as it
                    // isn't used for specific instantiations, and the universal canon MethodTable
                    // is never associated with an allocated object.
                    objectSize = pointerSize;
                }
                else
                {
                    objectSize = pointerSize +
                        ((DefType)type).InstanceByteCount.AsInt; // +pointerSize for SyncBlock
                }

                if (type.IsValueType)
                    objectSize += pointerSize; // + EETypePtr field inherited from System.Object
            }
            else if (type.IsArray)
            {
                objectSize = 3 * pointerSize; // SyncBlock + EETypePtr + Length
                if (type.IsMdArray)
                    objectSize +=
                        2 * sizeof(int) * ((ArrayType)type).Rank;
            }
            else if (type.IsPointer)
            {
                // These never get boxed and don't have a base size. Use a sentinel value recognized by the runtime.
                return ParameterizedTypeShapeConstants.Pointer;
            }
            else if (type.IsByRef)
            {
                // These never get boxed and don't have a base size. Use a sentinel value recognized by the runtime.
                return ParameterizedTypeShapeConstants.ByRef;
            }
            else if (type.IsFunctionPointer)
            {
                // These never get boxed and don't have a base size. We store the 'unmanaged' flag and number of parameters.
                MethodSignature sig = ((FunctionPointerType)type).Signature;
                return (sig.Flags & MethodSignatureFlags.UnmanagedCallingConventionMask) switch
                {
                    0 => sig.Length,
                    _ => sig.Length | unchecked((int)FunctionPointerFlags.IsUnmanaged),
                };
            }
            else
                throw new NotImplementedException();

            objectSize = AlignmentHelper.AlignUp(objectSize, pointerSize);
            objectSize = Math.Max(GetMinimumObjectSize(type.Context), objectSize);

            if (type.IsString)
            {
                // If this is a string, throw away objectSize we computed so far. Strings are special.
                // SyncBlock + EETypePtr + length + firstChar
                objectSize = 2 * pointerSize +
                    sizeof(int) +
                    StringComponentSize.Value;
            }

            return objectSize;
        }

        public static ushort ComputeFlagsEx(TypeDesc type)
        {
            ushort flagsEx = 0;

            if (type is MetadataType mdType &&
                            mdType.Module == mdType.Context.SystemModule &&
                            (mdType.Name.SequenceEqual("WeakReference"u8) || mdType.Name.SequenceEqual("WeakReference`1"u8)) &&
                            mdType.Namespace.SequenceEqual("System"u8))
            {
                flagsEx |= (ushort)EETypeFlagsEx.HasEagerFinalizerFlag;
            }

            if (HasCriticalFinalizer(type))
            {
                flagsEx |= (ushort)EETypeFlagsEx.HasCriticalFinalizerFlag;
            }

            if (type.Context.Target.IsApplePlatform && IsTrackedReferenceWithFinalizer(type))
            {
                flagsEx |= (ushort)EETypeFlagsEx.IsTrackedReferenceWithFinalizerFlag;
            }

            if (type.IsIDynamicInterfaceCastable)
            {
                flagsEx |= (ushort)EETypeFlagsEx.IDynamicInterfaceCastableFlag;
            }

            if (type.IsByRefLike)
            {
                flagsEx |= (ushort)EETypeFlagsEx.IsByRefLikeFlag;
            }

            if (type.RequiresAlign8())
            {
                flagsEx |= (ushort)EETypeFlagsEx.RequiresAlign8Flag;
            }

            if (type.IsValueType && type != type.Context.UniversalCanonType)
            {
                int numInstanceFieldBytes = ((DefType)type).InstanceByteCountUnaligned.AsInt;

                // Value types should have at least 1 byte of size
                Debug.Assert(numInstanceFieldBytes >= 1);

                // The size of value types doesn't include the MethodTable pointer.  We need to add this so that
                // the number of instance field bytes consistently represents the boxed size.
                numInstanceFieldBytes += type.Context.Target.PointerSize;

                // For unboxing to work correctly and for supporting dynamic type loading for derived types we need
                // to record the actual size of the fields of a type without any padding for GC heap allocation (since
                // we can unbox into locals or arrays where this padding is not used, and because field layout for derived
                // types is effected by the unaligned base size). We don't want to store this information for all EETypes
                // since it's only relevant for value types, so it's added as an optional field. It's
                // also enough to simply store the size of the padding which cuts down our storage requirements.

                int valueTypeFieldPadding = (ComputeBaseSize(type) - type.Context.Target.PointerSize) - numInstanceFieldBytes;
                Debug.Assert(int.TrailingZeroCount((int)EETypeFlagsEx.ValueTypeFieldPaddingMask) == ValueTypeFieldPaddingConsts.Shift);
                Debug.Assert((valueTypeFieldPadding & ((int)EETypeFlagsEx.ValueTypeFieldPaddingMask >> ValueTypeFieldPaddingConsts.Shift)) == valueTypeFieldPadding);
                flagsEx |= (ushort)(valueTypeFieldPadding << ValueTypeFieldPaddingConsts.Shift);
            }

            if (type.IsNullable)
            {
                FieldDesc field = type.GetField("value"u8);

                int nullableValueOffset = field.Offset.AsInt;

                // In the definition of Nullable<T>, the first field should be the boolean representing "hasValue"
                Debug.Assert(nullableValueOffset > 0);

                // The field is offset due to alignment. This should be a power of two.
                Debug.Assert((nullableValueOffset & (nullableValueOffset - 1)) == 0);

                int log2nullableOffset = int.TrailingZeroCount(nullableValueOffset);

                Debug.Assert(int.TrailingZeroCount((int)EETypeFlagsEx.NullableValueOffsetMask) == NullableValueOffsetConsts.Shift);
                Debug.Assert((log2nullableOffset & ((int)EETypeFlagsEx.NullableValueOffsetMask >> NullableValueOffsetConsts.Shift)) == log2nullableOffset);
                flagsEx |= (ushort)(log2nullableOffset << NullableValueOffsetConsts.Shift);
            }

            return flagsEx;
        }

        private static bool HasCriticalFinalizer(TypeDesc type)
        {
            do
            {
                if (!type.HasFinalizer)
                    return false;

                if (type is MetadataType mdType &&
                            mdType.Module == mdType.Context.SystemModule &&
                            mdType.Name.SequenceEqual("CriticalFinalizerObject"u8) &&
                            mdType.Namespace.SequenceEqual("System.Runtime.ConstrainedExecution"u8))
                    return true;

                type = type.BaseType;
            }
            while (type != null);

            return false;
        }

        private static bool IsTrackedReferenceWithFinalizer(TypeDesc type)
        {
            do
            {
                if (!type.HasFinalizer)
                    return false;

                if (((MetadataType)type).HasCustomAttribute("System.Runtime.InteropServices.ObjectiveC", "ObjectiveCTrackedTypeAttribute"))
                    return true;

                type = type.BaseType;
            }
            while (type != null);

            return false;
        }
    }
}
