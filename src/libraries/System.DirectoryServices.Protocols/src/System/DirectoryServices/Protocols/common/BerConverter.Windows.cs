// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.Protocols
{
    public static partial class BerConverter
    {
        private static int DecodeBitStringHelper(ArrayList resultList, SafeBerHandle berElement)
        {
            int error;
            // return a bitstring and its length
            IntPtr ptrResult = IntPtr.Zero;
            uint length = 0;
            error = BerPal.ScanNextBitString(berElement, "B", ref ptrResult, ref length);

            if (!BerPal.IsBerDecodeError(error))
            {
                byte[] byteArray = null;
                if (ptrResult != IntPtr.Zero)
                {
                    byteArray = new byte[length];
                    Marshal.Copy(ptrResult, byteArray, 0, (int)length);
                }
                resultList.Add(byteArray);
            }
            else
            {
                Debug.WriteLine("ber_scanf for format character 'B' failed");
            }

            // no need to free memory as wldap32 returns the original pointer instead of a duplicating memory pointer that
            // needs to be freed
            return error;
        }
    }
}
