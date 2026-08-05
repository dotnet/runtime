// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler.Wasm
{
    /// <summary>
    /// Instance field layout matching what crossgen2 computes, so that a struct size resolved here
    /// is the same size the compiler will encode into a wasm ABI signature.
    /// </summary>
    /// <remarks>
    /// This mirrors <c>ReadyToRunMetadataFieldLayoutAlgorithm.ComputeInstanceFieldLayout</c>. That type
    /// cannot be reused directly because it also implements ReadyToRun static field layout, which drags
    /// in the whole compilation-module-group and node-factory machinery. Only the instance side matters
    /// for struct sizes, and it is small enough to mirror exactly.
    ///
    /// <c>WasmLoweringParityTests</c> compares this against a real <c>ReadyToRunCompilerContext</c> over
    /// every value type in CoreLib, which is what keeps the mirroring honest if crossgen2 changes.
    /// </remarks>
    internal sealed class WasmMetadataFieldLayoutAlgorithm : MetadataFieldLayoutAlgorithm
    {
        protected override ComputedInstanceFieldLayout ComputeInstanceFieldLayout(MetadataType type, int numInstanceFields)
        {
            ClassLayoutMetadata layoutMetadata = type.GetClassLayout();
            return layoutMetadata.Kind switch
            {
                MetadataLayoutKind.CStruct => ComputeCStructFieldLayout(type, numInstanceFields),
                MetadataLayoutKind.CUnion => ComputeCUnionFieldLayout(type, numInstanceFields),
                MetadataLayoutKind.Explicit => ComputeExplicitFieldLayout(type, numInstanceFields, layoutMetadata),
                MetadataLayoutKind.Sequential when !type.ContainsGCPointers => ComputeSequentialFieldLayout(type, numInstanceFields, layoutMetadata),
                _ => ComputeAutoFieldLayout(type, numInstanceFields, layoutMetadata),
            };
        }

        protected override void PrepareRuntimeSpecificStaticFieldLayout(TypeSystemContext context, ref ComputedStaticFieldLayout layout)
        {
            layout.GcStatics.Size = context.Target.LayoutPointerSize;
            layout.ThreadGcStatics.Size = context.Target.LayoutPointerSize;
        }

        protected override void FinalizeRuntimeSpecificStaticFieldLayout(TypeSystemContext context, ref ComputedStaticFieldLayout layout)
        {
            if (layout.GcStatics.Size == context.Target.LayoutPointerSize)
            {
                layout.GcStatics.Size = LayoutInt.Zero;
            }
            if (layout.ThreadGcStatics.Size == context.Target.LayoutPointerSize)
            {
                layout.ThreadGcStatics.Size = LayoutInt.Zero;
            }
        }
    }
}
