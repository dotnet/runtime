// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Internal.Cryptography
{
    //
    // These constants enumerate the supported AlgorithmIdentifier.KeyLength values.
    //
    // The AlgorithmIdentifier.KeyLength property has one valid use - to specify to the EnvelopedCms constructor which key size to use for RC2 if 
    // you don't want the default (128). In all other cases, it is ignored and is best thought of as an obsolete member.
    //
    // DES and 3DES only support one size. AES has separate Oids for various key sizes. ECC has shown that key size isn't even
    // a valid way to pinpoint an algorithm in the long run.
    //
    // The AlgorithmIdentifier you get back from a decoded EnvelopedCms's ContentEncryptionAlgorithm only populates the KeySize
    // property for RC2/RC4/Des/3DES for backward compatibility. For everything else, it is set to zero and code should not rely on it.
    //
    internal static class KeyLengths
    {
        public const int Rc2_40Bit = 40;
        public const int Rc2_56Bit = 56;
        public const int Rc2_64Bit = 64;
        public const int Rc2_128Bit = 128;
        public const int Rc4Max_128Bit = 128;
        public const int Des_64Bit = 64;
        public const int TripleDes_192Bit = 192;

        public const int DefaultKeyLengthForRc2AndRc4 = 128;
    }
}
