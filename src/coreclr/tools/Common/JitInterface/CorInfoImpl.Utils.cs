// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem.Interop;
using Internal.TypeSystem;

namespace Internal.JitInterface
{
    internal sealed unsafe partial class CorInfoImpl
    {
        /// <summary>
        /// Managed implementation of CEEInfo::getClassAlignmentRequirementStatic
        /// </summary>
        public static int GetClassAlignmentRequirementStatic(DefType type)
        {
            int alignment = type.Context.Target.PointerSize;

            if (type is MetadataType metadataType && !metadataType.IsAutoLayout)
            {
                if (metadataType.IsSequentialLayout ||
                    MarshalUtils.IsBlittableType(metadataType))
                {
                    alignment = metadataType.InstanceFieldAlignment.AsInt;
                }
            }
            if (type.Context.Target.SupportsAlign8 &&
                alignment < 8 && type.RequiresAlign8())
            {
                // If the structure contains 64-bit primitive fields and the platform requires 8-byte alignment for
                // such fields then make sure we return at least 8-byte alignment. Note that it's technically possible
                // to create unmanaged APIs that take unaligned structures containing such fields and this
                // unconditional alignment bump would cause us to get the calling convention wrong on platforms such
                // as ARM. If we see such cases in the future we'd need to add another control (such as an alignment
                // property for the StructLayout attribute or a marshaling directive attribute for p/invoke arguments)
                // that allows more precise control. For now we'll go with the likely scenario.
                alignment = 8;
            }

            return alignment;
        }
    }
}
