// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.DirectoryServices.Protocols
{
    public static partial class BerConverter
    {
        private static int DecodeBitStringHelper(ArrayList resultList, SafeBerHandle berElement)
        {
            // Windows doesn't really decode BitStrings correctly, and wldap32 will internally treat it as 'O' Octet string.
            // In order to match behavior, in Linux we will interpret 'B' as 'O' when passing the call to libldap.

            // return BerVal
            byte[] byteArray = DecodingByteArrayHelper(berElement, 'O', out int error);
            if (!BerPal.IsBerDecodeError(error))
            {
                // add result to the list
                resultList.Add(byteArray);
            }

            return error;
        }
    }
}
