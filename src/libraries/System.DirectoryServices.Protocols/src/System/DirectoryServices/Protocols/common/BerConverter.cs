// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace System.DirectoryServices.Protocols
{
    public static partial class BerConverter
    {
        public static byte[] Encode(string format, params object[] value)
        {
            ArgumentNullException.ThrowIfNull(format);

            // no need to turn on invalid encoding detection as we just do string->byte[] conversion.
            UTF8Encoding utf8Encoder = new UTF8Encoding();
            byte[] encodingResult = null;
            // value is allowed to be null in certain scenario, so if it is null, just set it to empty array.
            if (value == null)
                value = Array.Empty<object>();

            Debug.WriteLine("Begin encoding\n");

            // allocate the berelement
            SafeBerHandle berElement = new SafeBerHandle();

            int valueCount = 0;
            int error = 0;

            // We can't use vararg on Unix and can't do ber_printf(tag), so we use ber_put_int(val, tag)
            // and this local keeps tag value for the next element.
            nuint tag = 0;
            bool tagIsSet = false;
            for (int formatCount = 0; formatCount < format.Length; formatCount++)
            {
                if (tagIsSet)
                {
                    tagIsSet = false;
                }
                else
                {
                    int lberTagDefaultInt = -1;
                    nuint lberTagDefaultNuint = (nuint)lberTagDefaultInt;
                    tag = lberTagDefaultNuint;
                }
                char fmt = format[formatCount];
                if (fmt == '{' || fmt == '}' || fmt == '[' || fmt == ']' || fmt == 'n')
                {
                    // no argument needed
                    error = BerPal.PrintEmptyArgument(berElement, new string(fmt, 1), tag);
                }
                else if (fmt == 'i' || fmt == 'e')
                {
                    if (valueCount >= value.Length)
                    {
                        // we don't have enough argument for the format string
                        Debug.WriteLine("value argument is not valid, valueCount >= value.Length\n");
                        throw new ArgumentException(SR.BerConverterNotMatch);
                    }

                    if (!(value[valueCount] is int))
                    {
                        // argument is wrong
                        Debug.WriteLine("type should be int\n");
                        throw new ArgumentException(SR.BerConverterNotMatch);
                    }

                    // one int argument
                    error = BerPal.PrintInt(berElement, new string(fmt, 1), (int)value[valueCount], tag);

                    // increase the value count
                    valueCount++;
                }
                else if (fmt == 'b')
                {
                    if (valueCount >= value.Length)
                    {
                        // we don't have enough argument for the format string
                        Debug.WriteLine("value argument is not valid, valueCount >= value.Length\n");
                        throw new ArgumentException(SR.BerConverterNotMatch);
                    }

                    if (!(value[valueCount] is bool))
                    {
                        // argument is wrong
                        Debug.WriteLine("type should be boolean\n");
                        throw new ArgumentException(SR.BerConverterNotMatch);
                    }

                    // one int argument
                    error = BerPal.PrintInt(berElement, new string(fmt, 1), (bool)value[valueCount] ? 1 : 0, tag);

                    // increase the value count
                    valueCount++;
                }
                else if (fmt == 's')
                {
                    if (valueCount >= value.Length)
                    {
                        // we don't have enough argument for the format string
                        Debug.WriteLine("value argument is not valid, valueCount >= value.Length\n");
                        throw new ArgumentException(SR.BerConverterNotMatch);
                    }

                    if (value[valueCount] != null && !(value[valueCount] is string))
                    {
                        // argument is wrong
                        Debug.WriteLine("type should be string, but receiving value has type of ");
                        Debug.WriteLine(value[valueCount].GetType());
                        throw new ArgumentException(SR.BerConverterNotMatch);
                    }

                    // one string argument
                    byte[] tempValue = null;
                    if (value[valueCount] != null)
                    {
                        tempValue = utf8Encoder.GetBytes((string)value[valueCount]);
                    }
                    error = EncodingByteArrayHelper(berElement, tempValue, 'o', tag);

                    // increase the value count
                    valueCount++;
                }
                else if (fmt == 'o' || fmt == 'X')
                {
                    // we need to have one arguments
                    if (valueCount >= value.Length)
                    {
                        // we don't have enough argument for the format string
                        Debug.WriteLine("value argument is not valid, valueCount >= value.Length\n");
                        throw new ArgumentException(SR.BerConverterNotMatch);
                    }

                    if (value[valueCount] != null && !(value[valueCount] is byte[]))
                    {
                        // argument is wrong
                        Debug.WriteLine("type should be byte[], but receiving value has type of ");
                        Debug.WriteLine(value[valueCount].GetType());
                        throw new ArgumentException(SR.BerConverterNotMatch);
                    }

                    byte[] tempValue = (byte[])value[valueCount];
                    error = EncodingByteArrayHelper(berElement, tempValue, fmt, tag);

                    valueCount++;
                }
                else if (fmt == 'v')
                {
                    // we need to have one arguments
                    if (valueCount >= value.Length)
                    {
                        // we don't have enough argument for the format string
                        Debug.WriteLine("value argument is not valid, valueCount >= value.Length\n");
                        throw new ArgumentException(SR.BerConverterNotMatch);
                    }

                    if (value[valueCount] != null && !(value[valueCount] is string[]))
                    {
                        // argument is wrong
                        Debug.WriteLine("type should be string[], but receiving value has type of ");
                        Debug.WriteLine(value[valueCount].GetType());
                        throw new ArgumentException(SR.BerConverterNotMatch);
                    }

                    string[] stringValues = (string[])value[valueCount];
                    byte[][] tempValues;
                    if (stringValues != null)
                    {
                        tempValues = new byte[stringValues.Length][];
                        for (int i = 0; i < stringValues.Length; i++)
                        {
                            string s = stringValues[i];
                            if (s == null)
                                tempValues[i] = null;
                            else
                            {
                                tempValues[i] = utf8Encoder.GetBytes(s);
                            }
                            error = EncodingByteArrayHelper(berElement, tempValues[i], 'o', tag);
                            if (error == -1)
                            {
                                break;
                            }
                        }
                    }
                    valueCount++;
                }
                else if (fmt == 'V')
                {
                    // we need to have one arguments
                    if (valueCount >= value.Length)
                    {
                        // we don't have enough argument for the format string
                        Debug.WriteLine("value argument is not valid, valueCount >= value.Length\n");
                        throw new ArgumentException(SR.BerConverterNotMatch);
                    }

                    if (value[valueCount] != null && !(value[valueCount] is byte[][]))
                    {
                        // argument is wrong
                        Debug.WriteLine("type should be byte[][], but receiving value has type of ");
                        Debug.WriteLine(value[valueCount].GetType());
                        throw new ArgumentException(SR.BerConverterNotMatch);
                    }

                    byte[][] tempValue = (byte[][])value[valueCount];

                    error = EncodingMultiByteArrayHelper(berElement, tempValue, fmt, tag);

                    valueCount++;
                }
                else if (fmt == 't')
                {
                    if (valueCount >= value.Length)
                    {
                        // we don't have enough argument for the format string
                        Debug.WriteLine("value argument is not valid, valueCount >= value.Length\n");
                        throw new ArgumentException(SR.BerConverterNotMatch);
                    }

                    if (!(value[valueCount] is int))
                    {
                        // argument is wrong
                        Debug.WriteLine("type should be int\n");
                        throw new ArgumentException(SR.BerConverterNotMatch);
                    }
                    tag = (uint)(int)value[valueCount];
                    tagIsSet = true;
                    // It will set the tag on Windows and only check the tag on Unix.
                    error = BerPal.PrintTag(berElement, new string(fmt, 1), tag);

                    // increase the value count
                    valueCount++;
                }
                else
                {
                    Debug.WriteLine("Format string contains undefined character: ");
                    Debug.WriteLine(new string(fmt, 1));
                    throw new ArgumentException(SR.BerConverterUndefineChar);
                }

                // process the return value
                if (error == -1)
                {
                    Debug.WriteLine("ber_printf failed\n");
                    throw new BerConversionException();
                }
            }

            // get the binary value back
            BerVal binaryValue = new BerVal();
            IntPtr flattenptr = IntPtr.Zero;

            try
            {
                // can't use SafeBerval here as CLR creates a SafeBerval which points to a different memory location, but when doing memory
                // deallocation, wldap has special check. So have to use IntPtr directly here.
                error = BerPal.FlattenBerElement(berElement, ref flattenptr);

                if (error == -1)
                {
                    Debug.WriteLine("ber_flatten failed\n");
                    throw new BerConversionException();
                }

                if (flattenptr != IntPtr.Zero)
                {
                    Marshal.PtrToStructure(flattenptr, binaryValue);
                }

                if (binaryValue == null || binaryValue.bv_len == 0)
                {
                    encodingResult = Array.Empty<byte>();
                }
                else
                {
                    encodingResult = new byte[binaryValue.bv_len];

                    Marshal.Copy(binaryValue.bv_val, encodingResult, 0, binaryValue.bv_len);
                }
            }
            finally
            {
                if (flattenptr != IntPtr.Zero)
                    BerPal.FreeBerval(flattenptr);
            }

            return encodingResult;
        }

        public static object[] Decode(string format, byte[] value)
        {
            bool decodeSucceeded;
            object[] decodeResult = TryDecode(format, value, out decodeSucceeded);
            if (decodeSucceeded)
                return decodeResult;
            else
                throw new BerConversionException();
        }

        internal static object[] TryDecode(string format, byte[] value, out bool decodeSucceeded)
        {
            ArgumentNullException.ThrowIfNull(format);

            Debug.WriteLine("Begin decoding");

            UTF8Encoding utf8Encoder = new UTF8Encoding(false, true);
            BerVal berValue = new BerVal();
            ArrayList resultList = new ArrayList();
            SafeBerHandle berElement = null;

            object[] decodeResult = null;
            decodeSucceeded = false;

            if (value == null)
            {
                berValue.bv_len = 0;
                berValue.bv_val = IntPtr.Zero;
            }
            else
            {
                berValue.bv_len = value.Length;
                berValue.bv_val = Marshal.AllocHGlobal(value.Length);
                Marshal.Copy(value, 0, berValue.bv_val, value.Length);
            }

            try
            {
                berElement = new SafeBerHandle(berValue);
            }
            finally
            {
                if (berValue.bv_val != IntPtr.Zero)
                    Marshal.FreeHGlobal(berValue.bv_val);
            }

            int error;

            for (int formatCount = 0; formatCount < format.Length; formatCount++)
            {
                char fmt = format[formatCount];
                if (fmt == '{' || fmt == '}' || fmt == '[' || fmt == ']' || fmt == 'n' || fmt == 'x')
                {
                    error = BerPal.ScanNext(berElement, new string(fmt, 1));

                    if (BerPal.IsBerDecodeError(error))
                        Debug.WriteLine("ber_scanf for {, }, [, ], n or x failed");
                }
                else if (fmt == 'i' || fmt == 'e' || fmt == 'b')
                {
                    int result = 0;
                    error = BerPal.ScanNextInt(berElement, new string(fmt, 1), ref result);

                    if (!BerPal.IsBerDecodeError(error))
                    {
                        if (fmt == 'b')
                        {
                            // should return a bool
                            bool boolResult;
                            if (result == 0)
                                boolResult = false;
                            else
                                boolResult = true;
                            resultList.Add(boolResult);
                        }
                        else
                        {
                            resultList.Add(result);
                        }
                    }
                    else
                        Debug.WriteLine("ber_scanf for format character 'i', 'e' or 'b' failed");
                }
                else if (fmt == 'a')
                {
                    // return a string
                    byte[] byteArray = DecodingByteArrayHelper(berElement, 'O', out error);
                    if (!BerPal.IsBerDecodeError(error))
                    {
                        string s = null;
                        if (byteArray != null)
                            s = utf8Encoder.GetString(byteArray);

                        resultList.Add(s);
                    }
                }
                else if (fmt == 'O')
                {
                    // return BerVal
                    byte[] byteArray = DecodingByteArrayHelper(berElement, fmt, out error);
                    if (!BerPal.IsBerDecodeError(error))
                    {
                        // add result to the list
                        resultList.Add(byteArray);
                    }
                }
                else if (fmt == 'B')
                {
                    error = DecodeBitStringHelper(resultList, berElement);
                }
                else if (fmt == 'v')
                {
                    //null terminate strings
                    byte[][] byteArrayresult;
                    string[] stringArray = null;

                    byteArrayresult = DecodingMultiByteArrayHelper(berElement, 'V', out error);
                    if (!BerPal.IsBerDecodeError(error))
                    {
                        if (byteArrayresult != null)
                        {
                            stringArray = new string[byteArrayresult.Length];
                            for (int i = 0; i < byteArrayresult.Length; i++)
                            {
                                if (byteArrayresult[i] == null)
                                {
                                    stringArray[i] = null;
                                }
                                else
                                {
                                    stringArray[i] = utf8Encoder.GetString(byteArrayresult[i]);
                                }
                            }
                        }

                        resultList.Add(stringArray);
                    }
                }
                else if (fmt == 'V')
                {
                    byte[][] result;

                    result = DecodingMultiByteArrayHelper(berElement, fmt, out error);
                    if (!BerPal.IsBerDecodeError(error))
                    {
                        resultList.Add(result);
                    }
                }
                else
                {
                    Debug.WriteLine("Format string contains undefined character\n");
                    throw new ArgumentException(SR.BerConverterUndefineChar);
                }

                if (BerPal.IsBerDecodeError(error))
                {
                    // decode failed, just return
                    return decodeResult;
                }
            }

            decodeResult = new object[resultList.Count];
            for (int count = 0; count < resultList.Count; count++)
            {
                decodeResult[count] = resultList[count];
            }

            decodeSucceeded = true;
            return decodeResult;
        }

        private static int EncodingByteArrayHelper(SafeBerHandle berElement, byte[] tempValue, char fmt, nuint tag)
        {
            int error;

            // one byte array, one int arguments
            if (tempValue != null)
            {
                IntPtr tmp = Marshal.AllocHGlobal(tempValue.Length);
                Marshal.Copy(tempValue, 0, tmp, tempValue.Length);
                HGlobalMemHandle memHandle = new HGlobalMemHandle(tmp);
                error = BerPal.PrintByteArray(berElement, new string(fmt, 1), memHandle, (uint)tempValue.Length, tag);
            }
            else
            {
                HGlobalMemHandle memHandle = new HGlobalMemHandle(HGlobalMemHandle._dummyPointer);
                error = BerPal.PrintByteArray(berElement, new string(fmt, 1), memHandle, 0, tag);
            }

            return error;
        }

        private static byte[] DecodingByteArrayHelper(SafeBerHandle berElement, char fmt, out int error)
        {
            IntPtr result = IntPtr.Zero;
            BerVal binaryValue = new BerVal();
            byte[] byteArray = null;

            // can't use SafeBerval here as CLR creates a SafeBerval which points to a different memory location, but when doing memory
            // deallocation, wldap has special check. So have to use IntPtr directly here.
            error = BerPal.ScanNextPtr(berElement, new string(fmt, 1), ref result);

            try
            {
                if (!BerPal.IsBerDecodeError(error))
                {
                    if (result != IntPtr.Zero)
                    {
                        Marshal.PtrToStructure(result, binaryValue);

                        byteArray = new byte[binaryValue.bv_len];
                        Marshal.Copy(binaryValue.bv_val, byteArray, 0, binaryValue.bv_len);
                    }
                }
                else
                    Debug.WriteLine("ber_scanf for format character 'O' failed");
            }
            finally
            {
                if (result != IntPtr.Zero)
                    BerPal.FreeBerval(result);
            }

            return byteArray;
        }

        private static int EncodingMultiByteArrayHelper(SafeBerHandle berElement, byte[][] tempValue, char fmt, nuint tag)
        {
            IntPtr berValArray = IntPtr.Zero;
            IntPtr tempPtr;
            BerVal[] managedBervalArray = null;
            int error = 0;

            try
            {
                if (tempValue != null)
                {
                    int i = 0;
                    berValArray = Utility.AllocHGlobalIntPtrArray(tempValue.Length + 1);
                    int structSize = Marshal.SizeOf(typeof(BerVal));
                    managedBervalArray = new BerVal[tempValue.Length];

                    for (i = 0; i < tempValue.Length; i++)
                    {
                        byte[] byteArray = tempValue[i];

                        // construct the managed BerVal
                        managedBervalArray[i] = new BerVal();

                        if (byteArray != null)
                        {
                            managedBervalArray[i].bv_len = byteArray.Length;
                            managedBervalArray[i].bv_val = Marshal.AllocHGlobal(byteArray.Length);
                            Marshal.Copy(byteArray, 0, managedBervalArray[i].bv_val, byteArray.Length);
                        }

                        // allocate memory for the unmanaged structure
                        IntPtr valPtr = Marshal.AllocHGlobal(structSize);
                        Marshal.StructureToPtr(managedBervalArray[i], valPtr, false);

                        tempPtr = (IntPtr)((long)berValArray + IntPtr.Size * i);
                        Marshal.WriteIntPtr(tempPtr, valPtr);
                    }

                    tempPtr = (IntPtr)((long)berValArray + IntPtr.Size * i);
                    Marshal.WriteIntPtr(tempPtr, IntPtr.Zero);
                }

                error = BerPal.PrintBerArray(berElement, new string(fmt, 1), berValArray, tag);
            }
            finally
            {
                if (berValArray != IntPtr.Zero)
                {
                    for (int i = 0; i < tempValue.Length; i++)
                    {
                        IntPtr ptr = Marshal.ReadIntPtr(berValArray, IntPtr.Size * i);
                        if (ptr != IntPtr.Zero)
                            Marshal.FreeHGlobal(ptr);
                    }
                    Marshal.FreeHGlobal(berValArray);
                }
                if (managedBervalArray != null)
                {
                    foreach (BerVal managedBerval in managedBervalArray)
                    {
                        if (managedBerval.bv_val != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(managedBerval.bv_val);
                        }
                    }
                }
            }

            return error;
        }

        private static byte[][] DecodingMultiByteArrayHelper(SafeBerHandle berElement, char fmt, out int error)
        {
            // several BerVal
            IntPtr ptrResult = IntPtr.Zero;
            int i = 0;
            ArrayList binaryList = new ArrayList();
            IntPtr tempPtr;
            byte[][] result = null;

            try
            {
                error = BerPal.ScanNextPtr(berElement, new string(fmt, 1), ref ptrResult);

                if (!BerPal.IsBerDecodeError(error))
                {
                    if (ptrResult != IntPtr.Zero)
                    {
                        tempPtr = Marshal.ReadIntPtr(ptrResult);
                        while (tempPtr != IntPtr.Zero)
                        {
                            BerVal ber = new BerVal();
                            Marshal.PtrToStructure(tempPtr, ber);

                            byte[] berArray = new byte[ber.bv_len];
                            Marshal.Copy(ber.bv_val, berArray, 0, ber.bv_len);

                            binaryList.Add(berArray);

                            i++;
                            tempPtr = Marshal.ReadIntPtr(ptrResult, i * IntPtr.Size);
                        }

                        result = new byte[binaryList.Count][];
                        for (int j = 0; j < binaryList.Count; j++)
                        {
                            result[j] = (byte[])binaryList[j];
                        }
                    }
                }
                else
                    Debug.WriteLine("ber_scanf for format character 'V' failed");
            }
            finally
            {
                if (ptrResult != IntPtr.Zero)
                {
                    BerPal.FreeBervalArray(ptrResult);
                }
            }

            return result;
        }
    }
}
