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
            Debug.Assert(type.IsTemplateCanonical());
            return type.ComputeTemplate().RuntimeTypeHandle.ToEETypePtr()->HasGCPointers;
        }

        public override ComputedInstanceFieldLayout ComputeInstanceLayout(DefType type, InstanceLayoutKind layoutKind)
        {
            Debug.Assert(!type.IsTemplateUniversal() && (layoutKind == InstanceLayoutKind.TypeOnly));
            DefType template = (DefType)type.ComputeTemplate();
            return _noMetadataFieldLayoutAlgorithm.ComputeInstanceLayout(template, InstanceLayoutKind.TypeOnly);
        }

        public override ComputedStaticFieldLayout ComputeStaticFieldLayout(DefType type, StaticLayoutKind layoutKind)
        {
            Debug.Assert(!type.IsTemplateUniversal() && (layoutKind == StaticLayoutKind.StaticRegionSizes));
            return ParseStaticRegionSizesFromNativeLayout(type);
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
            Debug.Assert(templateType != null && !templateType.IsCanonicalSubtype(CanonicalFormKind.Universal));
            MethodTable* pEETemplate = templateType.GetRuntimeTypeHandle().ToEETypePtr();
            if (!pEETemplate->IsHFA)
                return NotHA;

            if (pEETemplate->RequiresAlign8)
                return ValueTypeShapeCharacteristics.Float64Aggregate;
            else
                return ValueTypeShapeCharacteristics.Float32Aggregate;
        }

        public override bool ComputeIsUnsafeValueType(DefType type)
        {
            throw new NotSupportedException();
        }
    }
}
