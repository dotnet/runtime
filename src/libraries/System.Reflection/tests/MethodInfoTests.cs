// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Tests
{
    public class MethodInfoTests
    {
        [Fact]
        public void CreateDelegate_PublicMethod()
        {
            Type typeTestClass = typeof(MI_BaseClass);

            MI_BaseClass baseClass = (MI_BaseClass)Activator.CreateInstance(typeTestClass);
            MethodInfo virtualMethodInfo = GetMethod(typeTestClass, nameof(MI_BaseClass.VirtualMethod));
            MethodInfo privateInstanceMethodInfo = GetMethod(typeTestClass, "PrivateInstanceMethod");
            MethodInfo publicStaticMethodInfo = GetMethod(typeTestClass, nameof(MI_BaseClass.PublicStaticMethod));

            Delegate methodDelegate = virtualMethodInfo.CreateDelegate(typeof(Delegate_TC_Int));
            object returnValue = ((Delegate_TC_Int)methodDelegate).DynamicInvoke(new object[] { baseClass });
            Assert.Equal(baseClass.VirtualMethod(), returnValue);

            Delegate genMethodDelegate = virtualMethodInfo.CreateDelegate<Delegate_TC_Int>();
            object genReturnValue = genMethodDelegate.DynamicInvoke(new object[] { baseClass });
            Assert.Equal(returnValue, genReturnValue);

            methodDelegate = privateInstanceMethodInfo.CreateDelegate(typeof(Delegate_TC_Int));
            returnValue = ((Delegate_TC_Int)methodDelegate).DynamicInvoke(new object[] { baseClass });
            Assert.Equal(21, returnValue);

            genMethodDelegate = privateInstanceMethodInfo.CreateDelegate<Delegate_TC_Int>();
            genReturnValue = genMethodDelegate.DynamicInvoke(new object[] { baseClass });
            Assert.Equal(returnValue, genReturnValue);

            methodDelegate = virtualMethodInfo.CreateDelegate(typeof(Delegate_Void_Int), baseClass);
            returnValue = ((Delegate_Void_Int)methodDelegate).DynamicInvoke(null);
            Assert.Equal(baseClass.VirtualMethod(), returnValue);

            genMethodDelegate = virtualMethodInfo.CreateDelegate<Delegate_Void_Int>(baseClass);
            genReturnValue = genMethodDelegate.DynamicInvoke(null);
            Assert.Equal(returnValue, genReturnValue);

            methodDelegate = publicStaticMethodInfo.CreateDelegate(typeof(Delegate_Str_Str));
            returnValue = ((Delegate_Str_Str)methodDelegate).DynamicInvoke(new object[] { "85" });
            Assert.Equal("85", returnValue);

            genMethodDelegate = publicStaticMethodInfo.CreateDelegate<Delegate_Str_Str>();
            genReturnValue = genMethodDelegate.DynamicInvoke(new object[] { "85" });
            Assert.Equal(returnValue, genReturnValue);

            methodDelegate = publicStaticMethodInfo.CreateDelegate(typeof(Delegate_Void_Str), "93");
            returnValue = ((Delegate_Void_Str)methodDelegate).DynamicInvoke(null);
            Assert.Equal("93", returnValue);

            genMethodDelegate = publicStaticMethodInfo.CreateDelegate<Delegate_Void_Str>("93");
            genReturnValue = genMethodDelegate.DynamicInvoke(null);
            Assert.Equal(returnValue, genReturnValue);
        }

        [Fact]
        public void CreateDelegate_InheritedMethod()
        {
            Type typeTestClass = typeof(MI_BaseClass);
            Type TestSubClassType = typeof(MI_SubClass);

            MI_SubClass testSubClass = (MI_SubClass)Activator.CreateInstance(TestSubClassType);
            MI_BaseClass testClass = (MI_BaseClass)Activator.CreateInstance(typeTestClass);
            MethodInfo virtualMethodInfo = GetMethod(typeTestClass, nameof(MI_BaseClass.VirtualMethod));

            Delegate methodDelegate = virtualMethodInfo.CreateDelegate(typeof(Delegate_TC_Int));
            object returnValue = ((Delegate_TC_Int)methodDelegate).DynamicInvoke(new object[] { testSubClass });
            Assert.Equal(testSubClass.VirtualMethod(), returnValue);

            Delegate genMethodDelegate = virtualMethodInfo.CreateDelegate<Delegate_TC_Int>();
            object genReturnValue = genMethodDelegate.DynamicInvoke(new object[] { testSubClass });
            Assert.Equal(returnValue, genReturnValue);

            methodDelegate = virtualMethodInfo.CreateDelegate(typeof(Delegate_Void_Int), testSubClass);
            returnValue = ((Delegate_Void_Int)methodDelegate).DynamicInvoke();
            Assert.Equal(testSubClass.VirtualMethod(), returnValue);

            genMethodDelegate = virtualMethodInfo.CreateDelegate<Delegate_Void_Int>(testSubClass);
            genReturnValue = genMethodDelegate.DynamicInvoke();
            Assert.Equal(returnValue, genReturnValue);
        }

        [Fact]
        public void CreateDelegate_GenericMethod()
        {
            Type typeGenericClassString = typeof(MI_GenericClass<string>);

            MI_GenericClass<string> genericClass = (MI_GenericClass<string>)Activator.CreateInstance(typeGenericClassString);

            MethodInfo miMethod1String = GetMethod(typeGenericClassString, nameof(MI_GenericClass<string>.GenericMethod1));
            MethodInfo miMethod2String = GetMethod(typeGenericClassString, nameof(MI_GenericClass<string>.GenericMethod3));
            MethodInfo miMethod2IntGeneric = miMethod2String.MakeGenericMethod(new Type[] { typeof(int) });
            MethodInfo miMethod2StringGeneric = miMethod2String.MakeGenericMethod(new Type[] { typeof(string) });

            Delegate methodDelegate = miMethod1String.CreateDelegate(typeof(Delegate_GC_T_T<string>));
            object returnValue = ((Delegate_GC_T_T<string>)methodDelegate).DynamicInvoke(new object[] { genericClass, "TestGeneric" });
            Assert.Equal(genericClass.GenericMethod1("TestGeneric"), returnValue);

            Delegate genMethodDelegate = miMethod1String.CreateDelegate<Delegate_GC_T_T<string>>();
            object genReturnValue = genMethodDelegate.DynamicInvoke(new object[] { genericClass, "TestGeneric" });
            Assert.Equal(returnValue, genReturnValue);

            methodDelegate = miMethod1String.CreateDelegate(typeof(Delegate_T_T<string>), genericClass);
            returnValue = ((Delegate_T_T<string>)methodDelegate).DynamicInvoke(new object[] { "TestGeneric" });
            Assert.Equal(genericClass.GenericMethod1("TestGeneric"), returnValue);

            genMethodDelegate = miMethod1String.CreateDelegate<Delegate_T_T<string>>(genericClass);
            genReturnValue = genMethodDelegate.DynamicInvoke(new object[] { "TestGeneric" });
            Assert.Equal(returnValue, genReturnValue);

            methodDelegate = miMethod2IntGeneric.CreateDelegate(typeof(Delegate_T_T<int>));
            returnValue = ((Delegate_T_T<int>)methodDelegate).DynamicInvoke(new object[] { 58 });
            Assert.Equal(58, returnValue);

            genMethodDelegate = miMethod2IntGeneric.CreateDelegate<Delegate_T_T<int>>();
            genReturnValue = genMethodDelegate.DynamicInvoke(new object[] { 58 });
            Assert.Equal(returnValue, genReturnValue);

            methodDelegate = miMethod2StringGeneric.CreateDelegate(typeof(Delegate_Void_T<string>), "firstArg");
            returnValue = ((Delegate_Void_T<string>)methodDelegate).DynamicInvoke();
            Assert.Equal("firstArg", returnValue);

            genMethodDelegate = miMethod2StringGeneric.CreateDelegate<Delegate_Void_T<string>>("firstArg");
            genReturnValue = genMethodDelegate.DynamicInvoke();
            Assert.Equal(returnValue, genReturnValue);
        }

        [Fact]
        public void CreateDelegate_ValueTypeParameters()
        {
            MethodInfo miPublicStructMethod = GetMethod(typeof(MI_BaseClass), nameof(MI_BaseClass.PublicStructMethod));
            MI_BaseClass testClass = new MI_BaseClass();

            Delegate methodDelegate = miPublicStructMethod.CreateDelegate(typeof(Delegate_DateTime_Str));
            object returnValue = ((Delegate_DateTime_Str)methodDelegate).DynamicInvoke(new object[] { testClass, null });
            Assert.Equal(testClass.PublicStructMethod(new DateTime()), returnValue);

            Delegate genMethodDelegate = miPublicStructMethod.CreateDelegate<Delegate_DateTime_Str>();
            object genReturnValue = genMethodDelegate.DynamicInvoke(new object[] { testClass, null });
            Assert.Equal(returnValue, genReturnValue);
        }

        [Theory]
        [InlineData(typeof(MI_BaseClass), nameof(MI_BaseClass.VirtualMethod), null, typeof(ArgumentNullException))]
        [InlineData(typeof(MI_BaseClass), nameof(MI_BaseClass.VirtualMethod), typeof(Delegate_Void_Int), typeof(ArgumentException))]
        public void CreateDelegate_Invalid(Type type, string name, Type delegateType, Type exceptionType)
        {
            MethodInfo methodInfo = GetMethod(type, name);
            Assert.Throws(exceptionType, () => methodInfo.CreateDelegate(delegateType));
        }

        public static IEnumerable<object[]> CreateDelegate_Target_Invalid_TestData()
        {
            yield return new object[] { typeof(MI_BaseClass), nameof(MI_BaseClass.VirtualMethod), null, new MI_BaseClass(), typeof(ArgumentNullException) }; // DelegateType is null
            yield return new object[] { typeof(MI_BaseClass), nameof(MI_BaseClass.VirtualMethod), typeof(Delegate_TC_Int), new MI_BaseClass(), typeof(ArgumentException) }; // DelegateType is incorrect
            yield return new object[] { typeof(MI_BaseClass), nameof(MI_BaseClass.VirtualMethod), typeof(Delegate_Void_Int), new DummyClass(), typeof(ArgumentException) }; // Target is incorrect
            yield return new object[] { typeof(MI_BaseClass), nameof(MI_BaseClass.VirtualMethod), typeof(Delegate_Void_Str), new DummyClass(), typeof(ArgumentException) }; // Target is incorrect
        }

        [Theory]
        [MemberData(nameof(CreateDelegate_Target_Invalid_TestData))]
        public void CreateDelegate_Target_Invalid(Type type, string name, Type delegateType, object target, Type exceptionType)
        {
            MethodInfo methodInfo = GetMethod(type, name);
            Assert.Throws(exceptionType, () => methodInfo.CreateDelegate(delegateType, target));
        }

        [Theory]
        [InlineData(typeof(Int32Attr), "[System.Reflection.Tests.Int32Attr((Int32)77, name = \"Int32AttrSimple\")]")]
        [InlineData(typeof(Int64Attr), "[System.Reflection.Tests.Int64Attr((Int64)77, name = \"Int64AttrSimple\")]")]
        [InlineData(typeof(StringAttr), "[System.Reflection.Tests.StringAttr(\"hello\", name = \"StringAttrSimple\")]")]
        [InlineData(typeof(EnumAttr), "[System.Reflection.Tests.EnumAttr((System.Reflection.Tests.PublicEnum)1, name = \"EnumAttrSimple\")]")]
        [InlineData(typeof(TypeAttr), "[System.Reflection.Tests.TypeAttr(typeof(System.Object), name = \"TypeAttrSimple\")]")]
        [InlineData(typeof(Attr), "[System.Reflection.Tests.Attr((Int32)77, name = \"AttrSimple\")]")]
        public void CustomAttributes(Type type, string expectedToString)
        {
            MethodInfo methodInfo = GetMethod(typeof(MI_SubClass), "MethodWithAttributes");
            CustomAttributeData attributeData = methodInfo.CustomAttributes.First(attribute => attribute.AttributeType.Equals(type));
            Assert.Equal(expectedToString, attributeData.ToString());
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ObjectMethodReturningString), typeof(MI_SubClass), nameof(MI_SubClass.ObjectMethodReturningString), true)]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ObjectMethodReturningString), typeof(MI_SubClass), nameof(MI_SubClass.VoidMethodReturningInt), false)]
        [InlineData(typeof(MI_SubClass), nameof(MI_GenericClass<int>.GenericMethod1), typeof(MI_GenericClass<>), nameof(MI_GenericClass<int>.GenericMethod1), false)]
        [InlineData(typeof(MI_SubClass), nameof(MI_GenericClass<int>.GenericMethod2), typeof(MI_GenericClass<string>), nameof(MI_GenericClass<int>.GenericMethod2), false)]
        public void EqualsTest(Type type1, string name1, Type type2, string name2, bool expected)
        {
            MethodInfo methodInfo1 = GetMethod(type1, name1);
            MethodInfo methodInfo2 = GetMethod(type2, name2);
            Assert.Equal(expected, methodInfo1.Equals(methodInfo2));
        }

        [Theory]
        //Verify two same MethodInfo objects are equal
        [InlineData("DummyMethod1", "DummyMethod1", true)]
        //Verify two different MethodInfo objects are not equal
        [InlineData("DummyMethod1", "DummyMethod2", false)]

        public void Equality1(string str1, string str2, bool expected)
        {
            MethodInfo mi1 = GetMethod(typeof(MethodInfoTests), str1);
            MethodInfo mi2 = GetMethod(typeof(MethodInfoTests), str2);

            Assert.Equal(expected, mi1 == mi2);
            Assert.NotEqual(expected, mi1 != mi2);
        }

        public static IEnumerable<object[]> TestEqualityMethodData2()
        {
            //Verify two different MethodInfo objects with same name from two different classes are not equal
            yield return new object[] { typeof(Sample), typeof(SampleG<>), "Method1", "Method1", false };
            //Verify two different MethodInfo objects with same name from two different classes are not equal
            yield return new object[] { typeof(Sample), typeof(SampleG<string>), "Method2", "Method2", false };
        }

        [Theory]
        [MemberData(nameof(TestEqualityMethodData2))]
        public void Equality2(Type sample1, Type sample2, string str1, string str2, bool expected)
        {
            MethodInfo mi1 = GetMethod(sample1, str1);
            MethodInfo mi2 = GetMethod(sample2, str2);

            Assert.Equal(expected, mi1 == mi2);
            Assert.NotEqual(expected, mi1 != mi2);
        }

        [Theory]
        [InlineData(typeof(MethodInfoBaseDefinitionBaseClass), "InterfaceMethod1", typeof(MethodInfoBaseDefinitionBaseClass))]
        [InlineData(typeof(MethodInfoBaseDefinitionSubClass), "InterfaceMethod1", typeof(MethodInfoBaseDefinitionBaseClass))]
        [InlineData(typeof(MethodInfoBaseDefinitionSubClass), "BaseClassVirtualMethod", typeof(MethodInfoBaseDefinitionBaseClass))]
        [InlineData(typeof(MethodInfoBaseDefinitionSubClass), "BaseClassMethod", typeof(MethodInfoBaseDefinitionSubClass))]
        [InlineData(typeof(MethodInfoBaseDefinitionSubClass), "ToString", typeof(object))]
        [InlineData(typeof(MethodInfoBaseDefinitionSubClass), "DerivedClassMethod", typeof(MethodInfoBaseDefinitionSubClass))]
        public void GetBaseDefinition(Type type1, string name, Type type2)
        {
            MethodInfo method = GetMethod(type1, name).GetBaseDefinition();
            Assert.Equal(GetMethod(type2, name), method);
            Assert.Equal(MemberTypes.Method, method.MemberType);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.IntLongMethodReturningLong), new string[] { "i", "l" })]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.StringArrayMethod), new string[] { "strArray" })]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.Increment), new string[] { "location" })]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.Decrement), new string[] { "location" })]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.Exchange), new string[] { "location1", "value" })]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.CompareExchange), new string[] { "location1", "value", "comparand" })]
        public void GetParameters(Type type, string name, string[] expectedParameterNames)
        {
            MethodInfo method = GetMethod(type, name);
            ParameterInfo[] parameters = method.GetParameters();

            Assert.Equal(expectedParameterNames.Length, parameters.Length);
            for (int i = 0; i < parameters.Length; i++)
            {
                Assert.Equal(parameters[i].Name, expectedParameterNames[i]);
            }
        }

        [Fact]
        public void GetParameters_IsDeepCopy()
        {
            MethodInfo method = GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.IntLongMethodReturningLong));
            ParameterInfo[] parameters = method.GetParameters();
            parameters[0] = null;

            // If GetParameters is a deep copy, then this change
            // should not affect another call to GetParameters()
            ParameterInfo[] parameters2 = method.GetParameters();
            for (int i = 0; i < parameters2.Length; i++)
            {
                Assert.NotNull(parameters2[i]);
            }
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod1), true)]
        public void ContainsGenericParameters(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).ContainsGenericParameters);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            MethodInfo methodInfo = GetMethod(typeof(MI_SubClass), "VoidMethodReturningInt");
            Assert.NotEqual(0, methodInfo.GetHashCode());
        }

        public static IEnumerable<object[]> Invoke_TestData()
        {
            yield return new object[] { typeof(MI_BaseClass), nameof(MI_BaseClass.VirtualReturnIntMethod), new MI_BaseClass(), null, 0 };
            yield return new object[] { typeof(MI_BaseClass), nameof(MI_BaseClass.VirtualReturnIntMethod), new MethodInfoDummySubClass(), null, 1 }; // From parent class

            yield return new object[] { typeof(MI_SubClass), nameof(MI_SubClass.ObjectMethodReturningString), new MI_SubClass(), new object[] { 42 }, "42" }; // Box primitive integer
            yield return new object[] { typeof(MI_SubClass), nameof(MI_SubClass.VoidMethodReturningInt), new MI_SubClass(), null, 3 }; // No parameters
            yield return new object[] { typeof(MI_SubClass), nameof(MI_SubClass.VoidMethodReturningLong), new MI_SubClass(), null, long.MaxValue }; // No parameters

            yield return new object[] { typeof(MI_SubClass), nameof(MI_SubClass.IntLongMethodReturningLong), new MI_SubClass(), new object[] { 200, 10000 }, 10200L }; // Primitive parameters
            yield return new object[] { typeof(MI_SubClass), nameof(MI_SubClass.StaticIntIntMethodReturningInt), null, new object[] { 10, 100 }, 110 }; // Static primitive parameters
            yield return new object[] { typeof(MI_SubClass), nameof(MI_SubClass.StaticIntIntMethodReturningInt), new MI_SubClass(), new object[] { 10, 100 }, 110 }; // Static primitive parameters
            yield return new object[] { typeof(MI_BaseClass), nameof(MI_SubClass.StaticIntMethodReturningBool), new MI_SubClass(), new object[] { 10 }, true }; // Static from parent class

            yield return new object[] { typeof(MI_SubClass), nameof(MI_SubClass.EnumMethodReturningEnum), new MI_SubClass(), new object[] { PublicEnum.Case1 }, PublicEnum.Case2 }; // Enum
            yield return new object[] { typeof(MI_Interface), nameof(MI_Interface.IMethod), new MI_SubClass(), new object[0], 10 }; // Interface
            yield return new object[] { typeof(MI_Interface), nameof(MI_Interface.IMethodNew), new MI_SubClass(), new object[0], 20 }; // Interface

            yield return new object[] { typeof(MethodInfoDefaultParameters), "Integer", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, 1 }; // Default int parameter, missing
            yield return new object[] { typeof(MethodInfoDefaultParameters), "Integer", new MethodInfoDefaultParameters(), new object[] { 2 }, 2 }; // Default int parameter, present
            yield return new object[] { typeof(MethodInfoDefaultParameters), "AllPrimitives", new MethodInfoDefaultParameters(), Enumerable.Repeat(Type.Missing, 13), "True, test, c, 2, -1, -3, 4, -5, 6, -7, 8, 9.1, 11.12" }; // Default parameters, all missing

            object[] allPrimitives = new object[] { false, "value", 'd', (byte)102, (sbyte)-101, (short)-103, (ushort)104, -105, (uint)106, (long)-107, (ulong)108, 109.1f, 111.12 };
            yield return new object[] { typeof(MethodInfoDefaultParameters), "AllPrimitives", new MethodInfoDefaultParameters(), allPrimitives, "False, value, d, 102, -101, -103, 104, -105, 106, -107, 108, 109.1, 111.12" }; // Default parameters, all present

            object[] somePrimitives = new object[] { false, Type.Missing, 'd', Type.Missing, (sbyte)-101, Type.Missing, (ushort)104, Type.Missing, (uint)106, Type.Missing, (ulong)108, Type.Missing, 111.12 };
            yield return new object[] { typeof(MethodInfoDefaultParameters), "AllPrimitives", new MethodInfoDefaultParameters(), somePrimitives, "False, test, d, 2, -101, -3, 104, -5, 106, -7, 108, 9.1, 111.12" }; // Default parameters, some present

            yield return new object[] { typeof(MethodInfoDefaultParameters), "String", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, "test" }; // Default string parameter, missing
            yield return new object[] { typeof(MethodInfoDefaultParameters), "String", new MethodInfoDefaultParameters(), new object[] { "value" }, "value" }; // Default string parameter, present

            yield return new object[] { typeof(MethodInfoDefaultParameters), "Reference", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, null }; // Default reference parameter, missing
            object referenceType = new MethodInfoDefaultParameters.CustomReferenceType();
            yield return new object[] { typeof(MethodInfoDefaultParameters), "Reference", new MethodInfoDefaultParameters(), new object[] { referenceType }, referenceType }; // Default reference parameter, present

            yield return new object[] { typeof(MethodInfoDefaultParameters), "ValueType", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, new MethodInfoDefaultParameters.CustomValueType() { Id = 0 } }; // Default value type parameter, missing
            yield return new object[] { typeof(MethodInfoDefaultParameters), "ValueType", new MethodInfoDefaultParameters(), new object[] { new MethodInfoDefaultParameters.CustomValueType() { Id = 1 } }, new MethodInfoDefaultParameters.CustomValueType() { Id = 1 } }; // Default value type parameter, present

            yield return new object[] { typeof(MethodInfoDefaultParameters), "DateTime", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, new DateTime(42) }; // Default DateTime parameter, missing
            yield return new object[] { typeof(MethodInfoDefaultParameters), "DateTime", new MethodInfoDefaultParameters(), new object[] { new DateTime(43) }, new DateTime(43) }; // Default DateTime parameter, present

            yield return new object[] { typeof(MethodInfoDefaultParameters), "DecimalWithAttribute", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, new decimal(4, 3, 2, true, 1) }; // Default decimal parameter, missing
            yield return new object[] { typeof(MethodInfoDefaultParameters), "DecimalWithAttribute", new MethodInfoDefaultParameters(), new object[] { new decimal(12, 13, 14, true, 1) }, new decimal(12, 13, 14, true, 1) }; // Default decimal parameter, present
            yield return new object[] { typeof(MethodInfoDefaultParameters), "Decimal", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, 3.14m }; // Default decimal parameter, missing
            yield return new object[] { typeof(MethodInfoDefaultParameters), "Decimal", new MethodInfoDefaultParameters(), new object[] { 103.14m }, 103.14m }; // Default decimal parameter, present

            yield return new object[] { typeof(MethodInfoDefaultParameters), "NullableInt", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, null }; // Default nullable parameter, missing
            yield return new object[] { typeof(MethodInfoDefaultParameters), "NullableInt", new MethodInfoDefaultParameters(), new object[] { (int?)42 }, (int?)42 }; // Default nullable parameter, present

            yield return new object[] { typeof(MethodInfoDefaultParameters), "Enum", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, PublicEnum.Case1 }; // Default enum parameter, missing

            yield return new object[] { typeof(MethodInfoDefaultParametersInterface), "InterfaceMethod", new MethodInfoDefaultParameters(), new object[] { Type.Missing, Type.Missing, Type.Missing }, "1, test, 3.14" }; // Default interface parameter, missing
            yield return new object[] { typeof(MethodInfoDefaultParametersInterface), "InterfaceMethod", new MethodInfoDefaultParameters(), new object[] { 101, "value", 103.14m }, "101, value, 103.14" }; // Default interface parameter, present

            yield return new object[] { typeof(MethodInfoDefaultParameters), "StaticMethod", null, new object[] { Type.Missing, Type.Missing, Type.Missing }, "1, test, 3.14" }; // Default static parameter, missing
            yield return new object[] { typeof(MethodInfoDefaultParameters), "StaticMethod", null, new object[] { 101, "value", 103.14m }, "101, value, 103.14" }; // Default static parameter, present

            yield return new object[] { typeof(MethodInfoDefaultParameters), "OptionalObjectParameter", new MethodInfoDefaultParameters(), new object[] { "value" }, "value" }; // Default static parameter, present
            yield return new object[] { typeof(MethodInfoDefaultParameters), "String", new MethodInfoDefaultParameters(), new string[] { "value" }, "value" }; // String array
        }

        [Theory]
        [MemberData(nameof(Invoke_TestData))]
        public void Invoke(Type methodDeclaringType, string methodName, object obj, object[] parameters, object result)
        {
            MethodInfo method = GetMethod(methodDeclaringType, methodName);
            Assert.Equal(result, method.Invoke(obj, parameters));
        }

        [Fact]
        public void Invoke_ParameterSpecification_ArrayOfMissing()
        {
            Invoke(typeof(MethodInfoDefaultParameters), "OptionalObjectParameter", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, Type.Missing);
            Invoke(typeof(MethodInfoDefaultParameters), "OptionalObjectParameter", new MethodInfoDefaultParameters(), new Missing[] { Missing.Value }, Missing.Value);
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15025", TestRuntimes.Mono)]
        public static void Invoke_OptionalParameterUnassingableFromMissing_WithMissingValue_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => GetMethod(typeof(MethodInfoDefaultParameters), "OptionalStringParameter").Invoke(new MethodInfoDefaultParameters(), new object[] { Type.Missing }));
        }

        [Fact]
        public void Invoke_TwoParameters_CustomBinder_IncorrectTypeArguments()
        {
            MethodInfo method = GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.StaticIntIntMethodReturningInt));
            var args = new object[] { "10", "100" };
            Assert.Equal(110, method.Invoke(null, BindingFlags.Default, new ConvertStringToIntBinder(), args, null));
            Assert.True(args[0] is int);
            Assert.True(args[1] is int);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod1), new Type[] { typeof(int) })]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod2), new Type[] { typeof(string), typeof(int) })]
        public void MakeGenericMethod(Type type, string name, Type[] typeArguments)
        {
            MethodInfo methodInfo = GetMethod(type, name);
            MethodInfo genericMethodInfo = methodInfo.MakeGenericMethod(typeArguments);
            Assert.True(genericMethodInfo.IsGenericMethod);
            Assert.False(genericMethodInfo.IsGenericMethodDefinition);

            MethodInfo genericMethodDefinition = genericMethodInfo.GetGenericMethodDefinition();
            Assert.Equal(methodInfo, genericMethodDefinition);
            Assert.True(genericMethodDefinition.IsGenericMethod);
            Assert.True(genericMethodDefinition.IsGenericMethodDefinition);
        }

        [Fact]
        public void MakeGenericMethod_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod1)).MakeGenericMethod(null)); // TypeArguments is null
            Assert.Throws<ArgumentNullException>(() => GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod2)).MakeGenericMethod(typeof(string), null)); // TypeArguments has null Type
            Assert.Throws<InvalidOperationException>(() => GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.VoidMethodReturningInt)).MakeGenericMethod(typeof(int))); // Method is non generic

            // Number of typeArguments does not match
            AssertExtensions.Throws<ArgumentException>(null, () => GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod1)).MakeGenericMethod());
            AssertExtensions.Throws<ArgumentException>(null, () => GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod1)).MakeGenericMethod(typeof(string), typeof(int)));
            AssertExtensions.Throws<ArgumentException>(null, () => GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod2)).MakeGenericMethod(typeof(int)));
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod1), 1)]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod2), 2)]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.VoidMethodReturningLong), 0)]
        public void GetGenericArguments(Type type, string name, int expectedCount)
        {
            MethodInfo methodInfo = GetMethod(type, name);
            Type[] genericArguments = methodInfo.GetGenericArguments();
            Assert.Equal(expectedCount, genericArguments.Length);
        }

        [Fact]
        public void GetGenericMethodDefinition_MethodNotGeneric_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.VoidMethodReturningInt)).GetGenericMethodDefinition());
        }

        [Fact]
        public void Attributes()
        {
            MethodInfo methodInfo = GetMethod(typeof(MI_SubClass), "ReturnVoidMethod");
            MethodAttributes attributes = methodInfo.Attributes;
            Assert.True(attributes.HasFlag(MethodAttributes.Public));
        }

        [Fact]
        public void CallingConvention()
        {
            MethodInfo methodInfo = GetMethod(typeof(MI_SubClass), "ReturnVoidMethod");
            CallingConventions callingConvention = methodInfo.CallingConvention;
            Assert.True(callingConvention.HasFlag(CallingConventions.HasThis));
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        [InlineData(typeof(MI_AbstractBaseClass), nameof(MI_AbstractBaseClass.AbstractMethod), true)]
        public void IsAbstract(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsAbstract);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        public void IsAssembly(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsAssembly);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        public void IsConstructor(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsConstructor);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        public void IsFamily(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsFamily);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        public void IsFamilyAndAssembly(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsFamilyAndAssembly);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        public void IsFamilyOrAssembly(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsFamilyOrAssembly);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        [InlineData(typeof(MI_AbstractSubClass), nameof(MI_AbstractSubClass.VirtualMethod), true)]
        public void IsFinal(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsFinal);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.StaticIntIntMethodReturningInt), false)]
        public void IsGenericMethodDefinition(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsGenericMethodDefinition);
        }

        [Theory]
        [InlineData(typeof(MI_AbstractSubClass), nameof(MI_AbstractSubClass.AbstractMethod), true)]
        public void IsHideBySig(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsHideBySig);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        public void IsPrivate(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsPrivate);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), true)]
        public void IsPublic(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsPublic);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        public void IsSpecialName(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsSpecialName);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.StaticIntIntMethodReturningInt), true)]
        public void IsStatic(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsStatic);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.VirtualReturnBoolMethod), true)]
        public void IsVirtual(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsVirtual);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.VoidMethodReturningLong))]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.IntLongMethodReturningLong))]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.StringArrayMethod))]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.Increment))]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.Decrement))]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.Exchange))]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.CompareExchange))]
        public void Name(Type type, string name)
        {
            MethodInfo mi = GetMethod(type, name);
            Assert.Equal(name, mi.Name);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.StaticIntIntMethodReturningInt), typeof(int))]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), typeof(void))]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.VoidMethodReturningInt), typeof(int))]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ObjectMethodReturningString), typeof(string))]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.VirtualReturnStringArrayMethod), typeof(string[]))]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.VirtualReturnBoolMethod), typeof(bool))]
        public void ReturnType_ReturnParameter(Type type, string name, Type expected)
        {
            MethodInfo methodInfo = GetMethod(type, name);
            Assert.Equal(expected, methodInfo.ReturnType);

            Assert.Equal(methodInfo.ReturnType, methodInfo.ReturnParameter.ParameterType);
            Assert.Null(methodInfo.ReturnParameter.Name);
            Assert.Equal(-1, methodInfo.ReturnParameter.Position);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.VoidMethodReturningLong), "Int64 VoidMethodReturningLong()")]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.IntLongMethodReturningLong), "Int64 IntLongMethodReturningLong(Int32, Int64)")]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.StringArrayMethod), "Void StringArrayMethod(System.String[])")]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), "Void ReturnVoidMethod(System.DateTime)")]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod2), "Void GenericMethod2[T,U](T, U)")]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.Increment), "Int32 Increment(Int32 ByRef)")]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.Decrement), "Int32 Decrement(Int32 ByRef)")]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.Exchange), "Int32 Exchange(Int32 ByRef, Int32)")]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.CompareExchange), "Int32 CompareExchange(Int32 ByRef, Int32, Int32)")]
        [InlineData(typeof(MI_GenericClass<>), nameof(MI_GenericClass<string>.GenericMethod1), "T GenericMethod1(T)")]
        [InlineData(typeof(MI_GenericClass<>), nameof(MI_GenericClass<string>.GenericMethod2), "T GenericMethod2[S](S, T, System.String)")]
        [InlineData(typeof(MI_GenericClass<string>), nameof(MI_GenericClass<string>.GenericMethod1), "System.String GenericMethod1(System.String)")]
        [InlineData(typeof(MI_GenericClass<string>), nameof(MI_GenericClass<string>.GenericMethod2), "System.String GenericMethod2[S](S, System.String, System.String)")]
        public void ToStringTest(Type type, string name, string expected)
        {
            MethodInfo methodInfo = GetMethod(type, name);
            Assert.Equal(expected, methodInfo.ToString());
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            MethodInfo genericMethodInfo = GetMethod(typeof(MI_GenericClass<string>), nameof(MI_GenericClass<string>.GenericMethod2)).MakeGenericMethod(new Type[] { typeof(DateTime) });
            yield return new object[] { genericMethodInfo, "System.String GenericMethod2[DateTime](System.DateTime, System.String, System.String)" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToStringTest_ByMethodInfo(MethodInfo methodInfo, string expected)
        {
            Assert.Equal(expected, methodInfo.ToString());
        }

        [Fact]
        public void InvokeNullableRefs()
        {
            object?[] args;

            int? iNull = null;
            args = new object[] { iNull };
            Assert.True((bool)GetMethod(nameof(NullableRefMethods.Null)).Invoke(null, args));
            Assert.Null(args[0]);
            Assert.False(((int?)args[0]).HasValue);

            args = new object[] { iNull };
            Assert.True((bool)GetMethod(nameof(NullableRefMethods.NullBoxed)).Invoke(null, args));
            Assert.Null(args[0]);

            args = new object[] { iNull, 10 };
            Assert.True((bool)GetMethod(nameof(NullableRefMethods.NullToValue)).Invoke(null, args));
            Assert.IsType<int>(args[0]);
            Assert.Equal(10, (int)args[0]);

            iNull = 42;
            args = new object[] { iNull, 42 };
            Assert.True((bool)GetMethod(nameof(NullableRefMethods.ValueToNull)).Invoke(null, args));
            Assert.Null(args[0]);

            iNull = null;
            args = new object[] { iNull, 10 };
            Assert.True((bool)GetMethod(nameof(NullableRefMethods.NullToValueBoxed)).Invoke(null, args));
            Assert.IsType<int>(args[0]);
            Assert.Equal(10, (int)args[0]);

            static MethodInfo GetMethod(string name) => typeof(NullableRefMethods).GetMethod(
                name, BindingFlags.Public | BindingFlags.Static)!;
        }

        [Fact]
        public void InvokeBoxedNullableRefs()
        {
            object?[] args;

            object? iNull = null;
            args = new object[] { iNull };
            Assert.True((bool)GetMethod(nameof(NullableRefMethods.Null)).Invoke(null, args));
            Assert.Null(args[0]);

            args = new object[] { iNull };
            Assert.True((bool)GetMethod(nameof(NullableRefMethods.NullBoxed)).Invoke(null, args));
            Assert.Null(args[0]);

            args = new object[] { iNull, 10 };
            Assert.True((bool)GetMethod(nameof(NullableRefMethods.NullToValue)).Invoke(null, args));
            Assert.IsType<int>(args[0]);
            Assert.Equal(10, (int)args[0]);

            iNull = 42;
            args = new object[] { iNull, 42 };
            Assert.True((bool)GetMethod(nameof(NullableRefMethods.ValueToNull)).Invoke(null, args));
            Assert.Null(args[0]);

            iNull = null;
            args = new object[] { iNull, 10 };
            Assert.True((bool)GetMethod(nameof(NullableRefMethods.NullToValueBoxed)).Invoke(null, args));
            Assert.IsType<int>(args[0]);
            Assert.Equal(10, (int)args[0]);

            static MethodInfo GetMethod(string name) => typeof(NullableRefMethods).GetMethod(
                name, BindingFlags.Public | BindingFlags.Static)!;
        }

        [Fact]
        public void InvokeEnum()
        {
            // Enums only need to match by primitive type.
            Assert.True((bool)GetMethod(nameof(EnumMethods.PassColorsInt)).
                Invoke(null, new object[] { OtherColorsInt.Red }));

            // Widening allowed
            Assert.True((bool)GetMethod(nameof(EnumMethods.PassColorsInt)).
                Invoke(null, new object[] { ColorsShort.Red }));

            // Narrowing not allowed
            Assert.Throws<ArgumentException>(() => GetMethod(nameof(EnumMethods.PassColorsShort)).
                Invoke(null, new object[] { OtherColorsInt.Red }));

            static MethodInfo GetMethod(string name) => typeof(EnumMethods).GetMethod(
                name, BindingFlags.Public | BindingFlags.Static)!;
        }

        //Methods for Reflection Metadata
        private void DummyMethod1(string str, int iValue, long lValue)
        {
        }

        private void DummyMethod2()
        {
        }

        private static MethodInfo GetMethod(Type type, string name)
        {
            return type.GetTypeInfo().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).First(method => method.Name.Equals(name));
        }
    }

#pragma warning disable 0414
    public interface MI_Interface
    {
        int IMethod();
        int IMethodNew();
    }

    public class MI_BaseClass : MI_Interface
    {
        public int IMethod() => 10;
        public int IMethodNew() => 20;

        public static bool StaticIntMethodReturningBool(int int4a) => int4a % 2 == 0;
        public virtual int VirtualReturnIntMethod() => 0;

        public virtual int VirtualMethod() => 0;
        private int PrivateInstanceMethod() => 21;
        public static string PublicStaticMethod(string x) => x;
        public string PublicStructMethod(DateTime dt) => dt.ToString();
    }

    public class MI_SubClass : MI_BaseClass
    {
        public override int VirtualReturnIntMethod() => 2;

        public PublicEnum EnumMethodReturningEnum(PublicEnum myenum) => myenum == PublicEnum.Case1 ? PublicEnum.Case2 : PublicEnum.Case1;
        public string ObjectMethodReturningString(object obj) => obj.ToString();
        public int VoidMethodReturningInt() => 3;
        public long VoidMethodReturningLong() => long.MaxValue;
        public long IntLongMethodReturningLong(int i, long l) => i + l;
        public static int StaticIntIntMethodReturningInt(int i1, int i2) => i1 + i2;

        public static void StaticGenericMethod<T>(T t) { }

        public new int IMethodNew() => 200;

        public override int VirtualMethod() => 1;

        public void ReturnVoidMethod(DateTime dt) { }
        public virtual string[] VirtualReturnStringArrayMethod() => new string[0];
        public virtual bool VirtualReturnBoolMethod() => true;

        public string Method2<T, S>(string t2, T t1, S t3) => "";

        public IntPtr ReturnIntPtrMethod() => new IntPtr(200);
        public int[] ReturnArrayMethod() => new int[] { 2, 3, 5, 7, 11 };

        public void GenericMethod1<T>(T t) { }
        public void GenericMethod2<T, U>(T t, U u) { }

        public void StringArrayMethod(string[] strArray) { }

        [Attr(77, name = "AttrSimple"),
        Int32Attr(77, name = "Int32AttrSimple"),
        Int64Attr(77, name = "Int64AttrSimple"),
        StringAttr("hello", name = "StringAttrSimple"),
        EnumAttr(PublicEnum.Case1, name = "EnumAttrSimple"),
        TypeAttr(typeof(object), name = "TypeAttrSimple")]
        public void MethodWithAttributes() { }
    }

    public class MethodInfoDummySubClass : MI_BaseClass
    {
        public override int VirtualReturnIntMethod() => 1;
    }

    public class MI_Interlocked
    {
        public static int Increment(ref int location) => 0;
        public static int Decrement(ref int location) => 0;
        public static int Exchange(ref int location1, int value) => 0;
        public static int CompareExchange(ref int location1, int value, int comparand) => 0;

        public static float Exchange(ref float location1, float value) => 0;
        public static float CompareExchange(ref float location1, float value, float comparand) => 0;

        public static object Exchange(ref object location1, object value) => null;
        public static object CompareExchange(ref object location1, object value, object comparand) => null;
    }

    public class MI_GenericClass<T>
    {
        public T GenericMethod1(T t) => t;
        public T GenericMethod2<S>(S s1, T t, string s2) => t;
        public static S GenericMethod3<S>(S s) => s;
    }

    public interface MethodInfoBaseDefinitionInterface
    {
        void InterfaceMethod1();
        void InterfaceMethod2();
    }

    public class MethodInfoBaseDefinitionBaseClass : MethodInfoBaseDefinitionInterface
    {
        public void InterfaceMethod1() { }
        void MethodInfoBaseDefinitionInterface.InterfaceMethod2() { }

        public virtual void BaseClassVirtualMethod() { }
        public virtual void BaseClassMethod() { }

        public override string ToString() => base.ToString();
    }

    public class MethodInfoBaseDefinitionSubClass : MethodInfoBaseDefinitionBaseClass
    {
        public override void BaseClassVirtualMethod() => base.BaseClassVirtualMethod();
        public new void BaseClassMethod() { }
        public override string ToString() => base.ToString();

        public void DerivedClassMethod() { }
    }

    public abstract class MI_AbstractBaseClass
    {
        public abstract void AbstractMethod();
        public virtual void VirtualMethod() { }
    }

    public class MI_AbstractSubClass : MI_AbstractBaseClass
    {
        public sealed override void VirtualMethod() { }
        public override void AbstractMethod() { }
    }

    public interface MethodInfoDefaultParametersInterface
    {
        string InterfaceMethod(int p1 = 1, string p2 = "test", decimal p3 = 3.14m);
    }

    public class MethodInfoDefaultParameters : MethodInfoDefaultParametersInterface
    {
        public int Integer(int parameter = 1)
        {
            return parameter;
        }

        public string AllPrimitives(
            bool boolean = true,
            string str = "test",
            char character = 'c',
            byte unsignedbyte = 2,
            sbyte signedbyte = -1,
            short int16 = -3,
            ushort uint16 = 4,
            int int32 = -5,
            uint uint32 = 6,
            long int64 = -7,
            ulong uint64 = 8,
            float single = 9.1f,
            double dbl = 11.12)
        {
            return FormattableString.Invariant($"{boolean}, {str}, {character}, {unsignedbyte}, {signedbyte}, {int16}, {uint16}, {int32}, {uint32}, {int64}, {uint64}, {single}, {dbl}");
        }

        public string String(string parameter = "test") => parameter;

        public class CustomReferenceType
        {
            public override bool Equals(object obj) => ReferenceEquals(this, obj);
            public override int GetHashCode() => 0;
        }

        public CustomReferenceType Reference(CustomReferenceType parameter = null) => parameter;

        public struct CustomValueType
        {
            public int Id;
            public override bool Equals(object obj) => Id == ((CustomValueType)obj).Id;
            public override int GetHashCode() => Id.GetHashCode();
        }

        public CustomValueType ValueType(CustomValueType parameter = default(CustomValueType)) => parameter;

        public DateTime DateTime([DateTimeConstant(42)] DateTime parameter) => parameter;

        public decimal DecimalWithAttribute([DecimalConstant(1, 1, 2, 3, 4)] decimal parameter) => parameter;

        public decimal Decimal(decimal parameter = 3.14m) => parameter;

        public int? NullableInt(int? parameter = null) => parameter;

        public PublicEnum Enum(PublicEnum parameter = PublicEnum.Case1) => parameter;

        string MethodInfoDefaultParametersInterface.InterfaceMethod(int p1, string p2, decimal p3)
        {
            return FormattableString.Invariant($"{p1}, {p2}, {p3}");
        }

        public static string StaticMethod(int p1 = 1, string p2 = "test", decimal p3 = 3.14m)
        {
            return FormattableString.Invariant($"{p1}, {p2}, {p3}");
        }

        public object OptionalObjectParameter([Optional] object parameter) => parameter;
        public string OptionalStringParameter([Optional] string parameter) => parameter;
    }

    public delegate int Delegate_TC_Int(MI_BaseClass tc);
    public delegate int Delegate_Void_Int();
    public delegate string Delegate_Str_Str(string x);
    public delegate string Delegate_Void_Str();
    public delegate string Delegate_DateTime_Str(MI_BaseClass tc, DateTime dt);

    public delegate T Delegate_GC_T_T<T>(MI_GenericClass<T> gc, T x);
    public delegate T Delegate_T_T<T>(T x);
    public delegate T Delegate_Void_T<T>();

    public class DummyClass { }

    public class Sample
    {
        public string Method1(DateTime t)
        {
            return "";
        }
        public string Method2<T, S>(string t2, T t1, S t3)
        {
            return "";
        }
    }

    public class SampleG<T>
    {
        public T Method1(T t)
        {
            return t;
        }
        public T Method2<S>(S t1, T t2, string t3)
        {
            return t2;
        }
    }

    public static class NullableRefMethods
    {
        public static bool Null(ref int? i)
        {
            Assert.Null(i);
            return true;
        }

        public static bool NullBoxed(ref object? i)
        {
            Assert.Null(i);
            return true;
        }

        public static bool NullToValue(ref int? i, int value)
        {
            Assert.Null(i);
            i = value;
            return true;
        }

        public static bool NullToValueBoxed(ref object? i, int value)
        {
            Assert.Null(i);
            i = value;
            return true;
        }

        public static bool ValueToNull(ref int? i, int expected)
        {
            Assert.Equal(expected, i);
            i = null;
            return true;
        }

        public static bool ValueToNullBoxed(ref int? i, int expected)
        {
            Assert.Equal(expected, i);
            i = null;
            return true;
        }
    }

    public enum ColorsInt : int
    {
        Red = 1
    }

    public enum ColorsShort : short
    {
        Red = 1
    }

    public enum OtherColorsInt : int
    {
        Red = 1
    }

    public static class EnumMethods
    {
        public static bool PassColorsInt(ColorsInt color)
        {
            Assert.Equal(ColorsInt.Red, color);
            return true;
        }

        public static bool PassColorsShort(ColorsShort color)
        {
            Assert.Equal(ColorsShort.Red, color);
            return true;
        }
    }
#pragma warning restore 0414
}

