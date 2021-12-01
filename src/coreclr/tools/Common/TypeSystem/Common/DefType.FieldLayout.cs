// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Internal.TypeSystem
{
    // This is the api surface necessary to query the field layout of a type
    public abstract partial class DefType : TypeDesc
    {
        /// <summary>
        /// Bit flags for layout
        /// </summary>
        private static class FieldLayoutFlags
        {
            /// <summary>
            /// True if ContainsGCPointers has been computed
            /// </summary>
            public const int ComputedContainsGCPointers = 1;

            /// <summary>
            /// True if the type contains GC pointers
            /// </summary>
            public const int ContainsGCPointers = 2;

            /// <summary>
            /// True if the instance type only layout is computed
            /// </summary>
            public const int ComputedInstanceTypeLayout = 4;

            /// <summary>
            /// True if the static field layout for the static regions have been computed
            /// </summary>
            public const int ComputedStaticRegionLayout = 8;

            /// <summary>
            /// True if the instance type layout is complete including fields
            /// </summary>
            public const int ComputedInstanceTypeFieldsLayout = 0x10;

            /// <summary>
            /// True if the static field layout for the static fields have been computed
            /// </summary>
            public const int ComputedStaticFieldsLayout = 0x20;

            /// <summary>
            /// True if information about the shape of value type has been computed.
            /// </summary>
            public const int ComputedValueTypeShapeCharacteristics = 0x40;

            /// <summary>
            /// True if the layout of the type is not stable for use in the ABI
            /// </summary>
            public const int ComputedInstanceLayoutAbiUnstable = 0x80;

            /// <summary>
            /// True if IsUnsafeValueType has been computed
            /// </summary>
            public const int ComputedIsUnsafeValueType = 0x100;

            /// <summary>
            /// True if type transitively has UnsafeValueTypeAttribute
            /// </summary>
            public const int IsUnsafeValueType = 0x200;
        }

        private class StaticBlockInfo
        {
            public StaticsBlock NonGcStatics;
            public StaticsBlock GcStatics;
            public StaticsBlock ThreadNonGcStatics;
            public StaticsBlock ThreadGcStatics;
        }

        ThreadSafeFlags _fieldLayoutFlags;

        LayoutInt _instanceFieldSize;
        LayoutInt _instanceFieldAlignment;
        LayoutInt _instanceByteCountUnaligned;
        LayoutInt _instanceByteAlignment;

        // Information about various static blocks is rare, so we keep it out of line.
        StaticBlockInfo _staticBlockInfo;

        ValueTypeShapeCharacteristics _valueTypeShapeCharacteristics;

        /// <summary>
        /// Does a type transitively have any fields which are GC object pointers
        /// </summary>
        public bool ContainsGCPointers
        {
            get
            {
                if (!_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedContainsGCPointers))
                {
                    ComputeTypeContainsGCPointers();
                }
                return _fieldLayoutFlags.HasFlags(FieldLayoutFlags.ContainsGCPointers);
            }
        }

        /// <summary>
        /// Does a type transitively have any fields which are marked with System.Runtime.CompilerServices.UnsafeValueTypeAttribute
        /// </summary>
        public bool IsUnsafeValueType
        {
            get
            {
                if (!_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedIsUnsafeValueType))
                {
                    ComputeIsUnsafeValueType();
                }
                return _fieldLayoutFlags.HasFlags(FieldLayoutFlags.IsUnsafeValueType);
            }
        }


        /// <summary>
        /// The number of bytes required to hold a field of this type
        /// </summary>
        public LayoutInt InstanceFieldSize
        {
            get
            {
                if (!_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedInstanceTypeLayout))
                {
                    ComputeInstanceLayout(InstanceLayoutKind.TypeOnly);
                }
                return _instanceFieldSize;
            }
        }

        /// <summary>
        /// What is the alignment requirement of the fields of this type
        /// </summary>
        public LayoutInt InstanceFieldAlignment
        {
            get
            {
                if (!_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedInstanceTypeLayout))
                {
                    ComputeInstanceLayout(InstanceLayoutKind.TypeOnly);
                }
                return _instanceFieldAlignment;
            }
        }

        /// <summary>
        /// The number of bytes required when allocating this type on this GC heap
        /// </summary>
        public LayoutInt InstanceByteCount
        {
            get
            {
                return LayoutInt.AlignUp(InstanceByteCountUnaligned, InstanceByteAlignment, Context.Target);
            }
        }

        /// <summary>
        /// The number of bytes used by the instance fields of this type and its parent types without padding at the end for alignment/gc.
        /// </summary>
        public LayoutInt InstanceByteCountUnaligned
        {
            get
            {
                if (!_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedInstanceTypeLayout))
                {
                    ComputeInstanceLayout(InstanceLayoutKind.TypeOnly);
                }
                return _instanceByteCountUnaligned;
            }
        }

        /// <summary>
        /// The alignment required for instances of this type on the GC heap
        /// </summary>
        public LayoutInt InstanceByteAlignment
        {
            get
            {
                if (!_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedInstanceTypeLayout))
                {
                    ComputeInstanceLayout(InstanceLayoutKind.TypeOnly);
                }
                return _instanceByteAlignment;
            }
        }

        public bool IsZeroSizedReferenceType
        {
            get
            {
                if (Category != TypeFlags.Class)
                {
                    throw new InvalidOperationException("Only reference types are allowed.");
                }

                if (!_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedInstanceTypeLayout))
                {
                    ComputeInstanceLayout(InstanceLayoutKind.TypeOnly);
                }

                // test that size without padding is zero:
                //   _instanceByteCountUnaligned - _instanceByteAlignment == LayoutInt.Zero
                // simplified to:
                return _instanceByteCountUnaligned == _instanceByteAlignment;
            }
        }

        /// <summary>
        /// The type has stable Abi layout
        /// </summary>
        public bool LayoutAbiStable
        {
            get
            {
                if (!_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedInstanceTypeLayout))
                {
                    ComputeInstanceLayout(InstanceLayoutKind.TypeOnly);
                }
                return !_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedInstanceLayoutAbiUnstable);
            }
        }

        /// <summary>
        /// How many bytes must be allocated to represent the non GC visible static fields of this type.
        /// </summary>
        public LayoutInt NonGCStaticFieldSize
        {
            get
            {
                if (!_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedStaticRegionLayout))
                {
                    ComputeStaticFieldLayout(StaticLayoutKind.StaticRegionSizes);
                }
                return _staticBlockInfo == null ? LayoutInt.Zero : _staticBlockInfo.NonGcStatics.Size;
            }
        }

        /// <summary>
        /// What is the alignment required for allocating the non GC visible static fields of this type.
        /// </summary>
        public LayoutInt NonGCStaticFieldAlignment
        {
            get
            {
                if (!_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedStaticRegionLayout))
                {
                    ComputeStaticFieldLayout(StaticLayoutKind.StaticRegionSizes);
                }
                return _staticBlockInfo == null ? LayoutInt.Zero : _staticBlockInfo.NonGcStatics.LargestAlignment;
            }
        }

        /// <summary>
        /// How many bytes must be allocated to represent the GC visible static fields of this type.
        /// </summary>
        public LayoutInt GCStaticFieldSize
        {
            get
            {
                if (!_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedStaticRegionLayout))
                {
                    ComputeStaticFieldLayout(StaticLayoutKind.StaticRegionSizes);
                }
                return _staticBlockInfo == null ? LayoutInt.Zero : _staticBlockInfo.GcStatics.Size;
            }
        }

        /// <summary>
        /// What is the alignment required for allocating the GC visible static fields of this type.
        /// </summary>
        public LayoutInt GCStaticFieldAlignment
        {
            get
            {
                if (!_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedStaticRegionLayout))
                {
                    ComputeStaticFieldLayout(StaticLayoutKind.StaticRegionSizes);
                }
                return _staticBlockInfo == null ? LayoutInt.Zero : _staticBlockInfo.GcStatics.LargestAlignment;
            }
        }

        /// <summary>
        /// How many bytes must be allocated to represent the non GC visible thread static fields
        /// of this type.
        /// </summary>
        public LayoutInt ThreadNonGcStaticFieldSize
        {
            get
            {
                if (!_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedStaticRegionLayout))
                {
                    ComputeStaticFieldLayout(StaticLayoutKind.StaticRegionSizes);
                }
                return _staticBlockInfo == null ? LayoutInt.Zero : _staticBlockInfo.ThreadNonGcStatics.Size;
            }
        }

        /// <summary>
        /// What is the alignment required for allocating the non GC visible thread static fields
        /// of this type.
        /// </summary>
        public LayoutInt ThreadNonGcStaticFieldAlignment
        {
            get
            {
                if (!_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedStaticRegionLayout))
                {
                    ComputeStaticFieldLayout(StaticLayoutKind.StaticRegionSizes);
                }
                return _staticBlockInfo == null ? LayoutInt.Zero : _staticBlockInfo.ThreadNonGcStatics.LargestAlignment;
            }
        }

        /// <summary>
        /// How many bytes must be allocated to represent the (potentially GC visible) thread static
        /// fields of this type.
        /// </summary>
        public LayoutInt ThreadGcStaticFieldSize
        {
            get
            {
                if (!_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedStaticRegionLayout))
                {
                    ComputeStaticFieldLayout(StaticLayoutKind.StaticRegionSizes);
                }
                return _staticBlockInfo == null ? LayoutInt.Zero : _staticBlockInfo.ThreadGcStatics.Size;
            }
        }

        /// <summary>
        /// What is the alignment required for allocating the (potentially GC visible) thread static
        /// fields of this type.
        /// </summary>
        public LayoutInt ThreadGcStaticFieldAlignment
        {
            get
            {
                if (!_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedStaticRegionLayout))
                {
                    ComputeStaticFieldLayout(StaticLayoutKind.StaticRegionSizes);
                }
                return _staticBlockInfo == null ? LayoutInt.Zero : _staticBlockInfo.ThreadGcStatics.LargestAlignment;
            }
        }

        public ValueTypeShapeCharacteristics ValueTypeShapeCharacteristics
        {
            get
            {
                if (!_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedValueTypeShapeCharacteristics))
                {
                    ComputeValueTypeShapeCharacteristics();
                }
                return _valueTypeShapeCharacteristics;
            }
        }

        private void ComputeValueTypeShapeCharacteristics()
        {
            _valueTypeShapeCharacteristics = this.Context.GetLayoutAlgorithmForType(this).ComputeValueTypeShapeCharacteristics(this);
            _fieldLayoutFlags.AddFlags(FieldLayoutFlags.ComputedValueTypeShapeCharacteristics);
        }

        /// <summary>
        /// Gets a value indicating whether the type is a homogeneous floating-point or short-vector aggregate.
        /// </summary>
        public bool IsHomogeneousAggregate
        {
            get
            {
                return (ValueTypeShapeCharacteristics & ValueTypeShapeCharacteristics.AggregateMask) != 0;
            }
        }

        /// <summary>
        /// If the type is a homogeneous floating-point or short-vector aggregate, returns its element size.
        /// </summary>
        public int GetHomogeneousAggregateElementSize()
        {
            return (ValueTypeShapeCharacteristics & ValueTypeShapeCharacteristics.AggregateMask) switch
            {
                ValueTypeShapeCharacteristics.Float32Aggregate => 4,
                ValueTypeShapeCharacteristics.Float64Aggregate => 8,
                ValueTypeShapeCharacteristics.Vector64Aggregate => 8,
                ValueTypeShapeCharacteristics.Vector128Aggregate => 16,
                _ => throw new InvalidOperationException()
            };
        }

        public void ComputeInstanceLayout(InstanceLayoutKind layoutKind)
        {
            if (_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedInstanceTypeFieldsLayout | FieldLayoutFlags.ComputedInstanceTypeLayout))
                return;

            var computedLayout = this.Context.GetLayoutAlgorithmForType(this).ComputeInstanceLayout(this, layoutKind);

            _instanceFieldSize = computedLayout.FieldSize;
            _instanceFieldAlignment = computedLayout.FieldAlignment;
            _instanceByteCountUnaligned = computedLayout.ByteCountUnaligned;
            _instanceByteAlignment = computedLayout.ByteCountAlignment;
            if (!computedLayout.LayoutAbiStable)
            {
                _fieldLayoutFlags.AddFlags(FieldLayoutFlags.ComputedInstanceLayoutAbiUnstable);
            }

            if (computedLayout.Offsets != null)
            {
                foreach (var fieldAndOffset in computedLayout.Offsets)
                {
                    Debug.Assert(fieldAndOffset.Field.OwningType == this);
                    fieldAndOffset.Field.InitializeOffset(fieldAndOffset.Offset);
                }
                _fieldLayoutFlags.AddFlags(FieldLayoutFlags.ComputedInstanceTypeFieldsLayout);
            }

            _fieldLayoutFlags.AddFlags(FieldLayoutFlags.ComputedInstanceTypeLayout);
        }

        public void ComputeStaticFieldLayout(StaticLayoutKind layoutKind)
        {
            if (_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedStaticFieldsLayout | FieldLayoutFlags.ComputedStaticRegionLayout))
                return;

            var computedStaticLayout = this.Context.GetLayoutAlgorithmForType(this).ComputeStaticFieldLayout(this, layoutKind);

            if ((computedStaticLayout.NonGcStatics.Size != LayoutInt.Zero) ||
                (computedStaticLayout.GcStatics.Size != LayoutInt.Zero) ||
                (computedStaticLayout.ThreadNonGcStatics.Size != LayoutInt.Zero) ||
                (computedStaticLayout.ThreadGcStatics.Size != LayoutInt.Zero))
            {
                var staticBlockInfo = new StaticBlockInfo
                {
                    NonGcStatics = computedStaticLayout.NonGcStatics,
                    GcStatics = computedStaticLayout.GcStatics,
                    ThreadNonGcStatics = computedStaticLayout.ThreadNonGcStatics,
                    ThreadGcStatics = computedStaticLayout.ThreadGcStatics
                };
                _staticBlockInfo = staticBlockInfo;
            }

            if (computedStaticLayout.Offsets != null)
            {
                foreach (var fieldAndOffset in computedStaticLayout.Offsets)
                {
                    Debug.Assert(fieldAndOffset.Field.OwningType == this);
                    fieldAndOffset.Field.InitializeOffset(fieldAndOffset.Offset);
                }
                _fieldLayoutFlags.AddFlags(FieldLayoutFlags.ComputedStaticFieldsLayout);
            }

            _fieldLayoutFlags.AddFlags(FieldLayoutFlags.ComputedStaticRegionLayout);
        }

        public void ComputeTypeContainsGCPointers()
        {
            if (_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedContainsGCPointers))
                return;

            int flagsToAdd = FieldLayoutFlags.ComputedContainsGCPointers;

            if (!IsValueType && HasBaseType && BaseType.ContainsGCPointers)
            {
                _fieldLayoutFlags.AddFlags(flagsToAdd | FieldLayoutFlags.ContainsGCPointers);
                return;
            }

            if (this.Context.GetLayoutAlgorithmForType(this).ComputeContainsGCPointers(this))
            {
                flagsToAdd |= FieldLayoutFlags.ContainsGCPointers;
            }

            _fieldLayoutFlags.AddFlags(flagsToAdd);
        }

        public void ComputeIsUnsafeValueType()
        {
            if (_fieldLayoutFlags.HasFlags(FieldLayoutFlags.ComputedIsUnsafeValueType))
                return;

            int flagsToAdd = FieldLayoutFlags.ComputedIsUnsafeValueType;

            if (this.Context.GetLayoutAlgorithmForType(this).ComputeIsUnsafeValueType(this))
            {
                flagsToAdd |= FieldLayoutFlags.IsUnsafeValueType;
            }

            _fieldLayoutFlags.AddFlags(flagsToAdd);
        }
    }
}
