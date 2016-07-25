// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Emit.Tests
{
    public class TestConstructor
    {
        static TestConstructor() { }
        internal TestConstructor(bool b) { }
    }

    public class CustomAttributeBuilderTest : Attribute
    {
        public string TestString
        {
            get { return TestStringField; }
            set { TestStringField = value; }
        }

        public int TestInt32
        {
            get { return TestInt; }
            set { TestInt = value; }
        }

        public string GetOnlyString
        {
            get { return GetString; }
        }

        public int GetOnlyInt32
        {
            get { return GetInt; }
        }

        public string TestStringField;
        public int TestInt;
        public string GetString;
        public int GetInt;

        public CustomAttributeBuilderTest() { }

        public CustomAttributeBuilderTest(string getOnlyString, int getOnlyInt32)
        {
            GetString = getOnlyString;
            GetInt = getOnlyInt32;
        }

        public CustomAttributeBuilderTest(string testString, int testInt32, string getOnlyString, int getOnlyInt32)
        {
            TestStringField = testString;
            TestInt = testInt32;
            GetString = getOnlyString;
            GetInt = getOnlyInt32;
        }
    }

    public static class Helpers
    {
        public static FieldInfo[] GetFields(params string[] fieldNames)
        {
            FieldInfo[] fields = new FieldInfo[fieldNames.Length];
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
            for (int i = 0; i < fieldNames.Length; i++)
            {
                string name = fieldNames[i];
                fields[i] = name == null ? null : typeof(CustomAttributeBuilderTest).GetField(name, Flags);
            }
            return fields;
        }

        public static PropertyInfo[] GetProperties(params string[] propertyNames)
        {
            PropertyInfo[] properties = new PropertyInfo[propertyNames.Length];
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
            for (int i = 0; i < propertyNames.Length; i++)
            {
                string name = propertyNames[i];
                properties[i] = name == null ? null : typeof(CustomAttributeBuilderTest).GetProperty(name, Flags);
            }
            return properties;
        }
        public static AssemblyBuilder DynamicAssembly(string name = "TestAssembly")
        {
            AssemblyName assemblyName = new AssemblyName(name);
            return AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        }

        public static ModuleBuilder DynamicModule(string assemblyName = "TestAssembly", string moduleName = "TestModule")
        {
            return DynamicAssembly(assemblyName).DefineDynamicModule(moduleName);
        }

        public static TypeBuilder DynamicType(TypeAttributes attributes, string assemblyName = "TestAssembly", string moduleName = "TestModule", string typeName = "TestType")
        {
            return DynamicModule(assemblyName, moduleName).DefineType(typeName, attributes);
        }
    }
}
