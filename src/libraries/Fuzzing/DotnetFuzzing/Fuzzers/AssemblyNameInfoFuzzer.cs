// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Globalization;
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

            Test(inputPoisonedBefore.Span);
            Test(inputPoisonedAfter.Span);
        }

        private static void Test(Span<char> span)
        {
            if (AssemblyNameInfo.TryParse(span, out AssemblyNameInfo? fromTryParse))
            {
                AssemblyNameInfo fromParse = AssemblyNameInfo.Parse(span);

                Assert.Equal(fromTryParse.Name, fromParse.Name);
                Assert.Equal(fromTryParse.FullName, fromParse.FullName);
                Assert.Equal(fromTryParse.CultureName, fromParse.CultureName);
                Assert.Equal(fromTryParse.Flags, fromParse.Flags);
                Assert.Equal(fromTryParse.Version, fromParse.Version);
                Assert.SequenceEqual(fromTryParse.PublicKeyOrToken.AsSpan(), fromParse.PublicKeyOrToken.AsSpan());

                if (!string.IsNullOrEmpty(fromParse.CultureName))
                {
                    try
                    {
                        _ = CultureInfo.GetCultureInfo(fromParse.CultureName);
                    }
                    catch (CultureNotFoundException)
                    {
                        // ToAssemblyName would try to create such a culture and fail.
                        return;
                    }
                }

                Assert.Equal(fromTryParse.ToAssemblyName().Name, fromParse.ToAssemblyName().Name);
                Assert.Equal(fromTryParse.ToAssemblyName().Version, fromParse.ToAssemblyName().Version);
                Assert.Equal(fromTryParse.ToAssemblyName().ContentType, fromParse.ToAssemblyName().ContentType);
                Assert.Equal(fromTryParse.ToAssemblyName().CultureName, fromParse.ToAssemblyName().CultureName);

                Assert.Equal(fromTryParse.Name, fromParse.ToAssemblyName().Name);
                Assert.Equal(fromTryParse.Version, fromParse.ToAssemblyName().Version);

                if (fromTryParse.CultureName is not null)
                {
                    // When converting to AssemblyName, the culture name is lower-cased
                    // by the CultureInfo ctor that calls CultureData.GetCultureData
                    // which lowers the name for caching and normalization purposes.
                    // It lowers only the part before the `-` character, but we lower
                    // the whole string for the sake of simplicity of this test.

                    string lowerCase = fromTryParse.CultureName.ToLower();
                    if (lowerCase != "c")
                    {
                        Assert.Equal(lowerCase, fromParse.ToAssemblyName().CultureName!.ToLower());
                    }
                    else
                    {
                        // Cultures "c" and "C" get mapped to Invariant Culture.
                        Assert.Equal("", fromParse.ToAssemblyName().CultureName);
                        Assert.Equal(CultureInfo.InvariantCulture, fromParse.ToAssemblyName().CultureInfo);
                    }
                }
                else
                {
                    Assert.True(fromParse.ToAssemblyName().CultureName is null);
                }

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
                    _ = AssemblyNameInfo.Parse(span);
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
