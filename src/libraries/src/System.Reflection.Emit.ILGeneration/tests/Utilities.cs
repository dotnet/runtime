// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Emit.Tests
{
    public class TestAttribute : Attribute
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

        public object ObjectProperty { get; set; }

        public string TestStringField;
        public int TestInt;
        public string GetString;
        public int GetInt;

        public object ObjectField;

        public static int StaticProperty { get; set; }
        public const int ConstField = 1;
        public readonly int ReadonlyField = 2;
        public static int StaticField = 3;
        public static int StaticReadonlyField = 4;

        public TestAttribute() { }
        public TestAttribute(int i) { }
        public TestAttribute(object o) { }
        public TestAttribute(int i, bool b) { }
        private TestAttribute(int i, int j, int k) { }
        static TestAttribute() { }

        public TestAttribute(string getOnlyString, int getOnlyInt32)
        {
            GetString = getOnlyString;
            GetInt = getOnlyInt32;
        }

        public TestAttribute(string testString, int testInt32, string getOnlyString, int getOnlyInt32)
        {
            TestStringField = testString;
            TestInt = testInt32;
            GetString = getOnlyString;
            GetInt = getOnlyInt32;
        }

        public static FieldInfo[] AllFields => Helpers.GetFields(typeof(TestAttribute), nameof(TestInt), nameof(TestStringField), nameof(GetString), nameof(GetInt));
        public static PropertyInfo[] AllProperties => Helpers.GetProperties(typeof(TestAttribute), nameof(TestInt32), nameof(TestString), nameof(GetOnlyString), nameof(GetOnlyInt32));
    }

    public class SubAttribute : TestAttribute
    {
        public int SubProperty { get; set; }
        public int SubField;
    }

    public static class Helpers
    {
        public static FieldInfo[] GetFields(Type type, params string[] fieldNames)
        {
            FieldInfo[] fields = new FieldInfo[fieldNames.Length];
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
            for (int i = 0; i < fieldNames.Length; i++)
            {
                string name = fieldNames[i];
                fields[i] = name == null ? null : type.GetField(name, Flags);
            }
            return fields;
        }

        public static PropertyInfo[] GetProperties(Type type,params string[] propertyNames)
        {
            PropertyInfo[] properties = new PropertyInfo[propertyNames.Length];
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
            for (int i = 0; i < propertyNames.Length; i++)
            {
                string name = propertyNames[i];
                properties[i] = name == null ? null : type.GetProperty(name, Flags);
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
