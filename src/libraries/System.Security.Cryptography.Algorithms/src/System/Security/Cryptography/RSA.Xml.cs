// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace System.Security.Cryptography
{
    public abstract partial class RSA : AsymmetricAlgorithm
    {
        private static byte[] ReadRequiredElement(
            ref XmlKeyHelper.ParseState state,
            string name,
            int sizeHint = -1)
        {
            byte[] ret = XmlKeyHelper.ReadCryptoBinary(ref state, name, sizeHint);

            if (ret == null)
            {
                throw new CryptographicException(
                    SR.Format(SR.Cryptography_InvalidFromXmlString, nameof(RSA), name));
            }

            return ret;
        }

        public override void FromXmlString(string xmlString)
        {
            // ParseDocument does the nullcheck for us.
            XmlKeyHelper.ParseState state = XmlKeyHelper.ParseDocument(xmlString);

            byte[] n = ReadRequiredElement(ref state, nameof(RSAParameters.Modulus));
            byte[] e = ReadRequiredElement(ref state, nameof(RSAParameters.Exponent));

            int halfN = (n.Length + 1) / 2;

            // .NET Framework doesn't report any element other than Modulus/Exponent as required,
            // it just lets import fail if they're imbalanced.
            byte[] p = XmlKeyHelper.ReadCryptoBinary(ref state, nameof(RSAParameters.P), halfN);
            byte[] q = XmlKeyHelper.ReadCryptoBinary(ref state, nameof(RSAParameters.Q), halfN);
            byte[] dp = XmlKeyHelper.ReadCryptoBinary(ref state, nameof(RSAParameters.DP), halfN);
            byte[] dq = XmlKeyHelper.ReadCryptoBinary(ref state, nameof(RSAParameters.DQ), halfN);
            byte[] qInv = XmlKeyHelper.ReadCryptoBinary(ref state, nameof(RSAParameters.InverseQ), halfN);
            byte[] d = XmlKeyHelper.ReadCryptoBinary(ref state, nameof(RSAParameters.D), n.Length);

            RSAParameters keyParameters = new RSAParameters
            {
                Modulus = n,
                Exponent = e,
                D = d,
                P = p,
                Q = q,
                DP = dp,
                DQ = dq,
                InverseQ = qInv,
            };

            ImportParameters(keyParameters);
        }

        public override string ToXmlString(bool includePrivateParameters)
        {
            // The format of this output is based on the xmldsig ds:RSAKeyValue value, except
            // * It writes values as xml:base64Binary instead of ds:CryptoBinary
            //   * It doesn't strip off leading 0x00 byte values before base64
            // * It doesn't emit the output in a namespace
            // * When includePrivateParameters is true it writes the private key elements.
            //   * D, P, Q, DP, DQ, InverseQ
            //
            // These deviations are inherited from .NET Framework.

            // For a public-only export, the output is like the following, but with no whitespace
            //
            //   <RSAKeyValue>
            //     <Modulus>[base64 modulus]</Modulus>
            //     <Exponent>AQAB</Exponent>
            //   </RSAKeyValue>
            //
            // (using the knowledge that 99.9(etc)% of RSA keys use the same exponent, 65537).
            // rsa.KeySize (bits) / 6 will produce a value just slightly smaller than needed:
            //
            //    KeySize | BytesReq | Div5 | Div6
            //    --------|----------|------|-----
            //      16384 |     2732 | 3276 | 2730
            //       2048 |      344 |  409 |  341
            //       1024 |      172 |  204 |  170
            //        512 |       88 |  102 |   85
            //
            // So just add 3 chars to the overhead.
            // The overhead, otherwise, is 65 chars, plus exponent's actual value.
            // While most keys are AQAB (0x010001) it's technically a variable.
            // CAPI has a limit of 32 bits. CNG-Win7 is unbounded, CNG-Win10 is 64-bits.
            // So call it 12 chars ((64/8 + 2) / 3 * 4).
            // 65 + 32 + 3 = 100.  Nice, round, number.
            //
            // For private keys, D is the same size as Modulus, and P/Q/DP/DQ/InverseQ are
            // each half the size of Modulus.  So their variable payload is 5 * (KeySize / 2 / 6).
            //
            // Their element tags add 58 extra characters, and sprinkle in another 3 each (18 total) for
            // base64 vs div6 padding, for a conditional overhead of 76 chars.

            int keySizeDiv6 = KeySize / 6;
            int initialCapacity = 100 + keySizeDiv6;

            if (includePrivateParameters)
            {
                initialCapacity += 76 + 5 * keySizeDiv6 / 2;
            }

            RSAParameters keyParameters = ExportParameters(includePrivateParameters);
            StringBuilder builder = new StringBuilder(initialCapacity);
            builder.Append("<RSAKeyValue>");
            XmlKeyHelper.WriteCryptoBinary(nameof(RSAParameters.Modulus), keyParameters.Modulus, builder);
            XmlKeyHelper.WriteCryptoBinary(nameof(RSAParameters.Exponent), keyParameters.Exponent, builder);

            if (includePrivateParameters)
            {
                // Match .NET Framework field order.
                XmlKeyHelper.WriteCryptoBinary(nameof(RSAParameters.P), keyParameters.P, builder);
                XmlKeyHelper.WriteCryptoBinary(nameof(RSAParameters.Q), keyParameters.Q, builder);
                XmlKeyHelper.WriteCryptoBinary(nameof(RSAParameters.DP), keyParameters.DP, builder);
                XmlKeyHelper.WriteCryptoBinary(nameof(RSAParameters.DQ), keyParameters.DQ, builder);
                XmlKeyHelper.WriteCryptoBinary(nameof(RSAParameters.InverseQ), keyParameters.InverseQ, builder);
                XmlKeyHelper.WriteCryptoBinary(nameof(RSAParameters.D), keyParameters.D, builder);
            }

            builder.Append("</RSAKeyValue>");
            return builder.ToString();
        }
    }
}
