// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Security.Cryptography.Algorithms.Tests
{
    public abstract class CommonAEADTests
    {
        public static IEnumerable<object[]> EncryptTamperAADDecryptTestInputs()
        {
            // yield return { dataLength, additionalDataLength };
            yield return new object[] { 0, 1 };
            yield return new object[] { 0, 30 };
            yield return new object[] { 1, 1 };
            yield return new object[] { 1, 100 };
            yield return new object[] { 7, 12 };
            yield return new object[] { 16, 16 };
            yield return new object[] { 17, 29 };
            yield return new object[] { 32, 7 };
            yield return new object[] { 41, 25 };
            yield return new object[] { 48, 22 };
            yield return new object[] { 50, 5 };
        }

        public static IEnumerable<object[]> PlaintextAndCiphertextSizeDifferTestInputs()
        {
            // yield return { ptLen, ctLen };
            yield return new object[] { 0, 1 };
            yield return new object[] { 1, 0 };
            yield return new object[] { 3, 4 };
            yield return new object[] { 4, 3 };
            yield return new object[] { 20, 120 };
            yield return new object[] { 120, 20 };
        }

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
