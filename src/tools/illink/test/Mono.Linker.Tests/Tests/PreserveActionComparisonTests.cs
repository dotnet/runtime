// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Mono.Linker.Tests
{
    public class PreserveActionComparisonTests
    {
        [Theory]
        [InlineData(TypePreserve.All, TypePreserve.All, TypePreserve.All)]
        [InlineData(TypePreserve.All, TypePreserve.Methods, TypePreserve.All)]
        [InlineData(TypePreserve.All, TypePreserve.Fields, TypePreserve.All)]
        [InlineData(TypePreserve.All, TypePreserve.Nothing, TypePreserve.All)]
        [InlineData(TypePreserve.Methods, TypePreserve.All, TypePreserve.All)]
        [InlineData(TypePreserve.Methods, TypePreserve.Methods, TypePreserve.Methods)]
        [InlineData(TypePreserve.Methods, TypePreserve.Fields, TypePreserve.All)]
        [InlineData(TypePreserve.Methods, TypePreserve.Nothing, TypePreserve.Methods)]
        [InlineData(TypePreserve.Fields, TypePreserve.All, TypePreserve.All)]
        [InlineData(TypePreserve.Fields, TypePreserve.Methods, TypePreserve.All)]
        [InlineData(TypePreserve.Fields, TypePreserve.Fields, TypePreserve.Fields)]
        [InlineData(TypePreserve.Fields, TypePreserve.Nothing, TypePreserve.Fields)]
        [InlineData(TypePreserve.Nothing, TypePreserve.All, TypePreserve.All)]
        [InlineData(TypePreserve.Nothing, TypePreserve.Methods, TypePreserve.Methods)]
        [InlineData(TypePreserve.Nothing, TypePreserve.Fields, TypePreserve.Fields)]
        public void VerifyBehaviorOfChoosePreserveActionWhichPreservesTheMost(TypePreserve left, TypePreserve right, TypePreserve expected)
        {
            Assert.Equal(expected, AnnotationStore.ChoosePreserveActionWhichPreservesTheMost(left, right));
            Assert.Equal(expected, AnnotationStore.ChoosePreserveActionWhichPreservesTheMost(right, left));
        }
    }
}
