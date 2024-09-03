// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;

namespace DotnetFuzzing.Fuzzers
{
    internal sealed class AssemblyNameInfoFuzzer : IFuzzer
    {
        public string[] TargetAssemblies => ["System.Reflection.Metadata"];

        public string[] TargetCoreLibPrefixes => [];

        public string Dictionary => "assemblynameinfo.dict";

        public void FuzzTarget(ReadOnlySpan<byte> bytes)
        {
            Span<char> chars = new char[Encoding.UTF8.GetCharCount(bytes)];
            Encoding.UTF8.GetChars(bytes, chars);

            using PooledBoundedMemory<char> inputPoisonedBefore = PooledBoundedMemory<char>.Rent(chars, PoisonPagePlacement.Before);
            using PooledBoundedMemory<char> inputPoisonedAfter = PooledBoundedMemory<char>.Rent(chars, PoisonPagePlacement.After);

            Test(inputPoisonedBefore);
            Test(inputPoisonedAfter);
        }

        private static void Test(PooledBoundedMemory<char> inputPoisoned)
        {
            if (AssemblyNameInfo.TryParse(inputPoisoned.Span, out AssemblyNameInfo? fromTryParse))
            {
                AssemblyNameInfo fromParse = AssemblyNameInfo.Parse(inputPoisoned.Span);

                Assert.Equal(fromTryParse.Name, fromParse.Name);
                Assert.Equal(fromTryParse.FullName, fromParse.FullName);
                Assert.Equal(fromTryParse.CultureName, fromParse.CultureName);
                Assert.Equal(fromTryParse.Flags, fromParse.Flags);
                Assert.Equal(fromTryParse.Version, fromParse.Version);
                Assert.SequenceEqual(fromTryParse.PublicKeyOrToken.AsSpan(), fromParse.PublicKeyOrToken.AsSpan());

                Assert.Equal(fromTryParse.ToAssemblyName().Name, fromParse.ToAssemblyName().Name);
                Assert.Equal(fromTryParse.ToAssemblyName().Version, fromParse.ToAssemblyName().Version);
                Assert.Equal(fromTryParse.ToAssemblyName().ContentType, fromParse.ToAssemblyName().ContentType);
                Assert.Equal(fromTryParse.ToAssemblyName().CultureName, fromParse.ToAssemblyName().CultureName);

                Assert.Equal(fromTryParse.Name, fromParse.ToAssemblyName().Name);
                Assert.Equal(fromTryParse.CultureName, fromParse.ToAssemblyName().CultureName);
                Assert.Equal(fromTryParse.Version, fromParse.ToAssemblyName().Version);

                // AssemblyNameInfo.FullName can be different than AssemblyName.FullName:
                // AssemblyNameInfo includes public key, AssemblyName only its Token.

                try
                {
                    Assert.Equal(fromTryParse.ToAssemblyName().FullName, fromParse.ToAssemblyName().FullName);
                }
                catch (System.Security.SecurityException)
                {
                    // AssemblyName.FullName performs public key validation, AssemblyNameInfo does not (on purpose).
                }
            }
            else
            {
                try
                {
                    _ = AssemblyNameInfo.Parse(inputPoisoned.Span);
                }
                catch (ArgumentException)
                {
                    return;
                }

                throw new Exception("Parsing was supposed to fail!");
            }
        }
    }
}
