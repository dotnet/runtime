// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public sealed partial class MLDsaCng : MLDsa
    {
        private CngKey _key;

        /// <summary>
        /// TODO
        /// argnull when key is null, arg when key wrong algo, crypto on error
        /// </summary>
        /// <param name="key"></param>
        [SupportedOSPlatform("windows")]
        public MLDsaCng(CngKey key)
            : base(AlgorithmFromHandle(key, out CngKey duplicateKey))
        {
            _key = duplicateKey;
        }

        private static partial MLDsaAlgorithm AlgorithmFromHandle(CngKey key, out CngKey duplicateKey);

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        public partial CngKey GetCngKey();
    }
}
