// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using System;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyGetCurveType")]
        internal static extern ECCurve.ECCurveType EcKeyGetCurveType(SafeEcKeyHandle key);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyCreateByKeyParameters", CharSet = CharSet.Ansi)]
        private static extern int EcKeyCreateByKeyParameters(
            out SafeEcKeyHandle key,
            string oid, 
            byte[] qx, int qxLength, 
            byte[] qy, int qyLength, 
            byte[] d, int dLength);

        internal static SafeEcKeyHandle EcKeyCreateByKeyParameters(
            string oid,
            byte[] qx, int qxLength,
            byte[] qy, int qyLength,
            byte[] d, int dLength)
        {
            SafeEcKeyHandle key;
            int rc = EcKeyCreateByKeyParameters(out key, oid, qx, qxLength, qy, qyLength, d, dLength);
            if (rc == -1)
            {
                if (key != null)
                    key.Dispose();
                throw new PlatformNotSupportedException(string.Format(SR.Cryptography_CurveNotSupported, oid));
            }
            return key;
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyCreateByExplicitParameters")]
        internal static extern SafeEcKeyHandle EcKeyCreateByExplicitParameters(
            ECCurve.ECCurveType curveType,
            byte[] qx, int qxLength,
            byte[] qy, int qyLength,
            byte[] d, int dLength,
            byte[] p, int pLength,
            byte[] a, int aLength,
            byte[] b, int bLength,
            byte[] gx, int gxLength,
            byte[] gy, int gyLength,
            byte[] order, int nLength,
            byte[] cofactor, int cofactorLength,
            byte[] seed, int seedLength);

        internal static SafeEcKeyHandle EcKeyCreateByExplicitCurve(ECCurve curve)
        {
            byte[] p;
            if (curve.IsPrime)
            {
                p = curve.Prime;
            }
            else if (curve.IsCharacteristic2)
            {
                p = curve.Polynomial;
            }
            else
            {
                throw new PlatformNotSupportedException(string.Format(SR.Cryptography_CurveNotSupported, curve.CurveType.ToString()));
            }

            SafeEcKeyHandle key = Interop.Crypto.EcKeyCreateByExplicitParameters(
                curve.CurveType,
                null, 0,
                null, 0,
                null, 0,
                p, p.Length,
                curve.A, curve.A.Length,
                curve.B, curve.B.Length,
                curve.G.X, curve.G.X.Length,
                curve.G.Y, curve.G.Y.Length,
                curve.Order, curve.Order.Length,
                curve.Cofactor, curve.Cofactor.Length,
                curve.Seed, curve.Seed == null ? 0 : curve.Seed.Length);

            if (key == null || key.IsInvalid)
            {
                if (key != null)
                    key.Dispose();
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            return key;
        }


        [DllImport(Libraries.CryptoNative)]
        private static extern int CryptoNative_GetECKeyParameters(
            SafeEcKeyHandle key, 
            bool includePrivate,
            out SafeBignumHandle qx_bn, out int x_cb,
            out SafeBignumHandle qy_bn, out int y_cb,
            out IntPtr d_bn_not_owned, out int d_cb);

        internal static ECParameters GetECKeyParameters(
            SafeEcKeyHandle key,
            bool includePrivate)
        {
            SafeBignumHandle qx_bn, qy_bn, d_bn;
            IntPtr d_bn_not_owned;
            int qx_cb, qy_cb, d_cb;
            ECParameters parameters = new ECParameters();

            bool refAdded = false;
            try
            {
                key.DangerousAddRef(ref refAdded); // Protect access to d_bn_not_owned
                int rc = CryptoNative_GetECKeyParameters(
                    key,
                    includePrivate,
                    out qx_bn, out qx_cb,
                    out qy_bn, out qy_cb,
                    out d_bn_not_owned, out d_cb);
                    
                if (rc == -1)
                {
                    throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                }
                else if (rc != 1)
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                using (qx_bn)
                using (qy_bn)
                using (d_bn = new SafeBignumHandle(d_bn_not_owned, false))
                {
                    // Match Windows semantics where qx, qy, and d have same length
                    int keySizeBits = EcKeyGetSize(key);
                    int expectedSize = (keySizeBits + 7) / 8;
                    int cbKey = GetMax(qx_cb, qy_cb, d_cb);

                    Debug.Assert(
                        cbKey <= expectedSize,
                        $"Expected output size was {expectedSize}, which a parameter exceeded. qx={qx_cb}, qy={qy_cb}, d={d_cb}");

                    cbKey = GetMax(cbKey, expectedSize);

                    parameters.Q = new ECPoint
                    {
                        X = Crypto.ExtractBignum(qx_bn, cbKey),
                        Y = Crypto.ExtractBignum(qy_bn, cbKey)
                    };
                    parameters.D = d_cb == 0 ? null : Crypto.ExtractBignum(d_bn, cbKey);
                }
            }
            finally
            {
                if (refAdded)
                    key.DangerousRelease();
            }

            return parameters;
        }

        [DllImport(Libraries.CryptoNative)]
        private static extern int CryptoNative_GetECCurveParameters(
            SafeEcKeyHandle key,
            bool includePrivate,
            out ECCurve.ECCurveType curveType,
            out SafeBignumHandle qx, out int x_cb,
            out SafeBignumHandle qy, out int y_cb,
            out IntPtr d_bn_not_owned, out int d_cb,
            out SafeBignumHandle p, out int P_cb,
            out SafeBignumHandle a, out int A_cb,
            out SafeBignumHandle b, out int B_cb,
            out SafeBignumHandle gx, out int Gx_cb,
            out SafeBignumHandle gy, out int Gy_cb,
            out SafeBignumHandle order, out int order_cb,
            out SafeBignumHandle cofactor, out int cofactor_cb,
            out SafeBignumHandle seed, out int seed_cb);

        internal static ECParameters GetECCurveParameters(
            SafeEcKeyHandle key,
            bool includePrivate)
        {
            ECCurve.ECCurveType curveType;
            SafeBignumHandle qx_bn, qy_bn, p_bn, a_bn, b_bn, gx_bn, gy_bn, order_bn, cofactor_bn, seed_bn;
            IntPtr d_bn_not_owned;
            int qx_cb, qy_cb, p_cb, a_cb, b_cb, gx_cb, gy_cb, order_cb, cofactor_cb, seed_cb, d_cb;

            bool refAdded = false;
            try
            {
                key.DangerousAddRef(ref refAdded); // Protect access to d_bn_not_owned
                int rc = CryptoNative_GetECCurveParameters(
                    key,
                    includePrivate,
                    out curveType,
                    out qx_bn, out qx_cb,
                    out qy_bn, out qy_cb,
                    out d_bn_not_owned, out d_cb,
                    out p_bn, out p_cb,
                    out a_bn, out a_cb,
                    out b_bn, out b_cb,
                    out gx_bn, out gx_cb,
                    out gy_bn, out gy_cb,
                    out order_bn, out order_cb,
                    out cofactor_bn, out cofactor_cb,
                    out seed_bn, out seed_cb);
                    
                if (rc == -1)
                {
                    throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);                    
                }
                else if (rc != 1)
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                using (qx_bn)
                using (qy_bn)
                using (p_bn)
                using (a_bn)
                using (b_bn)
                using (gx_bn)
                using (gy_bn)
                using (order_bn)
                using (cofactor_bn)
                using (seed_bn)
                using (var d_h = new SafeBignumHandle(d_bn_not_owned, false))
                {
                    int cbFieldLength;
                    int pFieldLength;
                    if (curveType == ECCurve.ECCurveType.Characteristic2)
                    {
                        // Match Windows semantics where a,b,gx,gy,qx,qy have same length
                        // Treat length of m separately as it is not tied to other fields for Char2 (Char2 not supported by Windows) 
                        cbFieldLength = GetMax(new[] { a_cb, b_cb, gx_cb, gy_cb, qx_cb, qy_cb });
                        pFieldLength = p_cb;
                    }
                    else
                    {
                        // Match Windows semantics where p,a,b,gx,gy,qx,qy have same length
                        cbFieldLength = GetMax(new[] { p_cb, a_cb, b_cb, gx_cb, gy_cb, qx_cb, qy_cb });
                        pFieldLength = cbFieldLength;
                    }

                    // Match Windows semantics where order and d have same length
                    int cbSubgroupOrder = GetMax(order_cb, d_cb);

                    // Copy values to ECParameters
                    ECParameters parameters = new ECParameters();
                    parameters.Q = new ECPoint
                    {
                        X = Crypto.ExtractBignum(qx_bn, cbFieldLength),
                        Y = Crypto.ExtractBignum(qy_bn, cbFieldLength)
                    };
                    parameters.D = d_cb == 0 ? null : Crypto.ExtractBignum(d_h, cbSubgroupOrder);

                    var curve = parameters.Curve;
                    curve.CurveType = curveType;
                    curve.A = Crypto.ExtractBignum(a_bn, cbFieldLength);
                    curve.B = Crypto.ExtractBignum(b_bn, cbFieldLength);
                    curve.G = new ECPoint
                    {
                        X = Crypto.ExtractBignum(gx_bn, cbFieldLength),
                        Y = Crypto.ExtractBignum(gy_bn, cbFieldLength)
                    };
                    curve.Order = Crypto.ExtractBignum(order_bn, cbSubgroupOrder);

                    if (curveType == ECCurve.ECCurveType.Characteristic2)
                    {
                        curve.Polynomial = Crypto.ExtractBignum(p_bn, pFieldLength);
                    }
                    else
                    {
                        curve.Prime = Crypto.ExtractBignum(p_bn, pFieldLength);
                    }

                    // Optional parameters
                    curve.Cofactor = cofactor_cb == 0 ? null : Crypto.ExtractBignum(cofactor_bn, cofactor_cb);
                    curve.Seed = seed_cb == 0 ? null : Crypto.ExtractBignum(seed_bn, seed_cb);

                    parameters.Curve = curve;
                    return parameters;
                }
            }
            finally
            {
                if (refAdded)
                    key.DangerousRelease();
            }
        }

        /// <summary>
        /// Return the maximum value in the array; assumes non-negative values.
        /// </summary>
        private static int GetMax(int[] values)
        {
            int max = 0;

            foreach (var i in values)
            {
                Debug.Assert(i >= 0);
                if (i > max)
                    max = i;
            }

            return max;
        }

        /// <summary>
        /// Return the maximum value in the array; assumes non-negative values.
        /// </summary>
        private static int GetMax(int value1, int value2)
        {
            Debug.Assert(value1 >= 0);
            Debug.Assert(value2 >= 0);
            return (value1 > value2 ? value1 : value2);
        }

        /// <summary>
        /// Return the maximum value in the array; assumes non-negative values.
        /// </summary>
        private static int GetMax(int value1, int value2, int value3)
        {
            return GetMax(GetMax(value1, value2), value3);
        }
    }
}
