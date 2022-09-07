// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    public static partial class HardwareIntrinsicHelpers
    {
        /// <summary>
        /// Gets a value indicating whether this is a hardware intrinsic on the platform that we're compiling for.
        /// </summary>
        public static bool IsHardwareIntrinsic(MethodDesc method)
        {
            return !string.IsNullOrEmpty(InstructionSetSupport.GetHardwareIntrinsicId(method.Context.Target.Architecture, method.OwningType));
        }
    }
}
