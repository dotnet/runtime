// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Cryptography;

class Program
{
    static int Main(string[] args)
    {
        using (RSA rsa = RSA.Create())
        {
            RSAParameters pubPriv = rsa.ExportParameters(true);
            string xmlPriv = rsa.ToXmlString(true);

            using (RSA rsaPriv = RSA.Create())
            {
                rsaPriv.FromXmlString(xmlPriv);

                if (!KeyEquals(pubPriv, rsaPriv.ExportParameters(true)))
                {
                    return -1;
                }
            }
        }

        return 100;
    }

    private static bool KeyEquals(in RSAParameters expected, in RSAParameters actual)
    {
        return expected.Modulus.SequenceEqual(actual.Modulus) &&
            expected.Exponent.SequenceEqual(actual.Exponent) &&
            expected.P.SequenceEqual(actual.P) &&
            expected.DP.SequenceEqual(actual.DP) &&
            expected.Q.SequenceEqual(actual.Q) &&
            expected.DQ.SequenceEqual(actual.DQ) &&
            expected.InverseQ.SequenceEqual(actual.InverseQ) &&
            expected.D.SequenceEqual(actual.D);
    }
}
