// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Reflection.Tests
{
    public class MethodInfoTests
    {
        [Fact]
        public void MemberType_Get_ReturnsExpected()
        {
            var method = new SubMethodInfo();
            Assert.Equal(MemberTypes.Method, method.MemberType);
        }

        [Fact]
        public void ReturnParameter_Get_ThrowsNotImplementedException()
        {
            var method = new SubMethodInfo();
            Assert.Throws<NotImplementedException>(() => method.ReturnParameter);
        }

        [Fact]
        public void ReturnType_Get_ThrowsNotImplementedException()
        {
            var method = new SubMethodInfo();
            Assert.Throws<NotImplementedException>(() => method.ReturnType);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(int))]
        public void CreateDelegate_InvokeType_ThrowsNotSupportedException(Type delegateType)
        {
            var method = new SubMethodInfo();
            Assert.Throws<NotSupportedException>(() => method.CreateDelegate(delegateType));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(typeof(int), 1)]
        public void CreateDelegate_InvokeTypeObject_ThrowsNotSupportedException(Type delegateType, object target)
        {
            var method = new SubMethodInfo();
            Assert.Throws<NotSupportedException>(() => method.CreateDelegate(delegateType, target));
        }

        [Fact]
        public void CreateDelegate_InvokeGeneric_ThrowsNotSupportedException()
        {
            var method = new SubMethodInfo();
            Assert.Throws<NotSupportedException>(() => method.CreateDelegate<EventHandler>());
        }

        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        public void CreateDelegate_InvokeGenericObject_ThrowsNotSupportedException(object target)
        {
            var method = new SubMethodInfo();
            Assert.Throws<NotSupportedException>(() => method.CreateDelegate<EventHandler>(target));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var method = new SubMethodInfo();
            yield return new object[] { method, method, true };
            yield return new object[] { method, new SubMethodInfo(), false };
            yield return new object[] { method, new object(), false };
            yield return new object[] { method, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Invoke_ReturnsExpected(MethodInfo method, object other, bool expected)
        {
            Assert.Equal(expected, method.Equals(other));
        }

        public static IEnumerable<object[]> OperatorEquals_TestData()
        {
            var method = new SubMethodInfo();
            yield return new object[] { null, null, true };
            yield return new object[] { null, method, false };
            yield return new object[] { method, method, true };
            yield return new object[] { method, new SubMethodInfo(), false };
            yield return new object[] { method, null, false };

            yield return new object[] { new AlwaysEqualsMethodInfo(), null, false };
            yield return new object[] { null, new AlwaysEqualsMethodInfo(), false };
            yield return new object[] { new AlwaysEqualsMethodInfo(), new SubMethodInfo(), true };
            yield return new object[] { new SubMethodInfo(), new AlwaysEqualsMethodInfo(), false };
            yield return new object[] { new AlwaysEqualsMethodInfo(), new AlwaysEqualsMethodInfo(), true };
        }

        [Theory]
        [MemberData(nameof(OperatorEquals_TestData))]
        public void OperatorEquals_Invoke_ReturnsExpected(MethodInfo method1, MethodInfo method2, bool expected)
        {
            Assert.Equal(expected, method1 == method2);
            Assert.Equal(!expected, method1 != method2);
        }

        [Fact]
        public void GetGenericArguments_Invoke_ReturnsExpected()
        {
            var method = new SubMethodInfo();
            Assert.Throws<NotSupportedException>(() => method.GetGenericArguments());
        }

        [Fact]
        public void GetGenericMethodDefinition_Invoke_ReturnsExpected()
        {
            var method = new SubMethodInfo();
            Assert.Throws<NotSupportedException>(() => method.GetGenericMethodDefinition());
        }

        [Fact]
        public void GetHashCode_Invoke_ReturnsExpected()
        {
            var method = new SubMethodInfo();
            Assert.NotEqual(0, method.GetHashCode());
            Assert.Equal(method.GetHashCode(), method.GetHashCode());
        }

        public static IEnumerable<object[]> MakeGenericType_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new Type[0] };
            yield return new object[] { new Type[] { null } };
            yield return new object[] { new Type[] { typeof(int) } };
        }

        [Theory]
        [MemberData(nameof(MakeGenericType_TestData))]
        public void MakeGenericMethod_Invoke_ReturnsExpected(Type[] typeArguments)
        {
            var method = new SubMethodInfo();
            Assert.Throws<NotSupportedException>(() => method.MakeGenericMethod(typeArguments));
        }

        private class AlwaysEqualsMethodInfo : SubMethodInfo
        {
            public override bool Equals(object obj) => true;

            public override int GetHashCode() => base.GetHashCode();
        }

        private class SubMethodInfo : MethodInfo
        {
            public MethodAttributes AttributesResult { get; set; }

            public override MethodAttributes Attributes => AttributesResult;

            public override ICustomAttributeProvider ReturnTypeCustomAttributes => throw new NotImplementedException();

            public override RuntimeMethodHandle MethodHandle => throw new NotImplementedException();

            public override Type DeclaringType => throw new NotImplementedException();

            public override string Name => throw new NotImplementedException();

            public override Type ReflectedType => throw new NotImplementedException();

            public override MethodInfo GetBaseDefinition() => throw new NotImplementedException();

            public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();

            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();

            public override MethodImplAttributes GetMethodImplementationFlags() => throw new NotImplementedException();

            public override ParameterInfo[] GetParameters() => throw new NotImplementedException();

            public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) => throw new NotImplementedException();

            public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
        }
    }
}
