// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        internal struct AndroidECKeyArrayParameters
        {
            public byte[]? qx;
            public byte[]? qy;
            public byte[]? d;
            public int qx_length;
            public int qy_length;
            public int d_length;
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_EcKeyCreateByKeyParameters", CharSet = CharSet.Ansi)]
        private static extern int EcKeyCreateByKeyParameters(
            out SafeEcKeyHandle key,
            string oid,
            ref AndroidECKeyArrayParameters parameters);

        internal static SafeEcKeyHandle EcKeyCreateByKeyParameters(
            string oid,
            ref AndroidECKeyArrayParameters parameters)
        {
            SafeEcKeyHandle key;
            int rc = EcKeyCreateByKeyParameters(out key, oid, ref parameters);
            if (rc == -1)
            {
                key?.Dispose();

                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CurveNotSupported, oid));
            }
            return key;
        }

        internal struct AndroidECKeyExplicitParameters
        {
            public byte[]? qx;
            public byte[]? qy;
            public byte[]? d;
            public byte[]? p;
            public byte[]? a;
            public byte[]? b;
            public byte[]? gx;
            public byte[]? gy;
            public byte[]? order;
            public byte[]? cofactor;
            public byte[]? seed;
            public int qx_length;
            public int qy_length;
            public int d_length;
            public int p_length;
            public int a_length;
            public int b_length;
            public int gx_length;
            public int gy_length;
            public int order_length;
            public int cofactor_length;
            public int seed_length;
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_EcKeyCreateByExplicitParameters")]
        internal static extern SafeEcKeyHandle EcKeyCreateByExplicitParameters(
            ECCurve.ECCurveType curveType, ref AndroidECKeyExplicitParameters parameters);

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

            var androidParameters = new AndroidECKeyExplicitParameters
            {
                qx = null, qx_length = 0,
                qy = null, qy_length = 0,
                d = null, d_length = 0,
                p = p, p_length = p.Length,
                a = curve.A, a_length = curve.A!.Length,
                b = curve.B, b_length = curve.B!.Length,
                gx = curve.G.X, gx_length = curve.G.X!.Length,
                gy = curve.G.Y, gy_length = curve.G.Y!.Length,
                order = curve.Order, order_length = curve.Order!.Length,
                cofactor = curve.Cofactor, cofactor_length = curve.Cofactor!.Length,
                seed = curve.Seed, seed_length = curve.Seed == null ? 0 : curve.Seed.Length
            };

            SafeEcKeyHandle key = EcKeyCreateByExplicitParameters(
                curve.CurveType, ref androidParameters);

            if (key == null || key.IsInvalid)
            {
                if (key != null)
                    key.Dispose();
                throw new CryptographicException();
            }

            return key;
        }

        internal struct AndroidECKeyParameters : IDisposable
        {
            public SafeBignumHandle? qx_bn;
            public SafeBignumHandle? qy_bn;
            public SafeBignumHandle? d_bn;
            public int qx_cb;
            public int qy_cb;
            public int d_cb;

            public void Dispose()
            {
                qx_bn?.Dispose();
                qy_bn?.Dispose();
                d_bn?.Dispose();
            }
        }

        [DllImport(Libraries.CryptoNative)]
        private static extern int AndroidCryptoNative_GetECKeyParameters(
            SafeEcKeyHandle key,
            [MarshalAs(UnmanagedType.U4)] bool includePrivate,
            out AndroidECKeyParameters parameters);

        internal static ECParameters GetECKeyParameters(
            SafeEcKeyHandle key,
            bool includePrivate)
        {
            AndroidECKeyParameters androidParameters = default;
            ECParameters parameters = default;

            int rc = AndroidCryptoNative_GetECKeyParameters(
                key,
                includePrivate,
                out androidParameters);

            using (androidParameters)
            {
                if (rc == -1)
                {
                    throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                }
                else if (rc != 1)
                {
                    throw new CryptographicException();
                }

                // Match Windows semantics where qx, qy, and d have same length
                int keySizeBits = EcKeyGetSize(key);
                int expectedSize = (keySizeBits + 7) / 8;
                int cbKey = GetMax(androidParameters.qx_cb, androidParameters.qy_cb, androidParameters.d_cb);

                Debug.Assert(
                    cbKey <= expectedSize,
                    $"Expected output size was {expectedSize}, which a parameter exceeded. qx={androidParameters.qx_cb}, qy={androidParameters.qy_cb}, d={androidParameters.d_cb}");

                cbKey = GetMax(cbKey, expectedSize);

                parameters.Q = new ECPoint
                {
                    X = Crypto.ExtractBignum(androidParameters.qx_bn, cbKey),
                    Y = Crypto.ExtractBignum(androidParameters.qy_bn, cbKey)
                };
                parameters.D = androidParameters.d_cb == 0 ? null : Crypto.ExtractBignum(androidParameters.d_bn, cbKey);
            }

            return parameters;
        }

        private struct AndroidECCurveParameters : IDisposable
        {
            public SafeBignumHandle? qx_bn;
            public SafeBignumHandle? qy_bn;
            public SafeBignumHandle? p_bn;
            public SafeBignumHandle? a_bn;
            public SafeBignumHandle? b_bn;
            public SafeBignumHandle? gx_bn;
            public SafeBignumHandle? gy_bn;
            public SafeBignumHandle? order_bn;
            public SafeBignumHandle? cofactor_bn;
            public SafeBignumHandle? seed_bn;
            public SafeBignumHandle? d_bn;
            public int qx_cb;
            public int qy_cb;
            public int p_cb;
            public int a_cb;
            public int b_cb;
            public int gx_cb;
            public int gy_cb;
            public int order_cb;
            public int cofactor_cb;
            public int seed_cb;
            public int d_cb;

            public void Dispose()
            {
                qx_bn?.Dispose();
                qy_bn?.Dispose();
                p_bn?.Dispose();
                a_bn?.Dispose();
                b_bn?.Dispose();
                gx_bn?.Dispose();
                gy_bn?.Dispose();
                order_bn?.Dispose();
                cofactor_bn?.Dispose();
                seed_bn?.Dispose();
                d_bn?.Dispose();
            }
        }

        [DllImport(Libraries.CryptoNative)]
        private static extern int AndroidCryptoNative_GetECCurveParameters(
            SafeEcKeyHandle key,
            bool includePrivate,
            out ECCurve.ECCurveType curveType,
            out AndroidECCurveParameters parameters);

        internal static ECParameters GetECCurveParameters(
            SafeEcKeyHandle key,
            bool includePrivate)
        {
            ECCurve.ECCurveType curveType;
            AndroidECCurveParameters androidParameters = default;

            int rc = AndroidCryptoNative_GetECCurveParameters(
                key,
                includePrivate,
                out curveType,
                out androidParameters);

            using (androidParameters)
            {
                if (rc == -1)
                {
                    throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                }
                else if (rc != 1)
                {
                    throw new CryptographicException();
                }

                int cbFieldLength;
                int pFieldLength;
                if (curveType == ECCurve.ECCurveType.Characteristic2)
                {
                    // Match Windows semantics where a,b,gx,gy,qx,qy have same length
                    // Treat length of m separately as it is not tied to other fields for Char2 (Char2 not supported by Windows)
                    cbFieldLength = GetMax(new[] { androidParameters.a_cb, androidParameters.b_cb, androidParameters.gx_cb, androidParameters.gy_cb, androidParameters.qx_cb, androidParameters.qy_cb });
                    pFieldLength = androidParameters.p_cb;
                }
                else
                {
                    // Match Windows semantics where p,a,b,gx,gy,qx,qy have same length
                    cbFieldLength = GetMax(new[] { androidParameters.p_cb, androidParameters.a_cb, androidParameters.b_cb, androidParameters.gx_cb, androidParameters.gy_cb, androidParameters.qx_cb, androidParameters.qy_cb });
                    pFieldLength = cbFieldLength;
                }

                // Match Windows semantics where order and d have same length
                int cbSubgroupOrder = GetMax(androidParameters.order_cb, androidParameters.d_cb);

                // Copy values to ECParameters
                ECParameters parameters = default;
                parameters.Q = new ECPoint
                {
                    X = Crypto.ExtractBignum(androidParameters.qx_bn, cbFieldLength),
                    Y = Crypto.ExtractBignum(androidParameters.qy_bn, cbFieldLength)
                };
                parameters.D = androidParameters.d_cb == 0 ? null : Crypto.ExtractBignum(androidParameters.d_bn, cbSubgroupOrder);

                var curve = parameters.Curve;
                curve.CurveType = curveType;
                curve.A = Crypto.ExtractBignum(androidParameters.a_bn, cbFieldLength)!;
                curve.B = Crypto.ExtractBignum(androidParameters.b_bn, cbFieldLength)!;
                curve.G = new ECPoint
                {
                    X = Crypto.ExtractBignum(androidParameters.gx_bn, cbFieldLength),
                    Y = Crypto.ExtractBignum(androidParameters.gy_bn, cbFieldLength)
                };
                curve.Order = Crypto.ExtractBignum(androidParameters.order_bn, cbSubgroupOrder)!;

                if (curveType == ECCurve.ECCurveType.Characteristic2)
                {
                    curve.Polynomial = Crypto.ExtractBignum(androidParameters.p_bn, pFieldLength)!;
                }
                else
                {
                    curve.Prime = Crypto.ExtractBignum(androidParameters.p_bn, pFieldLength)!;
                }

                // Optional parameters
                curve.Cofactor = androidParameters.cofactor_cb == 0 ? null : Crypto.ExtractBignum(androidParameters.cofactor_bn, androidParameters.cofactor_cb);
                curve.Seed = androidParameters.seed_cb == 0 ? null : Crypto.ExtractBignum(androidParameters.seed_bn, androidParameters.seed_cb);

                parameters.Curve = curve;
                return parameters;
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
