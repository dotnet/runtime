// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Collections;
using System.Text;

namespace System.DirectoryServices.Protocols
{
    public static class BerConverter
    {
        public static byte[] Encode(string format, params object[] value)
        {
            if (format == null)
                throw new ArgumentNullException(nameof(format));

            // no need to turn on invalid encoding detection as we just do string->byte[] conversion.
            UTF8Encoding utf8Encoder = new UTF8Encoding();
            byte[] encodingResult = null;
            // value is allowed to be null in certain scenario, so if it is null, just set it to empty array.
            if (value == null)
                value = Array.Empty<object>();

            Debug.WriteLine("Begin encoding\n");

            // allocate the berelement
            BerSafeHandle berElement = new BerSafeHandle();

            int valueCount = 0;
            int error = 0;
            for (int formatCount = 0; formatCount < format.Length; formatCount++)
            {
                char fmt = format[formatCount];
                if (fmt == '{' || fmt == '}' || fmt == '[' || fmt == ']' || fmt == 'n')
                {
                    // no argument needed
                    error = Wldap32.ber_printf_emptyarg(berElement, new string(fmt, 1));
                }
                else if (fmt == 't' || fmt == 'i' || fmt == 'e')
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
                    error = Wldap32.ber_printf_int(berElement, new string(fmt, 1), (int)value[valueCount]);

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
                    error = Wldap32.ber_printf_int(berElement, new string(fmt, 1), (bool)value[valueCount] ? 1 : 0);

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
                    error = EncodingByteArrayHelper(berElement, tempValue, 'o');

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
                    error = EncodingByteArrayHelper(berElement, tempValue, fmt);

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
                    byte[][] tempValues = null;
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
                        }
                    }

                    error = EncodingMultiByteArrayHelper(berElement, tempValues, 'V');

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

                    error = EncodingMultiByteArrayHelper(berElement, tempValue, fmt);

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
            berval binaryValue = new berval();
            IntPtr flattenptr = IntPtr.Zero;

            try
            {
                // can't use SafeBerval here as CLR creates a SafeBerval which points to a different memory location, but when doing memory
                // deallocation, wldap has special check. So have to use IntPtr directly here.
                error = Wldap32.ber_flatten(berElement, ref flattenptr);

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
                    Wldap32.ber_bvfree(flattenptr);
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
            if (format == null)
                throw new ArgumentNullException(nameof(format));

            Debug.WriteLine("Begin decoding\n");

            UTF8Encoding utf8Encoder = new UTF8Encoding(false, true);
            berval berValue = new berval();
            ArrayList resultList = new ArrayList();
            BerSafeHandle berElement = null;

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
                berElement = new BerSafeHandle(berValue);
            }
            finally
            {
                if (berValue.bv_val != IntPtr.Zero)
                    Marshal.FreeHGlobal(berValue.bv_val);
            }

            int error = 0;

            for (int formatCount = 0; formatCount < format.Length; formatCount++)
            {
                char fmt = format[formatCount];
                if (fmt == '{' || fmt == '}' || fmt == '[' || fmt == ']' || fmt == 'n' || fmt == 'x')
                {
                    error = Wldap32.ber_scanf(berElement, new string(fmt, 1));

                    if (error != 0)
                        Debug.WriteLine("ber_scanf for {, }, [, ], n or x failed");
                }
                else if (fmt == 'i' || fmt == 'e' || fmt == 'b')
                {
                    int result = 0;
                    error = Wldap32.ber_scanf_int(berElement, new string(fmt, 1), ref result);

                    if (error == 0)
                    {
                        if (fmt == 'b')
                        {
                            // should return a bool
                            bool boolResult = false;
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
                    byte[] byteArray = DecodingByteArrayHelper(berElement, 'O', ref error);
                    if (error == 0)
                    {
                        string s = null;
                        if (byteArray != null)
                            s = utf8Encoder.GetString(byteArray);

                        resultList.Add(s);
                    }
                }
                else if (fmt == 'O')
                {
                    // return berval                   
                    byte[] byteArray = DecodingByteArrayHelper(berElement, fmt, ref error);
                    if (error == 0)
                    {
                        // add result to the list
                        resultList.Add(byteArray);
                    }
                }
                else if (fmt == 'B')
                {
                    // return a bitstring and its length
                    IntPtr ptrResult = IntPtr.Zero;
                    int length = 0;
                    error = Wldap32.ber_scanf_bitstring(berElement, "B", ref ptrResult, ref length);

                    if (error == 0)
                    {
                        byte[] byteArray = null;
                        if (ptrResult != IntPtr.Zero)
                        {
                            byteArray = new byte[length];
                            Marshal.Copy(ptrResult, byteArray, 0, length);
                        }
                        resultList.Add(byteArray);
                    }
                    else
                        Debug.WriteLine("ber_scanf for format character 'B' failed");

                    // no need to free memory as wldap32 returns the original pointer instead of a duplicating memory pointer that
                    // needs to be freed
                }
                else if (fmt == 'v')
                {
                    //null terminate strings
                    byte[][] byteArrayresult = null;
                    string[] stringArray = null;

                    byteArrayresult = DecodingMultiByteArrayHelper(berElement, 'V', ref error);
                    if (error == 0)
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
                    byte[][] result = null;

                    result = DecodingMultiByteArrayHelper(berElement, fmt, ref error);
                    if (error == 0)
                    {
                        resultList.Add(result);
                    }
                }
                else
                {
                    Debug.WriteLine("Format string contains undefined character\n");
                    throw new ArgumentException(SR.BerConverterUndefineChar);
                }

                if (error != 0)
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

        private static int EncodingByteArrayHelper(BerSafeHandle berElement, byte[] tempValue, char fmt)
        {
            int error = 0;

            // one byte array, one int arguments
            if (tempValue != null)
            {
                IntPtr tmp = Marshal.AllocHGlobal(tempValue.Length);
                Marshal.Copy(tempValue, 0, tmp, tempValue.Length);
                HGlobalMemHandle memHandle = new HGlobalMemHandle(tmp);

                error = Wldap32.ber_printf_bytearray(berElement, new string(fmt, 1), memHandle, tempValue.Length);
            }
            else
            {
                error = Wldap32.ber_printf_bytearray(berElement, new string(fmt, 1), new HGlobalMemHandle(IntPtr.Zero), 0);
            }

            return error;
        }

        private static byte[] DecodingByteArrayHelper(BerSafeHandle berElement, char fmt, ref int error)
        {
            error = 0;
            IntPtr result = IntPtr.Zero;
            berval binaryValue = new berval();
            byte[] byteArray = null;

            // can't use SafeBerval here as CLR creates a SafeBerval which points to a different memory location, but when doing memory
            // deallocation, wldap has special check. So have to use IntPtr directly here.
            error = Wldap32.ber_scanf_ptr(berElement, new string(fmt, 1), ref result);

            try
            {
                if (error == 0)
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
                    Wldap32.ber_bvfree(result);
            }

            return byteArray;
        }

        private static int EncodingMultiByteArrayHelper(BerSafeHandle berElement, byte[][] tempValue, char fmt)
        {
            IntPtr berValArray = IntPtr.Zero;
            IntPtr tempPtr = IntPtr.Zero;
            SafeBerval[] managedBerVal = null;
            int error = 0;

            try
            {
                if (tempValue != null)
                {
                    int i = 0;
                    berValArray = Utility.AllocHGlobalIntPtrArray(tempValue.Length + 1);
                    int structSize = Marshal.SizeOf(typeof(SafeBerval));
                    managedBerVal = new SafeBerval[tempValue.Length];

                    for (i = 0; i < tempValue.Length; i++)
                    {
                        byte[] byteArray = tempValue[i];

                        // construct the managed berval
                        managedBerVal[i] = new SafeBerval();

                        if (byteArray == null)
                        {
                            managedBerVal[i].bv_len = 0;
                            managedBerVal[i].bv_val = IntPtr.Zero;
                        }
                        else
                        {
                            managedBerVal[i].bv_len = byteArray.Length;
                            managedBerVal[i].bv_val = Marshal.AllocHGlobal(byteArray.Length);
                            Marshal.Copy(byteArray, 0, managedBerVal[i].bv_val, byteArray.Length);
                        }

                        // allocate memory for the unmanaged structure
                        IntPtr valPtr = Marshal.AllocHGlobal(structSize);
                        Marshal.StructureToPtr(managedBerVal[i], valPtr, false);

                        tempPtr = (IntPtr)((long)berValArray + IntPtr.Size * i);
                        Marshal.WriteIntPtr(tempPtr, valPtr);
                    }

                    tempPtr = (IntPtr)((long)berValArray + IntPtr.Size * i);
                    Marshal.WriteIntPtr(tempPtr, IntPtr.Zero);
                }

                error = Wldap32.ber_printf_berarray(berElement, new string(fmt, 1), berValArray);

                GC.KeepAlive(managedBerVal);
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
            }

            return error;
        }

        private static byte[][] DecodingMultiByteArrayHelper(BerSafeHandle berElement, char fmt, ref int error)
        {
            error = 0;
            // several berval
            IntPtr ptrResult = IntPtr.Zero;
            int i = 0;
            ArrayList binaryList = new ArrayList();
            IntPtr tempPtr = IntPtr.Zero;
            byte[][] result = null;

            try
            {
                error = Wldap32.ber_scanf_ptr(berElement, new string(fmt, 1), ref ptrResult);

                if (error == 0)
                {
                    if (ptrResult != IntPtr.Zero)
                    {
                        tempPtr = Marshal.ReadIntPtr(ptrResult);
                        while (tempPtr != IntPtr.Zero)
                        {
                            berval ber = new berval();
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
                    Wldap32.ber_bvecfree(ptrResult);
                }
            }

            return result;
        }
    }
}
