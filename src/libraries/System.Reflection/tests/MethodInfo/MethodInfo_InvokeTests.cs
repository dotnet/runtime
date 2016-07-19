// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable 0414

namespace MethodInfoTests
{
    public class Test
    {
        // Invoke a method using an exact MethodInfo
        [Fact]
        public static void TestInvokeMethod_ExactMethodWithInheritance()
        {
            Co1333_b clsObj = new Co1333_b();
            Assert.Equal(0, (int)getMethod(typeof(Co1333_b), "ReturnTheIntPlus").Invoke(clsObj, null));
        }

        // Invoke a method using a matching MethodInfo from parent class
        [Fact]
        public static void TestInvokeMethod_MethodFromParentClass()
        {
            Co1333_b clsObj = new Co1333_b1();
            Assert.Equal(1, (int)getMethod(typeof(Co1333_b), "ReturnTheIntPlus").Invoke(clsObj, null));
        }

        // Invoke a method that requires Reflection to box a primitive integer for the invoked method's param. // Bug 10829 
        [Fact]
        public static void TestInvokeMethod_IntegerParameterBoxing()
        {
            Co1333Invoke clsObj = new Co1333Invoke();
            object[] varParams = { 42 };
            Assert.Equal("42", (string)getMethod(typeof(Co1333Invoke), "ConvertI4ObjToString").Invoke(clsObj, varParams));
        }

        // Invoke a vanilla method that takes no params but returns an integer 
        [Fact]
        public static void TestInvokeMethod_NoParametersInt4Return()
        {
            Co1333Invoke clsObj = new Co1333Invoke();
            Assert.Equal(3, (int)getMethod(typeof(Co1333Invoke), "Int4ReturnThree").Invoke(clsObj, null));
        }

        // Invoke a vanilla method that takes no params but returns a Int64
        [Fact]
        public static void TestInvokeMethod_NoParametersInt64Return()
        {
            Co1333Invoke clsObj = new Co1333Invoke();
            Assert.Equal(Int64.MaxValue, (Int64)getMethod(typeof(Co1333Invoke), "ReturnLongMax").Invoke(clsObj, null));
        }

        // Invoke a method that has parameters of primitive types
        [Fact]
        public static void TestInvokeMethod_PrimitiveParameters()
        {
            Co1333Invoke clsObj = new Co1333Invoke();
            object[] varParams =
            {
                200,
                100000
            };

            Assert.Equal(100200L, (long)getMethod(typeof(Co1333Invoke), "ReturnI8Sum").Invoke(clsObj, varParams));
        }

        // Invoke a static method using null, that has parameters of primitive types
        [Fact]
        public static void TestInvokeMethod_StaticOnNullWithPrimitiveParameters()
        {
            object[] varParams =
            {
                10,
                100
            };

            Assert.Equal(110, (int)getMethod(typeof(Co1333Invoke), "ReturnI4Sum").Invoke(null, varParams));
        }

        // Invoke a static method using class object, that has parameters of primitive types
        [Fact]
        public static void TestInvokeMethod_StaticOnInstanceWithPrimitiveParameters()
        {
            Co1333Invoke clsObj = new Co1333Invoke();
            object[] varParams =
            {
                10,
                100
            };

            Assert.Equal(110, (int)getMethod(typeof(Co1333Invoke), "ReturnI4Sum").Invoke(clsObj, varParams));
        }

        // Invoke inherited static method.
        [Fact]
        public static void TestInvokeMethod_InheritedStaticOnInstance()
        {
            Co1333Invoke clsObj = new Co1333Invoke();
            object[] varParams =
            {
                10,
            };

            Assert.Equal(true, (bool)getMethod(typeof(Co1333_a), "IsEvenStatic").Invoke(clsObj, varParams));
        }

        // Enum reflection in method signature
        [Fact]
        public static void TestInvokeMethod_EnumParameterAndEnumReturnValue()
        {
            Co1333Invoke clsObj = new Co1333Invoke();
            object[] vars =
            {
                MyColorEnum.RED
            };

            Assert.Equal(MyColorEnum.GREEN, (MyColorEnum)getMethod(typeof(Co1333Invoke), "GetAndRetMyEnum").Invoke(clsObj, vars));
        }

        // Call Interface Method
        [Fact]
        public static void TestInvokeMethod_InterfaceMethod()
        {
            MyClass clsObj = new MyClass();
            Assert.Equal(10, (int)getMethod(typeof(MyInterface), "IMethod").Invoke(clsObj, new object[] { }));
        }

        // Call Interface Method marked as new in class
        [Fact]
        public static void TestInvokeMethod_InterfaceMethodMarkedAsNew()
        {
            MyClass clsObj = new MyClass();
            Assert.Equal(20, (int)getMethod(typeof(MyInterface), "IMethodNew").Invoke(clsObj, new object[] { }));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_IntegerWithMissingValue()
        {
            Assert.Equal(1, (int)getMethod(typeof(DefaultParameters), "Integer").Invoke(new DefaultParameters(), new object[] { Type.Missing }));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_IntegerWithExplicitValue()
        {
            Assert.Equal(2, (int)getMethod(typeof(DefaultParameters), "Integer").Invoke(new DefaultParameters(), new object[] { 2 }));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_AllPrimitiveParametersWithMissingValues()
        {
            object[] parameters = new object[13];
            for (int i = 0; i < parameters.Length; i++) { parameters[i] = Type.Missing; }

            Assert.Equal(
                "True, test, c, 2, -1, -3, 4, -5, 6, -7, 8, 9.1, 11.12",
                (string)getMethod(typeof(DefaultParameters), "AllPrimitives").Invoke(
                    new DefaultParameters(),
                    parameters
                    ));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_AllPrimitiveParametersWithAllExplicitValues()
        {
            Assert.Equal(
                "False, value, d, 102, -101, -103, 104, -105, 106, -107, 108, 109.1, 111.12",
                (string)getMethod(typeof(DefaultParameters), "AllPrimitives").Invoke(
                    new DefaultParameters(),
                    new object[13]
                    {
                        false,
                        "value",
                        'd',
                        (byte)102,
                        (sbyte)-101,
                        (short)-103,
                        (UInt16)104,
                        (int)-105,
                        (UInt32)106,
                        (long)-107,
                        (UInt64)108,
                        (Single)109.1,
                        (Double)111.12
                    }
                    ));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_AllPrimitiveParametersWithSomeExplicitValues()
        {
            Assert.Equal(
                "False, test, d, 2, -101, -3, 104, -5, 106, -7, 108, 9.1, 111.12",
                (string)getMethod(typeof(DefaultParameters), "AllPrimitives").Invoke(
                    new DefaultParameters(),
                    new object[13]
                    {
                        false,
                        Type.Missing,
                        'd',
                        Type.Missing,
                        (sbyte)-101,
                        Type.Missing,
                        (UInt16)104,
                        Type.Missing,
                        (UInt32)106,
                        Type.Missing,
                        (UInt64)108,
                        Type.Missing,
                        (Double)111.12
                    }
                    ));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_StringParameterWithMissingValue()
        {
            Assert.Equal
                ("test", 
                (string)getMethod(typeof(DefaultParameters), "String").Invoke(new DefaultParameters(), new object[] { Type.Missing }));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_StringParameterWithExplicitValue()
        {
            Assert.Equal(
                "value", 
                (string)getMethod(typeof(DefaultParameters), "String").Invoke(new DefaultParameters(), new object[] { "value" }));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_ReferenceTypeParameterWithMissingValue()
        {
            Assert.Null(getMethod(typeof(DefaultParameters), "Reference").Invoke(new DefaultParameters(), new object[] { Type.Missing }));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_ReferenceTypeParameterWithExplicitValue()
        {
            DefaultParameters.CustomReferenceType referenceInstance = new DefaultParameters.CustomReferenceType();
            Assert.Same(
                referenceInstance,
                getMethod(typeof(DefaultParameters), "Reference").Invoke(new DefaultParameters(), new object[] { referenceInstance }));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_ValueTypeParameterWithMissingValue()
        {
            Assert.Equal(
                0,
                ((DefaultParameters.CustomValueType)getMethod(typeof(DefaultParameters), "ValueType").Invoke(
                    new DefaultParameters(), 
                    new object[] { Type.Missing })).Id);
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_ValueTypeParameterWithExplicitValue()
        {
            Assert.Equal(
                1,
                ((DefaultParameters.CustomValueType)getMethod(typeof(DefaultParameters), "ValueType").Invoke(
                    new DefaultParameters(),
                    new object[] { new DefaultParameters.CustomValueType { Id = 1 } })).Id);
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_DateTimeParameterWithMissingValue()
        {
            Assert.Equal(
                new DateTime(42),
                (DateTime)getMethod(typeof(DefaultParameters), "DateTime").Invoke(new DefaultParameters(), new object[] { Type.Missing }));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_DateTimeParameterWithExplicitValue()
        {
            Assert.Equal(
                new DateTime(43),
                (DateTime)getMethod(typeof(DefaultParameters), "DateTime").Invoke(new DefaultParameters(), new object[] { new DateTime(43) }));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_DecimalParameterWithAttributeAndMissingValue()
        {
            Assert.Equal(
                new decimal(4, 3, 2, true, 1),
                (decimal)getMethod(typeof(DefaultParameters), "DecimalWithAttribute").Invoke(new DefaultParameters(), new object[] { Type.Missing }));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_DecimalParameterWithAttributeAndExplicitValue()
        {
            Assert.Equal(
                new decimal(12, 13, 14, true, 1),
                (decimal)getMethod(typeof(DefaultParameters), "DecimalWithAttribute").Invoke(new DefaultParameters(), new object[] { new decimal(12, 13, 14, true, 1) }));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_DecimalParameterWithMissingValue()
        {
            Assert.Equal(
                3.14m,
                (decimal)getMethod(typeof(DefaultParameters), "Decimal").Invoke(new DefaultParameters(), new object[] { Type.Missing }));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_DecimalParameterWithExplicitValue()
        {
            Assert.Equal(
                103.14m,
                (decimal)getMethod(typeof(DefaultParameters), "Decimal").Invoke(new DefaultParameters(), new object[] { 103.14m }));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_NullableIntWithMissingValue()
        {
            Assert.Null((int?)getMethod(typeof(DefaultParameters), "NullableInt").Invoke(new DefaultParameters(), new object[] { Type.Missing }));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_NullableIntWithExplicitValue()
        {
            Assert.Equal(
                (int?)42,
                (int?)getMethod(typeof(DefaultParameters), "NullableInt").Invoke(new DefaultParameters(), new object[] { (int?)42 }));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_EnumParameterWithMissingValue()
        {
            Assert.Equal(
                DefaultParameters.CustomEnum.Default,
                (DefaultParameters.CustomEnum)getMethod(typeof(DefaultParameters), "Enum").Invoke(new DefaultParameters(), new object[] { Type.Missing }));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_InterfaceMethodWithMissingValues()
        {
            Assert.Equal(
                "1, test, 3.14",
                (string)getMethod(typeof(IDefaultParameters), "InterfaceMethod").Invoke(
                    new DefaultParameters(), 
                    new object[] { Type.Missing, Type.Missing, Type.Missing }));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_InterfaceMethodWithMissingExplicitValues()
        {
            Assert.Equal(
                "101, value, 103.14",
                (string)getMethod(typeof(IDefaultParameters), "InterfaceMethod").Invoke(
                    new DefaultParameters(),
                    new object[] { 101, "value", 103.14m }));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_StaticMethodWithMissingValues()
        {
            Assert.Equal(
                "1, test, 3.14",
                (string)getMethod(typeof(DefaultParameters), "StaticMethod").Invoke(
                    null,
                    new object[] { Type.Missing, Type.Missing, Type.Missing }));
        }

        [Fact]
        public static void TestInvokeMethod_DefaultParameter_StaticMethodWithMissingExplicitValues()
        {
            Assert.Equal(
                "101, value, 103.14",
                (string)getMethod(typeof(DefaultParameters), "StaticMethod").Invoke(
                    null,
                    new object[] { 101, "value", 103.14m }));
        }

        [Fact]
        public static void TestInvokeMethod_OptionalParameter_WithExplicitValue()
        {
            Assert.Equal(
                "value",
                getMethod(typeof(DefaultParameters), "OptionalObjectParameter").Invoke(new DefaultParameters(), new object[] { "value" }));
        }

        [Fact]
        public static void TestInvokeMethod_OptionalParameter_WithMissingValue()
        {
            Assert.Equal(
                Type.Missing,
                getMethod(typeof(DefaultParameters), "OptionalObjectParameter").Invoke(new DefaultParameters(), new object[] { Type.Missing }));
        }

        [Fact]
        public static void TestInvokeMethod_OptionalParameterUnassingableFromMissing_WithMissingValue()
        {
            Assert.Throws<ArgumentException>(() => getMethod(typeof(DefaultParameters), "OptionalStringParameter").Invoke(new DefaultParameters(), new object[] { Type.Missing }));
        }

        [Fact]
        public static void TestInvokeMethod_ParameterSpecification_ArrayOfStrings()
        {
            Assert.Equal(
                "value",
                (string)getMethod(typeof(ParameterTypes), "String").Invoke(new ParameterTypes(), new string[] { "value" }));
        }

        [Fact]
        public static void TestInvokeMethod_ParameterSpecification_ArrayOfMissing()
        {
            Assert.Same(
                Missing.Value,
                (object)getMethod(typeof(DefaultParameters), "OptionalObjectParameter").Invoke(new DefaultParameters(), new Missing[] { Missing.Value }));
        }

        // Gets MethodInfo object from a Type
        private static MethodInfo getMethod(Type t, string method)
        {
            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<MethodInfo> alldefinedMethods = ti.DeclaredMethods.GetEnumerator();
            MethodInfo mi = null;

            while (alldefinedMethods.MoveNext())
            {
                if (alldefinedMethods.Current.Name.Equals(method))
                {
                    // Found the method
                    mi = alldefinedMethods.Current;
                    break;
                }
            }
            return mi;
        }

        private interface IDefaultParameters
        {
            string InterfaceMethod(int p1 = 1, string p2 = "test", decimal p3 = 3.14m);
        }

        private class DefaultParameters : IDefaultParameters
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
                Int16 int16 = -3,
                UInt16 uint16 = 4,
                Int32 int32 = -5,
                UInt32 uint32 = 6,
                Int64 int64 = -7,
                UInt64 uint64 = 8,
                Single single = (Single)9.1,
                Double dbl = 11.12)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(boolean.ToString() + ", ");
                builder.Append(str + ", ");
                builder.Append(character.ToString() + ", ");
                builder.Append(unsignedbyte.ToString() + ", ");
                builder.Append(signedbyte.ToString() + ", ");
                builder.Append(int16.ToString() + ", ");
                builder.Append(uint16.ToString() + ", ");
                builder.Append(int32.ToString() + ", ");
                builder.Append(uint32.ToString() + ", ");
                builder.Append(int64.ToString() + ", ");
                builder.Append(uint64.ToString() + ", ");
                builder.Append(single.ToString() + ", ");
                builder.Append(dbl);
                return builder.ToString();
            }

            public string String(string parameter = "test")
            {
                return parameter;
            }

            public class CustomReferenceType { };

            public CustomReferenceType Reference(CustomReferenceType parameter = null)
            {
                return parameter;
            }

            public struct CustomValueType { public int Id; };

            public CustomValueType ValueType(CustomValueType parameter = default(CustomValueType))
            {
                return parameter;
            }

            public DateTime DateTime([DateTimeConstant(42)] DateTime parameter)
            {
                return parameter;
            }

            public decimal DecimalWithAttribute([DecimalConstant(1, 1, 2, 3, 4)] decimal parameter)
            {
                return parameter;
            }

            public decimal Decimal(decimal parameter = 3.14m)
            {
                return parameter;
            }

            public int? NullableInt(int? parameter = null)
            {
                return parameter;
            }

            public enum CustomEnum
            {
                Default = 1,
                Explicit = 2
            };

            public CustomEnum Enum(CustomEnum parameter = CustomEnum.Default)
            {
                return parameter;
            }

            string IDefaultParameters.InterfaceMethod(int p1, string p2, decimal p3)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(p1.ToString() + ", ");
                builder.Append(p2 + ", ");
                builder.Append(p3.ToString());
                return builder.ToString();
            }

            public static string StaticMethod(int p1 = 1, string p2 = "test", decimal p3 = 3.14m)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(p1.ToString() + ", ");
                builder.Append(p2 + ", ");
                builder.Append(p3.ToString());
                return builder.ToString();
            }

            public object OptionalObjectParameter([Optional]object parameter)
            {
                return parameter;
            }

            public string OptionalStringParameter([Optional]string parameter)
            {
                return parameter;
            }
        }

        private class ParameterTypes
        {
            public string String(string parameter)
            {
                return parameter;
            }
        }
    }

    public class Co1333_a
    {
        public static bool IsEvenStatic(int int4a)
        { return (int4a % 2 == 0) ? true : false; }
    }

    public class Co1333_b
    {
        public virtual int ReturnTheIntPlus()
        { return (0); }
    }
    public class Co1333_b1 : Co1333_b
    {
        public override int ReturnTheIntPlus()
        { return (1); }
    }
    public class Co1333_b2 : Co1333_b
    {
        public override int ReturnTheIntPlus()
        { return (2); }
    }

    public enum MyColorEnum
    {
        RED = 1,
        YELLOW = 2,
        GREEN = 3
    }

    public class Co1333Invoke
        : Co1333_a
    {
        public MyColorEnum GetAndRetMyEnum(MyColorEnum myenum)
        {
            if (myenum == MyColorEnum.RED)
            {
                return MyColorEnum.GREEN;
            }
            else
                return MyColorEnum.RED;
        }

        public string ConvertI4ObjToString(object p_obj)  // Bug 10829.  Expecting Integer4 type of obj.
        {
            return (p_obj.ToString());
        }

        public int Int4ReturnThree()
        {
            return (3);
        }

        public long ReturnLongMax()
        {
            return (Int64.MaxValue);
        }

        public long ReturnI8Sum(int iFoo, long lFoo)
        {
            return ((long)iFoo + lFoo);
        }

        static public int ReturnI4Sum(int i4Foo, int i4Bar)
        {
            return (i4Foo + i4Bar);
        }
    }

    public interface MyInterface
    {
        int IMethod();
        int IMethodNew();
    }

    public class MyBaseClass : MyInterface
    {
        public int IMethod() { return 10; }
        public int IMethodNew() { return 20; }
    }

    public class MyClass : MyBaseClass
    {
        public new int IMethodNew() { return 200; }
    }
}
