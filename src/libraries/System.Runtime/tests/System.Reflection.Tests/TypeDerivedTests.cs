// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    internal class TypeWithNullUnderlyingSystemType : MockType
    {
        protected override TypeAttributes GetAttributeFlagsImpl() => TypeAttributes.Class;
        public override bool IsGenericParameter => false;
        public override Type UnderlyingSystemType => null;
    }

    public class TypeDerivedTests
    {
        [Fact]
        public void IsAssignableFrom_NullUnderlyingSystemType()
        {
            var testType = new TypeWithNullUnderlyingSystemType();
            Assert.Null(testType.UnderlyingSystemType);
            Assert.True(testType.IsAssignableFrom(testType));

            Type compareType = typeof(int);
            Assert.False(testType.IsAssignableFrom(compareType));
            Assert.False(compareType.IsAssignableFrom(testType));
        }

        [Fact]
        public void IsAssignableTo_NullUnderlyingSystemType()
        {
            var testType = new TypeWithNullUnderlyingSystemType();
            Assert.Null(testType.UnderlyingSystemType);
            Assert.True(testType.IsAssignableTo(testType));

            Type compareType = typeof(int);
            Assert.False(testType.IsAssignableTo(compareType));
            Assert.False(compareType.IsAssignableTo(testType));

            Assert.False(testType.IsAssignableTo(null));
            Assert.False(typeof(object).IsAssignableTo(null));
        }
    }
}
