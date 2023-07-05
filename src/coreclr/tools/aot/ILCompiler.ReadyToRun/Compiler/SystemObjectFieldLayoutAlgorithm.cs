// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    /// <summary>
    /// Represents an algorithm that adds a target pointer of space at the beginning of all types
    /// deriving from System.Object used for the MethodTable pointer in the CoreCLR runtime.
    /// </summary>
    internal class SystemObjectFieldLayoutAlgorithm : FieldLayoutAlgorithm
    {
        private readonly FieldLayoutAlgorithm _fallbackAlgorithm;

        public SystemObjectFieldLayoutAlgorithm(FieldLayoutAlgorithm fallbackAlgorithm)
        {
            _fallbackAlgorithm = fallbackAlgorithm;
        }

        public override ComputedInstanceFieldLayout ComputeInstanceLayout(DefType defType, InstanceLayoutKind layoutKind)
        {
            TargetDetails targetDetails = defType.Context.Target;
            ComputedInstanceFieldLayout layoutFromMetadata = _fallbackAlgorithm.ComputeInstanceLayout(defType, layoutKind);

            // System.Object has an MethodTable field in the standard AOT version used in this repo.
            // Make sure that we always use the CoreCLR version which (currently) has no fields.
            Debug.Assert(0 == layoutFromMetadata.Offsets.Length, "Incompatible system library. The CoreCLR System.Private.CoreLib must be used when compiling in ready-to-run mode.");

            return new ComputedInstanceFieldLayout
            {
                ByteCountUnaligned = targetDetails.LayoutPointerSize,
                ByteCountAlignment = targetDetails.LayoutPointerSize,
                FieldAlignment = layoutFromMetadata.FieldAlignment,
                FieldSize = layoutFromMetadata.FieldSize,
                Offsets = layoutFromMetadata.Offsets,
                LayoutAbiStable = true,
            };
        }

        public unsafe override ComputedStaticFieldLayout ComputeStaticFieldLayout(DefType defType, StaticLayoutKind layoutKind)
        {
            return _fallbackAlgorithm.ComputeStaticFieldLayout(defType, layoutKind);
        }

        public override bool ComputeContainsGCPointers(DefType type)
        {
            return false;
        }

        public override bool ComputeIsUnsafeValueType(DefType type)
        {
            return false;
        }

        public override ValueTypeShapeCharacteristics ComputeValueTypeShapeCharacteristics(DefType type)
        {
            return _fallbackAlgorithm.ComputeValueTypeShapeCharacteristics(type);
        }
    }
}
