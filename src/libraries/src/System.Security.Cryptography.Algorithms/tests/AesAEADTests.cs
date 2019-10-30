// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Security.Cryptography.Algorithms.Tests
{
    public abstract class AesAEADTests
    {
        protected static bool MatchesKeySizes(int size, KeySizes keySizes)
        {
            if (size < keySizes.MinSize || size > keySizes.MaxSize)
                return false;

            return ((size - keySizes.MinSize) % keySizes.SkipSize) == 0;
        }

        public static IEnumerable<object[]> GetValidSizes(KeySizes validSizes, int minValue = 0, int maxValue = 17)
        {
            for (int i = minValue; i <= maxValue; i++)
            {
                if (MatchesKeySizes(i, validSizes))
                    yield return new object[] { i };
            }
        }

        public static IEnumerable<object[]> GetInvalidSizes(KeySizes validSizes, int minValue = 0, int maxValue = 17)
        {
            for (int i = minValue; i <= maxValue; i++)
            {
                if (!MatchesKeySizes(i, validSizes))
                    yield return new object[] { i };
            }
        }

        public class AEADTest
        {
            public string Source { get; set; }
            public int CaseId { get; set; }
            public byte[] Key { get; set; }
            public byte[] Nonce { get; set; }
            public byte[] Plaintext { get; set; }
            public byte[] Ciphertext { get; set; }
            public byte[] AssociatedData { get; set; }
            public byte[] Tag { get; set; }

            private static string BitLength(byte[] data)
            {
                if (data == null)
                    return "0";
                return (data.Length * 8).ToString();
            }

            public override string ToString()
            {
                return
                    $"{Source} - {CaseId} ({BitLength(Key)}/{BitLength(Nonce)}/" +
                    $"{BitLength(Plaintext)}/{BitLength(Tag)}/{BitLength(AssociatedData)})";
            }
        }
    }
}
