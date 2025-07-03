// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace System.Security.Cryptography.Tests
{
    public static partial class CompositeMLDsaTestData
    {
        // TODO add ToString
        public class CompositeMLDsaTestVector
        {
            internal string Id { get; }
            internal CompositeMLDsaAlgorithm Algorithm { get; }
            internal byte[] Message { get; }
            internal byte[] PublicKey { get; }
            internal byte[] Certificate { get; }
            internal byte[] SecretKey { get; }
            internal byte[] Pkcs8 { get; }
            internal byte[] Signature { get; }

            internal CompositeMLDsaTestVector(string tcId, CompositeMLDsaAlgorithm algo, string pk, string x5c, string sk, string sk_pkcs8, string m, string s)
            {
                Id = tcId;
                Algorithm = algo;
                PublicKey = Convert.FromBase64String(pk);
                Certificate = Convert.FromBase64String(x5c);
                SecretKey = Convert.FromBase64String(sk);
                Pkcs8 = Convert.FromBase64String(sk_pkcs8);
                Message = Convert.FromBase64String(m);
                Signature = Convert.FromBase64String(s);
            }

            public override string ToString() => Id;
        }

        // TODO The test vectors from the current spec (at the time of writing) are not correct. The script to
        // generate them is correct, so the ones in this class were generated from that script. These should
        // be updated when the spec is updated with correct test vectors.
        internal static partial CompositeMLDsaTestVector[] AllIetfVectors { get; }

        internal static CompositeMLDsaTestVector[] SupportedAlgorithmIetfVectors =>
            field ??= AllIetfVectors.Where(v => CompositeMLDsa.IsAlgorithmSupported(v.Algorithm)).ToArray();

        public static IEnumerable<object[]>SupportedAlgorithmIetfVectorsTestData =>
            SupportedAlgorithmIetfVectors.Select(v => new object[] { v });
    }
}
