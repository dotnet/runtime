// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.TypeSystem;

namespace TypeSystemTests
{
    class TestMetadataFieldLayoutAlgorithm : MetadataFieldLayoutAlgorithm
    {
        protected override void PrepareRuntimeSpecificStaticFieldLayout(TypeSystemContext context, ref ComputedStaticFieldLayout layout)
        {
            // GC statics start with a pointer to the "MethodTable" that signals the size and GCDesc to the GC
            layout.GcStatics.Size = context.Target.LayoutPointerSize;
            layout.ThreadGcStatics.Size = context.Target.LayoutPointerSize;
        }

        protected override void FinalizeRuntimeSpecificStaticFieldLayout(TypeSystemContext context, ref ComputedStaticFieldLayout layout)
        {
            // If the size of GCStatics is equal to the size set in PrepareRuntimeSpecificStaticFieldLayout, we
            // don't have any GC statics
            if (layout.GcStatics.Size == context.Target.LayoutPointerSize)
            {
                layout.GcStatics.Size = LayoutInt.Zero;
            }
            if (layout.ThreadGcStatics.Size == context.Target.LayoutPointerSize)
            {
                layout.ThreadGcStatics.Size = LayoutInt.Zero;
            }
        }

        protected override ComputedInstanceFieldLayout ComputeInstanceFieldLayout(MetadataType type, int numInstanceFields)
        {
            if (type.IsExplicitLayout)
            {
                return ComputeExplicitFieldLayout(type, numInstanceFields);
            }
            else if (type.IsSequentialLayout || type.IsEnum)
            {
                return ComputeSequentialFieldLayout(type, numInstanceFields);
            }
            else
            {
                return ComputeAutoFieldLayout(type, numInstanceFields);
            }
        }
    }
}
