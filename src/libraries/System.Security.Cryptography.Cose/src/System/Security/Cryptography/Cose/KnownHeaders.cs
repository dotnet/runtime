// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Cose
{
    // https://datatracker.ietf.org/doc/html/rfc8152#section-3.1 Table 2.
    internal static class KnownHeaders
    {
        public const int Alg = 1;
        public const int Crit = 2;
        public const int ContentType = 3;
        public const int Kid = 4;
        public const int IV = 5;
        public const int PartialIV = 6;
        public const int CounterSignature = 7;
    }
}
