// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        internal static string CurveNidToOidValue(int nidCurveName)
        {
            if (nidCurveName == Interop.Crypto.NID_undef)
            {
                Debug.Fail("Key is invalid or doesn't have a curve");
                return string.Empty;
            }

            IntPtr objCurveName = Interop.Crypto.ObjNid2Obj(nidCurveName);
            if (objCurveName != IntPtr.Zero)
            {
                return Interop.Crypto.GetOidValue(objCurveName);
            }

            throw Interop.Crypto.CreateOpenSslCryptographicException();
        }
    }
}
