// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        [Fact]
        public void VerifyConstructor2()
        {
            var header = new SignatureHeader(SignatureKind.Method, SignatureCallingConvention.StdCall, SignatureAttributes.ExplicitThis | SignatureAttributes.Generic);
            Assert.Equal(0x52, header.RawValue);

            Assert.Equal(SignatureKind.Method, header.Kind);
            Assert.Equal(SignatureCallingConvention.StdCall, header.CallingConvention);
            Assert.Equal(SignatureAttributes.ExplicitThis | SignatureAttributes.Generic, header.Attributes);

            // no validation, since the header can be created with arbitrary raw value anyways
            Assert.Equal(0xff, new SignatureHeader((SignatureKind)0xff, 0, 0).RawValue);
            Assert.Equal(0xff, new SignatureHeader(0, (SignatureCallingConvention)0xff, 0).RawValue);
            Assert.Equal(0xff, new SignatureHeader(0, 0, (SignatureAttributes)0xff).RawValue);
        }
    }
}
