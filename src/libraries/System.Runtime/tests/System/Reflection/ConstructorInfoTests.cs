// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Reflection.Tests
{
    public class ConstructorInfoTests
    {
        [Fact]
        public void ConstructorName_Get_ReturnsExpected()
        {
            Assert.Equal(".ctor", ConstructorInfo.ConstructorName);
        }

        [Fact]
        public void MemberType_Get_ReturnsExpected()
        {
            var ctor = new SubConstructorInfo();
            Assert.Equal(MemberTypes.Constructor, ctor.MemberType);
        }

        [Fact]
        public void TypeConstructorName_Get_ReturnsExpected()
        {
            Assert.Equal(".cctor", ConstructorInfo.TypeConstructorName);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var ctor = new SubConstructorInfo();
            yield return new object[] { ctor, ctor, true };
            yield return new object[] { ctor, new SubConstructorInfo(), false };
            yield return new object[] { ctor, new object(), false };
            yield return new object[] { ctor, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Invoke_ReturnsExpected(ConstructorInfo ctor, object other, bool expected)
        {
            Assert.Equal(expected, ctor.Equals(other));
        }

        public static IEnumerable<object[]> OperatorEquals_TestData()
        {
            var ctor = new SubConstructorInfo();
            yield return new object[] { null, null, true };
            yield return new object[] { null, ctor, false };
            yield return new object[] { ctor, ctor, true };
            yield return new object[] { ctor, new SubConstructorInfo(), false };
            yield return new object[] { ctor, null, false };

            yield return new object[] { new AlwaysEqualsConstructorInfo(), null, false };
            yield return new object[] { null, new AlwaysEqualsConstructorInfo(), false };
            yield return new object[] { new AlwaysEqualsConstructorInfo(), new SubConstructorInfo(), true };
            yield return new object[] { new SubConstructorInfo(), new AlwaysEqualsConstructorInfo(), false };
            yield return new object[] { new AlwaysEqualsConstructorInfo(), new AlwaysEqualsConstructorInfo(), true };
        }

        [Theory]
        [MemberData(nameof(OperatorEquals_TestData))]
        public void OperatorEquals_Invoke_ReturnsExpected(ConstructorInfo ctor1, ConstructorInfo ctor2, bool expected)
        {
            Assert.Equal(expected, ctor1 == ctor2);
            Assert.Equal(!expected, ctor1 != ctor2);
        }

        [Fact]
        public void GetGenericArguments_Invoke_ReturnsExpected()
        {
            var ctor = new SubConstructorInfo();
            Assert.Throws<NotSupportedException>(() => ctor.GetGenericArguments());
        }

        [Fact]
        public void GetHashCode_Invoke_ReturnsExpected()
        {
            var ctor = new SubConstructorInfo();
            Assert.NotEqual(0, ctor.GetHashCode());
            Assert.Equal(ctor.GetHashCode(), ctor.GetHashCode());
        }

        public static IEnumerable<object[]> Invoke_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { Array.Empty<object>(), new object() };
            yield return new object[] { new object[] { null }, new object() };
            yield return new object[] { new object[] { new object() }, new object() };
        }

        [Theory]
        [MemberData(nameof(Invoke_TestData))]
        public void Invoke_Invoke_ReturnsExpected(object[] parameters, object result)
        {
            var method = new SubConstructorInfo
            {
                InvokeAction = (invokeAttrParam, binderParam, parametersParam, cultureParam) =>
                {
                    Assert.Equal(BindingFlags.Default, invokeAttrParam);
                    Assert.Null(binderParam);
                    Assert.Same(parameters, parametersParam);
                    Assert.Null(cultureParam);
                    return result;
                }
            };
            Assert.Same(result, method.Invoke(parameters));
        }

        private class AlwaysEqualsConstructorInfo : SubConstructorInfo
        {
            public override bool Equals(object obj) => true;

            public override int GetHashCode() => base.GetHashCode();
        }

        private class SubConstructorInfo : ConstructorInfo
        {
            public MethodAttributes AttributesResult { get; set; }

            public override MethodAttributes Attributes => AttributesResult;

            public override RuntimeMethodHandle MethodHandle => throw new NotImplementedException();

            public override Type DeclaringType => throw new NotImplementedException();

            public override string Name => throw new NotImplementedException();

            public override Type ReflectedType => throw new NotImplementedException();

            public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();

            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();

            public override MethodImplAttributes GetMethodImplementationFlags() => throw new NotImplementedException();

            public override ParameterInfo[] GetParameters() => throw new NotImplementedException();

            public Func<BindingFlags, Binder, object[], CultureInfo, object> InvokeAction { get; set; }

            public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) => InvokeAction(invokeAttr, binder, parameters, culture);

            public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) => throw new NotImplementedException();

            public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
        }
    }
}
