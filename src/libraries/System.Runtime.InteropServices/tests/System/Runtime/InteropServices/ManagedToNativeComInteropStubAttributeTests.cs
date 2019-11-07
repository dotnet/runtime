// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class ManagedToNativeComInteropStubAttributeTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData(typeof(int), "MethodName")]
        public void Ctor_ClassType_MethodName(Type classType, string methodName)
        {
            var attribute = new ManagedToNativeComInteropStubAttribute(classType, methodName);
            Assert.Equal(classType, attribute.ClassType);
            Assert.Equal(methodName, attribute.MethodName);
        }
    }
}
