// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal static class InvokeUtils
    {
        // This method is similar to the NativeAot method ConvertOrWidenPrimitivesEnumsAndPointersIfPossible().
        public static object ConvertOrWiden(RuntimeType srcType, object srcObject, RuntimeType dstType, CorElementType dstElementType)
        {
            object dstObject;

            if (dstType.IsPointer || dstType.IsFunctionPointer)
            {
                if (TryConvertPointer(srcObject, out object? dstPtr))
                {
                    return dstPtr;
                }

                Debug.Fail($"Unexpected CorElementType: {dstElementType}. Not a valid widening target.");
                throw new NotSupportedException();
            }

            switch (dstElementType)
            {
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    dstObject = Convert.ToBoolean(srcObject);
                    break;

                case CorElementType.ELEMENT_TYPE_CHAR:
                    char charValue = Convert.ToChar(srcObject);
                    dstObject = dstType.IsEnum ? Enum.ToObject(dstType, charValue) : charValue;
                    break;

                case CorElementType.ELEMENT_TYPE_I1:
                    sbyte sbyteValue = Convert.ToSByte(srcObject);
                    dstObject = dstType.IsEnum ? Enum.ToObject(dstType, sbyteValue) : sbyteValue;
                    break;

                case CorElementType.ELEMENT_TYPE_I2:
                    short shortValue = Convert.ToInt16(srcObject);
                    dstObject = dstType.IsEnum ? Enum.ToObject(dstType, shortValue) : shortValue;
                    break;

                case CorElementType.ELEMENT_TYPE_I4:
                    int intValue = Convert.ToInt32(srcObject);
                    dstObject = dstType.IsEnum ? Enum.ToObject(dstType, intValue) : intValue;
                    break;

                case CorElementType.ELEMENT_TYPE_I8:
                    long longValue = Convert.ToInt64(srcObject);
                    dstObject = dstType.IsEnum ? Enum.ToObject(dstType, longValue) : longValue;
                    break;

                case CorElementType.ELEMENT_TYPE_U1:
                    byte byteValue = Convert.ToByte(srcObject);
                    dstObject = dstType.IsEnum ? Enum.ToObject(dstType, byteValue) : byteValue;
                    break;

                case CorElementType.ELEMENT_TYPE_U2:
                    ushort ushortValue = Convert.ToUInt16(srcObject);
                    dstObject = dstType.IsEnum ? Enum.ToObject(dstType, ushortValue) : ushortValue;
                    break;

                case CorElementType.ELEMENT_TYPE_U4:
                    uint uintValue = Convert.ToUInt32(srcObject);
                    dstObject = dstType.IsEnum ? Enum.ToObject(dstType, uintValue) : uintValue;
                    break;

                case CorElementType.ELEMENT_TYPE_U8:
                    ulong ulongValue = Convert.ToUInt64(srcObject);
                    dstObject = dstType.IsEnum ? Enum.ToObject(dstType, (long)ulongValue) : ulongValue;
                    break;

                case CorElementType.ELEMENT_TYPE_R4:
                    if (srcType == typeof(char))
                    {
                        dstObject = (float)(char)srcObject;
                    }
                    else
                    {
                        dstObject = Convert.ToSingle(srcObject);
                    }
                    break;

                case CorElementType.ELEMENT_TYPE_R8:
                    if (srcType == typeof(char))
                    {
                        dstObject = (double)(char)srcObject;
                    }
                    else
                    {
                        dstObject = Convert.ToDouble(srcObject);
                    }
                    break;

                default:
                    Debug.Fail($"Unexpected CorElementType: {dstElementType}. Not a valid widening target.");
                    throw new NotSupportedException();
            }

            Debug.Assert(dstObject != null);
            Debug.Assert(dstObject.GetType() == dstType);
            return dstObject;
        }

        private static bool TryConvertPointer(object srcObject, [NotNullWhen(true)] out object? dstPtr)
        {
            if (srcObject is IntPtr or UIntPtr)
            {
                dstPtr = srcObject;
                return true;
            }

            // The source pointer should already have been converted to an IntPtr.
            Debug.Assert(srcObject is not Pointer);

            dstPtr = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Two callers, one of them is potentially perf sensitive
        public static void PrimitiveWiden(ref byte srcElement, ref byte destElement, CorElementType srcElType, CorElementType destElType)
        {
            switch (srcElType)
            {
                case CorElementType.ELEMENT_TYPE_U1:
                    switch (destElType)
                    {
                        case CorElementType.ELEMENT_TYPE_CHAR:
                        case CorElementType.ELEMENT_TYPE_I2:
                        case CorElementType.ELEMENT_TYPE_U2:
                            Unsafe.As<byte, ushort>(ref destElement) = srcElement; break;
                        case CorElementType.ELEMENT_TYPE_I4:
                        case CorElementType.ELEMENT_TYPE_U4:
                            Unsafe.As<byte, uint>(ref destElement) = srcElement; break;
                        case CorElementType.ELEMENT_TYPE_I8:
                        case CorElementType.ELEMENT_TYPE_U8:
                            Unsafe.As<byte, ulong>(ref destElement) = srcElement; break;
                        case CorElementType.ELEMENT_TYPE_R4:
                            Unsafe.As<byte, float>(ref destElement) = srcElement; break;
                        case CorElementType.ELEMENT_TYPE_R8:
                            Unsafe.As<byte, double>(ref destElement) = srcElement; break;
                        default:
                            Debug.Fail("Expected to be unreachable"); break;
                    }
                    break;

                case CorElementType.ELEMENT_TYPE_I1:
                    switch (destElType)
                    {
                        case CorElementType.ELEMENT_TYPE_I2:
                            Unsafe.As<byte, short>(ref destElement) = Unsafe.As<byte, sbyte>(ref srcElement); break;
                        case CorElementType.ELEMENT_TYPE_I4:
                            Unsafe.As<byte, int>(ref destElement) = Unsafe.As<byte, sbyte>(ref srcElement); break;
                        case CorElementType.ELEMENT_TYPE_I8:
                            Unsafe.As<byte, long>(ref destElement) = Unsafe.As<byte, sbyte>(ref srcElement); break;
                        case CorElementType.ELEMENT_TYPE_R4:
                            Unsafe.As<byte, float>(ref destElement) = Unsafe.As<byte, sbyte>(ref srcElement); break;
                        case CorElementType.ELEMENT_TYPE_R8:
                            Unsafe.As<byte, double>(ref destElement) = Unsafe.As<byte, sbyte>(ref srcElement); break;
                        default:
                            Debug.Fail("Array.Copy from I1 to another type hit unsupported widening conversion"); break;
                    }
                    break;

                case CorElementType.ELEMENT_TYPE_U2:
                case CorElementType.ELEMENT_TYPE_CHAR:
                    switch (destElType)
                    {
                        case CorElementType.ELEMENT_TYPE_U2:
                        case CorElementType.ELEMENT_TYPE_CHAR:
                            // U2 and CHAR are identical in conversion
                            Unsafe.As<byte, ushort>(ref destElement) = Unsafe.As<byte, ushort>(ref srcElement); break;
                        case CorElementType.ELEMENT_TYPE_I4:
                        case CorElementType.ELEMENT_TYPE_U4:
                            Unsafe.As<byte, uint>(ref destElement) = Unsafe.As<byte, ushort>(ref srcElement); break;
                        case CorElementType.ELEMENT_TYPE_I8:
                        case CorElementType.ELEMENT_TYPE_U8:
                            Unsafe.As<byte, ulong>(ref destElement) = Unsafe.As<byte, ushort>(ref srcElement); break;
                        case CorElementType.ELEMENT_TYPE_R4:
                            Unsafe.As<byte, float>(ref destElement) = Unsafe.As<byte, ushort>(ref srcElement); break;
                        case CorElementType.ELEMENT_TYPE_R8:
                            Unsafe.As<byte, double>(ref destElement) = Unsafe.As<byte, ushort>(ref srcElement); break;
                        default:
                            Debug.Fail("Array.Copy from U2 to another type hit unsupported widening conversion"); break;
                    }
                    break;

                case CorElementType.ELEMENT_TYPE_I2:
                    switch (destElType)
                    {
                        case CorElementType.ELEMENT_TYPE_I4:
                            Unsafe.As<byte, int>(ref destElement) = Unsafe.As<byte, short>(ref srcElement); break;
                        case CorElementType.ELEMENT_TYPE_I8:
                            Unsafe.As<byte, long>(ref destElement) = Unsafe.As<byte, short>(ref srcElement); break;
                        case CorElementType.ELEMENT_TYPE_R4:
                            Unsafe.As<byte, float>(ref destElement) = Unsafe.As<byte, short>(ref srcElement); break;
                        case CorElementType.ELEMENT_TYPE_R8:
                            Unsafe.As<byte, double>(ref destElement) = Unsafe.As<byte, short>(ref srcElement); break;
                        default:
                            Debug.Fail("Array.Copy from I2 to another type hit unsupported widening conversion"); break;
                    }
                    break;

                case CorElementType.ELEMENT_TYPE_U4:
                    switch (destElType)
                    {
                        case CorElementType.ELEMENT_TYPE_I8:
                        case CorElementType.ELEMENT_TYPE_U8:
                            Unsafe.As<byte, ulong>(ref destElement) = Unsafe.As<byte, uint>(ref srcElement); break;
                        case CorElementType.ELEMENT_TYPE_R4:
                            Unsafe.As<byte, float>(ref destElement) = Unsafe.As<byte, uint>(ref srcElement); break;
                        case CorElementType.ELEMENT_TYPE_R8:
                            Unsafe.As<byte, double>(ref destElement) = Unsafe.As<byte, uint>(ref srcElement); break;
                        default:
                            Debug.Fail("Array.Copy from U4 to another type hit unsupported widening conversion"); break;
                    }
                    break;

                case CorElementType.ELEMENT_TYPE_I4:
                    switch (destElType)
                    {
                        case CorElementType.ELEMENT_TYPE_I8:
                            Unsafe.As<byte, long>(ref destElement) = Unsafe.As<byte, int>(ref srcElement); break;
                        case CorElementType.ELEMENT_TYPE_R4:
                            Unsafe.As<byte, float>(ref destElement) = Unsafe.As<byte, int>(ref srcElement); break;
                        case CorElementType.ELEMENT_TYPE_R8:
                            Unsafe.As<byte, double>(ref destElement) = Unsafe.As<byte, int>(ref srcElement); break;
                        default:
                            Debug.Fail("Array.Copy from I4 to another type hit unsupported widening conversion"); break;
                    }
                    break;

                case CorElementType.ELEMENT_TYPE_U8:
                    switch (destElType)
                    {
                        case CorElementType.ELEMENT_TYPE_R4:
                            Unsafe.As<byte, float>(ref destElement) = Unsafe.As<byte, ulong>(ref srcElement); break;
                        case CorElementType.ELEMENT_TYPE_R8:
                            Unsafe.As<byte, double>(ref destElement) = Unsafe.As<byte, ulong>(ref srcElement); break;
                        default:
                            Debug.Fail("Array.Copy from U8 to another type hit unsupported widening conversion"); break;
                    }
                    break;

                case CorElementType.ELEMENT_TYPE_I8:
                    switch (destElType)
                    {
                        case CorElementType.ELEMENT_TYPE_R4:
                            Unsafe.As<byte, float>(ref destElement) = Unsafe.As<byte, long>(ref srcElement); break;
                        case CorElementType.ELEMENT_TYPE_R8:
                            Unsafe.As<byte, double>(ref destElement) = Unsafe.As<byte, long>(ref srcElement); break;
                        default:
                            Debug.Fail("Array.Copy from I8 to another type hit unsupported widening conversion"); break;
                    }
                    break;

                case CorElementType.ELEMENT_TYPE_R4:
                    Debug.Assert(destElType == CorElementType.ELEMENT_TYPE_R8);
                    Unsafe.As<byte, double>(ref destElement) = Unsafe.As<byte, float>(ref srcElement); break;

                default:
                    Debug.Fail("Fell through outer switch in PrimitiveWiden!  Unknown primitive type for source array!"); break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanPrimitiveWiden(CorElementType srcET, CorElementType dstET)
        {
            // The primitive widen table
            //  The index represents source type. The value in the table is a bit vector of destination types.
            //  If corresponding bit is set in the bit vector, source type can be widened into that type.
            //  All types widen to themselves.
            ReadOnlySpan<short> primitiveWidenTable =
            [
                0x00,      // ELEMENT_TYPE_END
                0x00,      // ELEMENT_TYPE_VOID
                0x0004,    // ELEMENT_TYPE_BOOLEAN
                0x3F88,    // ELEMENT_TYPE_CHAR (W = U2, CHAR, I4, U4, I8, U8, R4, R8) (U2 == Char)
                0x3550,    // ELEMENT_TYPE_I1   (W = I1, I2, I4, I8, R4, R8)
                0x3FE8,    // ELEMENT_TYPE_U1   (W = CHAR, U1, I2, U2, I4, U4, I8, U8, R4, R8)
                0x3540,    // ELEMENT_TYPE_I2   (W = I2, I4, I8, R4, R8)
                0x3F88,    // ELEMENT_TYPE_U2   (W = U2, CHAR, I4, U4, I8, U8, R4, R8)
                0x3500,    // ELEMENT_TYPE_I4   (W = I4, I8, R4, R8)
                0x3E00,    // ELEMENT_TYPE_U4   (W = U4, I8, R4, R8)
                0x3400,    // ELEMENT_TYPE_I8   (W = I8, R4, R8)
                0x3800,    // ELEMENT_TYPE_U8   (W = U8, R4, R8)
                0x3000,    // ELEMENT_TYPE_R4   (W = R4, R8)
                0x2000,    // ELEMENT_TYPE_R8   (W = R8)
            ];

            Debug.Assert(srcET.IsPrimitiveType() && dstET.IsPrimitiveType());
            if ((int)srcET >= primitiveWidenTable.Length)
            {
                // I or U
                return srcET == dstET;
            }
            return (primitiveWidenTable[(int)srcET] & (1 << (int)dstET)) != 0;
        }
    }
}
