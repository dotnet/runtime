// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Assemblies;
using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyAlgorithmIdAttributeTests
    {
        [Theory]
        [InlineData((Configuration.Assemblies.AssemblyHashAlgorithm)(Configuration.Assemblies.AssemblyHashAlgorithm.None - 1))]
        [InlineData(Configuration.Assemblies.AssemblyHashAlgorithm.MD5)]
        [InlineData(Configuration.Assemblies.AssemblyHashAlgorithm.SHA1)]
        [InlineData(Configuration.Assemblies.AssemblyHashAlgorithm.SHA256)]
        [InlineData(Configuration.Assemblies.AssemblyHashAlgorithm.SHA384)]
        [InlineData(Configuration.Assemblies.AssemblyHashAlgorithm.SHA512)]
        public void Ctor_AssemblyHashAlgorithm(Configuration.Assemblies.AssemblyHashAlgorithm algorithmId)
        {
            var attribute = new AssemblyAlgorithmIdAttribute(algorithmId);
            Assert.Equal((uint)algorithmId, attribute.AlgorithmId);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(32771)]
        [InlineData(32772)]
        [InlineData(32780)]
        [InlineData(32781)]
        [InlineData(32782)]
        [InlineData(uint.MaxValue)]
        public void Ctor_UInt(uint algorithmId)
        {
            var attribute = new AssemblyAlgorithmIdAttribute(algorithmId);
            Assert.Equal(algorithmId, attribute.AlgorithmId);
        }
    }
}
