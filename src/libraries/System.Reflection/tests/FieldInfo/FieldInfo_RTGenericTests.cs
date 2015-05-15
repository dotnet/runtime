// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;


#pragma warning disable 0414


namespace System.Reflection.Tests
{
    public class FieldInfoRTGenericTests
    {
        //Verify FieldInfo for generic Types
        [Fact]
        public static void TestGenericFields()
        {
            FieldInfoRTGenericTests myInstance = new FieldInfoRTGenericTests();
            myInstance.populateScenario();

            Type openType = typeof(PublicFieldGeneric<>);
            foreach (Scenario sc in list)
            {
                Type type = openType.MakeGenericType(new Type[] { sc.gaType });
                Object obj = Activator.CreateInstance(type);
                FieldInfo fi = null;

                fi = getField(type, sc.fieldName);
                Assert.True((fi.GetValue(obj)).Equals(sc.initialValue), "Get Value should return value that was set for generic type: " + sc.fieldName);

                fi.SetValue(obj, sc.changedValue);
                Assert.True((fi.GetValue(obj)).Equals(sc.changedValue), "Get Value should return value that was set for generic type: " + sc.fieldName);

                fi.SetValue(obj, null);
                Assert.True((fi.GetValue(obj)).Equals(sc.initialValue), "Get Value should return value that was set for generic type: " + sc.fieldName);
            }
        }



        //Verify FieldInfo for static generic Types
        [Fact]
        public static void TestStaticGenericFields()
        {
            FieldInfoRTGenericTests myInstance = new FieldInfoRTGenericTests();
            myInstance.populateScenarioForStaticTests();

            Type openType = typeof(StaticFieldGeneric<>);
            foreach (Scenario sc in list)
            {
                Type type = openType.MakeGenericType(new Type[] { sc.gaType });
                Object obj = Activator.CreateInstance(type);
                FieldInfo fi = null;

                fi = getField(type, sc.fieldName);
                Assert.True((fi.GetValue(obj)).Equals(sc.initialValue), "Get Value should return value that was set for generic type: " + sc.fieldName);

                fi.SetValue(obj, sc.changedValue);
                Assert.True((fi.GetValue(obj)).Equals(sc.changedValue), "Get Value should return value that was set for generic type: " + sc.fieldName);

                fi.SetValue(obj, null);
                Assert.True((fi.GetValue(obj)).Equals(sc.initialValue), "Get Value should return value that was set for generic type: " + sc.fieldName);
            }
        }




        // Helper method to get field from class FieldInfoRTGenericTests
        private static FieldInfo getField(string field)
        {
            Type t = typeof(FieldInfoRTGenericTests);
            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<FieldInfo> alldefinedFields = ti.DeclaredFields.GetEnumerator();
            FieldInfo fi = null, found = null;

            while (alldefinedFields.MoveNext())
            {
                fi = alldefinedFields.Current;
                if (fi.Name.Equals(field))
                {
                    //found type
                    found = fi;
                    break;
                }
            }
            return found;
        }


        // Helper method to get field from Type type
        private static FieldInfo getField(Type ptype, string field)
        {
            TypeInfo ti = ptype.GetTypeInfo();
            IEnumerator<FieldInfo> alldefinedFields = ti.DeclaredFields.GetEnumerator();
            FieldInfo fi = null, found = null;

            while (alldefinedFields.MoveNext())
            {
                fi = alldefinedFields.Current;
                if (fi.Name.Equals(field))
                {
                    //found type
                    found = fi;
                    break;
                }
            }
            return found;
        }




        private void populateScenario()
        {
            FieldInfoGeneric<int> g_int = new FieldInfoGeneric<int>();
            PublicFieldGeneric<int> pfg_int = new PublicFieldGeneric<int>();
            int[] gpa_int = new int[] { 300, 400 };
            FieldInfoGeneric<int>[] ga_int = new FieldInfoGeneric<int>[] { g_int };

            FieldInfoGeneric<string> g_string = new FieldInfoGeneric<string>();
            PublicFieldGeneric<string> pfg_string = new PublicFieldGeneric<string>();
            string[] gpa_string = new string[] { "forget", "about this" };
            FieldInfoGeneric<string>[] ga_string = new FieldInfoGeneric<string>[] { g_string, g_string };

            FieldInfoGeneric<object> g_object = new FieldInfoGeneric<object>();
            PublicFieldGeneric<object> pfg_object = new PublicFieldGeneric<object>();
            object[] gpa_object = new object[] { "world", 300, g_object };
            FieldInfoGeneric<object>[] ga_object = new FieldInfoGeneric<object>[] { g_object, g_object, g_object };

            FieldInfoGeneric<FieldInfoGeneric<object>> g_g_object = new FieldInfoGeneric<FieldInfoGeneric<object>>();
            PublicFieldGeneric<FieldInfoGeneric<object>> pfg_g_object = new PublicFieldGeneric<FieldInfoGeneric<object>>();
            FieldInfoGeneric<object>[] gpa_g_object = new FieldInfoGeneric<object>[] { g_object, g_object };
            FieldInfoGeneric<FieldInfoGeneric<object>>[] ga_g_object = new FieldInfoGeneric<FieldInfoGeneric<object>>[] { g_g_object, g_g_object, g_g_object, g_g_object };

            List<Scenario> list = new List<Scenario>();

            list.Add(new Scenario(typeof(int), "genparamField", 0, -300));
            list.Add(new Scenario(typeof(int), "dependField", null, g_int));
            list.Add(new Scenario(typeof(int), "gparrayField", null, gpa_int));
            list.Add(new Scenario(typeof(int), "arrayField", null, ga_int));

            list.Add(new Scenario(typeof(string), "genparamField", null, "hello   !"));
            list.Add(new Scenario(typeof(string), "dependField", null, g_string));
            list.Add(new Scenario(typeof(string), "gparrayField", null, gpa_string));
            list.Add(new Scenario(typeof(string), "arrayField", null, ga_string));

            list.Add(new Scenario(typeof(object), "genparamField", null, (object)300));
            list.Add(new Scenario(typeof(object), "dependField", null, g_object));
            list.Add(new Scenario(typeof(object), "gparrayField", null, gpa_object));
            list.Add(new Scenario(typeof(object), "arrayField", null, ga_object));

            list.Add(new Scenario(typeof(FieldInfoGeneric<object>), "genparamField", null, g_object));
            list.Add(new Scenario(typeof(FieldInfoGeneric<object>), "dependField", null, g_g_object));
            list.Add(new Scenario(typeof(FieldInfoGeneric<object>), "gparrayField", null, gpa_g_object));
            list.Add(new Scenario(typeof(FieldInfoGeneric<object>), "arrayField", null, ga_g_object));

            list.Add(new Scenario(typeof(int), "selfField", null, pfg_int));
            list.Add(new Scenario(typeof(string), "selfField", null, pfg_string));
            list.Add(new Scenario(typeof(object), "selfField", null, pfg_object));
            list.Add(new Scenario(typeof(FieldInfoGeneric<object>), "selfField", null, pfg_g_object));
        }



        private void populateScenarioForStaticTests()
        {
            List<Scenario> list = new List<Scenario>();

            StaticFieldGeneric<int> sfg_int = new StaticFieldGeneric<int>();
            StaticFieldGeneric<string> sfg_string = new StaticFieldGeneric<string>();
            StaticFieldGeneric<object> sfg_object = new StaticFieldGeneric<object>();
            StaticFieldGeneric<FieldInfoGeneric<object>> sfg_g_object = new StaticFieldGeneric<FieldInfoGeneric<object>>();

            list.Add(new Scenario(typeof(int), "genparamField", 0, -300));
            list.Add(new Scenario(typeof(int), "dependField", null, g_int));
            list.Add(new Scenario(typeof(int), "gparrayField", null, gpa_int));
            list.Add(new Scenario(typeof(int), "arrayField", null, ga_int));

            list.Add(new Scenario(typeof(string), "genparamField", null, "hello   !"));
            list.Add(new Scenario(typeof(string), "dependField", null, g_string));
            list.Add(new Scenario(typeof(string), "gparrayField", null, gpa_string));
            list.Add(new Scenario(typeof(string), "arrayField", null, ga_string));

            list.Add(new Scenario(typeof(object), "genparamField", null, (object)300));
            list.Add(new Scenario(typeof(object), "dependField", null, g_object));
            list.Add(new Scenario(typeof(object), "gparrayField", null, gpa_object));
            list.Add(new Scenario(typeof(object), "arrayField", null, ga_object));

            list.Add(new Scenario(typeof(FieldInfoGeneric<object>), "genparamField", null, g_object));
            list.Add(new Scenario(typeof(FieldInfoGeneric<object>), "dependField", null, g_g_object));
            list.Add(new Scenario(typeof(FieldInfoGeneric<object>), "gparrayField", null, gpa_g_object));
            list.Add(new Scenario(typeof(FieldInfoGeneric<object>), "arrayField", null, ga_g_object));

            list.Add(new Scenario(typeof(int), "selfField", null, sfg_int));
            list.Add(new Scenario(typeof(string), "selfField", null, sfg_string));
            list.Add(new Scenario(typeof(object), "selfField", null, sfg_object));
            list.Add(new Scenario(typeof(FieldInfoGeneric<object>), "selfField", null, sfg_g_object));
        }

        //Reflection Fields

        public FieldInfoGeneric<int> g_int;
        public PublicFieldGeneric<int> pfg_int;
        public int[] gpa_int;
        public FieldInfoGeneric<int>[] ga_int;

        public FieldInfoGeneric<string> g_string;
        public PublicFieldGeneric<string> pfg_string;
        public string[] gpa_string;
        public FieldInfoGeneric<string>[] ga_string;

        public FieldInfoGeneric<object> g_object;
        public PublicFieldGeneric<object> pfg_object;
        public object[] gpa_object;
        public FieldInfoGeneric<object>[] ga_object;

        public FieldInfoGeneric<FieldInfoGeneric<object>> g_g_object;
        public PublicFieldGeneric<FieldInfoGeneric<object>> pfg_g_object;
        public FieldInfoGeneric<object>[] gpa_g_object;
        public FieldInfoGeneric<FieldInfoGeneric<object>>[] ga_g_object;

        public static List<Scenario> list = new List<Scenario>();
    }


    // Fields for Refletion
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
