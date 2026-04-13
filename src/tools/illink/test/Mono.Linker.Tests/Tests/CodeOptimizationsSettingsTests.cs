// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mono.Linker.Tests
{
    [TestClass]
    public class CodeOptimizationsSettingsTests
    {
        [TestMethod]
        public void GlobalSettingsOnly()
        {
            CodeOptimizationsSettings cos = new CodeOptimizationsSettings(CodeOptimizations.BeforeFieldInit);
            Assert.AreEqual(CodeOptimizations.BeforeFieldInit, cos.Global);
            Assert.IsTrue(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "any"));
            Assert.IsFalse(cos.IsEnabled(CodeOptimizations.Sealer, "any"));
        }

        [TestMethod]
        public void OneAssemblyIsExcluded()
        {
            CodeOptimizationsSettings cos = new CodeOptimizationsSettings(CodeOptimizations.BeforeFieldInit);
            cos.Disable(CodeOptimizations.BeforeFieldInit, "testasm.dll");

            Assert.AreEqual(CodeOptimizations.BeforeFieldInit, cos.Global);
            Assert.IsTrue(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "any"));
            Assert.IsFalse(cos.IsEnabled(CodeOptimizations.Sealer, "any"));
            Assert.IsFalse(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "testasm.dll"));
        }

        [TestMethod]
        public void ExcludedThenIncluded()
        {
            CodeOptimizationsSettings cos = new CodeOptimizationsSettings(CodeOptimizations.BeforeFieldInit);
            cos.Disable(CodeOptimizations.BeforeFieldInit, "testasm.dll");
            cos.Enable(CodeOptimizations.OverrideRemoval | CodeOptimizations.BeforeFieldInit, "testasm.dll");

            Assert.AreEqual(CodeOptimizations.BeforeFieldInit, cos.Global);
            Assert.IsTrue(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "any"));
            Assert.IsFalse(cos.IsEnabled(CodeOptimizations.OverrideRemoval, "any"));

            Assert.IsFalse(cos.IsEnabled(CodeOptimizations.Sealer, "any"));
            Assert.IsTrue(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "testasm.dll"));
        }

        [TestMethod]
        public void OnlyOneOptIsDisabled()
        {
            CodeOptimizationsSettings cos = new CodeOptimizationsSettings(CodeOptimizations.OverrideRemoval);
            cos.Disable(CodeOptimizations.BeforeFieldInit, "testasm.dll");

            Assert.IsFalse(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "testasm.dll"));
            Assert.IsFalse(cos.IsEnabled(CodeOptimizations.Sealer, "testasm.dll"));
            Assert.IsFalse(cos.IsEnabled(CodeOptimizations.UnreachableBodies, "testasm.dll"));
        }

        [TestMethod]
        public void PropagateFromGlobal()
        {
            CodeOptimizationsSettings cos = new CodeOptimizationsSettings(CodeOptimizations.BeforeFieldInit);
            cos.Disable(CodeOptimizations.IPConstantPropagation | CodeOptimizations.OverrideRemoval, "testasm.dll");

            Assert.IsFalse(cos.IsEnabled(CodeOptimizations.IPConstantPropagation, "testasm.dll"));
            Assert.IsFalse(cos.IsEnabled(CodeOptimizations.IPConstantPropagation, "any"));

            Assert.IsFalse(cos.IsEnabled(CodeOptimizations.OverrideRemoval, "testasm.dll"));
            Assert.IsFalse(cos.IsEnabled(CodeOptimizations.OverrideRemoval, "any"));

            Assert.IsTrue(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "testasm.dll"));
            Assert.IsTrue(cos.IsEnabled(CodeOptimizations.BeforeFieldInit, "any"));
        }
    }
}
