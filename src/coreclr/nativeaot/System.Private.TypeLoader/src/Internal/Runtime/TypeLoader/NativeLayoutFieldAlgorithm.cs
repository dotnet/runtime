// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;
using System.Diagnostics;
using Internal.NativeFormat;
using System.Collections.Generic;
using Internal.Runtime.Augments;

namespace Internal.Runtime.TypeLoader
{
    /// <summary>
    /// Reads field layout based on native layout data
    /// information
    /// </summary>
    internal class NativeLayoutFieldAlgorithm : FieldLayoutAlgorithm
    {
        private NoMetadataFieldLayoutAlgorithm _noMetadataFieldLayoutAlgorithm = new NoMetadataFieldLayoutAlgorithm();
        private const int InstanceAlignmentEntry = 4;

        public override unsafe bool ComputeContainsGCPointers(DefType type)
        {
            if (type.IsTemplateCanonical())
            {
                return type.ComputeTemplate().RuntimeTypeHandle.ToEETypePtr()->HasGCPointers;
            }
            else
            {
                if (type.RetrieveRuntimeTypeHandleIfPossible())
                {
                    return type.RuntimeTypeHandle.ToEETypePtr()->HasGCPointers;
                }

                return type.GetOrCreateTypeBuilderState().InstanceGCLayout != null;
            }
        }

        public override ComputedInstanceFieldLayout ComputeInstanceLayout(DefType type, InstanceLayoutKind layoutKind)
        {
            if (!type.IsTemplateUniversal() && (layoutKind == InstanceLayoutKind.TypeOnly))
            {
                // Non universal generics can just use the template's layout
                DefType template = (DefType)type.ComputeTemplate();
                return _noMetadataFieldLayoutAlgorithm.ComputeInstanceLayout(template, InstanceLayoutKind.TypeOnly);
            }

            // Only needed for universal generics, or when looking up an offset for a field for a universal generic
            LowLevelList<LayoutInt> fieldOffsets;
            LayoutInt[] position = ComputeTypeSizeAndAlignment(type, FieldLoadState.Instance, out fieldOffsets);

            int numInstanceFields = 0;
            foreach (NativeLayoutFieldDesc field in type.NativeLayoutFields)
            {
                if (!field.IsStatic)
                {
                    numInstanceFields++;
                }
            }

            TargetDetails target = type.Context.Target;

            LayoutInt byteCountAlignment = position[InstanceAlignmentEntry];
            byteCountAlignment = target.GetObjectAlignment(byteCountAlignment);

            ComputedInstanceFieldLayout layout = new ComputedInstanceFieldLayout()
            {
                Offsets = new FieldAndOffset[numInstanceFields],
                ByteCountAlignment = byteCountAlignment,
                ByteCountUnaligned = position[(int)NativeFormat.FieldStorage.Instance],
            };

            if (!type.IsValueType)
            {
                layout.FieldAlignment = target.LayoutPointerSize;
                layout.FieldSize = target.LayoutPointerSize;
            }
            else
            {
                layout.FieldAlignment = position[InstanceAlignmentEntry];
                layout.FieldSize = LayoutInt.AlignUp(position[(int)NativeFormat.FieldStorage.Instance], layout.FieldAlignment, target);
            }

            int curInstanceField = 0;
            foreach (NativeLayoutFieldDesc field in type.NativeLayoutFields)
            {
                if (!field.IsStatic)
                {
                    layout.Offsets[curInstanceField] = new FieldAndOffset(field, fieldOffsets[curInstanceField]);
                    curInstanceField++;
                }
            }

            return layout;
        }

        public override ComputedStaticFieldLayout ComputeStaticFieldLayout(DefType type, StaticLayoutKind layoutKind)
        {
            if (!type.IsTemplateUniversal() && (layoutKind == StaticLayoutKind.StaticRegionSizes))
            {
                return ParseStaticRegionSizesFromNativeLayout(type);
            }

            LowLevelList<LayoutInt> fieldOffsets;
            LayoutInt[] position = ComputeTypeSizeAndAlignment(type, FieldLoadState.Statics, out fieldOffsets);

            int numStaticFields = 0;
            foreach (NativeLayoutFieldDesc field in type.NativeLayoutFields)
            {
                if (field.IsStatic)
                {
                    numStaticFields++;
                }
            }

            ComputedStaticFieldLayout layout = new ComputedStaticFieldLayout();

            layout.Offsets = new FieldAndOffset[numStaticFields];

            if (numStaticFields > 0)
            {
                layout.GcStatics = new StaticsBlock() { Size = position[(int)NativeFormat.FieldStorage.GCStatic], LargestAlignment = DefType.MaximumAlignmentPossible };
                layout.NonGcStatics = new StaticsBlock() { Size = position[(int)NativeFormat.FieldStorage.NonGCStatic], LargestAlignment = DefType.MaximumAlignmentPossible };
                layout.ThreadGcStatics = new StaticsBlock() { Size = position[(int)NativeFormat.FieldStorage.TLSStatic], LargestAlignment = DefType.MaximumAlignmentPossible };
                layout.ThreadNonGcStatics = new StaticsBlock() { Size = LayoutInt.Zero, LargestAlignment = LayoutInt.Zero };
            }

            int curStaticField = 0;
            foreach (NativeLayoutFieldDesc field in type.NativeLayoutFields)
            {
                if (field.IsStatic)
                {
                    layout.Offsets[curStaticField] = new FieldAndOffset(field, fieldOffsets[curStaticField]);
                    curStaticField++;
                }
            }

            return layout;
        }

        private static ComputedStaticFieldLayout ParseStaticRegionSizesFromNativeLayout(TypeDesc type)
        {
            LayoutInt nonGcDataSize = LayoutInt.Zero;
            LayoutInt gcDataSize = LayoutInt.Zero;
            LayoutInt threadDataSize = LayoutInt.Zero;

            TypeBuilderState state = type.GetOrCreateTypeBuilderState();
            NativeParser typeInfoParser = state.GetParserForNativeLayoutInfo();

            BagElementKind kind;
            while ((kind = typeInfoParser.GetBagElementKind()) != BagElementKind.End)
            {
                switch (kind)
                {
                    case BagElementKind.NonGcStaticDataSize:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.NonGcStaticDataSize");
                        // Use checked typecast to int to ensure there aren't any overflows/truncations (size value used in allocation of memory later)
                        nonGcDataSize = new LayoutInt(checked((int)typeInfoParser.GetUnsigned()));
                        break;

                    case BagElementKind.GcStaticDataSize:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.GcStaticDataSize");
                        // Use checked typecast to int to ensure there aren't any overflows/truncations (size value used in allocation of memory later)
                        gcDataSize = new LayoutInt(checked((int)typeInfoParser.GetUnsigned()));
                        break;

                    case BagElementKind.ThreadStaticDataSize:
                        TypeLoaderLogger.WriteLine("Found BagElementKind.ThreadStaticDataSize");
                        // Use checked typecast to int to ensure there aren't any overflows/truncations (size value used in allocation of memory later)
                        threadDataSize = new LayoutInt(checked((int)typeInfoParser.GetUnsigned()));
                        break;

                    default:
                        typeInfoParser.SkipInteger();
                        break;
                }
            }

            ComputedStaticFieldLayout staticLayout = new ComputedStaticFieldLayout()
            {
                GcStatics = new StaticsBlock() { Size = gcDataSize, LargestAlignment = DefType.MaximumAlignmentPossible },
                NonGcStatics = new StaticsBlock() { Size = nonGcDataSize, LargestAlignment = DefType.MaximumAlignmentPossible },
                Offsets = null, // We're not computing field offsets here, so return null
                ThreadGcStatics = new StaticsBlock() { Size = threadDataSize, LargestAlignment = DefType.MaximumAlignmentPossible },
                ThreadNonGcStatics = new StaticsBlock() { Size = LayoutInt.Zero, LargestAlignment = LayoutInt.Zero },
            };

            return staticLayout;
        }

        internal static void EnsureFieldLayoutLoadedForGenericType(DefType type)
        {
            if (type.NativeLayoutFields != null)
                return;

            if (!type.IsTemplateUniversal())
            {
                // We can hit this case where the template of type in question is not a universal canonical type.
                // Example:
                //  BaseType<T> { ... }
                //  DerivedType<T, U> : BaseType<T> { ... }
                // and an instantiation like DerivedType<string, int>. In that case, BaseType<string> will have a non-universal
                // template type, and requires special handling to compute its size and field layout.
                EnsureFieldLayoutLoadedForNonUniversalType(type);
            }
            else
            {
                TypeBuilderState state = type.GetOrCreateTypeBuilderState();
                NativeParser typeInfoParser = state.GetParserForNativeLayoutInfo();
                NativeParser fieldLayoutParser = typeInfoParser.GetParserForBagElementKind(BagElementKind.FieldLayout);
                EnsureFieldLayoutLoadedForUniversalType(type, state.NativeLayoutInfo.LoadContext, fieldLayoutParser);
            }
        }

        private static void EnsureFieldLayoutLoadedForUniversalType(DefType type, NativeLayoutInfoLoadContext loadContext, NativeParser fieldLayoutParser)
        {
            Debug.Assert(type.HasInstantiation);
            Debug.Assert(type.ComputeTemplate().IsCanonicalSubtype(CanonicalFormKind.Universal));

            if (type.NativeLayoutFields != null)
                return;

            type.NativeLayoutFields = ParseFieldLayout(type, loadContext, fieldLayoutParser);
        }

        private static void EnsureFieldLayoutLoadedForNonUniversalType(DefType type)
        {
            Debug.Assert(type.HasInstantiation);
            Debug.Assert(!type.ComputeTemplate().IsCanonicalSubtype(CanonicalFormKind.Universal));

            if (type.NativeLayoutFields != null)
                return;

            // Look up the universal template for this type.  Only the universal template has field layout
            // information, so we have to use it to parse the field layout.
            NativeLayoutInfoLoadContext universalLayoutLoadContext;
            NativeParser typeInfoParser = type.GetOrCreateTypeBuilderState().GetParserForUniversalNativeLayoutInfo(out universalLayoutLoadContext, out _);

            if (typeInfoParser.IsNull)
                throw new TypeBuilder.MissingTemplateException();

            // Now parse that layout into the NativeLayoutFields array.
            NativeParser fieldLayoutParser = typeInfoParser.GetParserForBagElementKind(BagElementKind.FieldLayout);
            type.NativeLayoutFields = ParseFieldLayout(type, universalLayoutLoadContext, fieldLayoutParser);
        }

        private static NativeLayoutFieldDesc[] ParseFieldLayout(DefType owningType,
            NativeLayoutInfoLoadContext nativeLayoutInfoLoadContext, NativeParser fieldLayoutParser)
        {
            if (fieldLayoutParser.IsNull)
                return Empty<NativeLayoutFieldDesc>.Array;

            uint numFields = fieldLayoutParser.GetUnsigned();
            var fields = new NativeLayoutFieldDesc[numFields];

            for (int i = 0; i < numFields; i++)
            {
                TypeDesc fieldType = nativeLayoutInfoLoadContext.GetType(ref fieldLayoutParser);
                NativeFormat.FieldStorage storage = (NativeFormat.FieldStorage)fieldLayoutParser.GetUnsigned();
                fields[i] = new NativeLayoutFieldDesc(owningType, fieldType, storage);
            }

            return fields;
        }

        /// <summary>
        /// Determine the state of things before we start processing the fields of a specific type.
        /// This will initialize the state to be aware of the size/characteristics of base types,
        /// and whether or not this type is a valuetype.
        /// </summary>
        /// <param name="type">Type we are computing layout for</param>
        /// <param name="initialSize">What the initial Instance size should be</param>
        /// <param name="alignRequired">What is the basic alignment requirement of the base type or 1 if there is no base type to consider</param>
        internal static void ComputeTypeSizeBeforeFields(TypeDesc type, out LayoutInt initialSize, out LayoutInt alignRequired)
        {
            // Account for the MethodTable pointer in objects...
            initialSize = new LayoutInt(IntPtr.Size);
            alignRequired = LayoutInt.One;

            if (type.IsValueType)
            {
                // ...unless the type is a ValueType which doesn't have the MethodTable pointer.
                initialSize = LayoutInt.Zero;
            }
            else if (type.BaseType != null)
            {
                // If there is a base type, use the initialSize and alignRequired from that
                DefType baseType = type.BaseType;
                initialSize = baseType.InstanceByteCountUnaligned;
                alignRequired = baseType.InstanceByteAlignment;
            }
        }

        /// <summary>
        /// While computing layout, we don't generally compute the full field information. This function is used to
        /// gate how much of field layout to run
        /// </summary>
        /// <param name="fieldStorage">the conceptual location of the field</param>
        /// <param name="loadRequested">what sort of load was requested</param>
        /// <returns></returns>
        internal static bool ShouldProcessField(NativeFormat.FieldStorage fieldStorage, FieldLoadState loadRequested)
        {
            if (fieldStorage == (int)NativeFormat.FieldStorage.Instance)
            {
                // Make sure we wanted to load instance fields.
                if ((loadRequested & FieldLoadState.Instance) == FieldLoadState.None)
                    return false;
            }
            else if ((loadRequested & FieldLoadState.Statics) == FieldLoadState.None)
            {
                // Otherwise the field is a static, and we only want instance fields.
                return false;
            }

            return true;
        }

        // The layout algorithm should probably compute results and let the caller set things
        internal unsafe LayoutInt[] ComputeTypeSizeAndAlignment(TypeDesc type, FieldLoadState loadRequested, out LowLevelList<LayoutInt> fieldOffsets)
        {
            fieldOffsets = null;
            TypeLoaderLogger.WriteLine("Laying out type " + type.ToString() + ". IsValueType: " + (type.IsValueType ? "true" : "false") + ". LoadRequested = " + ((int)loadRequested).LowLevelToString());

            Debug.Assert(loadRequested != FieldLoadState.None);
            Debug.Assert(type is ArrayType || (type is DefType && ((DefType)type).HasInstantiation));

            bool isArray = type is ArrayType;

            LayoutInt[] position = new LayoutInt[5];
            LayoutInt alignRequired = LayoutInt.One;

            if ((loadRequested & FieldLoadState.Instance) == FieldLoadState.Instance)
            {
                ComputeTypeSizeBeforeFields(type, out position[(int)NativeFormat.FieldStorage.Instance], out alignRequired);
            }

            if (!isArray)
            {
                // Once this is done, the NativeLayoutFields on the type are initialized
                EnsureFieldLayoutLoadedForGenericType((DefType)type);
                Debug.Assert(type.NativeLayoutFields != null);
            }

            int instanceFields = 0;

            if (!isArray && type.NativeLayoutFields.Length > 0)
            {
                fieldOffsets = new LowLevelList<LayoutInt>(type.NativeLayoutFields.Length);
                for (int i = 0; i < type.NativeLayoutFields.Length; i++)
                {
                    TypeDesc fieldType = type.NativeLayoutFields[i].FieldType;
                    int fieldStorage = (int)type.NativeLayoutFields[i].FieldStorage;

                    if (!ShouldProcessField((NativeFormat.FieldStorage)fieldStorage, loadRequested))
                        continue;

                    // For value types, we will attempt to get the size and alignment from
                    // the runtime if possible, otherwise GetFieldSizeAndAlignment will
                    // recurse to lay out nested struct fields.
                    LayoutInt alignment;
                    LayoutInt size;
                    GetFieldSizeAlignment(fieldType, out size, out alignment);

                    Debug.Assert(alignment.AsInt > 0);

                    if (fieldStorage == (int)NativeFormat.FieldStorage.Instance)
                    {
                        instanceFields++;

                        // Ensure alignment of type is sufficient for this field
                        alignRequired = LayoutInt.Max(alignRequired, alignment);
                    }

                    position[fieldStorage] = LayoutInt.AlignUp(position[fieldStorage], alignment, type.Context.Target);
                    TypeLoaderLogger.WriteLine(" --> Field type " + fieldType.ToString() +
                        " storage " + ((uint)(type.NativeLayoutFields[i].FieldStorage)).LowLevelToString() +
                        " offset " + position[fieldStorage].LowLevelToString() +
                        " alignment " + alignment.LowLevelToString());

                    fieldOffsets.Add(position[fieldStorage]);
                    position[fieldStorage] += size;
                }
            }

            // Pad the length of structs to be 1 if they are empty so we have no zero-length structures
            if ((position[(int)NativeFormat.FieldStorage.Instance] == LayoutInt.Zero) && type.IsValueType)
                position[(int)NativeFormat.FieldStorage.Instance] = LayoutInt.One;

            Debug.Assert(alignRequired == new LayoutInt(1) ||
                         alignRequired == new LayoutInt(2) ||
                         alignRequired == new LayoutInt(4) ||
                         alignRequired == new LayoutInt(8));

            position[InstanceAlignmentEntry] = alignRequired;

            return position;
        }

        internal void GetFieldSizeAlignment(TypeDesc fieldType, out LayoutInt size, out LayoutInt alignment)
        {
            Debug.Assert(!fieldType.IsCanonicalSubtype(CanonicalFormKind.Any));

            // All reference and array types are pointer-sized
            if (!fieldType.IsValueType)
            {
                size = new LayoutInt(IntPtr.Size);
                alignment = new LayoutInt(IntPtr.Size);
                return;
            }

            // Is this a type that already exists? If so, get its size from the MethodTable directly
            if (fieldType.RetrieveRuntimeTypeHandleIfPossible())
            {
                unsafe
                {
                    MethodTable* MethodTable = fieldType.RuntimeTypeHandle.ToEETypePtr();
                    size = new LayoutInt((int)MethodTable->ValueTypeSize);
                    alignment = new LayoutInt(MethodTable->FieldAlignmentRequirement);
                    return;
                }
            }

            // The type of the field must be a generic valuetype that is dynamically being constructed
            Debug.Assert(fieldType.IsValueType);
            DefType fieldDefType = (DefType)fieldType;

            TypeBuilderState state = fieldType.GetOrCreateTypeBuilderState();

            size = fieldDefType.InstanceFieldSize;
            alignment = fieldDefType.InstanceFieldAlignment;
        }

        public override unsafe ValueTypeShapeCharacteristics ComputeValueTypeShapeCharacteristics(DefType type)
        {
            // Use this constant to make the code below more laconic
            const ValueTypeShapeCharacteristics NotHA = ValueTypeShapeCharacteristics.None;

            Debug.Assert(type.IsValueType);

            TargetArchitecture targetArch = type.Context.Target.Architecture;
            if ((targetArch != TargetArchitecture.ARM) && (targetArch != TargetArchitecture.ARM64))
                return NotHA;

            if (!type.IsValueType)
                return NotHA;

            // There is no reason to compute the entire field layout for the HA type/flag if
            // the template type is not a universal generic type (information stored in rare flags on the MethodTable)
            TypeDesc templateType = type.ComputeTemplate(false);
            if (templateType != null && !templateType.IsCanonicalSubtype(CanonicalFormKind.Universal))
            {
                MethodTable* pEETemplate = templateType.GetRuntimeTypeHandle().ToEETypePtr();
                if (!pEETemplate->IsHFA)
                    return NotHA;

                if (pEETemplate->RequiresAlign8)
                    return ValueTypeShapeCharacteristics.Float64Aggregate;
                else
                    return ValueTypeShapeCharacteristics.Float32Aggregate;
            }

            // Once this is done, the NativeLayoutFields on the type are initialized
            EnsureFieldLayoutLoadedForGenericType((DefType)type);
            Debug.Assert(type.NativeLayoutFields != null);

            // Empty types are not HA
            if (type.NativeLayoutFields.Length == 0)
                return NotHA;

            // Find the common HA element type if any
            ValueTypeShapeCharacteristics haResultType = NotHA;

            for (int i = 0; i < type.NativeLayoutFields.Length; i++)
            {
                TypeDesc fieldType = type.NativeLayoutFields[i].FieldType;
                if (type.NativeLayoutFields[i].FieldStorage != NativeFormat.FieldStorage.Instance)
                    continue;

                // If a field isn't a DefType, then this type cannot be a HA type
                if (!(fieldType is DefType fieldDefType))
                    return NotHA;

                // HA types cannot contain non-HA types
                ValueTypeShapeCharacteristics haFieldType = fieldDefType.ValueTypeShapeCharacteristics & ValueTypeShapeCharacteristics.AggregateMask;
                if (haFieldType == NotHA)
                    return NotHA;

                if (haResultType == NotHA)
                    haResultType = haFieldType;
                else if (haResultType != haFieldType)
                    return NotHA; // If the field doesn't have the same HA type as the one we've looked at before, the type cannot be HA
            }

            // If we didn't find any instance fields, then this can't be a HA type
            if (haResultType == NotHA)
                return NotHA;

            int haElementSize = haResultType switch
            {
                ValueTypeShapeCharacteristics.Float32Aggregate => 4,
                ValueTypeShapeCharacteristics.Float64Aggregate => 8,
                ValueTypeShapeCharacteristics.Vector64Aggregate => 8,
                ValueTypeShapeCharacteristics.Vector128Aggregate => 16,
                _ => throw new ArgumentOutOfRangeException()
            };

            // Note that we check the total size, but do not perform any checks on number of fields:
            // - Type of fields can be HA valuetype itself
            // - Managed C++ HFA valuetypes have just one <alignment member> of type float to signal that
            //   the valuetype is HFA and explicitly specified size
            int maxSize = haElementSize * type.Context.Target.MaxHomogeneousAggregateElementCount;
            if (type.InstanceFieldSize.AsInt > maxSize)
                return NotHA;

            return haResultType;
        }

        public override bool ComputeIsUnsafeValueType(DefType type)
        {
            throw new NotSupportedException();
        }
    }
}
