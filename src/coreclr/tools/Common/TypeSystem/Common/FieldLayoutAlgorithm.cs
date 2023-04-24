// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    /// <summary>
    /// Pluggable field layout algorithm. Provides means to compute static/instance sizes for types,
    /// offsets for their fields and other type information that depends on type's fields.
    /// The information computed by this algorithm is exposed on various properties of
    /// <see cref="DefType"/> and <see cref="FieldDesc"/>.
    /// </summary>
    /// <remarks>
    /// The algorithms are expected to be directly used by <see cref="TypeSystemContext"/> derivatives
    /// only. The most obvious implementation of this algorithm that uses type's metadata to
    /// compute the answers is in <see cref="MetadataFieldLayoutAlgorithm"/>.
    /// </remarks>
    public abstract class FieldLayoutAlgorithm
    {
        /// <summary>
        /// Compute the instance field layout for a DefType. Must not depend on static field layout for any other type.
        /// </summary>
        public abstract ComputedInstanceFieldLayout ComputeInstanceLayout(DefType type, InstanceLayoutKind layoutKind);

        /// <summary>
        /// Compute the static field layout for a DefType. Must not depend on static field layout for any other type.
        /// </summary>
        public abstract ComputedStaticFieldLayout ComputeStaticFieldLayout(DefType type, StaticLayoutKind layoutKind);

        /// <summary>
        /// Compute whether the fields of the specified type contain a GC pointer.
        /// </summary>
        public abstract bool ComputeContainsGCPointers(DefType type);

        /// <summary>
        /// Compute whether the specified type is a value type that transitively has UnsafeValueTypeAttribute
        /// </summary>
        public abstract bool ComputeIsUnsafeValueType(DefType type);

        /// <summary>
        /// Compute the shape of a value type. The shape information is used to control code generation and allocation
        /// (such as vectorization, passing the value type by value across method calls, or boxing alignment).
        /// </summary>
        public abstract ValueTypeShapeCharacteristics ComputeValueTypeShapeCharacteristics(DefType type);
    }

    /// <summary>
    /// Specifies the level to which to compute the instance field layout.
    /// </summary>
    public enum InstanceLayoutKind
    {
        /// <summary>
        /// Compute instance sizes and alignments.
        /// </summary>
        TypeOnly,

        /// <summary>
        /// Compute instance sizes, alignments and field offsets.
        /// </summary>
        TypeAndFields
    }

    /// <summary>
    /// Specifies the level to which to compute static field layout.
    /// </summary>
    public enum StaticLayoutKind
    {
        /// <summary>
        /// Compute static region sizes.
        /// </summary>
        StaticRegionSizes,

        /// <summary>
        /// Compute static region sizes and static field offsets.
        /// </summary>
        StaticRegionSizesAndFields
    }

    public struct ComputedInstanceFieldLayout
    {
        public LayoutInt FieldSize;
        public LayoutInt FieldAlignment;
        public LayoutInt ByteCountUnaligned;
        public LayoutInt ByteCountAlignment;
        public bool LayoutAbiStable; // Is the layout stable such that it can safely be used in function calling conventions
        public bool IsAutoLayoutOrHasAutoLayoutFields;
        public bool IsInt128OrHasInt128Fields;

        /// <summary>
        /// If Offsets is non-null, then all field based layout is complete.
        /// Otherwise, only the non-field based data is considered to be complete
        /// </summary>
        public FieldAndOffset[] Offsets;
    }

    public struct StaticsBlock
    {
        public LayoutInt Size;
        public LayoutInt LargestAlignment;
    }

    public struct ComputedStaticFieldLayout
    {
        public StaticsBlock NonGcStatics;
        public StaticsBlock GcStatics;
        public StaticsBlock ThreadNonGcStatics;
        public StaticsBlock ThreadGcStatics;

        /// <summary>
        /// If Offsets is non-null, then all field based layout is complete.
        /// Otherwise, only the non-field based data is considered to be complete
        /// </summary>
        public FieldAndOffset[] Offsets;
    }

    /// <summary>
    /// Describes shape of a value type for code generation and allocation purposes.
    /// </summary>
    public enum ValueTypeShapeCharacteristics
    {
        None = 0x00,

        /// <summary>
        /// The type is an aggregate of 32-bit floating-point values.
        /// </summary>
        Float32Aggregate = 0x01,

        /// <summary>
        /// The type is an aggregate of 64-bit floating-point values.
        /// </summary>
        Float64Aggregate = 0x02,

        /// <summary>
        /// The type is an aggregate of 64-bit short-vector values.
        /// </summary>
        Vector64Aggregate = 0x04,

        /// <summary>
        /// The type is an aggregate of 128-bit short-vector values.
        /// </summary>
        Vector128Aggregate = 0x08,

        /// <summary>
        /// The mask for homogeneous aggregates of floating-point values.
        /// </summary>
        FloatingPointAggregateMask = Float32Aggregate | Float64Aggregate,

        /// <summary>
        /// The mask for homogeneous aggregates of short-vector values.
        /// </summary>
        ShortVectorAggregateMask = Vector64Aggregate | Vector128Aggregate,

        /// <summary>
        /// The mask for homogeneous aggregates.
        /// </summary>
        AggregateMask = FloatingPointAggregateMask | ShortVectorAggregateMask,
    }
}
