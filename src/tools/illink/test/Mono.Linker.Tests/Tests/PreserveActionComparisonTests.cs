// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mono.Linker.Tests
{
    [TestClass]
    public class PreserveActionComparisonTests
    {
        [TestMethod]
        [DataRow(TypePreserve.All, TypePreserve.All, TypePreserve.All)]
        [DataRow(TypePreserve.All, TypePreserve.Methods, TypePreserve.All)]
        [DataRow(TypePreserve.All, TypePreserve.Fields, TypePreserve.All)]
        [DataRow(TypePreserve.All, TypePreserve.Nothing, TypePreserve.All)]
        [DataRow(TypePreserve.Methods, TypePreserve.All, TypePreserve.All)]
        [DataRow(TypePreserve.Methods, TypePreserve.Methods, TypePreserve.Methods)]
        [DataRow(TypePreserve.Methods, TypePreserve.Fields, TypePreserve.All)]
        [DataRow(TypePreserve.Methods, TypePreserve.Nothing, TypePreserve.Methods)]
        [DataRow(TypePreserve.Fields, TypePreserve.All, TypePreserve.All)]
        [DataRow(TypePreserve.Fields, TypePreserve.Methods, TypePreserve.All)]
        [DataRow(TypePreserve.Fields, TypePreserve.Fields, TypePreserve.Fields)]
        [DataRow(TypePreserve.Fields, TypePreserve.Nothing, TypePreserve.Fields)]
        [DataRow(TypePreserve.Nothing, TypePreserve.All, TypePreserve.All)]
        [DataRow(TypePreserve.Nothing, TypePreserve.Methods, TypePreserve.Methods)]
        [DataRow(TypePreserve.Nothing, TypePreserve.Fields, TypePreserve.Fields)]
        public void VerifyBehaviorOfChoosePreserveActionWhichPreservesTheMost(TypePreserve left, TypePreserve right, TypePreserve expected)
        {
            Assert.AreEqual(expected, AnnotationStore.ChoosePreserveActionWhichPreservesTheMost(left, right));
            Assert.AreEqual(expected, AnnotationStore.ChoosePreserveActionWhichPreservesTheMost(right, left));
        }
    }
}
