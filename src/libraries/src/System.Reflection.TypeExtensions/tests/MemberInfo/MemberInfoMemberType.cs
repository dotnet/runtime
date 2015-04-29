// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests.MemberInfoTests
{
    // MemberInfo.MemberType Property  
    // When overridden in a derived class, gets a MemberTypes value indicating 
    // the type of the member  method, constructor, event, and so on. 
    public class ReflectionMemberInfoMemberType
    {
        // PosTest1: get accessor of public static property
        [Fact]
        public void PosTest1()
        {
            bool expectedValue = true;
            bool actualValue = false;
            MethodInfo methodInfo;
            MemberInfo memberInfo;
            methodInfo = typeof(TestClass1).GetProperty("InstanceCount").GetGetMethod();
            memberInfo = methodInfo as MemberInfo;
            actualValue = memberInfo.Name.Equals("get_InstanceCount");
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest2: set accessor of public instance property
        [Fact]
        public void PosTest2()
        {
            bool expectedValue = true;
            bool actualValue = false;
            MethodInfo methodInfo;
            MemberInfo memberInfo; methodInfo = typeof(TestClass1).GetProperty("Data1").GetSetMethod();
            memberInfo = methodInfo as MemberInfo;
            actualValue = memberInfo.Name.Equals("set_Data1");
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest3: public static property 
        [Fact]
        public void PosTest3()
        {
            bool expectedValue = true;
            bool actualValue = false;
            PropertyInfo propertyInfo;
            MemberInfo memberInfo;
            propertyInfo = typeof(TestClass1).GetProperty("InstanceCount");
            memberInfo = propertyInfo as MemberInfo;
            actualValue = memberInfo.Name.Equals("InstanceCount");
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest4: public instance property
        [Fact]
        public void PosTest4()
        {
            bool expectedValue = true;
            bool actualValue = false;
            PropertyInfo propertyInfo;
            MemberInfo memberInfo;
            propertyInfo = typeof(TestClass1).GetProperty("Data1");
            memberInfo = propertyInfo as MemberInfo;
            actualValue = memberInfo.Name.Equals("Data1");
            Assert.Equal(expectedValue, actualValue);
        }


        // PosTest5: public constructor 
        [Fact]
        public void PosTest5()
        {
            bool expectedValue = true;
            bool actualValue = false;
            ConstructorInfo constructorInfo;
            MemberInfo memberInfo;
            Type[] parameterTypes = { typeof(int) };
            constructorInfo = typeof(TestClass1).GetConstructor(parameterTypes);
            memberInfo = constructorInfo as MemberInfo;
            actualValue = memberInfo.Name.Equals(".ctor"); ;
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest6: private instance field
        [Fact]
        public void PosTest6()
        {
            bool expectedValue = true;
            bool actualValue = false;
            Type testType;
            FieldInfo fieldInfo;
            MemberInfo memberInfo;
            testType = typeof(TestClass1);
            fieldInfo = testType.GetField("_data1", BindingFlags.NonPublic | BindingFlags.Instance);
            memberInfo = fieldInfo as MemberInfo;
            actualValue = memberInfo.Name.Equals("_data1");
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest7: private static field
        [Fact]
        public void PosTest7()
        {
            bool expectedValue = true;
            bool actualValue = false;
            Type testType;
            FieldInfo fieldInfo;
            MemberInfo memberInfo;
            testType = typeof(TestClass1);
            fieldInfo = testType.GetField("s_count", BindingFlags.NonPublic | BindingFlags.Static);
            memberInfo = fieldInfo as MemberInfo;
            actualValue = memberInfo.Name.Equals("s_count"); ;
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest8: private instance event
        [Fact]
        public void PosTest8()
        {
            bool expectedValue = true;
            bool actualValue = false;
            Type testType;
            EventInfo eventInfo;
            MemberInfo memberInfo;
            testType = typeof(TestButton);
            eventInfo = testType.GetEvent("Click");
            memberInfo = eventInfo as MemberInfo;
            actualValue = memberInfo.Name.Equals("Click"); ;
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest9: nested type
        [Fact]
        public void PosTest9()
        {
            bool expectedValue = true;
            bool actualValue = false;
            Type testType;
            Type nestedType;
            testType = typeof(ReflectionMemberInfoMemberType);
            nestedType = testType.GetNestedType("TestClass1", BindingFlags.NonPublic);
            actualValue = nestedType.IsNested;
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest10: unnested type
        [Fact]
        public void PosTest10()
        {
            bool expectedValue = true;
            bool actualValue = false;
            Type testType;
            testType = typeof(ReflectionMemberInfoMemberType);
            actualValue = testType.Name.Equals("ReflectionMemberInfoMemberType");
            Assert.Equal(expectedValue, actualValue);
        }

        private class TestClass1
        {
            private static int s_count = 0;

            //Defualt constructor
            public TestClass1()
            {
                ++s_count;
            }

            public TestClass1(int data1)
            {
                ++s_count;
                _data1 = data1;
            }

            public static int InstanceCount
            {
                get
                {
                    return s_count;
                }
            }

            public int Data1
            {
                get
                {
                    return _data1;
                }
                set
                {
                    _data1 = value;
                }
            }

            private int _data1;

            public void Do()
            {
            }
        }

        private class TestButton
        {
            public event EventHandler Click;
            protected void OnClick(EventArgs e)
            {
                if (null != Click)
                {
                    Click(this, e);
                }
            }
        }
    }
}