// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace System.Security.Cryptography
{
    public abstract partial class DSA : AsymmetricAlgorithm
    {
        private const string CounterElementName = "PgenCounter";

        private static byte[] ReadRequiredElement(
            ref XmlKeyHelper.ParseState state,
            string name,
            int sizeHint = -1)
        {
            byte[] ret = XmlKeyHelper.ReadCryptoBinary(ref state, name, sizeHint);

            if (ret == null)
            {
                throw new CryptographicException(
                    SR.Format(SR.Cryptography_InvalidFromXmlString, nameof(DSA), name));
            }

            return ret;
        }

        public override void FromXmlString(string xmlString)
        {
            // ParseDocument does the nullcheck for us.
            XmlKeyHelper.ParseState state = XmlKeyHelper.ParseDocument(xmlString);

            byte[] p = ReadRequiredElement(ref state, nameof(DSAParameters.P));
            byte[] q = ReadRequiredElement(ref state, nameof(DSAParameters.Q));
            byte[] g = ReadRequiredElement(ref state, nameof(DSAParameters.G), p.Length);
            byte[] y = ReadRequiredElement(ref state, nameof(DSAParameters.Y), p.Length);
            byte[] j = XmlKeyHelper.ReadCryptoBinary(ref state, nameof(DSAParameters.J));
            byte[] seed = XmlKeyHelper.ReadCryptoBinary(ref state, nameof(DSAParameters.Seed));
            int counter = 0;
            byte[] x = XmlKeyHelper.ReadCryptoBinary(ref state, nameof(DSAParameters.X), q.Length);

            if (seed != null)
            {
                byte[] counterBytes = ReadRequiredElement(ref state, CounterElementName);
                counter = XmlKeyHelper.ReadCryptoBinaryInt32(counterBytes);
            }

            DSAParameters dsaParameters = new DSAParameters
            {
                P = p,
                Q = q,
                G = g,
                Y = y,
                J = j,
                Seed = seed,
                Counter = counter,
                X = x,
            };

            // Check for Counter without Seed after getting X, since that prevents an extra cycle in the
            // canonical element order.
            if (dsaParameters.Seed == null)
            {
                if (XmlKeyHelper.HasElement(ref state, CounterElementName))
                {
                    throw new CryptographicException(
                        SR.Format(
                            SR.Cryptography_InvalidFromXmlString,
                            nameof(DSA),
                            nameof(DSAParameters.Seed)));
                }
            }

            ImportParameters(dsaParameters);
        }

        public override string ToXmlString(bool includePrivateParameters)
        {
            // The format of this output is based on the xmldsig ds:DSAKeyValue value, except
            // * It writes values as xml:base64Binary instead of ds:CryptoBinary
            //   * It doesn't strip off leading 0x00 byte values before base64
            // * It doesn't emit the output in a namespace
            // * When includePrivateParameters is true it writes an X element.
            //
            // These deviations are inherited from .NET Framework.

            // P is KeySizeInBytes long.
            // Q is 160 to 256 bits long, or 20 to 32 bytes.
            // G is the same size as P
            // Y is the same size as P
            // X is the same size as Q
            //
            // Each field gets base64 encoded (after dropping leading 0x00 bytes)
            // so P is (KeySizeInBytes + 2) / 3 * 4, then plus 7 (<P></P>)
            // (For 3072 that's 519 chars, for 1024 it's 179.)
            // Add in maximum-Q: (32 + 2) / 3 * 4 + 7 => 51
            // Then the "<DSAKeyValue></DSAKeyValue>" (27).
            // Grand total, 3 * P + 2 * Q + 27 => 1686 (3072) or 666 (1024).
            // KeySizeInBytes * 2 / 3 comes out to 2048 or 682, so call that good enough.

            // Rarely, keys will export the J or Seed values, and they may cause the
            // StringBuilder to need to grow.

            DSAParameters keyParameters = ExportParameters(includePrivateParameters);
            StringBuilder builder = new StringBuilder((keyParameters.P.Length << 1) / 3);
            builder.Append("<DSAKeyValue>");
            XmlKeyHelper.WriteCryptoBinary(nameof(DSAParameters.P), keyParameters.P, builder);
            XmlKeyHelper.WriteCryptoBinary(nameof(DSAParameters.Q), keyParameters.Q, builder);
            XmlKeyHelper.WriteCryptoBinary(nameof(DSAParameters.G), keyParameters.G, builder);
            XmlKeyHelper.WriteCryptoBinary(nameof(DSAParameters.Y), keyParameters.Y, builder);

            if (keyParameters.J != null)
            {
                XmlKeyHelper.WriteCryptoBinary(nameof(DSAParameters.J), keyParameters.J, builder);
            }

            if (keyParameters.Seed != null)
            {
                XmlKeyHelper.WriteCryptoBinary(nameof(DSAParameters.Seed), keyParameters.Seed, builder);
                XmlKeyHelper.WriteCryptoBinary(CounterElementName, keyParameters.Counter, builder);
            }

            if (includePrivateParameters)
            {
                if (keyParameters.X == null)
                {
                    // NetFx compat when a 3rd party type lets X be null when
                    // includePrivateParameters is true
                    // (the exception would have been from Convert.ToBase64String)
                    throw new ArgumentNullException("inArray");
                }

                XmlKeyHelper.WriteCryptoBinary(nameof(DSAParameters.X), keyParameters.X, builder);
            }

            builder.Append("</DSAKeyValue>");
            return builder.ToString();
        }
    }
}
