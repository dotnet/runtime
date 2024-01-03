// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    /// <summary>
    /// MetadataFieldLayout algorithm which can be used to compute field layout
    /// for any MetadataType where all fields are available by calling GetFields.
    /// </summary>
    public abstract class MetadataFieldLayoutAlgorithm : FieldLayoutAlgorithm
    {
        public override ComputedInstanceFieldLayout ComputeInstanceLayout(DefType defType, InstanceLayoutKind layoutKind)
        {
            MetadataType type = (MetadataType)defType;

            if (type.IsGenericDefinition)
            {
                ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, type);
            }

            // CLI - Partition 1, section 9.5 - Generic types shall not be marked explicitlayout.
            if (type.HasInstantiation && type.IsExplicitLayout)
            {
                ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadExplicitGeneric, type.GetTypeDefinition());
            }

            // Count the number of instance fields in advance for convenience
            int numInstanceFields = 0;
            foreach (var field in type.GetFields())
            {
                if (field.IsStatic)
                    continue;

                TypeDesc fieldType = field.FieldType;

                // ByRef and byref-like instance fields on non-byref-like types are not allowed.
                if ((fieldType.IsByRef || fieldType.IsByRefLike) && !type.IsByRefLike)
                    ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, type);

                numInstanceFields++;
            }

            if (type.IsModuleType)
            {
                // This is a global type, it must not have instance fields.
                if (numInstanceFields > 0)
                {
                    ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, type);
                }

                // Global types do not do the rest of instance field layout.
                ComputedInstanceFieldLayout result = default(ComputedInstanceFieldLayout);
                result.Offsets = Array.Empty<FieldAndOffset>();
                return result;
            }

            // CLI - Partition 2, section 22.8
            // A type has layout if it is marked SequentialLayout or ExplicitLayout.  If any type within an inheritance chain has layout,
            // then so shall all its base classes, up to the one that descends immediately from System.ValueType (if it exists in the type's
            // hierarchy); otherwise, from System.Object
            // Note: While the CLI isn't clearly worded, the layout needs to be the same for the entire chain.
            // If the current type isn't ValueType or System.Object and has a layout and the parent type isn't
            // ValueType or System.Object then the layout type attributes need to match
            if ((!type.IsValueType && !type.IsObject) &&
                (type.IsSequentialLayout || type.IsExplicitLayout) &&
                (!type.BaseType.IsValueType && !type.BaseType.IsObject))
            {
                MetadataType baseType = type.MetadataBaseType;

                if (type.IsSequentialLayout != baseType.IsSequentialLayout ||
                    type.IsExplicitLayout != baseType.IsExplicitLayout)
                {
                    ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadBadFormat, type);
                }
            }

            // Enum types must have a single instance field
            if (type.IsEnum && numInstanceFields != 1)
            {
                ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, type);
            }

            if (type.IsPrimitive)
            {
                // Primitive types are special - they may have a single field of the same type
                // as the type itself. They do not do the rest of instance field layout.
                if (numInstanceFields > 1)
                {
                    ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, type);
                }

                SizeAndAlignment instanceByteSizeAndAlignment;
                var sizeAndAlignment = ComputeInstanceSize(
                    type,
                    type.Context.Target.GetWellKnownTypeSize(type),
                    type.Context.Target.GetWellKnownTypeAlignment(type),
                    0,
                    out instanceByteSizeAndAlignment
                    );

                ComputedInstanceFieldLayout result = new ComputedInstanceFieldLayout
                {
                    ByteCountUnaligned = instanceByteSizeAndAlignment.Size,
                    ByteCountAlignment = instanceByteSizeAndAlignment.Alignment,
                    FieldAlignment = sizeAndAlignment.Alignment,
                    FieldSize = sizeAndAlignment.Size,
                    LayoutAbiStable = true,
                    IsAutoLayoutOrHasAutoLayoutFields = false,
                    IsInt128OrHasInt128Fields = false,
                    IsVectorTOrHasVectorTFields = false,
                };

                if (numInstanceFields > 0)
                {
                    FieldDesc instanceField = null;
                    foreach (FieldDesc field in type.GetFields())
                    {
                        if (!field.IsStatic)
                        {
                            Debug.Assert(instanceField == null, "Unexpected extra instance field");
                            instanceField = field;
                        }
                    }

                    Debug.Assert(instanceField != null, "Null instance field");

                    result.Offsets = new FieldAndOffset[] {
                        new FieldAndOffset(instanceField, LayoutInt.Zero)
                    };
                }
                else
                {
                    result.Offsets = Array.Empty<FieldAndOffset>();
                }

                return result;
            }

            // If the type has layout, read its packing and size info
            // If the type has explicit layout, also read the field offset info
            if (type.IsExplicitLayout || type.IsSequentialLayout)
            {
                if (type.IsEnum)
                {
                    ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadBadFormat, type);
                }

                var layoutMetadata = type.GetClassLayout();

                // If packing is out of range or not a power of two, throw that the size is invalid
                int packing = layoutMetadata.PackingSize;
                if (packing < 0 || packing > 128 || ((packing & (packing - 1)) != 0))
                {
                    ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadBadFormat, type);
                }

                Debug.Assert(layoutMetadata.Offsets == null || layoutMetadata.Offsets.Length == numInstanceFields);
            }

            // At this point all special cases are handled and all inputs validated
            return ComputeInstanceFieldLayout(type, numInstanceFields);
        }

        protected abstract ComputedInstanceFieldLayout ComputeInstanceFieldLayout(MetadataType type, int numInstanceFields);

        public override ComputedStaticFieldLayout ComputeStaticFieldLayout(DefType defType, StaticLayoutKind layoutKind)
        {
            MetadataType type = (MetadataType)defType;
            int numStaticFields = 0;

            foreach (var field in type.GetFields())
            {
                if (!field.IsStatic || field.HasRva || field.IsLiteral)
                    continue;

                numStaticFields++;
            }

            ComputedStaticFieldLayout result;
            result.GcStatics = default(StaticsBlock);
            result.NonGcStatics = default(StaticsBlock);
            result.ThreadGcStatics = default(StaticsBlock);
            result.ThreadNonGcStatics = default(StaticsBlock);

            if (numStaticFields == 0)
            {
                result.Offsets = Array.Empty<FieldAndOffset>();
                return result;
            }

            result.Offsets = new FieldAndOffset[numStaticFields];

            TypeSystemContext context = type.Context;

            PrepareRuntimeSpecificStaticFieldLayout(context, ref result);

            int index = 0;

            foreach (var field in type.GetFields())
            {
                // Nonstatic fields, literal fields, and RVA mapped fields don't participate in layout
                if (!field.IsStatic || field.HasRva || field.IsLiteral)
                    continue;

                TypeDesc fieldType = field.FieldType;
                if (fieldType.IsByRef || (fieldType.IsValueType && ((DefType)fieldType).IsByRefLike))
                {
                    ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, type);
                }

                ref StaticsBlock block = ref GetStaticsBlockForField(ref result, field);
                SizeAndAlignment sizeAndAlignment = ComputeFieldSizeAndAlignment(fieldType, hasLayout: false, context.Target.DefaultPackingSize, out bool _, out bool _, out bool _, out bool _);

                block.Size = LayoutInt.AlignUp(block.Size, sizeAndAlignment.Alignment, context.Target);
                result.Offsets[index] = new FieldAndOffset(field, block.Size);
                block.Size += sizeAndAlignment.Size;

                block.LargestAlignment = LayoutInt.Max(block.LargestAlignment, sizeAndAlignment.Alignment);

                index++;
            }

            FinalizeRuntimeSpecificStaticFieldLayout(context, ref result);

            return result;
        }

        private static ref StaticsBlock GetStaticsBlockForField(ref ComputedStaticFieldLayout layout, FieldDesc field)
        {
            if (field.IsThreadStatic)
            {
                if (field.HasGCStaticBase)
                    return ref layout.ThreadGcStatics;
                else
                    return ref layout.ThreadNonGcStatics;
            }
            else if (field.HasGCStaticBase)
                return ref layout.GcStatics;
            else
                return ref layout.NonGcStatics;
        }

        public override bool ComputeContainsGCPointers(DefType type)
        {
            bool someFieldContainsPointers = false;

            foreach (var field in type.GetFields())
            {
                if (field.IsStatic)
                    continue;

                TypeDesc fieldType = field.FieldType;
                if (fieldType.IsValueType)
                {
                    if (fieldType.IsPrimitive)
                        continue;

                    if (((DefType)fieldType).ContainsGCPointers)
                    {
                        someFieldContainsPointers = true;
                        break;
                    }
                }
                else if (fieldType.IsGCPointer)
                {
                    someFieldContainsPointers = true;
                    break;
                }
            }

            return someFieldContainsPointers;
        }

        /// <summary>
        /// Called during static field layout to setup initial contents of statics blocks
        /// </summary>
        protected virtual void PrepareRuntimeSpecificStaticFieldLayout(TypeSystemContext context, ref ComputedStaticFieldLayout layout)
        {
        }

        /// <summary>
        /// Called during static field layout to finish static block layout
        /// </summary>
        protected virtual void FinalizeRuntimeSpecificStaticFieldLayout(TypeSystemContext context, ref ComputedStaticFieldLayout layout)
        {
        }

        protected ComputedInstanceFieldLayout ComputeExplicitFieldLayout(MetadataType type, int numInstanceFields)
        {
            // Instance slice size is the total size of instance not including the base type.
            // It is calculated as the field whose offset and size add to the greatest value.
            LayoutInt offsetBias = !type.IsValueType ? new LayoutInt(type.Context.Target.PointerSize) : LayoutInt.Zero;
            LayoutInt cumulativeInstanceFieldPos = CalculateFieldBaseOffset(type, requiresAlign8: false, requiresAlignedBase: false) - offsetBias;
            LayoutInt instanceSize = cumulativeInstanceFieldPos + offsetBias;

            var layoutMetadata = type.GetClassLayout();
            int packingSize = ComputePackingSize(type, layoutMetadata);
            LayoutInt largestAlignmentRequired = LayoutInt.One;

            var offsets = new FieldAndOffset[numInstanceFields];
            int fieldOrdinal = 0;
            bool layoutAbiStable = true;
            bool hasAutoLayoutField = false;
            bool hasInt128Field = false;
            bool hasVectorTField = false;

            if (type.BaseType is not null)
            {
                hasInt128Field = type.BaseType.IsInt128OrHasInt128Fields;
                hasVectorTField = type.BaseType.IsVectorTOrHasVectorTFields;
            }

            foreach (var fieldAndOffset in layoutMetadata.Offsets)
            {
                TypeDesc fieldType = fieldAndOffset.Field.FieldType;
                var fieldSizeAndAlignment = ComputeFieldSizeAndAlignment(fieldType.UnderlyingType, hasLayout: true, packingSize, out bool fieldLayoutAbiStable, out bool fieldHasAutoLayout, out bool fieldHasInt128Field, out bool fieldHasVectorTField);
                if (!fieldLayoutAbiStable)
                    layoutAbiStable = false;
                if (fieldHasAutoLayout)
                    hasAutoLayoutField = true;
                if (fieldHasInt128Field)
                    hasInt128Field = true;
                if (fieldHasVectorTField)
                    hasVectorTField = true;

                largestAlignmentRequired = LayoutInt.Max(fieldSizeAndAlignment.Alignment, largestAlignmentRequired);

                if (fieldAndOffset.Offset == FieldAndOffset.InvalidOffset)
                    ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadBadFormat, type);

                LayoutInt computedOffset = fieldAndOffset.Offset + cumulativeInstanceFieldPos + offsetBias;

                // GC pointers MUST be aligned.
                // We treat byref-like structs as GC pointers too.
                bool needsToBeAligned =
                    !computedOffset.IsIndeterminate
                    &&
                    (
                        fieldType.IsGCPointer
                        || fieldType.IsByRefLike
                        || (fieldType.IsValueType && ((DefType)fieldType).ContainsGCPointers)
                    );
                if (needsToBeAligned)
                {
                    int offsetModulo = computedOffset.AsInt % type.Context.Target.PointerSize;
                    if (offsetModulo != 0)
                    {
                        ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadExplicitLayout, type, fieldAndOffset.Offset.ToStringInvariant());
                    }
                }

                offsets[fieldOrdinal] = new FieldAndOffset(fieldAndOffset.Field, computedOffset);

                LayoutInt fieldExtent = computedOffset + fieldSizeAndAlignment.Size;
                instanceSize = LayoutInt.Max(fieldExtent, instanceSize);

                fieldOrdinal++;
            }

            SizeAndAlignment instanceByteSizeAndAlignment;
            var instanceSizeAndAlignment = ComputeInstanceSize(type,
                instanceSize,
                largestAlignmentRequired,
                layoutMetadata.Size,
                out instanceByteSizeAndAlignment);

            // inline array cannot have explicit layout
            if (type.IsInlineArray)
            {
                ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadInlineArrayExplicit, type);
            }

            ComputedInstanceFieldLayout computedLayout = new ComputedInstanceFieldLayout
            {
                IsAutoLayoutOrHasAutoLayoutFields = hasAutoLayoutField,
                IsInt128OrHasInt128Fields = hasInt128Field,
                IsVectorTOrHasVectorTFields = hasVectorTField,
            };
            computedLayout.FieldAlignment = instanceSizeAndAlignment.Alignment;
            computedLayout.FieldSize = instanceSizeAndAlignment.Size;
            computedLayout.ByteCountUnaligned = instanceByteSizeAndAlignment.Size;
            computedLayout.ByteCountAlignment = instanceByteSizeAndAlignment.Alignment;
            computedLayout.Offsets = offsets;
            computedLayout.LayoutAbiStable = layoutAbiStable;

            ExplicitLayoutValidator.Validate(type, computedLayout);

            return computedLayout;
        }

        private static LayoutInt AlignUpInstanceFieldOffset(LayoutInt cumulativeInstanceFieldPos, LayoutInt alignment, TargetDetails target)
        {
            return LayoutInt.AlignUp(cumulativeInstanceFieldPos, alignment, target);
        }

        protected ComputedInstanceFieldLayout ComputeSequentialFieldLayout(MetadataType type, int numInstanceFields)
        {
            var offsets = new FieldAndOffset[numInstanceFields];

            // For types inheriting from another type, field offsets continue on from where they left off
            // For reference types, we calculate field alignment as if the address after the method table pointer
            // has offset 0 (on 32-bit platforms, this location is guaranteed to be 8-aligned).
            LayoutInt offsetBias = !type.IsValueType ? new LayoutInt(type.Context.Target.PointerSize) : LayoutInt.Zero;
            LayoutInt cumulativeInstanceFieldPos = CalculateFieldBaseOffset(type, requiresAlign8: false, requiresAlignedBase: false) - offsetBias;

            var layoutMetadata = type.GetClassLayout();

            LayoutInt largestAlignmentRequirement = LayoutInt.One;
            int fieldOrdinal = 0;
            int packingSize = ComputePackingSize(type, layoutMetadata);
            bool layoutAbiStable = true;
            bool hasAutoLayoutField = false;
            bool hasInt128Field = false;
            bool hasVectorTField = false;

            if (type.BaseType is not null)
            {
                hasInt128Field = type.BaseType.IsInt128OrHasInt128Fields;
                hasVectorTField = type.BaseType.IsVectorTOrHasVectorTFields;
            }

            foreach (var field in type.GetFields())
            {
                if (field.IsStatic)
                    continue;

                var fieldSizeAndAlignment = ComputeFieldSizeAndAlignment(field.FieldType.UnderlyingType, hasLayout: true, packingSize, out bool fieldLayoutAbiStable, out bool fieldHasAutoLayout, out bool fieldHasInt128Field, out bool fieldHasVectorTField);
                if (!fieldLayoutAbiStable)
                    layoutAbiStable = false;
                if (fieldHasAutoLayout)
                    hasAutoLayoutField = true;
                if (fieldHasInt128Field)
                    hasInt128Field = true;
                if (fieldHasVectorTField)
                    hasVectorTField = true;

                largestAlignmentRequirement = LayoutInt.Max(fieldSizeAndAlignment.Alignment, largestAlignmentRequirement);

                cumulativeInstanceFieldPos = AlignUpInstanceFieldOffset(cumulativeInstanceFieldPos, fieldSizeAndAlignment.Alignment, type.Context.Target);
                offsets[fieldOrdinal] = new FieldAndOffset(field, cumulativeInstanceFieldPos + offsetBias);
                cumulativeInstanceFieldPos = checked(cumulativeInstanceFieldPos + fieldSizeAndAlignment.Size);

                fieldOrdinal++;
            }

            SizeAndAlignment instanceByteSizeAndAlignment;
            var instanceSizeAndAlignment = ComputeInstanceSize(
                type,
                cumulativeInstanceFieldPos + offsetBias,
                largestAlignmentRequirement,
                layoutMetadata.Size,
                out instanceByteSizeAndAlignment);

            if (type.IsInlineArray)
            {
                AdjustForInlineArray(type, numInstanceFields, ref instanceByteSizeAndAlignment, ref instanceSizeAndAlignment);
            }

            ComputedInstanceFieldLayout computedLayout = new ComputedInstanceFieldLayout
            {
                IsAutoLayoutOrHasAutoLayoutFields = hasAutoLayoutField,
                IsInt128OrHasInt128Fields = hasInt128Field,
                IsVectorTOrHasVectorTFields = hasVectorTField,
            };
            computedLayout.FieldAlignment = instanceSizeAndAlignment.Alignment;
            computedLayout.FieldSize = instanceSizeAndAlignment.Size;
            computedLayout.ByteCountUnaligned = instanceByteSizeAndAlignment.Size;
            computedLayout.ByteCountAlignment = instanceByteSizeAndAlignment.Alignment;
            computedLayout.Offsets = offsets;
            computedLayout.LayoutAbiStable = layoutAbiStable;

            return computedLayout;
        }

        private static void AdjustForInlineArray(
            MetadataType type,
            int instanceFieldCount,
            ref SizeAndAlignment instanceByteSizeAndAlignment,
            ref SizeAndAlignment instanceSizeAndAlignment)
        {
            int repeat = type.GetInlineArrayLength();

            if (repeat <= 0)
            {
                ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadInlineArrayLength, type);
            }

            if (instanceFieldCount != 1)
            {
                ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadInlineArrayFieldCount, type);
            }

            if (!instanceByteSizeAndAlignment.Size.IsIndeterminate)
            {
                long size = instanceByteSizeAndAlignment.Size.AsInt;
                size *= repeat;

                // limit the max size of array instance to 1MiB
                const int maxSize = 1024 * 1024;
                if (size > maxSize)
                {
                    ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadValueClassTooLarge, type);
                }

                instanceByteSizeAndAlignment.Size = new LayoutInt((int)size);
            }

            if (!instanceSizeAndAlignment.Size.IsIndeterminate)
            {
                instanceSizeAndAlignment.Size = new LayoutInt(instanceSizeAndAlignment.Size.AsInt * repeat);
            }
        }

        protected virtual void AlignBaseOffsetIfNecessary(MetadataType type, ref LayoutInt baseOffset, bool requiresAlign8, bool requiresAlignedBase)
        {
        }

        protected ComputedInstanceFieldLayout ComputeAutoFieldLayout(MetadataType type, int numInstanceFields)
        {
            TypeSystemContext context = type.Context;

            bool hasLayout = type.HasLayout();

            // Auto-layout in CoreCLR does not respect packing size.
            int packingSize = type.Context.Target.MaximumAlignment;

            var offsets = new FieldAndOffset[numInstanceFields];
            int fieldOrdinal = 0;

            // Iterate over the instance fields and keep track of the number of fields of each category
            // For the non-GC Pointer fields, we will keep track of the number of fields by log2(size)
            int maxLog2Size = CalculateLog2(TargetDetails.MaximumPrimitiveSize);
            int log2PointerSize = CalculateLog2(context.Target.PointerSize);
            int instanceValueClassFieldCount = 0;
            int instanceGCPointerFieldsCount = 0;
            int[] instanceNonGCPointerFieldsCount = new int[maxLog2Size + 1];
            bool hasInt128Field = false;
            bool hasVectorTField = false;

            foreach (var field in type.GetFields())
            {
                if (field.IsStatic)
                    continue;

                TypeDesc fieldType = field.FieldType;

                if (IsByValueClass(fieldType))
                {
                    // Valuetypes which are not primitives or enums
                    instanceValueClassFieldCount++;
                    if (((DefType)fieldType).IsInt128OrHasInt128Fields)
                        hasInt128Field = true;
                    if (((DefType)fieldType).IsVectorTOrHasVectorTFields)
                        hasVectorTField = true;
                }
                else if (fieldType.IsGCPointer)
                {
                    instanceGCPointerFieldsCount++;
                }
                else
                {
                    Debug.Assert(fieldType.IsPrimitive || fieldType.IsPointer || fieldType.IsFunctionPointer || fieldType.IsEnum || fieldType.IsByRef);

                    var fieldSizeAndAlignment = ComputeFieldSizeAndAlignment(fieldType, hasLayout, packingSize, out bool _, out bool _, out bool _, out bool _);
                    instanceNonGCPointerFieldsCount[CalculateLog2(fieldSizeAndAlignment.Size.AsInt)]++;
                }
            }

            // Initialize three different sets of lists to hold the instance fields
            //   1. Array of value class fields
            //   2. Array of GC Pointer fields
            //   3. Jagged array of remaining fields. To access the fields of size n, you must index the first array at index log2(n)
            FieldDesc[] instanceValueClassFieldsArr = new FieldDesc[instanceValueClassFieldCount];
            FieldDesc[] instanceGCPointerFieldsArr = new FieldDesc[instanceGCPointerFieldsCount];
            FieldDesc[][] instanceNonGCPointerFieldsArr = new FieldDesc[maxLog2Size + 1][];

            for (int i = 0; i <= maxLog2Size; i++)
            {
                instanceNonGCPointerFieldsArr[i] = new FieldDesc[instanceNonGCPointerFieldsCount[i]];

                // Reset the counters to be used later as the index to insert into the arrays
                instanceNonGCPointerFieldsCount[i] = 0;
            }

            // Reset the counters to be used later as the index to insert into the array
            instanceGCPointerFieldsCount = 0;
            instanceValueClassFieldCount = 0;
            LayoutInt largestAlignmentRequired = LayoutInt.One;
            bool layoutAbiStable = true;

            // Iterate over all fields and do the following
            //   - Add instance fields to the appropriate array (while maintaining the enumerated order)
            //   - Save the largest alignment we've seen
            foreach (var field in type.GetFields())
            {
                if (field.IsStatic)
                    continue;

                TypeDesc fieldType = field.FieldType;

                var fieldSizeAndAlignment = ComputeFieldSizeAndAlignment(fieldType, hasLayout, packingSize, out bool fieldLayoutAbiStable, out bool _, out bool _, out bool _);
                if (!fieldLayoutAbiStable)
                    layoutAbiStable = false;

                if (IsByValueClass(fieldType))
                {
                    // This block handles valuetypes which are not primitives or enums, it only has a meaningful effect, if the
                    // type has an alignment greater than pointer size.
                    largestAlignmentRequired = LayoutInt.Max(fieldSizeAndAlignment.Alignment, largestAlignmentRequired);
                    instanceValueClassFieldsArr[instanceValueClassFieldCount++] = field;
                }
                else
                {
                    // non-value-type (and primitive type) fields will add an alignment requirement of pointer size
                    // This alignment requirement will not be significant in the final alignment calculation unlesss the
                    // type is greater than the size of a single pointer.
                    //
                    // This does not account for types that are marked IsAlign8Candidate due to 8-byte fields
                    // but that is explicitly handled when we calculate the final alignment for the type.

                    // This behavior is extremely strange for primitive types, as it makes a struct with a single byte in it
                    // have 8 byte alignment, but that is the current implementation.

                    largestAlignmentRequired = LayoutInt.Max(new LayoutInt(context.Target.PointerSize), largestAlignmentRequired);

                    if (fieldType.IsGCPointer)
                    {
                        instanceGCPointerFieldsArr[instanceGCPointerFieldsCount++] = field;
                    }
                    else
                    {
                        Debug.Assert(fieldType.IsPrimitive || fieldType.IsPointer || fieldType.IsFunctionPointer || fieldType.IsEnum || fieldType.IsByRef);
                        int log2size = CalculateLog2(fieldSizeAndAlignment.Size.AsInt);
                        instanceNonGCPointerFieldsArr[log2size][instanceNonGCPointerFieldsCount[log2size]++] = field;

                        if (fieldType.IsPrimitive || fieldType.IsEnum)
                        {
                            // Handle alignment of long/ulong/double on ARM32
                            largestAlignmentRequired = LayoutInt.Max(context.Target.GetObjectAlignment(fieldSizeAndAlignment.Size), largestAlignmentRequired);
                        }
                    }
                }
            }

            bool requiresAlign8 = !largestAlignmentRequired.IsIndeterminate && context.Target.PointerSize == 4 && context.Target.GetObjectAlignment(largestAlignmentRequired).AsInt > 4;

            // For types inheriting from another type, field offsets continue on from where they left off
            // Base alignment is not always required, it's only applied when there's a version bubble boundary
            // between base type and the current type.
            LayoutInt cumulativeInstanceFieldPos = CalculateFieldBaseOffset(type, requiresAlign8, requiresAlignedBase: false);
            LayoutInt offsetBias = LayoutInt.Zero;

            // The following conditional statement mimics the behavior of MethodTableBuilder::PlaceInstanceFields;
            // the fundamental difference between CoreCLR native runtime and Crossgen2 regarding field placement is
            // that the native runtime doesn't count the method table pointer at the beginning of reference types as a 'field'
            // so that the first field in a class has offset 0 while its 'real' offset from the 'this' pointer is LayoutPointerSize.
            // On ARM32, native runtime employs a special logic internally calculating the field offsets relative to the 'this'
            // pointer (the Crossgen2 way) to ensure 8-alignment for longs and doubles as required by the ARM32 ISA. Please note
            // that for 16-alignment used by Vector128 this logic actually ensures that the fields are 16-misaligned
            // (they are 16-aligned after the 4-byte or 8-byte method table pointer).
            if (!type.IsValueType && cumulativeInstanceFieldPos != LayoutInt.Zero && type.Context.Target.Architecture != TargetArchitecture.ARM)
            {
                offsetBias = type.Context.Target.LayoutPointerSize;
                cumulativeInstanceFieldPos -= offsetBias;
            }

            // We've finished placing the fields into their appropriate arrays
            // The next optimization may place non-GC Pointers, so repurpose our
            // counter to keep track of the next non-GC Pointer that must be placed
            // for a given field size
            Array.Clear(instanceNonGCPointerFieldsCount, 0, instanceNonGCPointerFieldsCount.Length);

            // If the position is Indeterminate, proceed immediately to placing the fields
            // This avoids issues with Universal Generic Field layouts whose fields may have Indeterminate sizes or alignments
            if (!cumulativeInstanceFieldPos.IsIndeterminate)
            {
                // First, place small fields immediately after the parent field bytes if there are a number of field bytes that are not aligned
                // GC pointer fields and value class fields are not considered for this optimization
                int parentByteOffsetModulo = cumulativeInstanceFieldPos.AsInt % context.Target.PointerSize;
                if (parentByteOffsetModulo != 0)
                {
                    for (int i = 0; i < maxLog2Size; i++)
                    {
                        int j;

                        // Check if the position is aligned such that we could place a larger type
                        int offsetModulo = cumulativeInstanceFieldPos.AsInt % (1 << (i+1));
                        if (offsetModulo == 0)
                        {
                            continue;
                        }

                        // Check whether there are any bigger fields
                        // We must consider both GC Pointers and non-GC Pointers
                        for (j = i + 1; j <= maxLog2Size; j++)
                        {
                            // Check if there are any elements left to place of the given size
                            if (instanceNonGCPointerFieldsCount[j] < instanceNonGCPointerFieldsArr[j].Length
                                  || (j == log2PointerSize && instanceGCPointerFieldsArr.Length > 0))
                                break;
                        }

                        // Nothing to gain if there are no bigger fields
                        // (the subsequent loop will place fields from large to small fields)
                        if (j > maxLog2Size)
                            break;

                        // Check whether there are any small enough fields
                        // We must consider both GC Pointers and non-GC Pointers
                        for (j = i; j >= 0; j--)
                        {
                            if (instanceNonGCPointerFieldsCount[j] < instanceNonGCPointerFieldsArr[j].Length
                                  || (j == log2PointerSize && instanceGCPointerFieldsArr.Length > 0))
                                break;
                        }

                        // Nothing to do if there are no smaller fields
                        if (j < 0)
                            break;

                        // Go back and use the smaller field as filling
                        i = j;

                        // Assert that we have at least one field of this size
                        Debug.Assert(instanceNonGCPointerFieldsCount[i] < instanceNonGCPointerFieldsArr[i].Length
                                  || (i == log2PointerSize && instanceGCPointerFieldsArr.Length > 0));

                        // Avoid reordering of gc fields
                        // Exit if there are no more non-GC fields of this size (pointer size) to place
                        if (i == log2PointerSize)
                        {
                            if (instanceNonGCPointerFieldsCount[i] >= instanceNonGCPointerFieldsArr[i].Length)
                                break;
                        }

                        // Place the field
                        j = instanceNonGCPointerFieldsCount[i];
                        FieldDesc field = instanceNonGCPointerFieldsArr[i][j];
                        PlaceInstanceField(field, hasLayout, packingSize, offsets, ref cumulativeInstanceFieldPos, ref fieldOrdinal, offsetBias);

                        instanceNonGCPointerFieldsCount[i]++;
                    }
                }
            }

            // Next, place GC pointer fields and non-GC pointer fields
            // Starting with the largest-sized fields, place the GC pointer fields in order then place the non-GC pointer fields in order.
            // Once the largest-sized fields are placed, repeat with the next-largest-sized group of fields and continue.
            for (int i = maxLog2Size; i >= 0; i--)
            {
                // First, if we're placing the size that also corresponds to the pointer size, place GC pointer fields in order
                if (i == log2PointerSize)
                {
                    for (int j = 0; j < instanceGCPointerFieldsArr.Length; j++)
                    {
                        PlaceInstanceField(instanceGCPointerFieldsArr[j], hasLayout, packingSize, offsets, ref cumulativeInstanceFieldPos, ref fieldOrdinal, offsetBias);
                    }
                }

                // The start index will be the index that may have been increased in the previous optimization
                for (int j = instanceNonGCPointerFieldsCount[i]; j < instanceNonGCPointerFieldsArr[i].Length; j++)
                {
                    PlaceInstanceField(instanceNonGCPointerFieldsArr[i][j], hasLayout, packingSize, offsets, ref cumulativeInstanceFieldPos, ref fieldOrdinal, offsetBias);
                }
            }

            // Place value class fields last
            for (int i = 0; i < instanceValueClassFieldsArr.Length; i++)
            {
                // Align the cumulative field offset to the indeterminate value
                var fieldSizeAndAlignment = ComputeFieldSizeAndAlignment(instanceValueClassFieldsArr[i].FieldType, hasLayout, packingSize, out bool fieldLayoutAbiStable, out bool _, out bool _, out bool _);
                if (!fieldLayoutAbiStable)
                    layoutAbiStable = false;

                cumulativeInstanceFieldPos = AlignUpInstanceFieldOffset(cumulativeInstanceFieldPos, fieldSizeAndAlignment.Alignment, context.Target);
                offsets[fieldOrdinal] = new FieldAndOffset(instanceValueClassFieldsArr[i], cumulativeInstanceFieldPos + offsetBias);
                cumulativeInstanceFieldPos = checked(cumulativeInstanceFieldPos + fieldSizeAndAlignment.Size);

                fieldOrdinal++;
            }

            // The JITs like to copy full machine words,
            // so if the size is bigger than a void* round it up to minAlign
            // and if the size is smaller than void* round it up to next power of two
            LayoutInt minAlign;
            if (cumulativeInstanceFieldPos.IsIndeterminate)
            {
                minAlign = LayoutInt.Indeterminate;
            }
            else if (cumulativeInstanceFieldPos.AsInt > context.Target.PointerSize)
            {
                if (requiresAlign8)
                {
                    minAlign = new LayoutInt(8);
                }
                else if (type.ContainsGCPointers)
                {
                    minAlign = context.Target.LayoutPointerSize;
                }
                else
                {
                    minAlign = largestAlignmentRequired;
                }
            }
            else
            {
                minAlign = new LayoutInt(1);
                while (minAlign.AsInt < cumulativeInstanceFieldPos.AsInt)
                    minAlign = new LayoutInt(minAlign.AsInt * 2);
            }

            SizeAndAlignment instanceByteSizeAndAlignment;
            var instanceSizeAndAlignment = ComputeInstanceSize(type,
                cumulativeInstanceFieldPos + offsetBias,
                minAlign,
                classLayoutSize: 0,
                byteCount: out instanceByteSizeAndAlignment);

            if (type.IsInlineArray)
            {
                AdjustForInlineArray(type, numInstanceFields, ref instanceByteSizeAndAlignment, ref instanceSizeAndAlignment);
            }

            ComputedInstanceFieldLayout computedLayout = new ComputedInstanceFieldLayout
            {
                IsAutoLayoutOrHasAutoLayoutFields = true,
                IsInt128OrHasInt128Fields = hasInt128Field,
                IsVectorTOrHasVectorTFields = hasVectorTField,
            };
            computedLayout.FieldAlignment = instanceSizeAndAlignment.Alignment;
            computedLayout.FieldSize = instanceSizeAndAlignment.Size;
            computedLayout.ByteCountUnaligned = instanceByteSizeAndAlignment.Size;
            computedLayout.ByteCountAlignment = instanceByteSizeAndAlignment.Alignment;
            computedLayout.Offsets = offsets;
            computedLayout.LayoutAbiStable = layoutAbiStable;

            return computedLayout;
        }

        private static void PlaceInstanceField(FieldDesc field, bool hasLayout, int packingSize, FieldAndOffset[] offsets, ref LayoutInt instanceFieldPos, ref int fieldOrdinal, LayoutInt offsetBias)
        {
            var fieldSizeAndAlignment = ComputeFieldSizeAndAlignment(field.FieldType, hasLayout, packingSize, out bool _, out bool _, out bool _, out bool _);

            instanceFieldPos = AlignUpInstanceFieldOffset(instanceFieldPos, fieldSizeAndAlignment.Alignment, field.Context.Target);
            offsets[fieldOrdinal] = new FieldAndOffset(field, instanceFieldPos + offsetBias);
            instanceFieldPos = checked(instanceFieldPos + fieldSizeAndAlignment.Size);

            fieldOrdinal++;
        }

        // The aligned instance field bytes calculation here matches the calculation of CoreCLR MethodTable::GetAlignedNumInstanceFieldBytes
        // This will calculate the next multiple of 4 that is greater than or equal to the instance size
        private static LayoutInt GetAlignedNumInstanceFieldBytes(LayoutInt instanceSize)
        {
            uint inputSize = (uint) instanceSize.AsInt;
            uint result = (uint)(((inputSize + 3) & (~3)));
            return new LayoutInt((int) result);
        }

        private static int CalculateLog2(int size)
        {
            // Size must be a positive number
            Debug.Assert(size > 0);

            // Size must be a power of 2
            Debug.Assert( 0 == (size & (size - 1)));

            int log2size;
            for (log2size = 0; size > 1; log2size++)
            {
                size >>= 1;
            }

            return log2size;
        }

        private static bool IsByValueClass(TypeDesc type)
        {
            return type.IsValueType && !type.IsPrimitive && !type.IsEnum;
        }

        public LayoutInt CalculateFieldBaseOffset(MetadataType type, bool requiresAlign8, bool requiresAlignedBase)
        {
            LayoutInt cumulativeInstanceFieldPos = LayoutInt.Zero;

            if (!type.IsValueType && type.HasBaseType)
            {
                cumulativeInstanceFieldPos = type.BaseType.InstanceByteCountUnaligned;
                if (!cumulativeInstanceFieldPos.IsIndeterminate)
                {
                    if (requiresAlignedBase && type.BaseType.IsZeroSizedReferenceType && ((MetadataType)type.BaseType).HasLayout())
                    {
                        cumulativeInstanceFieldPos += LayoutInt.One;
                    }
                    AlignBaseOffsetIfNecessary(type, ref cumulativeInstanceFieldPos, requiresAlign8, requiresAlignedBase);
                }
            }

            return cumulativeInstanceFieldPos;
        }

        private static SizeAndAlignment ComputeFieldSizeAndAlignment(TypeDesc fieldType, bool hasLayout, int packingSize, out bool layoutAbiStable, out bool fieldTypeHasAutoLayout, out bool fieldTypeHasInt128Field, out bool fieldTypeHasVectorTField)
        {
            SizeAndAlignment result;
            layoutAbiStable = true;
            fieldTypeHasAutoLayout = true;
            fieldTypeHasInt128Field = false;
            fieldTypeHasVectorTField = false;

            if (fieldType.IsDefType)
            {
                if (fieldType.IsValueType)
                {
                    DefType defType = (DefType)fieldType;
                    result.Size = defType.InstanceFieldSize;
                    result.Alignment = defType.InstanceFieldAlignment;
                    layoutAbiStable = defType.LayoutAbiStable;
                    fieldTypeHasAutoLayout = defType.IsAutoLayoutOrHasAutoLayoutFields;
                    fieldTypeHasInt128Field = defType.IsInt128OrHasInt128Fields;
                    fieldTypeHasVectorTField = defType.IsVectorTOrHasVectorTFields;
                }
                else
                {
                    result.Size = fieldType.Context.Target.LayoutPointerSize;
                    result.Alignment = fieldType.Context.Target.LayoutPointerSize;
                }
            }
            else if (fieldType.IsArray)
            {
                // This could use InstanceFieldSize/Alignment (and those results should match what's here)
                // but, its more efficient to just assume pointer size instead of fulling processing
                // the instance field layout of fieldType here.
                result.Size = fieldType.Context.Target.LayoutPointerSize;
                result.Alignment = fieldType.Context.Target.LayoutPointerSize;
            }
            else
            {
                Debug.Assert(fieldType.IsPointer || fieldType.IsFunctionPointer || fieldType.IsByRef);
                result.Size = fieldType.Context.Target.LayoutPointerSize;
                result.Alignment = fieldType.Context.Target.LayoutPointerSize;
                fieldTypeHasAutoLayout = fieldType.IsByRef;
            }

            // For non-auto layouts, we need to respect tighter packing requests for alignment.
            if (hasLayout)
            {
                result.Alignment = LayoutInt.Min(result.Alignment, new LayoutInt(packingSize));
            }

            return result;
        }

        private static int ComputePackingSize(MetadataType type, ClassLayoutMetadata layoutMetadata)
        {
            if (layoutMetadata.PackingSize == 0)
                return type.Context.Target.MaximumAlignment;
            else
                return layoutMetadata.PackingSize;
        }

        private static SizeAndAlignment ComputeInstanceSize(MetadataType type, LayoutInt instanceSize, LayoutInt alignment, int classLayoutSize, out SizeAndAlignment byteCount)
        {
            SizeAndAlignment result;

            // Pad the length of structs to be 1 if they are empty so we have no zero-length structures
            if (type.IsValueType && instanceSize == LayoutInt.Zero)
            {
                instanceSize = LayoutInt.One;
            }

            TargetDetails target = type.Context.Target;

            if (classLayoutSize != 0)
            {
                LayoutInt parentSize;
                if (type.IsValueType)
                    parentSize = new LayoutInt(0);
                else
                    parentSize = type.BaseType.InstanceByteCountUnaligned;

                LayoutInt specifiedInstanceSize = parentSize + new LayoutInt(classLayoutSize);

                instanceSize = LayoutInt.Max(specifiedInstanceSize, instanceSize);
            }
            else
            {
                if (type.IsValueType)
                {
                    instanceSize = LayoutInt.AlignUp(instanceSize, alignment, target);
                }
            }

            if (type.IsValueType)
            {
                result.Size = instanceSize;
                result.Alignment = alignment;
            }
            else
            {
                result.Size = target.LayoutPointerSize;
                result.Alignment = target.LayoutPointerSize;
                if (type.HasBaseType)
                    alignment = LayoutInt.Max(alignment, type.BaseType.InstanceByteAlignment);
            }

            // Determine the alignment needed by the type when allocated
            // This is target specific, and not just pointer sized due to
            // 8 byte alignment requirements on ARM for longs and doubles
            alignment = target.GetObjectAlignment(alignment);

            byteCount.Size = instanceSize;
            byteCount.Alignment = alignment;

            return result;
        }

        public override ValueTypeShapeCharacteristics ComputeValueTypeShapeCharacteristics(DefType type)
        {
            if (!type.IsValueType)
                return ValueTypeShapeCharacteristics.None;

            return ComputeHomogeneousAggregateCharacteristic(type);
        }

        /// <summary>
        /// Identify whether a given type is a homogeneous floating-point aggregate. This code must be
        /// kept in sync with the CoreCLR runtime method EEClass::CheckForHFA, as of this change it
        /// can be found at
        /// https://github.com/dotnet/runtime/blob/1928cd2b65c04ebe6fe528d4ebb581e46f1fed47/src/coreclr/vm/class.cpp#L1567
        /// </summary>
        /// <param name="type">Type to analyze</param>
        /// <returns>HFA classification of the type parameter</returns>
        private static ValueTypeShapeCharacteristics ComputeHomogeneousAggregateCharacteristic(DefType type)
        {
            // Use this constant to make the code below more laconic
            const ValueTypeShapeCharacteristics NotHA = ValueTypeShapeCharacteristics.None;

            Debug.Assert(type.IsValueType);

            TargetArchitecture targetArch = type.Context.Target.Architecture;
            if ((targetArch != TargetArchitecture.ARM) && (targetArch != TargetArchitecture.ARM64))
                return NotHA;

            if (type.Context.Target.Abi == TargetAbi.NativeAotArmel)
                return NotHA;

            MetadataType metadataType = (MetadataType)type;
            int haElementSize = 0;

            switch (metadataType.Category)
            {
                // These are the primitive types that constitute a HFA type
                case TypeFlags.Single:
                    return ValueTypeShapeCharacteristics.Float32Aggregate;
                case TypeFlags.Double:
                    return ValueTypeShapeCharacteristics.Float64Aggregate;

                case TypeFlags.ValueType:
                    // Find the common HA element type if any
                    ValueTypeShapeCharacteristics haResultType = NotHA;
                    bool hasZeroOffsetField = false;

                    foreach (FieldDesc field in metadataType.GetFields())
                    {
                        if (field.IsStatic)
                            continue;

                        if (field.Offset == LayoutInt.Zero)
                        {
                            hasZeroOffsetField = true;
                        }

                        // If a field isn't a DefType, then this type cannot be a HA type
                        if (!(field.FieldType is DefType fieldType))
                            return NotHA;

                        // If a field isn't a HA type, then this type cannot be a HA type
                        ValueTypeShapeCharacteristics haFieldType = fieldType.ValueTypeShapeCharacteristics & ValueTypeShapeCharacteristics.AggregateMask;
                        if (haFieldType == NotHA)
                            return NotHA;

                        if (haResultType == NotHA)
                        {
                            // If we hadn't yet figured out what form of HA this type might be, we've now found one case
                            haResultType = haFieldType;

                            haElementSize = haResultType switch
                            {
                                ValueTypeShapeCharacteristics.Float32Aggregate => 4,
                                ValueTypeShapeCharacteristics.Float64Aggregate => 8,
                                ValueTypeShapeCharacteristics.Vector64Aggregate => 8,
                                ValueTypeShapeCharacteristics.Vector128Aggregate => 16,
                                _ => throw new ArgumentOutOfRangeException()
                            };
                        }
                        else if (haResultType != haFieldType)
                        {
                            // If we had already determined the possible HA type of the current type, but
                            // the field we've encountered is not of that type, then the current type cannot
                            // be a HA type.
                            return NotHA;
                        }

                        if (field.Offset.IsIndeterminate || field.Offset.AsInt % haElementSize != 0)
                        {
                            return NotHA;
                        }
                    }

                    // If the struct doesn't have a zero-offset field, it's not an HFA.
                    if (!hasZeroOffsetField)
                        return NotHA;

                    // Types which are indeterminate in field size are not considered to be HA
                    if (type.InstanceFieldSize.IsIndeterminate)
                        return NotHA;

                    // Note that we check the total size, but do not perform any checks on number of fields:
                    // - Type of fields can be HA valuetype itself.
                    // - Managed C++ HA valuetypes have just one <alignment member> of type float to signal that
                    //   the valuetype is HA and explicitly specified size.
                    int totalSize = type.InstanceFieldSize.AsInt;

                    if (totalSize % haElementSize != 0)
                        return NotHA;

                    // On ARM, HFAs can have a maximum of four fields regardless of whether those are float or double.
                    if (totalSize > haElementSize * type.Context.Target.MaxHomogeneousAggregateElementCount)
                        return NotHA;

                    // All the tests passed. This is a HA type.
                    return haResultType;
            }

            return NotHA;
        }

        public override bool ComputeIsUnsafeValueType(DefType type)
        {
            if (!type.IsValueType)
                return false;

            MetadataType metadataType = (MetadataType)type;
            if (metadataType.HasCustomAttribute("System.Runtime.CompilerServices", "UnsafeValueTypeAttribute"))
                return true;

            foreach (FieldDesc field in metadataType.GetFields())
            {
                if (field.IsStatic)
                    continue;

                TypeDesc fieldType = field.FieldType;
                if (!fieldType.IsValueType || fieldType.IsPrimitive)
                    continue;

                if (((DefType)fieldType).IsUnsafeValueType)
                    return true;
            }

            return false;
        }

        private struct SizeAndAlignment
        {
            public LayoutInt Size;
            public LayoutInt Alignment;
        }
    }
}
