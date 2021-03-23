// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Reflection.Tests
{
    public class MethodBaseTests
    {
        [Fact]
        public void CallingConvention_Get_ReturnsExpected()
        {
            var method = new SubMethodBase();
            Assert.Equal(CallingConventions.Standard, method.CallingConvention);
        }

        [Fact]
        public void ContainsGenericParameters_Get_ReturnsExpected()
        {
            var method = new SubMethodBase();
            Assert.False(method.ContainsGenericParameters);
        }

        [Theory]
        [InlineData((MethodAttributes)0, false)]
        [InlineData(MethodAttributes.Abstract, true)]
        [InlineData(MethodAttributes.Abstract | MethodAttributes.Virtual, true)]
        [InlineData(MethodAttributes.Virtual, false)]
        public void IsAbstract_Get_ReturnsExpected(MethodAttributes attributes, bool expected)
        {
            var method = new SubMethodBase
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, method.IsAbstract);
        }

        [Theory]
        [InlineData((MethodAttributes)0, false)]
        [InlineData(MethodAttributes.Assembly, true)]
        [InlineData(MethodAttributes.Assembly | MethodAttributes.Virtual, true)]
        [InlineData(MethodAttributes.Family, false)]
        [InlineData(MethodAttributes.FamANDAssem, false)]
        [InlineData(MethodAttributes.FamORAssem, false)]
        [InlineData(MethodAttributes.Private, false)]
        [InlineData(MethodAttributes.Public, false)]
        [InlineData(MethodAttributes.Virtual, false)]
        public void IsAssembly_Get_ReturnsExpected(MethodAttributes attributes, bool expected)
        {
            var method = new SubMethodBase
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, method.IsAssembly);
        }

        [Fact]
        public void IsConstructedGenericMethod_Get_ReturnsExpected()
        {
            var method = new SubMethodBase();
            Assert.False(method.IsConstructedGenericMethod);
        }

        [Theory]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        public void IsConstructedGenericMethod_GetCustom_ReturnsExpected(bool isGenericMethod, bool isGenericMethodDefinition, bool expected)
        {
            var method = new CustomMethodBase
            {
                IsGenericMethodAction = () => isGenericMethod,
                IsGenericMethodDefinitionAction = () => isGenericMethodDefinition,
            };
            Assert.Equal(expected, method.IsConstructedGenericMethod);
        }

        [Theory]
        [InlineData((MethodAttributes)0, false)]
        [InlineData(MethodAttributes.RTSpecialName, false)]
        [InlineData(MethodAttributes.RTSpecialName | MethodAttributes.Virtual, false)]
        [InlineData(MethodAttributes.RTSpecialName | MethodAttributes.Static, false)]
        [InlineData(MethodAttributes.Virtual, false)]
        [InlineData(MethodAttributes.Static, false)]
        public void IsConstructor_Get_ReturnsExpected(MethodAttributes attributes, bool expected)
        {
            var method = new SubMethodBase
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, method.IsConstructor);
        }

        [Theory]
        [InlineData((MethodAttributes)0, false)]
        [InlineData(MethodAttributes.Family, true)]
        [InlineData(MethodAttributes.Family | MethodAttributes.Virtual, true)]
        [InlineData(MethodAttributes.Assembly, false)]
        [InlineData(MethodAttributes.FamANDAssem, false)]
        [InlineData(MethodAttributes.FamORAssem, false)]
        [InlineData(MethodAttributes.Private, false)]
        [InlineData(MethodAttributes.Public, false)]
        [InlineData(MethodAttributes.Virtual, false)]
        public void IsFamily_Get_ReturnsExpected(MethodAttributes attributes, bool expected)
        {
            var method = new SubMethodBase
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, method.IsFamily);
        }

        [Theory]
        [InlineData((MethodAttributes)0, false)]
        [InlineData(MethodAttributes.FamANDAssem, true)]
        [InlineData(MethodAttributes.FamANDAssem | MethodAttributes.Virtual, true)]
        [InlineData(MethodAttributes.Family, false)]
        [InlineData(MethodAttributes.Assembly, false)]
        [InlineData(MethodAttributes.FamORAssem, false)]
        [InlineData(MethodAttributes.Private, false)]
        [InlineData(MethodAttributes.Public, false)]
        [InlineData(MethodAttributes.Virtual, false)]
        public void IsFamilyAndAssembly_Get_ReturnsExpected(MethodAttributes attributes, bool expected)
        {
            var method = new SubMethodBase
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, method.IsFamilyAndAssembly);
        }

        [Theory]
        [InlineData((MethodAttributes)0, false)]
        [InlineData(MethodAttributes.FamORAssem, true)]
        [InlineData(MethodAttributes.FamORAssem | MethodAttributes.Virtual, true)]
        [InlineData(MethodAttributes.Family, false)]
        [InlineData(MethodAttributes.FamANDAssem, false)]
        [InlineData(MethodAttributes.Assembly, false)]
        [InlineData(MethodAttributes.Private, false)]
        [InlineData(MethodAttributes.Public, false)]
        [InlineData(MethodAttributes.Virtual, false)]
        public void IsFamilyOrAssembly_Get_ReturnsExpected(MethodAttributes attributes, bool expected)
        {
            var method = new SubMethodBase
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, method.IsFamilyOrAssembly);
        }

        [Fact]
        public void IsGenericMethod_Get_ReturnsExpected()
        {
            var method = new SubMethodBase();
            Assert.False(method.IsGenericMethod);
        }

        [Fact]
        public void IsGenericMethodDefinition_Get_ReturnsExpected()
        {
            var method = new SubMethodBase();
            Assert.False(method.IsGenericMethodDefinition);
        }

        [Theory]
        [InlineData((MethodAttributes)0, false)]
        [InlineData(MethodAttributes.Final, true)]
        [InlineData(MethodAttributes.Final | MethodAttributes.Virtual, true)]
        [InlineData(MethodAttributes.Virtual, false)]
        public void IsFinal_Get_ReturnsExpected(MethodAttributes attributes, bool expected)
        {
            var method = new SubMethodBase
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, method.IsFinal);
        }

        [Theory]
        [InlineData((MethodAttributes)0, false)]
        [InlineData(MethodAttributes.HideBySig, true)]
        [InlineData(MethodAttributes.HideBySig | MethodAttributes.Virtual, true)]
        [InlineData(MethodAttributes.Virtual, false)]
        public void IsHideBySig_Get_ReturnsExpected(MethodAttributes attributes, bool expected)
        {
            var method = new SubMethodBase
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, method.IsHideBySig);
        }

        [Theory]
        [InlineData((MethodAttributes)0, false)]
        [InlineData(MethodAttributes.Private, true)]
        [InlineData(MethodAttributes.Private | MethodAttributes.Virtual, true)]
        [InlineData(MethodAttributes.Family, false)]
        [InlineData(MethodAttributes.FamANDAssem, false)]
        [InlineData(MethodAttributes.FamORAssem, false)]
        [InlineData(MethodAttributes.Assembly, false)]
        [InlineData(MethodAttributes.Public, false)]
        [InlineData(MethodAttributes.Virtual, false)]
        public void IsPrivate_Get_ReturnsExpected(MethodAttributes attributes, bool expected)
        {
            var method = new SubMethodBase
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, method.IsPrivate);
        }

        [Theory]
        [InlineData((MethodAttributes)0, false)]
        [InlineData(MethodAttributes.Public, true)]
        [InlineData(MethodAttributes.Public | MethodAttributes.Virtual, true)]
        [InlineData(MethodAttributes.Family, false)]
        [InlineData(MethodAttributes.FamANDAssem, false)]
        [InlineData(MethodAttributes.FamORAssem, false)]
        [InlineData(MethodAttributes.Assembly, false)]
        [InlineData(MethodAttributes.Private, false)]
        [InlineData(MethodAttributes.Virtual, false)]
        public void IsPublic_Get_ReturnsExpected(MethodAttributes attributes, bool expected)
        {
            var method = new SubMethodBase
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, method.IsPublic);
        }

        [Fact]
        public void IsSecurityCritical_Get_ThrowsNotImplementedException()
        {
            var method = new SubMethodBase();
            Assert.Throws<NotImplementedException>(() => method.IsSecurityCritical);
        }

        [Fact]
        public void IsSecuritySafeCritical_Get_ThrowsNotImplementedException()
        {
            var method = new SubMethodBase();
            Assert.Throws<NotImplementedException>(() => method.IsSecuritySafeCritical);
        }

        [Fact]
        public void IsSecurityTransparent_Get_ThrowsNotImplementedException()
        {
            var method = new SubMethodBase();
            Assert.Throws<NotImplementedException>(() => method.IsSecurityTransparent);
        }

        [Theory]
        [InlineData((MethodAttributes)0, false)]
        [InlineData(MethodAttributes.SpecialName, true)]
        [InlineData(MethodAttributes.SpecialName | MethodAttributes.Virtual, true)]
        [InlineData(MethodAttributes.Virtual, false)]
        public void IsSpecialName_Get_ReturnsExpected(MethodAttributes attributes, bool expected)
        {
            var method = new SubMethodBase
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, method.IsSpecialName);
        }

        [Theory]
        [InlineData((MethodAttributes)0, false)]
        [InlineData(MethodAttributes.Static, true)]
        [InlineData(MethodAttributes.Static | MethodAttributes.Virtual, true)]
        [InlineData(MethodAttributes.Virtual, false)]
        public void IsStatic_Get_ReturnsExpected(MethodAttributes attributes, bool expected)
        {
            var method = new SubMethodBase
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, method.IsStatic);
        }

        [Theory]
        [InlineData((MethodAttributes)0, false)]
        [InlineData(MethodAttributes.Virtual, true)]
        [InlineData(MethodAttributes.Virtual | MethodAttributes.Static, true)]
        [InlineData(MethodAttributes.Static, false)]
        public void IsVirtual_Get_ReturnsExpected(MethodAttributes attributes, bool expected)
        {
            var method = new SubMethodBase
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, method.IsVirtual);
        }

        [Fact]
        public void MethodImplementationFlags_Get_ReturnsExpected()
        {
            var method = new SubMethodBase
            {
                GetMethodImplementationFlagsAction = () => MethodImplAttributes.AggressiveInlining
            };
            Assert.Equal(MethodImplAttributes.AggressiveInlining, method.MethodImplementationFlags);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var method = new SubMethodBase();
            yield return new object[] { method, method, true };
            yield return new object[] { method, new SubMethodBase(), false };
            yield return new object[] { method, new object(), false };
            yield return new object[] { method, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Invoke_ReturnsExpected(MethodBase method, object other, bool expected)
        {
            Assert.Equal(expected, method.Equals(other));
        }

        public static IEnumerable<object[]> OperatorEquals_TestData()
        {
            var method = new SubMethodBase();
            yield return new object[] { null, null, true };
            yield return new object[] { null, method, false };
            yield return new object[] { method, method, true };
            yield return new object[] { method, new SubMethodBase(), false };
            yield return new object[] { method, null, false };

            yield return new object[] { new AlwaysEqualsMethodBase(), null, false };
            yield return new object[] { null, new AlwaysEqualsMethodBase(), false };
            yield return new object[] { new AlwaysEqualsMethodBase(), new SubMethodBase(), true };
            yield return new object[] { new SubMethodBase(), new AlwaysEqualsMethodBase(), false };
            yield return new object[] { new AlwaysEqualsMethodBase(), new AlwaysEqualsMethodBase(), true };
        }

        [Theory]
        [MemberData(nameof(OperatorEquals_TestData))]
        public void OperatorEquals_Invoke_ReturnsExpected(MethodBase method1, MethodBase method2, bool expected)
        {
            Assert.Equal(expected, method1 == method2);
            Assert.Equal(!expected, method1 != method2);
        }

        [Fact]
        public void GetGenericArguments_Invoke_ReturnsExpected()
        {
            var method = new SubMethodBase();
            Assert.Throws<NotSupportedException>(() => method.GetGenericArguments());
        }

        [Fact]
        public void GetHashCode_Invoke_ReturnsExpected()
        {
            var method = new SubMethodBase();
            Assert.NotEqual(0, method.GetHashCode());
            Assert.Equal(method.GetHashCode(), method.GetHashCode());
        }

        public static IEnumerable<object[]> Invoke_Object_ObjectArray_TestData()
        {
            yield return new object[] { null, null, null };
            yield return new object[] { new object(), Array.Empty<object>(), new object() };
            yield return new object[] { new object(), new object[] { null }, new object() };
            yield return new object[] { new object(), new object[] { new object() }, new object() };
        }

        [Theory]
        [MemberData(nameof(Invoke_Object_ObjectArray_TestData))]
        public void Invoke_InvokeObjectObjectArray_ReturnsExpected(object obj, object[] parameters, object result)
        {
            var method = new SubMethodBase
            {
                InvokeAction = (objParam, invokeAttrParam, binderParam, parametersParam, cultureParam) =>
                {
                    Assert.Same(obj, objParam);
                    Assert.Equal(BindingFlags.Default, invokeAttrParam);
                    Assert.Null(binderParam);
                    Assert.Same(parameters, parametersParam);
                    Assert.Null(cultureParam);
                    return result;
                }
            };
            Assert.Same(result, method.Invoke(obj, parameters));
        }

        [Fact]
        public static void Test_GetCurrentMethod()
        {
            MethodBase m = MethodBase.GetCurrentMethod();
            Assert.Equal(nameof(Test_GetCurrentMethod), m.Name);
            Assert.True(m.IsStatic);
            Assert.True(m.IsPublic);
            Assert.True(m.DeclaringType == typeof(MethodBaseTests));
        }

        [Fact]
        public static void Test_GetCurrentMethod_Inlineable()
        {
            // Verify that the result is not affected by inlining optimizations
            MethodBase m = GetCurrentMethod_InlineableWrapper();
            Assert.Equal(nameof(GetCurrentMethod_InlineableWrapper), m.Name);
            Assert.True(m.IsStatic);
            Assert.False(m.IsPublic);
            Assert.True(m.DeclaringType == typeof(MethodBaseTests));
        }

        private static MethodBase GetCurrentMethod_InlineableWrapper()
        {
            return MethodBase.GetCurrentMethod();
        }

        [Theory]
        [InlineData("MyOtherMethod", BindingFlags.Static | BindingFlags.Public, "MyOtherMethod", BindingFlags.Static | BindingFlags.Public, true)]  // Same methods
        [InlineData("MyOtherMethod", BindingFlags.Static | BindingFlags.Public, "MyOtherMethod", BindingFlags.Static | BindingFlags.NonPublic, false)]  // Two methods of the same name
        [InlineData("MyAnotherMethod", BindingFlags.Static | BindingFlags.NonPublic, "MyOtherMethod", BindingFlags.Static | BindingFlags.NonPublic, false)]  // Two similar methods with different names
        public static void TestEqualityMethods(string methodName1, BindingFlags bindingFlags1, string methodName2, BindingFlags bindingFlags2, bool expected)
        {
            MethodBase mb1 = typeof(MethodBaseTests).GetMethod(methodName1, bindingFlags1);
            MethodBase mb2 = typeof(MethodBaseTests).GetMethod(methodName2, bindingFlags2);
            Assert.Equal(expected, mb1 == mb2);
            Assert.NotEqual(expected, mb1 != mb2);
        }

        [Fact]
        public static void TestMethodBody()
        {
            MethodBase mbase = typeof(MethodBaseTests).GetMethod(nameof(MyOtherMethod), BindingFlags.Static | BindingFlags.Public);
            MethodBody mb = mbase.GetMethodBody();
            Assert.True(mb.InitLocals);  // local variables are initialized
#if DEBUG
            Assert.Equal(2, mb.MaxStackSize);
            Assert.Equal(3, mb.LocalVariables.Count);

            foreach (LocalVariableInfo lvi in mb.LocalVariables)
            {
                if (lvi.LocalIndex == 0) { Assert.Equal(typeof(int), lvi.LocalType); }
                if (lvi.LocalIndex == 1) { Assert.Equal(typeof(string), lvi.LocalType); }
                if (lvi.LocalIndex == 2) { Assert.Equal(typeof(bool), lvi.LocalType); }
            }
#else
            Assert.Equal(1, mb.MaxStackSize);
            Assert.Equal(2, mb.LocalVariables.Count);

            foreach (LocalVariableInfo lvi in mb.LocalVariables)
            {
                if (lvi.LocalIndex == 0) { Assert.Equal(typeof(int), lvi.LocalType); }
                if (lvi.LocalIndex == 1) { Assert.Equal(typeof(string), lvi.LocalType); }
            }
#endif
        }

        private static int MyAnotherMethod(int x)
        {
            return x+1;
        }

        private static int MyOtherMethod(int x)
        {
            return x+1;
        }

#pragma warning disable xUnit1013 // Public method should be marked as test
#pragma warning disable 0219  // field is never used
        public static void MyOtherMethod(object arg)
        {
            int var1 = 2;
            string var2 = "I am a string";

            if (arg == null)
            {
                throw new ArgumentNullException("Input arg cannot be null.");
            }
        }
#pragma warning restore 0219  // field is never used
#pragma warning restore xUnit1013 // Public method should be marked as test

        [Fact]
        public static void Test_GetCurrentMethod_ConstructedGenericMethod()
        {
            MethodInfo mi = typeof(MethodBaseTests).GetMethod(nameof(MyFakeGenericMethod), BindingFlags.NonPublic | BindingFlags.Static);
            MethodBase m = mi.MakeGenericMethod(typeof(byte));

            Assert.Equal(nameof(MyFakeGenericMethod), m.Name);
            Assert.Equal(typeof(MethodBaseTests), m.ReflectedType);
            Assert.True(m.IsGenericMethod);
            Assert.False(m.IsGenericMethodDefinition);
            Assert.True(m.IsConstructedGenericMethod);
            Assert.Equal(new Type[] { typeof(byte) }, m.GetGenericArguments());
        }

        [Fact]
        public static void Test_GetCurrentMethod_GenericMethodDefinition()
        {
            MethodBase m = typeof(MethodBaseTests).GetMethod(nameof(MyFakeGenericMethod), BindingFlags.NonPublic | BindingFlags.Static);

            Assert.Equal(nameof(MyFakeGenericMethod), m.Name);
            Assert.Equal(typeof(MethodBaseTests), m.ReflectedType);
            Assert.True(m.IsGenericMethod);
            Assert.True(m.IsGenericMethodDefinition);
            Assert.False(m.IsConstructedGenericMethod);
            Assert.Equal("T", Assert.Single(m.GetGenericArguments()).Name);
        }

        private static void MyFakeGenericMethod<T>()
        {
        }

        private class CustomMethodBase : SubMethodBase
        {
            public Func<bool> IsGenericMethodAction { get; set; }

            public override bool IsGenericMethod
            {
                get
                {
                    if (IsGenericMethodAction == null)
                    {
                        return base.IsGenericMethod;
                    }

                    return IsGenericMethodAction();
                }
            }

            public Func<bool> IsGenericMethodDefinitionAction { get; set; }

            public override bool IsGenericMethodDefinition
            {
                get
                {
                    if (IsGenericMethodDefinitionAction == null)
                    {
                        return base.IsGenericMethodDefinition;
                    }

                    return IsGenericMethodDefinitionAction();
                }
            }
        }

        private class AlwaysEqualsMethodBase : SubMethodBase
        {
            public override bool Equals(object obj) => true;

            public override int GetHashCode() => base.GetHashCode();
        }

        private class SubMethodBase : MethodBase
        {
            public MethodAttributes AttributesResult { get; set; }

            public override MethodAttributes Attributes => AttributesResult;

            public override RuntimeMethodHandle MethodHandle => throw new NotImplementedException();

            public override Type DeclaringType => throw new NotImplementedException();

            public override MemberTypes MemberType => throw new NotImplementedException();

            public override string Name => throw new NotImplementedException();

            public override Type ReflectedType => throw new NotImplementedException();

            public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();

            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();

            public Func<MethodImplAttributes> GetMethodImplementationFlagsAction { get; set; }

            public override MethodImplAttributes GetMethodImplementationFlags() => GetMethodImplementationFlagsAction();

            public override ParameterInfo[] GetParameters() => throw new NotImplementedException();

            public Func<object, BindingFlags, Binder, object[], CultureInfo, object> InvokeAction { get; set; }

            public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) => InvokeAction(obj, invokeAttr, binder, parameters, culture);

            public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
        }
    }
}
