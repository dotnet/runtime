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
    }
}
