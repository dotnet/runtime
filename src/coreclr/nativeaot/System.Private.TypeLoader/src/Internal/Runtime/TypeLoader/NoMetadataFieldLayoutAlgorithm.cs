// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;
using System.Diagnostics;

namespace Internal.Runtime.TypeLoader
{
    /// <summary>
    /// Useable when we have runtime MethodTable structures. Can represent the field layout necessary
    /// to represent the size/alignment of the overall type, but must delegate to either NativeLayoutFieldAlgorithm
    /// or MetadataFieldLayoutAlgorithm to get information about individual fields.
    /// </summary>
    internal class NoMetadataFieldLayoutAlgorithm : FieldLayoutAlgorithm
    {
        private static NativeLayoutFieldAlgorithm s_nativeLayoutFieldAlgorithm = new NativeLayoutFieldAlgorithm();

        public override unsafe bool ComputeContainsGCPointers(DefType type)
        {
            return type.RuntimeTypeHandle.ToEETypePtr()->HasGCPointers;
        }

        /// <summary>
        /// Reads the minimal information about type layout encoded in the
        /// MethodTable. That doesn't include field information.
        /// </summary>
        public override unsafe ComputedInstanceFieldLayout ComputeInstanceLayout(DefType type, InstanceLayoutKind layoutKind)
        {
            // If we need the field information, delegate to the native layout algorithm or metadata algorithm
            if (layoutKind != InstanceLayoutKind.TypeOnly)
            {
                Debug.Assert(type.HasNativeLayout);
                return s_nativeLayoutFieldAlgorithm.ComputeInstanceLayout(type, layoutKind);
            }

            type.RetrieveRuntimeTypeHandleIfPossible();
            Debug.Assert(!type.RuntimeTypeHandle.IsNull());
            MethodTable* MethodTable = type.RuntimeTypeHandle.ToEETypePtr();

            ComputedInstanceFieldLayout layout = new ComputedInstanceFieldLayout()
            {
                ByteCountAlignment = new LayoutInt(IntPtr.Size),
                ByteCountUnaligned = new LayoutInt(MethodTable->IsInterface ? IntPtr.Size : checked((int)MethodTable->FieldByteCountNonGCAligned)),
                FieldAlignment = new LayoutInt(MethodTable->FieldAlignmentRequirement),
                Offsets = (layoutKind == InstanceLayoutKind.TypeOnly) ? null : Array.Empty<FieldAndOffset>(), // No fields in EETypes
            };

            if (MethodTable->IsValueType)
            {
                int valueTypeSize = checked((int)MethodTable->ValueTypeSize);
                layout.FieldSize = new LayoutInt(valueTypeSize);
            }
            else
            {
                layout.FieldSize = new LayoutInt(IntPtr.Size);
            }

            if ((MethodTable->RareFlags & EETypeRareFlags.RequiresAlign8Flag) == EETypeRareFlags.RequiresAlign8Flag)
            {
                layout.ByteCountAlignment = new LayoutInt(8);
            }

            return layout;
        }

        public override ComputedStaticFieldLayout ComputeStaticFieldLayout(DefType type, StaticLayoutKind layoutKind)
        {
            Debug.Assert(false);
            return default;
        }

        public override ValueTypeShapeCharacteristics ComputeValueTypeShapeCharacteristics(DefType type)
        {
            if (type.Context.Target.Architecture == TargetArchitecture.ARM)
            {
                unsafe
                {
                    // On ARM, the HFA type is encoded into the MethodTable directly
                    type.RetrieveRuntimeTypeHandleIfPossible();
                    Debug.Assert(!type.RuntimeTypeHandle.IsNull());
                    MethodTable* MethodTable = type.RuntimeTypeHandle.ToEETypePtr();

                    if (!MethodTable->IsHFA)
                        return ValueTypeShapeCharacteristics.None;

                    if (MethodTable->RequiresAlign8)
                        return ValueTypeShapeCharacteristics.Float64Aggregate;
                    else
                        return ValueTypeShapeCharacteristics.Float32Aggregate;
                }
            }
            else
            {
                Debug.Assert(
                    type.Context.Target.Architecture == TargetArchitecture.X86 ||
                    type.Context.Target.Architecture == TargetArchitecture.X64);

                return ValueTypeShapeCharacteristics.None;
            }
        }

        public override bool ComputeIsUnsafeValueType(DefType type)
        {
            throw new NotSupportedException();
        }
    }
}
