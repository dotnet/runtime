// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace DotnetFuzzing.Fuzzers
{
    internal sealed class ConvertToBase64Fuzzer : IFuzzer
    {
        private const int Base64LineBreakPosition = 76;

        public string[] TargetAssemblies => [];

        public string[] TargetCoreLibPrefixes { get; } = ["System.Convert"];

        public void FuzzTarget(ReadOnlySpan<byte> bytes)
        {
            TestToStringToCharArray(bytes, Base64FormattingOptions.None, PoisonPagePlacement.Before);
            TestToStringToCharArray(bytes, Base64FormattingOptions.InsertLineBreaks, PoisonPagePlacement.Before);
            TestToStringToCharArray(bytes, Base64FormattingOptions.None, PoisonPagePlacement.After);
            TestToStringToCharArray(bytes, Base64FormattingOptions.InsertLineBreaks, PoisonPagePlacement.After);
        }

        private static void TestToStringToCharArray(ReadOnlySpan<byte> bytes, Base64FormattingOptions options, PoisonPagePlacement poison)
        { 
            using PooledBoundedMemory<byte> inputPoisoned = PooledBoundedMemory<byte>.Rent(bytes, poison);
            int encodedLength = ToBase64_CalculateOutputLength(bytes.Length, options == Base64FormattingOptions.InsertLineBreaks);
            using PooledBoundedMemory<char> destPoisoned = PooledBoundedMemory<char>.Rent(encodedLength, poison);
            Span<byte> input = inputPoisoned.Span;
            char[] dest = destPoisoned.Span.ToArray();

            string toStringResult = Convert.ToBase64String(input, options);
            byte[] decoded = Convert.FromBase64String(toStringResult);

            Assert.SequenceEqual(input, decoded);

            int written = Convert.ToBase64CharArray(input.ToArray(), 0, input.Length, dest, 0, options);
            decoded = Convert.FromBase64CharArray(dest, 0, written);

            Assert.SequenceEqual(input, decoded);
            Assert.Equal(toStringResult, new string(dest, 0, written));
        }

        private static int ToBase64_CalculateOutputLength(int inputLength, bool insertLineBreaks)
        {
            uint outlen = ((uint)inputLength + 2) / 3 * 4;

            if (outlen == 0)
                return 0;

            if (insertLineBreaks)
            {
                (uint newLines, uint remainder) = Math.DivRem(outlen, Base64LineBreakPosition);
                if (remainder == 0)
                {
                    --newLines;
                }
                outlen += newLines * 2; // 2 line break chars added: "\r\n"
            }

            return (int)outlen;
        }
    }
}
