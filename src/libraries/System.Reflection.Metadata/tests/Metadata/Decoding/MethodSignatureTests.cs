// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Xunit;

namespace System.Reflection.Metadata.Decoding.Tests
{
    public class MethodSignatureTests
    {
        [Fact]
        public void VerifyConstructor()
        {
            var header = new SignatureHeader(2);
            var returnType = 5;
            var requiredParameterCount = 10;
            var genericParameterCount = 3;
            var parameterTypes = ImmutableArray.Create(2, 4, 6, 8);

            var methodSignature = new MethodSignature<int>(header, returnType, requiredParameterCount, genericParameterCount, parameterTypes);

            // Verify each field was correctly set
            Assert.Equal(methodSignature.Header, header);
            Assert.Equal(methodSignature.ReturnType, returnType);
            Assert.Equal(methodSignature.RequiredParameterCount, requiredParameterCount);
            Assert.Equal(methodSignature.GenericParameterCount, genericParameterCount);
            Assert.Equal(methodSignature.ParameterTypes, parameterTypes);
        }
    }
}
