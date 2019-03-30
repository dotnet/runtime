// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Xunit;

namespace System.Tests
{
    public static partial class PseudoCustomAttributeTests
    {
        [Theory]
        [MemberData(nameof(TestData_AttributeExists))]
        [MemberData(nameof(TestData_AttributeDoesNotExist))]
        public static void IsDefined(object target, Type attributeType, Attribute attribute)
        {
            switch (target)
            {
                case Type type:
                    Assert.Equal(attribute != null, Attribute.IsDefined(type, attributeType));
                    Assert.Equal(attribute != null, Attribute.IsDefined(type, attributeType, true));
                    break;
                case MemberInfo memberInfo:
                    Assert.Equal(attribute != null, Attribute.IsDefined(memberInfo, attributeType));
                    Assert.Equal(attribute != null, Attribute.IsDefined(memberInfo, attributeType, true));
                    break;
                case ParameterInfo parameterInfo:
                    Assert.Equal(attribute != null, Attribute.IsDefined(parameterInfo, attributeType));
                    Assert.Equal(attribute != null, Attribute.IsDefined(parameterInfo, attributeType, true));
                    break;
                default:
                    Assert.True(false);
                    break;
            }
        }

        [Theory]
        [MemberData(nameof(TestData_AttributeExists))]
        [MemberData(nameof(TestData_AttributeDoesNotExist))]
        public static void GetCustomAttribute(object target, Type attributeType, Attribute attribute)
        {
            switch (target)
            {
                case Type type:
                    Assert.Equal(attribute, Attribute.GetCustomAttribute(type, attributeType));
                    Assert.Equal(attribute, Attribute.GetCustomAttribute(type, attributeType, true));
                    break;
                case MemberInfo memberInfo:
                    Assert.Equal(attribute, Attribute.GetCustomAttribute(memberInfo, attributeType));
                    Assert.Equal(attribute, Attribute.GetCustomAttribute(memberInfo, attributeType, true));
                    break;
                case ParameterInfo parameterInfo:
                    Assert.Equal(attribute, Attribute.GetCustomAttribute(parameterInfo, attributeType));
                    Assert.Equal(attribute, Attribute.GetCustomAttribute(parameterInfo, attributeType, true));
                    break;
                default:
                    Assert.True(false);
                    break;
            }
        }

        [Theory]
        [MemberData(nameof(TestData_AttributeExists))]
        [MemberData(nameof(TestData_AttributeDoesNotExist))]
        public static void GetCustomAttributes(object target, Type attributeType, Attribute attribute)
        {
            switch (target)
            {
                case Type type:
                    Assert.Equal(attribute, Attribute.GetCustomAttributes(type, typeof(Attribute))
                        .Where((e) => e.GetType() == attributeType).SingleOrDefault());
                    Assert.Equal(attribute, Attribute.GetCustomAttributes(type, typeof(Attribute), true)
                        .Where((e) => e.GetType() == attributeType).SingleOrDefault());
                    break;
                case MemberInfo memberInfo:
                    Assert.Equal(attribute, Attribute.GetCustomAttributes(memberInfo, typeof(Attribute))
                        .Where((e) => e.GetType() == attributeType).SingleOrDefault());
                    Assert.Equal(attribute, Attribute.GetCustomAttributes(memberInfo, typeof(Attribute), true)
                        .Where((e) => e.GetType() == attributeType).SingleOrDefault());
                    break;
                case ParameterInfo parameterInfo:
                    Assert.Equal(attribute, Attribute.GetCustomAttributes(parameterInfo, typeof(Attribute))
                        .Where((e) => e.GetType() == attributeType).SingleOrDefault());
                    Assert.Equal(attribute, Attribute.GetCustomAttributes(parameterInfo, typeof(Attribute), true)
                        .Where((e) => e.GetType() == attributeType).SingleOrDefault());
                    break;
                default:
                    Assert.True(false);
                    break;
            }
        }

        public static IEnumerable<object[]> TestData_AttributeExists()
        {
            yield return new object[] { typeof(TestTypeWithAttributes), typeof(SerializableAttribute), new SerializableAttribute() };
            yield return new object[] { typeof(ITestComInterface), typeof(ComImportAttribute), new ComImportAttribute() };

            FieldInfo testField = typeof(TestTypeWithAttributes).GetField("_testField");
            yield return new object[] { testField, typeof(FieldOffsetAttribute), new FieldOffsetAttribute(120) };
            yield return new object[] { testField, typeof(NonSerializedAttribute), new NonSerializedAttribute() };
            yield return new object[] { testField, typeof(MarshalAsAttribute), new MarshalAsAttribute(UnmanagedType.ByValTStr) { SizeConst = 100 } };

            MethodInfo testMethod = typeof(TestTypeWithAttributes).GetMethod("TestMethod");

            ParameterInfo testMethodParameter = testMethod.GetParameters()[0];
            yield return new object[] { testMethodParameter, typeof(MarshalAsAttribute), new MarshalAsAttribute(UnmanagedType.LPArray) { ArraySubType = UnmanagedType.I4 } };
            yield return new object[] { testMethodParameter, typeof(InAttribute), new InAttribute() };
            yield return new object[] { testMethodParameter, typeof(OutAttribute), new OutAttribute() };
            yield return new object[] { testMethodParameter, typeof(OptionalAttribute), new OptionalAttribute() };

            yield return new object[] { testMethod.ReturnParameter, typeof(MarshalAsAttribute), new MarshalAsAttribute(UnmanagedType.Bool) };

            yield return new object[] { testMethod, typeof(DllImportAttribute),
                new DllImportAttribute("nonexistent") { CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true, PreserveSig = true, EntryPoint = "MyEntryPoint" } };

            yield return new object[] { testMethod, typeof(PreserveSigAttribute), new PreserveSigAttribute() };
        }

        public static IEnumerable<object[]> TestData_AttributeDoesNotExist()
        {
            yield return new object[] { typeof(TestTypeWithoutAttributes), typeof(SerializableAttribute), null };

            yield return new object[] { typeof(TestTypeWithoutAttributes), typeof(ComImportAttribute), null };

            FieldInfo testField = typeof(TestTypeWithoutAttributes).GetField("_testField");
            yield return new object[] { testField, typeof(FieldOffsetAttribute), null };
            yield return new object[] { testField, typeof(NonSerializedAttribute), null };
            yield return new object[] { testField, typeof(MarshalAsAttribute), null };

            MethodInfo testMethod = typeof(TestTypeWithoutAttributes).GetMethod("TestMethod");

            ParameterInfo testMethodParameter = testMethod.GetParameters()[0];
            yield return new object[] { testMethodParameter, typeof(MarshalAsAttribute), null };
            yield return new object[] { testMethodParameter, typeof(InAttribute), null };
            yield return new object[] { testMethodParameter, typeof(OutAttribute), null };
            yield return new object[] { testMethodParameter, typeof(OptionalAttribute), null };

            yield return new object[] { testMethod.ReturnParameter, typeof(MarshalAsAttribute), null };

            yield return new object[] { testMethod, typeof(DllImportAttribute), null };

            yield return new object[] { testMethod, typeof(PreserveSigAttribute), null };
        }

        [SerializableAttribute]
        [StructLayoutAttribute(LayoutKind.Explicit)]
        public class TestTypeWithAttributes
        {
            [FieldOffsetAttribute(120)]
            [NonSerializedAttribute]
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public string _testField;

            [PreserveSigAttribute]
            [return: MarshalAsAttribute(UnmanagedType.Bool)]
            [DllImportAttribute("nonexistent", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true, PreserveSig = true, EntryPoint = "MyEntryPoint")]
            public extern static bool TestMethod([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I4), In, Out, Optional] int[] x);
        }

        public class TestTypeWithoutAttributes
        {
            public string _testField;

            public static bool TestMethod(int[] x) => false;
        }

        [ComImport]
        [Guid("42424242-4242-4242-4242-424242424242"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ITestComInterface
        {
        }
    }
}
