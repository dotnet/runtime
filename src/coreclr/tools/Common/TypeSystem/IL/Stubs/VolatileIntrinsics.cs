// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Provides method bodies for System.Threading.Volatile intrinsics.
    /// </summary>
    public static class VolatileIntrinsics
    {
        public static MethodIL EmitIL(MethodDesc method)
        {
            Debug.Assert(((MetadataType)method.OwningType).Name == "Volatile");

            bool isRead = method.Name == "Read";
            if (!isRead && method.Name != "Write")
                return null;

            // All interesting methods have a signature that starts with `ref location`
            if (method.Signature.Length == 0 || !method.Signature[0].IsByRef)
                return null;

            ILOpcode opcode;
            switch (((ByRefType)method.Signature[0]).ParameterType.Category)
            {
                case TypeFlags.SignatureMethodVariable:
                    opcode = isRead ? ILOpcode.ldind_ref : ILOpcode.stind_ref;
                    break;

                case TypeFlags.Boolean:
                case TypeFlags.SByte:
                    opcode = isRead ? ILOpcode.ldind_i1 : ILOpcode.stind_i1;
                    break;
                case TypeFlags.Byte:
                    opcode = isRead ? ILOpcode.ldind_u1 : ILOpcode.stind_i1;
                    break;
                case TypeFlags.Int16:
                    opcode = isRead ? ILOpcode.ldind_i2 : ILOpcode.stind_i2;
                    break;
                case TypeFlags.UInt16:
                    opcode = isRead ? ILOpcode.ldind_u2 : ILOpcode.stind_i2;
                    break;
                case TypeFlags.Int32:
                    opcode = isRead ? ILOpcode.ldind_i4 : ILOpcode.stind_i4;
                    break;
                case TypeFlags.UInt32:
                    opcode = isRead ? ILOpcode.ldind_u4 : ILOpcode.stind_i4;
                    break;
                case TypeFlags.IntPtr:
                case TypeFlags.UIntPtr:
                    opcode = isRead ? ILOpcode.ldind_i : ILOpcode.stind_i;
                    break;
                case TypeFlags.Single:
                    opcode = isRead ? ILOpcode.ldind_r4 : ILOpcode.stind_r4;
                    break;

                //
                // Ordinary volatile loads and stores only guarantee atomicity for pointer-sized (or smaller) data.
                // So, on 32-bit platforms we must use Interlocked operations instead for the 64-bit types.
                // The implementation in CoreLib already does this, so we will only substitute a new
                // IL body if we're running on a 64-bit platform.
                //
                case TypeFlags.Int64 when method.Context.Target.PointerSize == 8:
                case TypeFlags.UInt64 when method.Context.Target.PointerSize == 8:
                    opcode = isRead ? ILOpcode.ldind_i8 : ILOpcode.stind_i8;
                    break;
                case TypeFlags.Double when method.Context.Target.PointerSize == 8:
                    opcode = isRead ? ILOpcode.ldind_r8 : ILOpcode.stind_r8;
                    break;
                default:
                    return null;
            }

            byte[] ilBytes;
            if (isRead)
            {
                ilBytes = new byte[]
                {
                    (byte)ILOpcode.ldarg_0,
                    (byte)ILOpcode.prefix1, unchecked((byte)ILOpcode.volatile_),
                    (byte)opcode,
                    (byte)ILOpcode.ret
                };
            }
            else
            {
                ilBytes = new byte[]
                {
                    (byte)ILOpcode.ldarg_0,
                    (byte)ILOpcode.ldarg_1,
                    (byte)ILOpcode.prefix1, unchecked((byte)ILOpcode.volatile_),
                    (byte)opcode,
                    (byte)ILOpcode.ret
                };
            }

            return new ILStubMethodIL(method, ilBytes, Array.Empty<LocalVariableDefinition>(), null);
        }
    }
}
