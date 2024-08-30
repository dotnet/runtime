// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace DotnetFuzzing.Fuzzers
{
    internal sealed class TypeNameFuzzer : IFuzzer
    {
        public string[] TargetAssemblies { get; } = ["System.Reflection.Metadata"];
        public string[] TargetCoreLibPrefixes => [];
        public string Dictionary => "typename.dict";

        public void FuzzTarget(ReadOnlySpan<byte> bytes)
        {
            using var poisonAfter = PooledBoundedMemory<char>.Rent(MemoryMarshal.Cast<byte, char>(bytes), PoisonPagePlacement.After);
            using var poisonBefore = PooledBoundedMemory<char>.Rent(MemoryMarshal.Cast<byte, char>(bytes), PoisonPagePlacement.Before);

            Test(poisonAfter.Span);
            Test(poisonBefore.Span);
        }

        private static void Test(Span<char> testSpan)
        {
            if (TypeName.TryParse(testSpan, out TypeName? result1))
            {
                TypeName result2 = TypeName.Parse(testSpan);
                Assert.Equal(result1.Name, result2.Name);
                Assert.Equal(result1.FullName, result2.FullName);
                Assert.Equal(result1.AssemblyQualifiedName, result2.AssemblyQualifiedName);
                Assert.Equal(result1.IsSimple, result2.IsSimple);
                Assert.Equal(result1.IsNested, result2.IsNested);
                Assert.Equal(result1.IsArray, result2.IsArray);
                Assert.Equal(result1.GetNodeCount(), result2.GetNodeCount());
                if (result1.AssemblyName != null)
                {
                    Assert.Equal(result1.AssemblyName.Name, result2.AssemblyName!.Name);
                    Assert.Equal(result1.AssemblyName.FullName, result2.AssemblyName.FullName);
                    Assert.Equal(result1.AssemblyName.CultureName, result2.AssemblyName.CultureName);
                    Assert.Equal(result1.AssemblyName.Version?.ToString(), result2.AssemblyName.Version?.ToString());
                }
                else
                {
                    Assert.Equal(result1.AssemblyName, result2.AssemblyName);
                }
            }
            else
            {
                try
                {
                    TypeName.Parse(testSpan);
                    Assert.Equal(true, false); // should never succeed
                }
                catch (ArgumentException) { }
                catch (InvalidOperationException) { }
            }
        }
    }
}
