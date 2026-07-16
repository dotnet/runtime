// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Provides method bodies for System.Runtime.CompilerServices.RuntimeHelpers intrinsics.
    /// </summary>
    public static class RuntimeHelpersIntrinsics
    {
        public static MethodIL EmitIL(MethodDesc method)
        {
            Debug.Assert(((MetadataType)method.OwningType).Name == "RuntimeHelpers"u8);

            // All the methods handled below are per-instantiation generic methods
            if (method.Instantiation.Length != 1 || method.IsTypicalMethodDefinition)
                return null;

            TypeDesc elementType = method.Instantiation[0];

            // Fallback to non-intrinsic implementation for universal generics
            if (elementType.IsCanonicalSubtype(CanonicalFormKind.Universal))
                return null;

            bool result;
            if (method.Name == "IsBitwiseEquatable"u8)
            {
                // The runtime and the ILC share a single determination of what is bitwise-equatable.
                result = ComparerIntrinsics.IsBitwiseEquatable(elementType);
            }
            else
            {
                return null;
            }

            ILOpcode opcode = result ? ILOpcode.ldc_i4_1 : ILOpcode.ldc_i4_0;

            return new ILStubMethodIL(method, new byte[] { (byte)opcode, (byte)ILOpcode.ret }, Array.Empty<LocalVariableDefinition>(), Array.Empty<object>());
        }
    }
}
