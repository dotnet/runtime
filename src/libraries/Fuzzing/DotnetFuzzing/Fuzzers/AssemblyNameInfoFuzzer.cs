// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace DotnetFuzzing.Fuzzers
{
    internal sealed class AssemblyNameInfoFuzzer : IFuzzer
    {
        public string[] TargetAssemblies => ["System.Reflection.Metadata"];

        public string[] TargetCoreLibPrefixes => [];

        public void FuzzTarget(ReadOnlySpan<byte> bytes)
        {
            ReadOnlySpan<char> chars = MemoryMarshal.Cast<byte, char>(bytes);

            using PooledBoundedMemory<char> inputPoisonedBefore = PooledBoundedMemory<char>.Rent(chars, PoisonPagePlacement.Before);
            using PooledBoundedMemory<char> inputPoisonedAfter = PooledBoundedMemory<char>.Rent(chars, PoisonPagePlacement.After);

            Test(inputPoisonedBefore);
            Test(inputPoisonedAfter);
        }

        private static void Test(PooledBoundedMemory<char> inputPoisoned)
        {
            bool shouldSucceed = AssemblyNameInfo.TryParse(inputPoisoned.Span, out _);

            try
            {
                AssemblyNameInfo.Parse(inputPoisoned.Span);
            }
            catch (ArgumentException)
            {
                Assert.Equal(false, shouldSucceed);
                return;
            }

            Assert.Equal(true, shouldSucceed);
        }
    }
}
