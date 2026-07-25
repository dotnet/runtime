// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Mono.Linker.Tests
{
    public class CodeOptimizationsSettingsTests
    {
        [Fact]
        public void GlobalSettingsOnly()
        {
            CodeOptimizationsSettings cos = new CodeOptimizationsSettings(CodeOptimizations.BeforeFieldInit);
            Assert.Equal(CodeOptimizations.BeforeFieldInit, cos.Global);
            Assert.True(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "any"));
            Assert.False(cos.IsEnabled(CodeOptimizations.Sealer, "any"));
        }

        [Fact]
        public void OneAssemblyIsExcluded()
        {
            CodeOptimizationsSettings cos = new CodeOptimizationsSettings(CodeOptimizations.BeforeFieldInit);
            cos.Disable(CodeOptimizations.BeforeFieldInit, "testasm.dll");

            Assert.Equal(CodeOptimizations.BeforeFieldInit, cos.Global);
            Assert.True(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "any"));
            Assert.False(cos.IsEnabled(CodeOptimizations.Sealer, "any"));
            Assert.False(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "testasm.dll"));
        }

        [Fact]
        public void ExcludedThenIncluded()
        {
            CodeOptimizationsSettings cos = new CodeOptimizationsSettings(CodeOptimizations.BeforeFieldInit);
            cos.Disable(CodeOptimizations.BeforeFieldInit, "testasm.dll");
            cos.Enable(CodeOptimizations.OverrideRemoval | CodeOptimizations.BeforeFieldInit, "testasm.dll");

            Assert.Equal(CodeOptimizations.BeforeFieldInit, cos.Global);
            Assert.True(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "any"));
            Assert.False(cos.IsEnabled(CodeOptimizations.OverrideRemoval, "any"));

            Assert.False(cos.IsEnabled(CodeOptimizations.Sealer, "any"));
            Assert.True(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "testasm.dll"));
        }

        [Fact]
        public void OnlyOneOptIsDisabled()
        {
            CodeOptimizationsSettings cos = new CodeOptimizationsSettings(CodeOptimizations.OverrideRemoval);
            cos.Disable(CodeOptimizations.BeforeFieldInit, "testasm.dll");

            Assert.False(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "testasm.dll"));
            Assert.False(cos.IsEnabled(CodeOptimizations.Sealer, "testasm.dll"));
            Assert.False(cos.IsEnabled(CodeOptimizations.UnreachableBodies, "testasm.dll"));
        }

        [Fact]
        public void PropagateFromGlobal()
        {
            CodeOptimizationsSettings cos = new CodeOptimizationsSettings(CodeOptimizations.BeforeFieldInit);
            cos.Disable(CodeOptimizations.IPConstantPropagation | CodeOptimizations.OverrideRemoval, "testasm.dll");

            Assert.False(cos.IsEnabled(CodeOptimizations.IPConstantPropagation, "testasm.dll"));
            Assert.False(cos.IsEnabled(CodeOptimizations.IPConstantPropagation, "any"));

            Assert.False(cos.IsEnabled(CodeOptimizations.OverrideRemoval, "testasm.dll"));
            Assert.False(cos.IsEnabled(CodeOptimizations.OverrideRemoval, "any"));

            Assert.True(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "testasm.dll"));
            Assert.True(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "any"));
        }
    }
}
