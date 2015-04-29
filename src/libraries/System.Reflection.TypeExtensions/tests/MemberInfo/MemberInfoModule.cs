// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests.MemberInfoTests
{
    // MemberInfo.Module Property  
    public class ReflectionMemberInfoModule
    {
        // PosTest3: private instance field in current executing module
        [Fact]
        public void PosTest3()
        {
            bool expectedValue = true;
            bool actualValue = false;
            FieldInfo fieldInfo;
            fieldInfo = typeof(TestClass1).GetField("_data1", BindingFlags.NonPublic | BindingFlags.Instance);
            MemberInfo memberInfo = fieldInfo as MemberInfo;
            actualValue = memberInfo.DeclaringType.Equals(typeof(TestClass1)) && memberInfo.Name.Equals("_data1");
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest4: private static field in current executing module
        [Fact]
        public void PosTest4()
        {
            bool expectedValue = true;
            bool actualValue = false;
            FieldInfo fieldInfo;
            fieldInfo = typeof(TestClass1).GetField("s_count", BindingFlags.NonPublic | BindingFlags.Static);
            MemberInfo memberInfo = fieldInfo as MemberInfo;
            actualValue = memberInfo.DeclaringType.Equals(typeof(TestClass1)) && memberInfo.Name.Equals("s_count");
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest5: public instance method in current executing module
        [Fact]
        public void PosTest5()
        {
            bool expectedValue = true;
            bool actualValue = false;
            MethodInfo methodInfo;
            methodInfo = typeof(TestClass1).GetMethod("Do", BindingFlags.Public | BindingFlags.Instance);
            MemberInfo memberInfo = methodInfo as MemberInfo;
            actualValue = memberInfo.DeclaringType.Equals(typeof(TestClass1)) && memberInfo.Name.Equals("Do");
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest6: public static method in current executing module
        [Fact]
        public void PosTest6()
        {
            bool expectedValue = true;
            bool actualValue = false;
            PropertyInfo propertyInfo;
            MethodInfo methodInfo;
            propertyInfo = typeof(TestClass1).GetProperty("InstanceCount", BindingFlags.Static | BindingFlags.Public);
            methodInfo = propertyInfo.GetGetMethod();
            actualValue = methodInfo.DeclaringType.Equals(typeof(TestClass1)) && methodInfo.Name.Equals("get_InstanceCount");
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest7: public static property in current executing module
        [Fact]
        public void PosTest7()
        {
            bool actualValue = false;
            PropertyInfo propertyInfo;
            propertyInfo = typeof(TestClass1).GetProperty("InstanceCount", BindingFlags.Static | BindingFlags.Public);
            MemberInfo memberInfo = propertyInfo as MemberInfo;
            actualValue = memberInfo.DeclaringType.Equals(typeof(TestClass1)) && memberInfo.Name.Equals("InstanceCount");
        }

        // PosTest8: public instance property in current executing module
        [Fact]
        public void PosTest8()
        {
            bool expectedValue = true;
            bool actualValue = false;
            PropertyInfo propertyInfo; propertyInfo = typeof(TestClass1).GetProperty("Data1", BindingFlags.Instance | BindingFlags.Public);
            MemberInfo memberInfo = propertyInfo as MemberInfo;
            actualValue = memberInfo.DeclaringType.Equals(typeof(TestClass1)) && memberInfo.Name.Equals("Data1");
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest9: public instance event in current executing module
        [Fact]
        public void PosTest9()
        {
            bool expectedValue = true;
            bool actualValue = false;
            EventInfo eventInfo;
            eventInfo = typeof(TestButton).GetEvent("Click", BindingFlags.Instance | BindingFlags.Public);
            MemberInfo memberInfo = eventInfo as MemberInfo;
            actualValue = memberInfo.DeclaringType.Equals(typeof(TestButton)) && memberInfo.Name.Equals("Click");
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest10: public constructor in current executing module
        [Fact]
        public void PosTest10()
        {
            bool expectedValue = true;
            bool actualValue = false;
            ConstructorInfo constructorInfo;
            Type[] parametersTypes = { typeof(int) };
            constructorInfo = typeof(TestClass1).GetConstructor(parametersTypes);
            MemberInfo memberInfo = constructorInfo as MemberInfo;
            actualValue = memberInfo.DeclaringType.Equals(typeof(TestClass1)) && memberInfo.Name.Equals(".ctor");
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