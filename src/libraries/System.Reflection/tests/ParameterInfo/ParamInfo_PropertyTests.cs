// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace System.Reflection.Tests
{
    public class ParamInfoPropertyTests
    {
        //Verify Member 
        [Fact]
        public static void TestParamMember1()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "Method1", 0);
            MemberInfo mi = pi.Member;
            Assert.NotNull(mi);

            Type[] types = pi.GetOptionalCustomModifiers();
            Assert.NotNull(types);
            Assert.Equal(0, types.Length);

            types = pi.GetRequiredCustomModifiers();
            Assert.NotNull(types);
            Assert.Equal(0, types.Length);

            Assert.NotNull(pi.RawDefaultValue);
        }


        //Verify Member 
        [Fact]
        public static void TestParamMember2()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "virtualMethod", 0);
            MemberInfo mi = pi.Member;
            Assert.NotNull(mi);

            Type[] types = pi.GetOptionalCustomModifiers();
            Assert.NotNull(types);
            Assert.Equal(0, types.Length);

            types = pi.GetRequiredCustomModifiers();
            Assert.NotNull(types);
            Assert.Equal(0, types.Length);

            Assert.NotNull(pi.RawDefaultValue);
        }

        //Verify Member 
        [Fact]
        public static void TestParamMember3()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "MethodWithRefKW", 0);
            MemberInfo mi = pi.Member;
            Assert.NotNull(mi);

            Type[] types = pi.GetOptionalCustomModifiers();
            Assert.NotNull(types);
            Assert.Equal(0, types.Length);

            types = pi.GetRequiredCustomModifiers();
            Assert.NotNull(types);
            Assert.Equal(0, types.Length);

            Assert.NotNull(pi.RawDefaultValue);
        }


        //Verify Member 
        [Fact]
        public static void TestParamMember4()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "MethodWithOutKW", 0);
            MemberInfo mi = pi.Member;
            Assert.NotNull(mi);

            Type[] types = pi.GetOptionalCustomModifiers();
            Assert.NotNull(types);
            Assert.Equal(0, types.Length);

            types = pi.GetRequiredCustomModifiers();
            Assert.NotNull(types);
            Assert.Equal(0, types.Length);

            Assert.NotNull(pi.RawDefaultValue);
        }


        //Verify Member 
        [Fact]
        public static void TestParamMember5()
        {
            ParameterInfo pi = getParamInfo(typeof(GenericClass<string>), "genericMethod", 0);
            MemberInfo mi = pi.Member;
            Assert.NotNull(mi);

            Type[] types = pi.GetOptionalCustomModifiers();
            Assert.NotNull(types);
            Assert.Equal(0, types.Length);

            types = pi.GetRequiredCustomModifiers();
            Assert.NotNull(types);
            Assert.Equal(0, types.Length);

            Assert.NotNull(pi.RawDefaultValue);
        }


        //Verify HasDefaultValue for return param
        [Fact]
        public static void TestHasDefaultValue1()
        {
            ParameterInfo pi = getReturnParam(typeof(MyClass), "Method1");
            Assert.True(pi.HasDefaultValue);
        }


        //Verify HasDefaultValue for param that has default
        [Fact]
        public static void TestHasDefaultValue2()
        {
            Assert.True(getParamInfo(typeof(MyClass), "MethodWithdefault1", 1).HasDefaultValue);
        }

        //Verify HasDefaultValue for different types of default values
        [Fact]
        public static void TestHasDefaultValue3()
        {
            Assert.True(getParamInfo(typeof(MyClass), "MethodWithdefault2", 0).HasDefaultValue);
            Assert.True(getParamInfo(typeof(MyClass), "MethodWithdefault3", 0).HasDefaultValue);
            Assert.True(getParamInfo(typeof(MyClass), "MethodWithdefault4", 0).HasDefaultValue);
        }

        //Verify HasDefaultValue for generic method 
        [Fact]
        public static void TestHasDefaultValue4()
        {
            Assert.True(getParamInfo(typeof(GenericClass<int>), "genericMethodWithdefault", 1).HasDefaultValue);
        }


        //Verify HasDefaultValue for methods that do not have default
        [Fact]
        public static void TestHasDefaultValue5()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "Method1", 1);

            Assert.False(pi.HasDefaultValue);
        }



        //Verify HasDefaultValue for methods that do not have default
        [Fact]
        public static void TestHasDefaultValue6()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "MethodWithRefKW", 0);

            Assert.False(pi.HasDefaultValue);
        }

        //Verify HasDefaultValue for methods that do not have default
        [Fact]
        public static void TestHasDefaultValue7()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "MethodWithOutKW", 1);

            Assert.False(pi.HasDefaultValue);
        }


        //Verify HasDefaultValue for methods that do not have default
        [Fact]
        public static void TestHasDefaultValue8()
        {
            ParameterInfo pi = getParamInfo(typeof(GenericClass<int>), "genericMethod", 0);

            Assert.False(pi.HasDefaultValue);
        }


        //Verify IsOut
        [Fact]
        public static void TestIsOut1()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "MethodWithRefKW", 0);

            Assert.False(pi.IsOut);
        }

        //Verify IsOut
        [Fact]
        public static void TestIsOut2()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "MethodWithOutKW", 1);

            Assert.True(pi.IsOut);
        }


        //Verify IsOut
        [Fact]
        public static void TestIsOut3()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "Method1", 1);

            Assert.False(pi.IsOut);
        }

        //Verify IsIN
        [Fact]
        public static void TestIsIN1()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "MethodWithOutKW", 1);

            Assert.False(pi.IsIn);
        }


        //Verify DefaultValue for param that has default
        [Fact]
        public static void TestDefaultValue1()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "MethodWithdefault1", 1);
            int value = (int)pi.DefaultValue;

            Assert.Equal(0, value);
        }


        //Verify DefaultValue for param that has default
        [Fact]
        public static void TestDefaultValue2()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "MethodWithdefault2", 0);
            string value = (string)pi.DefaultValue;

            Assert.Equal("abc", value);
        }

        //Verify DefaultValue for param that has default
        [Fact]
        public static void TestDefaultValue3()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "MethodWithdefault3", 0);
            bool value = (bool)pi.DefaultValue;

            Assert.Equal(false, value);
        }


        //Verify DefaultValue for param that has default
        [Fact]
        public static void TestDefaultValue4()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "MethodWithdefault4", 0);
            char value = (char)pi.DefaultValue;

            Assert.Equal('\0', value);
        }

        [Fact]
        public static void TestDefaultValue5()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "MethodWithOptionalAndNoDefault", 0);
            Object defaultValue = pi.DefaultValue;

            Assert.Equal(Missing.Value, defaultValue);
        }

        //Verify IsOptional
        [Fact]
        public static void TestIsOptional1()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "Method1", 1);

            Assert.False(pi.IsOptional);
        }

        //Verify IsOptional
        [Fact]
        public static void TestIsOptional2()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "MethodWithOutKW", 1);

            Assert.False(pi.IsOptional);
        }


        //Verify IsRetval
        [Fact]
        public static void TestIsRetval1()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "Method1", 1);

            Assert.False(pi.IsRetval);
        }

        //Verify IsRetval
        [Fact]
        public static void TestIsRetval2()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "MethodWithdefault2", 0);

            Assert.False(pi.IsRetval);
        }

        [Fact]
        public static void AttributesTest()
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "MethodWithOptionalDefaultOutInMarshalParam", 0);

            Assert.True(pi.Attributes.HasFlag(ParameterAttributes.Optional));
            Assert.True(pi.Attributes.HasFlag(ParameterAttributes.HasDefault));
            Assert.True(pi.Attributes.HasFlag(ParameterAttributes.HasFieldMarshal));
            Assert.True(pi.Attributes.HasFlag(ParameterAttributes.Out));
            Assert.True(pi.Attributes.HasFlag(ParameterAttributes.In));
        }

        [Theory]
        [InlineData(typeof(OptionalAttribute))]
        [InlineData(typeof(MarshalAsAttribute))]
        [InlineData(typeof(OutAttribute))]
        [InlineData(typeof(InAttribute))]
        public static void CustomAttributesTest(Type attrType)
        {
            ParameterInfo pi = getParamInfo(typeof(MyClass), "MethodWithOptionalDefaultOutInMarshalParam", 0);

            CustomAttributeData attribute = pi.CustomAttributes.SingleOrDefault(a => a.AttributeType.Equals(attrType));

            Assert.NotNull(attribute);

            ICustomAttributeProvider prov = pi as ICustomAttributeProvider;
            Assert.NotNull(prov.GetCustomAttributes(attrType, false).SingleOrDefault());
            Assert.NotNull(prov.GetCustomAttributes(attrType, true).SingleOrDefault());
            Assert.NotNull(prov.GetCustomAttributes(false).SingleOrDefault(a => a.GetType().Equals(attrType)));
            Assert.NotNull(prov.GetCustomAttributes(true).SingleOrDefault(a => a.GetType().Equals(attrType)));
            Assert.True(prov.IsDefined(attrType, false));
            Assert.True(prov.IsDefined(attrType, true));
        }


        //Helper Method to get ParameterInfo object based on index
        private static ParameterInfo getParamInfo(Type type, string methodName, int index)
        {
            MethodInfo mi = getMethod(type, methodName);

            Assert.NotNull(mi);
            ParameterInfo[] allparams = mi.GetParameters();

            Assert.NotNull(allparams);

            if (index < allparams.Length)
                return allparams[index];
            else
                return null;
        }


        private static ParameterInfo getReturnParam(Type type, string methodName)
        {
            MethodInfo mi = getMethod(type, methodName);

            Assert.NotNull(mi);
            return mi.ReturnParameter;
        }


        private static MethodInfo getMethod(Type t, string method)
        {
            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<MethodInfo> alldefinedMethods = ti.DeclaredMethods.GetEnumerator();
            MethodInfo mi = null;

            while (alldefinedMethods.MoveNext())
            {
                if (alldefinedMethods.Current.Name.Equals(method))
                {
                    //found method
                    mi = alldefinedMethods.Current;
                    break;
                }
            }
            return mi;
        }


        // Class For Reflection Metadata
        public class MyClass
        {
            public int Method1(String str, int iValue, long lValue)
            {
                return 1;
            }

            public string Method2()
            {
                return "somestring";
            }

            public int MethodWithArray(String[] strArray)
            {
                for (int ii = 0; ii < strArray.Length; ++ii)
                {
                }
                return strArray.Length;
            }

            public virtual void virtualMethod(long data)
            {
            }

            public bool MethodWithRefKW(ref String str)
            {
                str = "newstring";
                return true;
            }

            public Object MethodWithOutKW(int i, out String str)
            {
                str = "newstring";

                return (object)str;
            }

            public int MethodWithdefault1(long lValue, int iValue = 0)
            {
                return 1;
            }

            public int MethodWithdefault2(string str = "abc")
            {
                return 1;
            }

            public int MethodWithdefault3(bool result = false)
            {
                return 1;
            }

            public int MethodWithdefault4(char c = '\0')
            {
                return 1;
            }

            public int MethodWithOptionalAndNoDefault([Optional] Object o)
            {
                return 1;
            }

            public int MethodWithOptionalDefaultOutInMarshalParam([MarshalAs(UnmanagedType.LPWStr)][Out][In] string str = "")
            {
                return 1;
            }
        }

        public class GenericClass<T>
        {
            public string genericMethod(T t) { return "somestring"; }

            public string genericMethodWithdefault(int i, T t = default(T)) { return "somestring"; }
        }
    }
}
