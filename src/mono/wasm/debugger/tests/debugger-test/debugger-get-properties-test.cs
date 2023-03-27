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
        private string _base_autoProperty { get; set; }
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
            _base_autoProperty = "private_autoproperty";
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

        internal bool b = true;

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

    public class BaseBaseClass2
    {
        // for new-hidding with a field:
        public int BaseBase_FieldForHidingWithField = 5;
        public int BaseBase_PropertyForHidingWithField => 10;
        public int BaseBase_AutoPropertyForHidingWithField { get; set; }

        // for new-hidding with a property:
        public string BaseBase_FieldForHidingWithProperty = "BaseBase#BaseBase_FieldForHidingWithProperty";
        public string BaseBase_PropertyForHidingWithProperty => "BaseBase#BaseBase_PropertyForHidingWithProperty";
        public string BaseBase_AutoPropertyForHidingWithProperty { get; set; }

        // for new-hidding with an auto-property:
        public string BaseBase_FieldForHidingWithAutoProperty = "BaseBase#BaseBase_FieldForHidingWithAutoProperty";
        public string BaseBase_PropertyForHidingWithAutoProperty => "BaseBase#BaseBase_PropertyForHidingWithAutoProperty";
        public string BaseBase_AutoPropertyForHidingWithAutoProperty { get; set; }

        // for virtual->override->new-hidden (VOH), virtual->new-hidden->override (VHO) and virtual->override->override (VOO)
        public virtual string BaseBase_PropertyForVOH => "BaseBase#BaseBase_PropertyForVOH";
        public virtual string BaseBase_PropertyForVHO => "BaseBase#BaseBase_PropertyForVHO";
        // public virtual string BaseBase_PropertyForVOO => "BaseBase#BaseBase_PropertyForVOO"; // FixMe: Issue #69788
        public virtual string BaseBase_AutoPropertyForVOH { get; set; }
        public virtual string BaseBase_AutoPropertyForVHO { get; set; }
        // public virtual string BaseBase_AutoPropertyForVOO { get; set; } // FixMe: Issue #69788

        // -----------------static members--------------
        // for new-hidding with a field:
        public static int S_BaseBase_FieldForHidingWithField = 5;
        public static int S_BaseBase_PropertyForHidingWithField => 10;
        public static int S_BaseBase_AutoPropertyForHidingWithField { get; set; }

        // for new-hidding with a property:
        public static string S_BaseBase_FieldForHidingWithProperty = "BaseBase#BaseBase_FieldForHidingWithProperty";
        public static string S_BaseBase_PropertyForHidingWithProperty => "BaseBase#BaseBase_PropertyForHidingWithProperty";
        public static string S_BaseBase_AutoPropertyForHidingWithProperty { get; set; }

        // for new-hidding with an auto-property:
        public static string S_BaseBase_FieldForHidingWithAutoProperty = "BaseBase#BaseBase_FieldForHidingWithAutoProperty";
        public static string S_BaseBase_PropertyForHidingWithAutoProperty => "BaseBase#BaseBase_PropertyForHidingWithAutoProperty";
        public static string S_BaseBase_AutoPropertyForHidingWithAutoProperty { get; set; }

        public BaseBaseClass2()
        {
            BaseBase_AutoPropertyForHidingWithField = 10 + S_BaseBase_FieldForHidingWithField; // = 15; suppressing non-used variable warnings
            BaseBase_AutoPropertyForHidingWithProperty = "BaseBase#BaseBase_AutoPropertyForHidingWithProperty";
            BaseBase_AutoPropertyForHidingWithAutoProperty = "BaseBase#BaseBase_AutoPropertyForHidingWithAutoProperty";

            BaseBase_AutoPropertyForVOH = "BaseBase#BaseBase_AutoPropertyForVOH";
            BaseBase_AutoPropertyForVHO = "BaseBase#BaseBase_AutoPropertyForVHO";
            // BaseBase_AutoPropertyForVOO = "BaseBase#BaseBase_AutoPropertyForVOO"; // FixMe: Issue #69788
        }
    }

    public class BaseClass2 : BaseBaseClass2, IName
    {
        // hiding with a field:
        private new int BaseBase_FieldForHidingWithField = 105;
        protected new int BaseBase_PropertyForHidingWithField = 110;
        public new int BaseBase_AutoPropertyForHidingWithField = 115;

        // hiding with a property:
        protected new string BaseBase_FieldForHidingWithProperty => "Base#BaseBase_FieldForHidingWithProperty";
        public new string BaseBase_PropertyForHidingWithProperty => "Base#BaseBase_PropertyForHidingWithProperty";
        private new string BaseBase_AutoPropertyForHidingWithProperty => "Base#BaseBase_AutoPropertyForHidingWithProperty";

        // hiding with an auto-property:
        public new string BaseBase_FieldForHidingWithAutoProperty { get; set; }
        private new string BaseBase_PropertyForHidingWithAutoProperty { get; set; }
        protected new string BaseBase_AutoPropertyForHidingWithAutoProperty { get; set; }

        // cannot override field and cannot override with a field: skipping

        // for overriding with a property:
        public virtual DateTime Base_PropertyForOverridingWithProperty => new (2104, 5, 7, 1, 9, 2);
        protected virtual DateTime Base_AutoPropertyForOverridingWithProperty { get; set; }

        // for overriding with a auto-property:
        internal virtual DateTime Base_PropertyForOverridingWithAutoProperty => new (2114, 5, 7, 1, 9, 2);
        protected virtual DateTime Base_AutoPropertyForOverridingWithAutoProperty { get; set; }

        // for not being overridden nor hidden:
        public virtual DateTime Base_VirtualPropertyNotOverriddenOrHidden => new (2124, 5, 7, 1, 9, 2);
        public virtual string FirstName => "BaseClass#FirstName";
        public virtual string LastName => "BaseClass#LastName";

        // for virtual->override->new-hidden (VOH), virtual->new-hidden->override (VHO) and virtual->override->override (VOO)
        public override string BaseBase_PropertyForVOH => "Base#BaseBase_PropertyForVOH";
        public new virtual string BaseBase_PropertyForVHO => "Base#BaseBase_PropertyForVHO";
        // public override string BaseBase_PropertyForVOO => "BaseBase#BaseBase_PropertyForVOO"; // FixMe: Issue #69788
        public override string BaseBase_AutoPropertyForVOH { get; set; }
        public new virtual string BaseBase_AutoPropertyForVHO { get; set; }
        // public override string BaseBase_AutoPropertyForVOO { get; set; }// FixMe: Issue #69788

        // -----------------static members--------------
        // hiding with a field:
        private static new int S_BaseBase_FieldForHidingWithField = 105;
        protected static new int S_BaseBase_PropertyForHidingWithField = 110;
        public static new int S_BaseBase_AutoPropertyForHidingWithField = 115;

        // hiding with a property:
        protected static new string S_BaseBase_FieldForHidingWithProperty => "Base#BaseBase_FieldForHidingWithProperty";
        public static new string S_BaseBase_PropertyForHidingWithProperty => "Base#BaseBase_PropertyForHidingWithProperty";
        private static new string S_BaseBase_AutoPropertyForHidingWithProperty => "Base#BaseBase_AutoPropertyForHidingWithProperty";

        // hiding with an auto-property:
        public static new string S_BaseBase_FieldForHidingWithAutoProperty { get; set; }
        private static new string S_BaseBase_PropertyForHidingWithAutoProperty { get; set; }
        protected static new string S_BaseBase_AutoPropertyForHidingWithAutoProperty { get; set; }

        public BaseClass2()
        {
            S_BaseBase_PropertyForHidingWithField = S_BaseBase_FieldForHidingWithField + 5; // suppressing non-used variable warning
            BaseBase_PropertyForHidingWithField = BaseBase_FieldForHidingWithField + 5; // suppressing non-used variable warning
            BaseBase_FieldForHidingWithAutoProperty = "Base#BaseBase_FieldForHidingWithAutoProperty";
            BaseBase_PropertyForHidingWithAutoProperty = "Base#BaseBase_PropertyForHidingWithAutoProperty";
            BaseBase_AutoPropertyForHidingWithAutoProperty = "Base#BaseBase_AutoPropertyForHidingWithAutoProperty";
            Base_AutoPropertyForOverridingWithProperty = new (2134, 5, 7, 1, 9, 2);
            Base_AutoPropertyForOverridingWithAutoProperty = new (2144, 5, 7, 1, 9, 2);

            BaseBase_AutoPropertyForVOH = "Base#BaseBase_AutoPropertyForVOH";
            BaseBase_AutoPropertyForVHO = "Base#BaseBase_AutoPropertyForVHO";
            // BaseBase_AutoPropertyForVOO = "Base#BaseBase_AutoPropertyForVOO"; // FixMe: Issue #69788
        }
    }

    public class DerivedClass2 : BaseClass2
    {
        // overriding with a property:
        public override DateTime Base_PropertyForOverridingWithProperty => new(2020, 7, 6, 5, 4, 3);
        protected override DateTime Base_AutoPropertyForOverridingWithProperty => new(2021, 7, 6, 5, 4, 3);

        // overriding with a auto-property:
        internal override DateTime Base_PropertyForOverridingWithAutoProperty { get; }
        protected override DateTime Base_AutoPropertyForOverridingWithAutoProperty { get; set; }

        // hiding sample members from BaseBase:
        public new int BaseBase_PropertyForHidingWithField = 210;
        protected new string BaseBase_AutoPropertyForHidingWithProperty => "Derived#BaseBase_AutoPropertyForHidingWithProperty";
        private new string BaseBase_FieldForHidingWithAutoProperty { get; set; }

        // for virtual->override->new-hidden (VOH), virtual->new-hidden->override (VHO) and virtual->override->override (VOO)
        public new string BaseBase_PropertyForVOH => "Derived#BaseBase_PropertyForVOH";
        public override string BaseBase_PropertyForVHO => "Derived#BaseBase_PropertyForVHO";
        // public override string BaseBase_PropertyForVOO => "Derived#BaseBase_PropertyForVOO"; // FixMe: Issue #69788
        public new string BaseBase_AutoPropertyForVOH { get; set; }
        public override string BaseBase_AutoPropertyForVHO { get; set; }
        // public override string BaseBase_AutoPropertyForVOO { get; set; } // FixMe: Issue #69788

        // -----------------static members--------------
        // hiding sample members from BaseBase:
        public static new int S_BaseBase_PropertyForHidingWithField = 210;
        protected static new string S_BaseBase_AutoPropertyForHidingWithProperty => "Derived#BaseBase_AutoPropertyForHidingWithProperty";
        private static new string S_BaseBase_FieldForHidingWithAutoProperty { get; set; }

        public DerivedClass2()
        {
            Base_PropertyForOverridingWithAutoProperty = new (2022, 7, 6, 5, 4, 3);
            Base_AutoPropertyForOverridingWithAutoProperty = new (2023, 7, 6, 5, 4, 3);
            BaseBase_FieldForHidingWithAutoProperty = "Derived#BaseBase_FieldForHidingWithAutoProperty";

            BaseBase_AutoPropertyForVOH = "Derived#BaseBase_AutoPropertyForVOH";
            BaseBase_AutoPropertyForVHO = "Derived#BaseBase_AutoPropertyForVHO";
            // BaseBase_AutoPropertyForVOO = "Derived#BaseBase_AutoPropertyForVOO"; // FixMe: Issue #69788
        }

        public static void run()
        {
            new DerivedClass2().InstanceMethod();
            new DerivedClass2().InstanceMethodAsync().Wait();
        }

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

        internal bool b = true;

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
