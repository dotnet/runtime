// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NUnit.Framework;

namespace Mono.Linker.Tests
{
    [TestFixture]
    public class CodeOptimizationsSettingsTests
    {
        [Test]
        public void GlobalSettingsOnly()
        {
            CodeOptimizationsSettings cos = new CodeOptimizationsSettings(CodeOptimizations.BeforeFieldInit);
            Assert.AreEqual(CodeOptimizations.BeforeFieldInit, cos.Global);
            Assert.That(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "any"));
            Assert.False(cos.IsEnabled(CodeOptimizations.Sealer, "any"));
        }

        [Test]
        public void OneAssemblyIsExcluded()
        {
            CodeOptimizationsSettings cos = new CodeOptimizationsSettings(CodeOptimizations.BeforeFieldInit);
            cos.Disable(CodeOptimizations.BeforeFieldInit, "testasm.dll");

            Assert.AreEqual(CodeOptimizations.BeforeFieldInit, cos.Global);
            Assert.That(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "any"));
            Assert.False(cos.IsEnabled(CodeOptimizations.Sealer, "any"));
            Assert.False(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "testasm.dll"));
        }

        [Test]
        public void ExcludedThenIncluded()
        {
            CodeOptimizationsSettings cos = new CodeOptimizationsSettings(CodeOptimizations.BeforeFieldInit);
            cos.Disable(CodeOptimizations.BeforeFieldInit, "testasm.dll");
            cos.Enable(CodeOptimizations.OverrideRemoval | CodeOptimizations.BeforeFieldInit, "testasm.dll");

            Assert.AreEqual(CodeOptimizations.BeforeFieldInit, cos.Global);
            Assert.That(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "any"));
            Assert.False(cos.IsEnabled(CodeOptimizations.OverrideRemoval, "any"));

            Assert.False(cos.IsEnabled(CodeOptimizations.Sealer, "any"));
            Assert.That(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "testasm.dll"));
        }

        [Test]
        public void OnlyOneOptIsDisabled()
        {
            CodeOptimizationsSettings cos = new CodeOptimizationsSettings(CodeOptimizations.OverrideRemoval);
            cos.Disable(CodeOptimizations.BeforeFieldInit, "testasm.dll");

            Assert.False(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "testasm.dll"));
            Assert.False(cos.IsEnabled(CodeOptimizations.Sealer, "testasm.dll"));
            Assert.False(cos.IsEnabled(CodeOptimizations.UnreachableBodies, "testasm.dll"));
        }

        [Test]
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
