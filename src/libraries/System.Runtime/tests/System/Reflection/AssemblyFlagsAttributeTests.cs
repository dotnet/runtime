// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

#pragma warning disable 0618

namespace System.Reflection.Tests
{
    public class AssemblyFlagsAttributeTests
    {
        [Theory]
        [InlineData(AssemblyNameFlags.None)]
        [InlineData(AssemblyNameFlags.EnableJITcompileOptimizer)]
        [InlineData(AssemblyNameFlags.EnableJITcompileTracking)]
        [InlineData(AssemblyNameFlags.PublicKey)]
        [InlineData(AssemblyNameFlags.Retargetable)]
        [InlineData((AssemblyNameFlags)int.MinValue)]
        [InlineData((AssemblyNameFlags)(AssemblyNameFlags.None - 1))]
        [InlineData((AssemblyNameFlags)int.MaxValue)]
        public void Ctor_AssemblyNameFlags(AssemblyNameFlags assemblyFlags)
        {
            var attribute = new AssemblyFlagsAttribute(assemblyFlags);
            Assert.Equal((uint)assemblyFlags, attribute.Flags);
            Assert.Equal((int)assemblyFlags, attribute.AssemblyFlags);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(16384)]
        [InlineData(32768)]
        [InlineData(1)]
        [InlineData(256)]
        [InlineData(uint.MaxValue)]
        public void Ctor_UInt(uint flags)
        {
            var attribute = new AssemblyFlagsAttribute(flags);
            Assert.Equal(flags, attribute.Flags);
            Assert.Equal((int)flags, attribute.AssemblyFlags);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(16384)]
        [InlineData(32768)]
        [InlineData(1)]
        [InlineData(256)]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        public void Ctor_Int(int assemblyFlags)
        {
            var attribute = new AssemblyFlagsAttribute(assemblyFlags);
            Assert.Equal((uint)assemblyFlags, attribute.Flags);
            Assert.Equal(assemblyFlags, attribute.AssemblyFlags);
        }
    }
}

#pragma warning restore 0618
