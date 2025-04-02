// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyCreateByKeyParameters", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int EcKeyCreateByKeyParameters(
            out SafeEcKeyHandle key,
            string oid,
            byte[]? qx, int qxLength,
            byte[]? qy, int qyLength,
            byte[]? d, int dLength);

        internal static SafeEcKeyHandle EcKeyCreateByKeyParameters(
            string oid,
            byte[]? qx, int qxLength,
            byte[]? qy, int qyLength,
            byte[]? d, int dLength)
        {
            SafeEcKeyHandle key;
            int rc = EcKeyCreateByKeyParameters(out key, oid, qx, qxLength, qy, qyLength, d, dLength);
            if (rc == -1)
            {
                key?.Dispose();
                Interop.Crypto.ErrClearError();

                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CurveNotSupported, oid));
            }
            return key;
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyCreateByExplicitParameters")]
        internal static partial SafeEcKeyHandle EcKeyCreateByExplicitParameters(
            ECCurve.ECCurveType curveType,
            byte[]? qx, int qxLength,
            byte[]? qy, int qyLength,
            byte[]? d, int dLength,
            byte[] p, int pLength,
            byte[] a, int aLength,
            byte[] b, int bLength,
            byte[] gx, int gxLength,
            byte[] gy, int gyLength,
            byte[] order, int nLength,
            byte[]? cofactor, int cofactorLength,
            byte[]? seed, int seedLength);

        internal static SafeEcKeyHandle EcKeyCreateByExplicitCurve(ECCurve curve)
        {
            byte[] p;
            if (curve.IsPrime)
            {
                p = curve.Prime!;
            }
            else if (curve.IsCharacteristic2)
            {
                p = curve.Polynomial!;
            }
            else
            {
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CurveNotSupported, curve.CurveType.ToString()));
            }

            SafeEcKeyHandle key = Interop.Crypto.EcKeyCreateByExplicitParameters(
                curve.CurveType,
                null, 0,
                null, 0,
                null, 0,
                p, p.Length,
                curve.A!, curve.A!.Length,
                curve.B!, curve.B!.Length,
                curve.G.X!, curve.G.X!.Length,
                curve.G.Y!, curve.G.Y!.Length,
                curve.Order!, curve.Order!.Length,
                curve.Cofactor, curve.Cofactor!.Length,
                curve.Seed, curve.Seed == null ? 0 : curve.Seed.Length);

            if (key == null || key.IsInvalid)
            {
                Exception e = Interop.Crypto.CreateOpenSslCryptographicException();
                key?.Dispose();
                throw e;
            }

            // EcKeyCreateByExplicitParameters may have polluted the error queue, but key was good in the end.
            // Clean up the error queue.
            Interop.Crypto.ErrClearError();

            return key;
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_EvpPKeyGetEcGroupNid(SafeEvpPKeyHandle pkey, out int nid);

        internal static bool EvpPKeyHasCurveName(SafeEvpPKeyHandle pkey)
        {
            int rc = CryptoNative_EvpPKeyGetEcGroupNid(pkey, out int nidCurveName);
            if (rc == 1)
            {
                // Key is invalid or doesn't have a curve
                return (nidCurveName != Interop.Crypto.NID_undef);
            }

            throw Interop.Crypto.CreateOpenSslCryptographicException();
        }

        /// <summary>
        /// Returns the OID as a string or null if curve is not named.
        /// </summary>
        internal static string? EvpPKeyGetCurveName(SafeEvpPKeyHandle pkey)
        {
            int rc = CryptoNative_EvpPKeyGetEcGroupNid(pkey, out int nidCurveName);
            if (rc == 1)
            {
                return nidCurveName != Interop.Crypto.NID_undef ? CurveNidToOidValue(nidCurveName) : null;
            }

            throw Interop.Crypto.CreateOpenSslCryptographicException();
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_GetECKeyParameters(
            SafeEcKeyHandle key,
            [MarshalAs(UnmanagedType.Bool)] bool includePrivate,
            out SafeBignumHandle qx_bn, out int x_cb,
            out SafeBignumHandle qy_bn, out int y_cb,
            out IntPtr d_bn_not_owned, out int d_cb);

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_EvpPKeyGetEcKeyParameters(
            SafeEvpPKeyHandle key,
            [MarshalAs(UnmanagedType.Bool)] bool includePrivate,
            out SafeBignumHandle qx_bn, out int x_cb,
            out SafeBignumHandle qy_bn, out int y_cb,
            out SafeBignumHandle d_bn, out int d_cb);

        internal static ECParameters GetECKeyParameters(
            SafeEcKeyHandle key,
            bool includePrivate)
        {
            SafeBignumHandle qx_bn, qy_bn, d_bn;
            IntPtr d_bn_not_owned;
            int qx_cb, qy_cb, d_cb;

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

                using (qx_bn)
                using (qy_bn)
                {
                    if (rc == -1)
                    {
                        throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                    }
                    else if (rc != 1)
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }

                    using (d_bn = new SafeBignumHandle(d_bn_not_owned, false))
                    {
                        // Match Windows semantics where qx, qy, and d have same length
                        int keySizeBits = EcKeyGetSize(key);
                        int expectedSize = (keySizeBits + 7) / 8;
                        return GetEcParameters(
                            expectedSize,
                            qx_bn, qx_cb,
                            qy_bn, qy_cb,
                            d_bn, d_cb);
                    }
                }
            }
            finally
            {
                if (refAdded)
                    key.DangerousRelease();
            }
        }

        internal static ECParameters EvpPKeyGetEcKeyParameters(
            SafeEvpPKeyHandle key,
            bool includePrivate)
        {
            SafeBignumHandle qx_bn, qy_bn, d_bn;
            int qx_cb, qy_cb, d_cb;

            int rc = CryptoNative_EvpPKeyGetEcKeyParameters(
                key,
                includePrivate,
                out qx_bn, out qx_cb,
                out qy_bn, out qy_cb,
                out d_bn, out d_cb);

            using (qx_bn)
            using (qy_bn)
            using (d_bn)
            {
                if (rc == -1)
                {
                    throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                }
                else if (rc != 1)
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                // Match Windows semantics where qx, qy, and d have same length
                int expectedSize = GetEvpPKeySizeBytes(key);
                return GetEcParameters(
                    expectedSize,
                    qx_bn, qx_cb,
                    qy_bn, qy_cb,
                    d_bn, d_cb);
            }
        }

        private static ECParameters GetEcParameters(
            int expectedKeySizeBytes,
            SafeBignumHandle qx_bn, int qx_cb,
            SafeBignumHandle qy_bn, int qy_cb,
            SafeBignumHandle d_bn, int d_cb
        )
        {
            ECParameters parameters = default;

            // Match Windows semantics where qx, qy, and d have same length
            int cbKey = GetMax(qx_cb, qy_cb, d_cb);

            Debug.Assert(
                cbKey <= expectedKeySizeBytes,
                $"Expected output size was {expectedKeySizeBytes}, which a parameter exceeded. qx={qx_cb}, qy={qy_cb}, d={d_cb}");

            cbKey = GetMax(cbKey, expectedKeySizeBytes);

            parameters.Q = new ECPoint
            {
                X = Crypto.ExtractBignum(qx_bn, cbKey),
                Y = Crypto.ExtractBignum(qy_bn, cbKey)
            };
            parameters.D = d_cb == 0 ? null : Crypto.ExtractBignum(d_bn, cbKey);

            return parameters;
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_GetECCurveParameters(
            SafeEcKeyHandle key,
            [MarshalAs(UnmanagedType.Bool)] bool includePrivate,
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

        [LibraryImport(Libraries.CryptoNative)]
        private static unsafe partial int CryptoNative_EvpPKeyGetEcCurveParameters(
            SafeEvpPKeyHandle key,
            [MarshalAs(UnmanagedType.Bool)] bool includePrivate,
            out ECCurve.ECCurveType curveType,
            out SafeBignumHandle qx, out int x_cb,
            out SafeBignumHandle qy, out int y_cb,
            out SafeBignumHandle d, out int d_cb,
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
                {
                    if (rc == -1)
                    {
                        throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                    }
                    else if (rc != 1)
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }

                    using (var d_h = new SafeBignumHandle(d_bn_not_owned, false))
                    {
                        return GetEcCurveParameters(
                            curveType,
                            qx_bn, qx_cb,
                            qy_bn, qy_cb,
                            d_h, d_cb,
                            p_bn, p_cb,
                            a_bn, a_cb,
                            b_bn, b_cb,
                            gx_bn, gx_cb,
                            gy_bn, gy_cb,
                            order_bn, order_cb,
                            cofactor_bn, cofactor_cb,
                            seed_bn, seed_cb);
                    }
                }
            }
            finally
            {
                if (refAdded)
                    key.DangerousRelease();
            }
        }

        internal static unsafe ECParameters EvpPKeyGetEcCurveParameters(
            SafeEvpPKeyHandle key,
            bool includePrivate)
        {
            ECCurve.ECCurveType curveType;
            SafeBignumHandle qx_bn, qy_bn, d_bn, p_bn, a_bn, b_bn, gx_bn, gy_bn, order_bn, cofactor_bn, seed_bn;
            int qx_cb, qy_cb, p_cb, a_cb, b_cb, gx_cb, gy_cb, order_cb, cofactor_cb, seed_cb, d_cb;

            int rc = CryptoNative_EvpPKeyGetEcCurveParameters(
                key,
                includePrivate,
                out curveType,
                out qx_bn, out qx_cb,
                out qy_bn, out qy_cb,
                out d_bn, out d_cb,
                out p_bn, out p_cb,
                out a_bn, out a_cb,
                out b_bn, out b_cb,
                out gx_bn, out gx_cb,
                out gy_bn, out gy_cb,
                out order_bn, out order_cb,
                out cofactor_bn, out cofactor_cb,
                out seed_bn, out seed_cb);

            using (qx_bn)
            using (qy_bn)
            using (d_bn)
            using (p_bn)
            using (a_bn)
            using (b_bn)
            using (gx_bn)
            using (gy_bn)
            using (order_bn)
            using (cofactor_bn)
            using (seed_bn)
            {
                if (rc == -1)
                {
                    throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                }
                else if (rc != 1)
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                return GetEcCurveParameters(
                    curveType,
                    qx_bn, qx_cb,
                    qy_bn, qy_cb,
                    d_bn, d_cb,
                    p_bn, p_cb,
                    a_bn, a_cb,
                    b_bn, b_cb,
                    gx_bn, gx_cb,
                    gy_bn, gy_cb,
                    order_bn, order_cb,
                    cofactor_bn, cofactor_cb,
                    seed_bn, seed_cb);
            }
        }

        private static ECParameters GetEcCurveParameters(
            ECCurve.ECCurveType curveType,
            SafeBignumHandle qx_bn, int qx_cb,
            SafeBignumHandle qy_bn, int qy_cb,
            SafeBignumHandle d_bn, int d_cb,
            SafeBignumHandle p_bn, int p_cb,
            SafeBignumHandle a_bn, int a_cb,
            SafeBignumHandle b_bn, int b_cb,
            SafeBignumHandle gx_bn, int gx_cb,
            SafeBignumHandle gy_bn, int gy_cb,
            SafeBignumHandle order_bn, int order_cb,
            SafeBignumHandle cofactor_bn, int cofactor_cb,
            SafeBignumHandle seed_bn, int seed_cb)
        {
            int cbFieldLength;
            int pFieldLength;
            if (curveType == ECCurve.ECCurveType.Characteristic2)
            {
                // Match Windows semantics where a,b,gx,gy,qx,qy have same length
                // Treat length of m separately as it is not tied to other fields for Char2 (Char2 not supported by Windows)
                cbFieldLength = GetMax([ a_cb, b_cb, gx_cb, gy_cb, qx_cb, qy_cb ]);
                pFieldLength = p_cb;
            }
            else
            {
                // Match Windows semantics where p,a,b,gx,gy,qx,qy have same length
                cbFieldLength = GetMax([ p_cb, a_cb, b_cb, gx_cb, gy_cb, qx_cb, qy_cb ]);
                pFieldLength = cbFieldLength;
            }

            // Match Windows semantics where order and d have same length
            int cbSubgroupOrder = GetMax(order_cb, d_cb);

            // Copy values to ECParameters
            ECParameters parameters = default;
            parameters.Q = new ECPoint
            {
                X = Crypto.ExtractBignum(qx_bn, cbFieldLength),
                Y = Crypto.ExtractBignum(qy_bn, cbFieldLength)
            };
            parameters.D = d_cb == 0 ? null : Crypto.ExtractBignum(d_bn, cbSubgroupOrder);

            ECCurve curve = parameters.Curve;
            curve.CurveType = curveType;
            curve.A = Crypto.ExtractBignum(a_bn, cbFieldLength)!;
            curve.B = Crypto.ExtractBignum(b_bn, cbFieldLength)!;
            curve.G = new ECPoint
            {
                X = Crypto.ExtractBignum(gx_bn, cbFieldLength),
                Y = Crypto.ExtractBignum(gy_bn, cbFieldLength)
            };
            curve.Order = Crypto.ExtractBignum(order_bn, cbSubgroupOrder)!;

            if (curveType == ECCurve.ECCurveType.Characteristic2)
            {
                curve.Polynomial = Crypto.ExtractBignum(p_bn, pFieldLength)!;
            }
            else
            {
                curve.Prime = Crypto.ExtractBignum(p_bn, pFieldLength)!;
            }

            // Optional parameters
            curve.Cofactor = cofactor_cb == 0 ? null : Crypto.ExtractBignum(cofactor_bn, cofactor_cb);
            curve.Seed = seed_cb == 0 ? null : Crypto.ExtractBignum(seed_bn, seed_cb);

            parameters.Curve = curve;
            return parameters;
        }
    }
}
