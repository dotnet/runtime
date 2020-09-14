// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace DebuggerTests.GetPropertiesTests
{

    public interface IFirstName
    {
        string FirstName { get; }
    }

    public interface ILastName
    {
        string LastName { get; }
    }

    public interface IName : IFirstName, ILastName
    { }

    public class BaseBaseClass
    {
        public string BaseBase_MemberForOverride { get; set; }
    }

    public class BaseClass : BaseBaseClass, IName
    {
        private string _base_name;
        private DateTime _base_dateTime => new DateTime(2134, 5, 7, 1, 9, 2);
        protected int base_num;

        public string Base_AutoStringProperty { get; set; }
        public virtual DateTime DateTimeForOverride { get; set; }
        public string Base_AutoStringPropertyForOverrideWithField { get; set; }

        public virtual string StringPropertyForOverrideWithAutoProperty => "base#StringPropertyForOverrideWithAutoProperty";
        public virtual string Base_GetterForOverrideWithField => "base#Base_GetterForOverrideWithField";
        public new string BaseBase_MemberForOverride => "Base#BaseBase_MemberForOverride";

        public string this[string s] => s + "_hello";

        public BaseClass()
        {
            _base_name = "private_name";
            base_num = 5;
            Base_AutoStringProperty = "base#Base_AutoStringProperty";
            DateTimeForOverride = new DateTime(2250, 4, 5, 6, 7, 8);
            //AutoStringPropertyForOverride = "base#AutoStringPropertyForOverride";
        }

        public string GetBaseName() => _base_name;

        public virtual string FirstName => "BaseClass#FirstName";
        public virtual string LastName => "BaseClass#LastName";
    }

    public class DerivedClass : BaseClass, ICloneable
    {
        // public string _base_name = "DerivedClass#_base_name";
        private string _stringField = "DerivedClass#_stringField";
        private DateTime _dateTime = new DateTime(2020, 7, 6, 5, 4, 3);
        private DateTime _DTProp => new DateTime(2200, 5, 6, 7, 8, 9);

        public int a;
        public DateTime DateTime => _DTProp.AddMinutes(10);
        public string AutoStringProperty { get; set; }
        public override string FirstName => "DerivedClass#FirstName";

        // Overrides an auto-property with a getter
        public override DateTime DateTimeForOverride => new DateTime(2190, 9, 7, 5, 3, 2);
        public override string StringPropertyForOverrideWithAutoProperty { get; }
        public new string Base_AutoStringPropertyForOverrideWithField = "DerivedClass#Base_AutoStringPropertyForOverrideWithField";
        public new string Base_GetterForOverrideWithField = "DerivedClass#Base_GetterForOverrideWithField";
        public new string BaseBase_MemberForOverride = "DerivedClass#BaseBase_MemberForOverride";

        public int this[int i, string s] => i + 1 + s.Length;

        object ICloneable.Clone()
        {
            // not meant to be used!
            return new DerivedClass();
        }

        public DerivedClass()
        {
            a = 4;
            AutoStringProperty = "DerivedClass#AutoStringProperty";
            StringPropertyForOverrideWithAutoProperty = "DerivedClass#StringPropertyForOverrideWithAutoProperty";
        }

        public static void run()
        {
            new DerivedClass().InstanceMethod();
            new DerivedClass().InstanceMethodAsync().Wait();
        }

        public string GetStringField() => _stringField;

        public void InstanceMethod()
        {
            Console.WriteLine($"break here");
        }

        public async Task InstanceMethodAsync()
        {
            Console.WriteLine($"break here");
            await Task.CompletedTask;
        }
    }

    public struct CloneableStruct : ICloneable, IName
    {
        private string _stringField;
        private DateTime _dateTime;
        private DateTime _DTProp => new DateTime(2200, 5, 6, 7, 8, 9);

        public int a;
        public DateTime DateTime => _DTProp.AddMinutes(10);
        public string AutoStringProperty { get; set; }
        public string FirstName => "CloneableStruct#FirstName";
        public string LastName => "CloneableStruct#LastName";
        public int this[int i] => i + 1;

        object ICloneable.Clone()
        {
            // not meant to be used!
            return new CloneableStruct(0);
        }

        public CloneableStruct(int bias)
        {
            a = 4;
            _stringField = "CloneableStruct#_stringField";
            _dateTime = new DateTime(2020, 7, 6, 5, 4, 3 + bias);
            AutoStringProperty = "CloneableStruct#AutoStringProperty";
        }

        public static void run()
        {
            new CloneableStruct(3).InstanceMethod();
            new CloneableStruct(3).InstanceMethodAsync().Wait();
        }

        public string GetStringField() => _stringField;

        public void InstanceMethod()
        {
            Console.WriteLine($"break here");
        }

        public async Task InstanceMethodAsync()
        {
            Console.WriteLine($"break here");
            await Task.CompletedTask;
        }

        public static void SimpleStaticMethod(DateTime dateTimeArg, string stringArg)
        {
            Console.WriteLine($"break here");
        }

    }

    public struct NestedStruct
    {
        public CloneableStruct cloneableStruct;

        public NestedStruct(int bias)
        {
            cloneableStruct = new CloneableStruct(bias);
        }

        public static void run()
        {
            TestNestedStructStatic();
            TestNestedStructStaticAsync().Wait();
        }

        public static void TestNestedStructStatic()
        {
            var ns = new NestedStruct(3);
            Console.WriteLine($"break here");
        }

        public static async Task TestNestedStructStaticAsync()
        {
            var ns = new NestedStruct(3);
            Console.WriteLine($"break here");
            await Task.CompletedTask;
        }
    }

    class BaseClassForJSTest
    {
        public string kind = "car";
        public string make = "mini";
        public bool available => true;
    }

    class DerivedClassForJSTest : BaseClassForJSTest
    {
        public string owner_name = "foo";
        public string owner_last_name => "bar";

        public static void run()
        {
            var obj = new DerivedClassForJSTest();
            Console.WriteLine($"break here");
        }
    }

    public class TestWithReflection
    {
        public static void run()
        {
            InvokeReflectedStaticMethod(10, "foobar", new DateTime(1234, 6, 7, 8, 9, 10), 100, "xyz", 345, "abc");
        }

        public static void InvokeReflectedStaticMethod(int num, string name, DateTime some_date, int num1, string str2, int num3, string str3)
        {
            var mi = typeof(CloneableStruct).GetMethod("SimpleStaticMethod");
            var dt = new DateTime(4210, 3, 4, 5, 6, 7);
            int i = 4;

            string[] strings = new[] { "abc" };
            CloneableStruct cs = new CloneableStruct();

            // var cs = new CloneableStruct();
            mi.Invoke(null, new object[] { dt, "called from run" });
        }
    }
}
