// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class FieldInfoRTGenericTests
    {
        public static IEnumerable<object[]> FieldInfoRTGenericTests_TestData()
        {
            yield return new object[] { typeof(PublicFieldGeneric<>), new List<Scenario>() {
                new Scenario(typeof(int), "genparamField", 0, -300),
                new Scenario(typeof(int), "dependField", null, g_int),
                new Scenario(typeof(int), "gparrayField", null, gpa_int),
                new Scenario(typeof(int), "arrayField", null, ga_int),

                new Scenario(typeof(string), "genparamField", null, "hello   !"),
                new Scenario(typeof(string), "dependField", null, g_string),
                new Scenario(typeof(string), "gparrayField", null, gpa_string),
                new Scenario(typeof(string), "arrayField", null, ga_string),

                new Scenario(typeof(object), "genparamField", null, 300),
                new Scenario(typeof(object), "dependField", null, g_object),
                new Scenario(typeof(object), "gparrayField", null, gpa_object),
                new Scenario(typeof(object), "arrayField", null, ga_object),

                new Scenario(typeof(FieldInfoGeneric<object>), "genparamField", null, g_object),
                new Scenario(typeof(FieldInfoGeneric<object>), "dependField", null, g_g_object),
                new Scenario(typeof(FieldInfoGeneric<object>), "gparrayField", null, gpa_g_object),
                new Scenario(typeof(FieldInfoGeneric<object>), "arrayField", null, ga_g_object),

                new Scenario(typeof(int), "selfField", null, pfg_int),
                new Scenario(typeof(string), "selfField", null, pfg_string),
                new Scenario(typeof(object), "selfField", null, pfg_object),
                new Scenario(typeof(FieldInfoGeneric<object>), "selfField", null, pfg_g_object)
            } };

            yield return new object[] { typeof(StaticFieldGeneric<>), new List<Scenario>() {
                new Scenario(typeof(int), "genparamField", 0, -300),
                new Scenario(typeof(int), "dependField", null, g_int),
                new Scenario(typeof(int), "gparrayField", null, gpa_int),
                new Scenario(typeof(int), "arrayField", null, ga_int),

                new Scenario(typeof(string), "genparamField", null, "hello   !"),
                new Scenario(typeof(string), "dependField", null, g_string),
                new Scenario(typeof(string), "gparrayField", null, gpa_string),
                new Scenario(typeof(string), "arrayField", null, ga_string),

                new Scenario(typeof(object), "genparamField", null, 300),
                new Scenario(typeof(object), "dependField", null, g_object),
                new Scenario(typeof(object), "gparrayField", null, gpa_object),
                new Scenario(typeof(object), "arrayField", null, ga_object),

                new Scenario(typeof(FieldInfoGeneric<object>), "genparamField", null, g_object),
                new Scenario(typeof(FieldInfoGeneric<object>), "dependField", null, g_g_object),
                new Scenario(typeof(FieldInfoGeneric<object>), "gparrayField", null, gpa_g_object),
                new Scenario(typeof(FieldInfoGeneric<object>), "arrayField", null, ga_g_object),

                new Scenario(typeof(int), "selfField", null, sfg_int),
                new Scenario(typeof(string), "selfField", null, sfg_string),
                new Scenario(typeof(object), "selfField", null, sfg_object),
                new Scenario(typeof(FieldInfoGeneric<object>), "selfField", null, sfg_g_object)
            } };
        }

        [Theory]
        [MemberData(nameof(FieldInfoRTGenericTests_TestData))]
        public static void TestFieldInfo_InstanceAndStaticGenericFields(Type openType, List<Scenario> list)
        {
            foreach (Scenario sc in list)
            {
                Type type = openType.MakeGenericType(new Type[] { sc.gaType });
                object obj = Activator.CreateInstance(type);
                FieldInfo fi = null;

                fi = GetField(type, sc.fieldName);
                Assert.True(Equals(fi.GetValue(obj), sc.initialValue), "Get Value should return value that was set for generic type: " + sc.fieldName);

                fi.SetValue(obj, sc.changedValue);
                Assert.True(Equals(fi.GetValue(obj), sc.changedValue), "Get Value should return value that was set for generic type: " + sc.fieldName);

                fi.SetValue(obj, null);
                Assert.True(Equals(fi.GetValue(obj), sc.initialValue), "Get Value should return value that was set for generic type: " + sc.fieldName);
            }
        }

        // Helper method to get field from Type type
        private static FieldInfo GetField(Type ptype, string fieldName)
        {
            TypeInfo ti = ptype.GetTypeInfo();
            IEnumerator<FieldInfo> alldefinedFields = ti.DeclaredFields.GetEnumerator();
            FieldInfo fi = null, found = null;

            while (alldefinedFields.MoveNext())
            {
                fi = alldefinedFields.Current;
                if (fi.Name.Equals(fieldName))
                {
                    //found type
                    found = fi;
                    break;
                }
            }
            return found;
        }

        // Reflection Fields

        public static FieldInfoGeneric<int> g_int = new FieldInfoGeneric<int>();
        public static PublicFieldGeneric<int> pfg_int = new PublicFieldGeneric<int>();
        public static StaticFieldGeneric<int> sfg_int = new StaticFieldGeneric<int>();
        public static int[] gpa_int = new int[] { 300, 400 };
        public static FieldInfoGeneric<int>[] ga_int = new FieldInfoGeneric<int>[] { g_int };

        public static FieldInfoGeneric<string> g_string = new FieldInfoGeneric<string>();
        public static PublicFieldGeneric<string> pfg_string = new PublicFieldGeneric<string>();
        public static StaticFieldGeneric<string> sfg_string = new StaticFieldGeneric<string>();
        public static string[] gpa_string = new string[] { "forget", "about this" };
        public static FieldInfoGeneric<string>[] ga_string = new FieldInfoGeneric<string>[] { g_string, g_string };

        public static FieldInfoGeneric<object> g_object = new FieldInfoGeneric<object>();
        public static PublicFieldGeneric<object> pfg_object = new PublicFieldGeneric<object>();
        public static StaticFieldGeneric<object> sfg_object = new StaticFieldGeneric<object>();
        public static object[] gpa_object = new object[] { "world", 300, g_object };
        public static FieldInfoGeneric<object>[] ga_object = new FieldInfoGeneric<object>[] { g_object, g_object, g_object };

        public static FieldInfoGeneric<FieldInfoGeneric<object>> g_g_object = new FieldInfoGeneric<FieldInfoGeneric<object>>();
        public static PublicFieldGeneric<FieldInfoGeneric<object>> pfg_g_object = new PublicFieldGeneric<FieldInfoGeneric<object>>();
        public static StaticFieldGeneric<FieldInfoGeneric<object>> sfg_g_object = new StaticFieldGeneric<FieldInfoGeneric<object>>();
        public static FieldInfoGeneric<object>[] gpa_g_object = new FieldInfoGeneric<object>[] { g_object, g_object };
        public static FieldInfoGeneric<FieldInfoGeneric<object>>[] ga_g_object = new FieldInfoGeneric<FieldInfoGeneric<object>>[] { g_g_object, g_g_object, g_g_object, g_g_object };
    }


    // Fields for Reflection
    public class FieldInfoGeneric<T> { public FieldInfoGeneric() { } }

    public class PublicFieldGeneric<T>
    {
        public T genparamField;
        public T[] gparrayField;
        public FieldInfoGeneric<T> dependField;
        public FieldInfoGeneric<T>[] arrayField;
        public PublicFieldGeneric<T> selfField;
    }

    public class StaticFieldGeneric<T>
    {
        public static T genparamField;
        public static T[] gparrayField;
        public static FieldInfoGeneric<T> dependField;
        public static FieldInfoGeneric<T>[] arrayField;
        public static StaticFieldGeneric<T> selfField;
    }

    public class Scenario
    {
        public Type gaType;
        public string fieldName;
        public object initialValue;
        public object changedValue;

        public Scenario(Type t, string n, object v1, object v2)
        {
            gaType = t;
            fieldName = n;
            initialValue = v1;
            changedValue = v2;
        }
    }
}
