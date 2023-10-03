// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test contains a large number of various types that are loaded recursively through inheritance
// either by extending a class or implementing an interface.
// The test was generated using a grammar and Geno-based grammar parser (testTeams\LTS\MWILK\loadertools\GenerateTypes\Generator.exe)
// see REAMDE2.txt for more info.

using System;
using Xunit;



public class Test770
{
    interface C770_I0<T>
    { }
    interface C770_I1<T>
    { }
    interface C770_I2<T>
    { }
    interface C770_I3<T>
    { }
    interface C770_I4<T>
    { }
    interface C770_I5<T>
    { }
    interface C770_I6<T>
    { }
    interface C770_I7<T>
    { }

    class C770<T> : C770_I0<C770<T>>, C770_I1<C770_I2<C770_I3<C770_I4<C770_I5<C770_I6<C770_I7<C770<T>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C770<object> c_object = new C770<object>();
        C770<int> c_int = new C770<int>();
        C770<float> c_float = new C770<float>();
    }
}

public class Test771
{
    interface C771_I0<T>
    { }
    interface C771_I1<T>
    { }
    interface C771_I2<T>
    { }
    interface C771_I3<T>
    { }
    interface C771_I4<T>
    { }
    interface C771_I5<T>
    { }
    interface C771_I6<T>
    { }
    interface C771_I7<T>
    { }
    interface C771_I8<T>
    { }

    class C771<T> : C771_I0<C771_I1<C771<T>>>, C771_I2<C771_I3<C771_I4<C771_I5<C771_I6<C771_I7<C771_I8<C771<T>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C771<object> c_object = new C771<object>();
        C771<int> c_int = new C771<int>();
        C771<float> c_float = new C771<float>();
    }
}

public class Test772
{
    interface C772_I0<T>
    { }
    interface C772_I1<T>
    { }
    interface C772_I2<T>
    { }
    interface C772_I3<T>
    { }
    interface C772_I4<T>
    { }
    interface C772_I5<T>
    { }
    interface C772_I6<T>
    { }
    interface C772_I7<T>
    { }
    interface C772_I8<T>
    { }
    interface C772_I9<T>
    { }

    class C772<T> : C772_I0<C772_I1<C772_I2<C772<T>>>>, C772_I3<C772_I4<C772_I5<C772_I6<C772_I7<C772_I8<C772_I9<C772<T>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C772<object> c_object = new C772<object>();
        C772<int> c_int = new C772<int>();
        C772<float> c_float = new C772<float>();
    }
}

public class Test773
{
    interface C773_I0<T>
    { }
    interface C773_I1<T>
    { }
    interface C773_I2<T>
    { }
    interface C773_I3<T>
    { }
    interface C773_I4<T>
    { }
    interface C773_I5<T>
    { }
    interface C773_I6<T>
    { }
    interface C773_I7<T>
    { }
    interface C773_I8<T>
    { }
    interface C773_I9<T>
    { }
    interface C773_I10<T>
    { }

    class C773<T> : C773_I0<C773_I1<C773_I2<C773_I3<C773<T>>>>>, C773_I4<C773_I5<C773_I6<C773_I7<C773_I8<C773_I9<C773_I10<C773<T>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C773<object> c_object = new C773<object>();
        C773<int> c_int = new C773<int>();
        C773<float> c_float = new C773<float>();
    }
}

public class Test774
{
    interface C774_I0<T>
    { }
    interface C774_I1<T>
    { }
    interface C774_I2<T>
    { }
    interface C774_I3<T>
    { }
    interface C774_I4<T>
    { }
    interface C774_I5<T>
    { }
    interface C774_I6<T>
    { }
    interface C774_I7<T>
    { }
    interface C774_I8<T>
    { }
    interface C774_I9<T>
    { }
    interface C774_I10<T>
    { }
    interface C774_I11<T>
    { }

    class C774<T> : C774_I0<C774_I1<C774_I2<C774_I3<C774_I4<C774<T>>>>>>, C774_I5<C774_I6<C774_I7<C774_I8<C774_I9<C774_I10<C774_I11<C774<T>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C774<object> c_object = new C774<object>();
        C774<int> c_int = new C774<int>();
        C774<float> c_float = new C774<float>();
    }
}


public class Test775
{
    interface C775_I0<T>
    { }
    interface C775_I1<T>
    { }
    interface C775_I2<T>
    { }
    interface C775_I3<T>
    { }
    interface C775_I4<T>
    { }
    interface C775_I5<T>
    { }
    interface C775_I6<T>
    { }
    interface C775_I7<T>
    { }
    interface C775_I8<T>
    { }
    interface C775_I9<T>
    { }
    interface C775_I10<T>
    { }
    interface C775_I11<T>
    { }
    interface C775_I12<T>
    { }

    class C775<T> : C775_I0<C775_I1<C775_I2<C775_I3<C775_I4<C775_I5<C775<T>>>>>>>, C775_I6<C775_I7<C775_I8<C775_I9<C775_I10<C775_I11<C775_I12<C775<T>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C775<object> c_object = new C775<object>();
        C775<int> c_int = new C775<int>();
        C775<float> c_float = new C775<float>();
    }
}

public class Test777
{
    interface C777_I0<T>
    { }
    interface C777_I1<T>
    { }
    interface C777_I2<T>
    { }
    interface C777_I3<T>
    { }
    interface C777_I4<T>
    { }
    interface C777_I5<T>
    { }
    interface C777_I6<T>
    { }
    interface C777_I7<T>
    { }

    class C777<T> : C777_I0<C777_I1<C777_I2<C777_I3<C777_I4<C777_I5<C777_I6<C777<T>>>>>>>>, C777_I7<C777<T>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C777<object> c_object = new C777<object>();
        C777<int> c_int = new C777<int>();
        C777<float> c_float = new C777<float>();
    }
}

public class Test778
{
    interface C778_I0<T>
    { }
    interface C778_I1<T>
    { }
    interface C778_I2<T>
    { }
    interface C778_I3<T>
    { }
    interface C778_I4<T>
    { }
    interface C778_I5<T>
    { }
    interface C778_I6<T>
    { }
    interface C778_I7<T>
    { }
    interface C778_I8<T>
    { }

    class C778<T> : C778_I0<C778_I1<C778_I2<C778_I3<C778_I4<C778_I5<C778_I6<C778<T>>>>>>>>, C778_I7<C778_I8<C778<T>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C778<object> c_object = new C778<object>();
        C778<int> c_int = new C778<int>();
        C778<float> c_float = new C778<float>();
    }
}

public class Test779
{
    interface C779_I0<T>
    { }
    interface C779_I1<T>
    { }
    interface C779_I2<T>
    { }
    interface C779_I3<T>
    { }
    interface C779_I4<T>
    { }
    interface C779_I5<T>
    { }
    interface C779_I6<T>
    { }
    interface C779_I7<T>
    { }
    interface C779_I8<T>
    { }
    interface C779_I9<T>
    { }

    class C779<T> : C779_I0<C779_I1<C779_I2<C779_I3<C779_I4<C779_I5<C779_I6<C779<T>>>>>>>>, C779_I7<C779_I8<C779_I9<C779<T>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C779<object> c_object = new C779<object>();
        C779<int> c_int = new C779<int>();
        C779<float> c_float = new C779<float>();
    }
}



public class Test780
{
    interface C780_I0<T>
    { }
    interface C780_I1<T>
    { }
    interface C780_I2<T>
    { }
    interface C780_I3<T>
    { }
    interface C780_I4<T>
    { }
    interface C780_I5<T>
    { }
    interface C780_I6<T>
    { }
    interface C780_I7<T>
    { }
    interface C780_I8<T>
    { }
    interface C780_I9<T>
    { }
    interface C780_I10<T>
    { }

    class C780<T> : C780_I0<C780_I1<C780_I2<C780_I3<C780_I4<C780_I5<C780_I6<C780<T>>>>>>>>, C780_I7<C780_I8<C780_I9<C780_I10<C780<T>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C780<object> c_object = new C780<object>();
        C780<int> c_int = new C780<int>();
        C780<float> c_float = new C780<float>();
    }
}

public class Test781
{
    interface C781_I0<T>
    { }
    interface C781_I1<T>
    { }
    interface C781_I2<T>
    { }
    interface C781_I3<T>
    { }
    interface C781_I4<T>
    { }
    interface C781_I5<T>
    { }
    interface C781_I6<T>
    { }
    interface C781_I7<T>
    { }
    interface C781_I8<T>
    { }
    interface C781_I9<T>
    { }
    interface C781_I10<T>
    { }
    interface C781_I11<T>
    { }

    class C781<T> : C781_I0<C781_I1<C781_I2<C781_I3<C781_I4<C781_I5<C781_I6<C781<T>>>>>>>>, C781_I7<C781_I8<C781_I9<C781_I10<C781_I11<C781<T>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C781<object> c_object = new C781<object>();
        C781<int> c_int = new C781<int>();
        C781<float> c_float = new C781<float>();
    }
}

public class Test782
{
    interface C782_I0<T>
    { }
    interface C782_I1<T>
    { }
    interface C782_I2<T>
    { }
    interface C782_I3<T>
    { }
    interface C782_I4<T>
    { }
    interface C782_I5<T>
    { }
    interface C782_I6<T>
    { }
    interface C782_I7<T>
    { }
    interface C782_I8<T>
    { }
    interface C782_I9<T>
    { }
    interface C782_I10<T>
    { }
    interface C782_I11<T>
    { }
    interface C782_I12<T>
    { }

    class C782<T> : C782_I0<C782_I1<C782_I2<C782_I3<C782_I4<C782_I5<C782_I6<C782<T>>>>>>>>, C782_I7<C782_I8<C782_I9<C782_I10<C782_I11<C782_I12<C782<T>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C782<object> c_object = new C782<object>();
        C782<int> c_int = new C782<int>();
        C782<float> c_float = new C782<float>();
    }
}

public class Test783
{
    interface C783_I0<T>
    { }
    interface C783_I1<T>
    { }
    interface C783_I2<T>
    { }
    interface C783_I3<T>
    { }
    interface C783_I4<T>
    { }
    interface C783_I5<T>
    { }
    interface C783_I6<T>
    { }
    interface C783_I7<T>
    { }
    interface C783_I8<T>
    { }
    interface C783_I9<T>
    { }
    interface C783_I10<T>
    { }
    interface C783_I11<T>
    { }
    interface C783_I12<T>
    { }
    interface C783_I13<T>
    { }

    class C783<T> : C783_I0<C783_I1<C783_I2<C783_I3<C783_I4<C783_I5<C783_I6<C783<T>>>>>>>>, C783_I7<C783_I8<C783_I9<C783_I10<C783_I11<C783_I12<C783_I13<C783<T>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C783<object> c_object = new C783<object>();
        C783<int> c_int = new C783<int>();
        C783<float> c_float = new C783<float>();
    }
}

public class Test784
{
    interface C784_I0<T>
    { }
    interface C784_I1<T>
    { }
    interface C784_I2<T>
    { }
    interface C784_I3<T>
    { }
    interface C784_I4<T>
    { }
    interface C784_I5<T>
    { }
    interface C784_I6<T>
    { }
    interface C784_I7<T>
    { }
    interface C784_I8<T>
    { }
    interface C784_I9<T>
    { }
    interface C784_I10<T>
    { }
    interface C784_I11<T>
    { }
    interface C784_I12<T>
    { }
    interface C784_I13<T>
    { }
    interface C784_I14<T>
    { }
    interface C784_I15<T>
    { }
    interface C784_I16<T>
    { }

    class C784<T> : C784_I0<C784_I1<C784_I2<C784_I3<C784_I4<C784_I5<C784_I6<C784<T>>>>>>>>, C784_I7<C784_I8<C784_I9<C784_I10<C784_I11<C784_I12<C784_I13<C784<T>>>>>>>>, C784_I14<C784_I15<C784_I16<C784<T>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C784<object> c_object = new C784<object>();
        C784<int> c_int = new C784<int>();
        C784<float> c_float = new C784<float>();
    }
}



public class Test785
{
    interface C785_I0<T>
    { }
    interface C785_I1<T>
    { }
    interface C785_I2<T>
    { }
    interface C785_I3<T>
    { }
    interface C785_I4<T>
    { }
    interface C785_I5<T>
    { }
    interface C785_I6<T>
    { }
    interface C785_I7<T>
    { }
    interface C785_I8<T>
    { }
    interface C785_I9<T>
    { }
    interface C785_I10<T>
    { }
    interface C785_I11<T>
    { }
    interface C785_I12<T>
    { }
    interface C785_I13<T>
    { }
    interface C785_I14<T>
    { }

    class C785<T> : C785_I0<C785_I1<C785_I2<C785_I3<C785_I4<C785_I5<C785_I6<C785<T>>>>>>>>, C785_I7<C785_I8<C785_I9<C785_I10<C785_I11<C785_I12<C785<T>>>>>>>, C785_I13<C785_I14<C785<T>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C785<object> c_object = new C785<object>();
        C785<int> c_int = new C785<int>();
        C785<float> c_float = new C785<float>();
    }
}

public class Test786
{
    interface C786_I0<T>
    { }
    interface C786_I1<T>
    { }
    interface C786_I2<T>
    { }
    interface C786_I3<T>
    { }
    interface C786_I4<T>
    { }
    interface C786_I5<T>
    { }
    interface C786_I6<T>
    { }
    interface C786_I7<T>
    { }
    interface C786_I8<T>
    { }
    interface C786_I9<T>
    { }
    interface C786_I10<T>
    { }
    interface C786_I11<T>
    { }
    interface C786_I12<T>
    { }
    interface C786_I13<T>
    { }
    interface C786_I14<T>
    { }
    interface C786_I15<T>
    { }
    interface C786_I16<T>
    { }

    class C786<T> : C786_I0<C786_I1<C786_I2<C786_I3<C786_I4<C786_I5<C786_I6<C786<T>>>>>>>>, C786_I7<C786_I8<C786_I9<C786_I10<C786_I11<C786<T>>>>>>, C786_I12<C786_I13<C786_I14<C786_I15<C786_I16<C786<T>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C786<object> c_object = new C786<object>();
        C786<int> c_int = new C786<int>();
        C786<float> c_float = new C786<float>();
    }
}

public class Test787
{
    interface C787_I0<T>
    { }
    interface C787_I1<T>
    { }
    interface C787_I2<T>
    { }
    interface C787_I3<T>
    { }
    interface C787_I4<T>
    { }
    interface C787_I5<T>
    { }
    interface C787_I6<T>
    { }
    interface C787_I7<T>
    { }
    interface C787_I8<T>
    { }
    interface C787_I9<T>
    { }
    interface C787_I10<T>
    { }
    interface C787_I11<T>
    { }
    interface C787_I12<T>
    { }
    interface C787_I13<T>
    { }
    interface C787_I14<T>
    { }

    class C787<T> : C787_I0<C787_I1<C787_I2<C787_I3<C787_I4<C787_I5<C787_I6<C787<T>>>>>>>>, C787_I7<C787_I8<C787_I9<C787_I10<C787<T>>>>>, C787_I11<C787_I12<C787_I13<C787_I14<C787<T>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C787<object> c_object = new C787<object>();
        C787<int> c_int = new C787<int>();
        C787<float> c_float = new C787<float>();
    }
}

public class Test790
{
    interface C790_I0<T>
    { }
    interface C790_I1<T>
    { }
    interface C790_I2<T>
    { }
    interface C790_I3<T>
    { }
    interface C790_I4<T>
    { }
    interface C790_I5<T>
    { }
    interface C790_I6<T>
    { }
    interface C790_I7<T>
    { }
    interface C790_I8<T>
    { }

    class C790<T> : C790_I0<C790_I1<C790_I2<C790_I3<C790_I4<C790_I5<C790_I6<C790<T>>>>>>>>, C790_I7<C790<T>>, C790_I8<C790<T>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C790<object> c_object = new C790<object>();
        C790<int> c_int = new C790<int>();
        C790<float> c_float = new C790<float>();
    }
}

public class Test792
{
    interface C792_I0<T>
    { }
    interface C792_I1<T>
    { }
    interface C792_I2<T>
    { }
    interface C792_I3<T>
    { }
    interface C792_I4<T>
    { }
    interface C792_I5<T>
    { }
    interface C792_I6<T>
    { }
    interface C792_I7<T>
    { }
    interface C792_I8<T>
    { }
    interface C792_I9<T>
    { }
    interface C792_I10<T>
    { }
    interface C792_I11<T>
    { }
    interface C792_I12<T>
    { }
    interface C792_I13<T>
    { }
    interface C792_I14<T>
    { }

    class C792<T> : C792_I0<C792_I1<C792_I2<C792_I3<C792_I4<C792_I5<C792<T>>>>>>>, C792_I6<C792_I7<C792_I8<C792_I9<C792_I10<C792_I11<C792_I12<C792<T>>>>>>>>, C792_I13<C792_I14<C792<T>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C792<object> c_object = new C792<object>();
        C792<int> c_int = new C792<int>();
        C792<float> c_float = new C792<float>();
    }
}

public class Test793
{
    interface C793_I0<T>
    { }
    interface C793_I1<T>
    { }
    interface C793_I2<T>
    { }
    interface C793_I3<T>
    { }
    interface C793_I4<T>
    { }
    interface C793_I5<T>
    { }
    interface C793_I6<T>
    { }
    interface C793_I7<T>
    { }
    interface C793_I8<T>
    { }
    interface C793_I9<T>
    { }
    interface C793_I10<T>
    { }
    interface C793_I11<T>
    { }
    interface C793_I12<T>
    { }
    interface C793_I13<T>
    { }
    interface C793_I14<T>
    { }

    class C793<T> : C793_I0<C793_I1<C793_I2<C793_I3<C793_I4<C793<T>>>>>>, C793_I5<C793_I6<C793_I7<C793_I8<C793_I9<C793_I10<C793_I11<C793<T>>>>>>>>, C793_I12<C793_I13<C793_I14<C793<T>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C793<object> c_object = new C793<object>();
        C793<int> c_int = new C793<int>();
        C793<float> c_float = new C793<float>();
    }
}

public class Test794
{
    interface C794_I0<T>
    { }
    interface C794_I1<T>
    { }
    interface C794_I2<T>
    { }
    interface C794_I3<T>
    { }
    interface C794_I4<T>
    { }
    interface C794_I5<T>
    { }
    interface C794_I6<T>
    { }
    interface C794_I7<T>
    { }
    interface C794_I8<T>
    { }
    interface C794_I9<T>
    { }
    interface C794_I10<T>
    { }
    interface C794_I11<T>
    { }

    class C794<T> : C794_I0<C794_I1<C794_I2<C794_I3<C794<T>>>>>, C794_I4<C794_I5<C794_I6<C794_I7<C794_I8<C794_I9<C794_I10<C794<T>>>>>>>>, C794_I11<C794<T>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C794<object> c_object = new C794<object>();
        C794<int> c_int = new C794<int>();
        C794<float> c_float = new C794<float>();
    }
}

public class Test795
{
    interface C795_I0<T>
    { }
    interface C795_I1<T>
    { }
    interface C795_I2<T>
    { }
    interface C795_I3<T>
    { }
    interface C795_I4<T>
    { }
    interface C795_I5<T>
    { }
    interface C795_I6<T>
    { }
    interface C795_I7<T>
    { }
    interface C795_I8<T>
    { }
    interface C795_I9<T>
    { }
    interface C795_I10<T>
    { }
    interface C795_I11<T>
    { }
    interface C795_I12<T>
    { }
    interface C795_I13<T>
    { }
    interface C795_I14<T>
    { }
    interface C795_I15<T>
    { }

    class C795<T> : C795_I0<C795_I1<C795_I2<C795<T>>>>, C795_I3<C795_I4<C795_I5<C795_I6<C795_I7<C795_I8<C795_I9<C795<T>>>>>>>>, C795_I10<C795_I11<C795_I12<C795_I13<C795_I14<C795_I15<C795<T>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C795<object> c_object = new C795<object>();
        C795<int> c_int = new C795<int>();
        C795<float> c_float = new C795<float>();
    }
}

public class Test796
{
    interface C796_I0<T>
    { }
    interface C796_I1<T>
    { }
    interface C796_I2<T>
    { }
    interface C796_I3<T>
    { }
    interface C796_I4<T>
    { }
    interface C796_I5<T>
    { }
    interface C796_I6<T>
    { }
    interface C796_I7<T>
    { }
    interface C796_I8<T>
    { }
    interface C796_I9<T>
    { }
    interface C796_I10<T>
    { }

    class C796<T> : C796_I0<C796_I1<C796<T>>>, C796_I2<C796_I3<C796_I4<C796_I5<C796_I6<C796_I7<C796_I8<C796<T>>>>>>>>, C796_I9<C796_I10<C796<T>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C796<object> c_object = new C796<object>();
        C796<int> c_int = new C796<int>();
        C796<float> c_float = new C796<float>();
    }
}

public class Test808
{
    interface C808_I0<T>
    { }
    interface C808_I1<T>
    { }
    interface C808_I2<T>
    { }
    interface C808_I3<T>
    { }
    interface C808_I4<T>
    { }
    interface C808_I5<T>
    { }
    interface C808_I6<T>
    { }
    interface C808_I7<T>
    { }
    interface C808_I8<T>
    { }

    class C808<T> : C808_I0<C808<T>>, C808_I1<C808_I2<C808_I3<C808_I4<C808_I5<C808_I6<C808_I7<C808<T>>>>>>>>, C808_I8<C808<T>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C808<object> c_object = new C808<object>();
        C808<int> c_int = new C808<int>();
        C808<float> c_float = new C808<float>();
    }
}

public class Test809
{
    interface C809_I0<T>
    { }
    interface C809_I1<T>
    { }
    interface C809_I2<T>
    { }
    interface C809_I3<T>
    { }
    interface C809_I4<T>
    { }
    interface C809_I5<T>
    { }
    interface C809_I6<T>
    { }
    interface C809_I7<T>
    { }
    interface C809_I8<T>
    { }
    interface C809_I9<T>
    { }

    class C809<T> : C809_I0<C809<T>>, C809_I1<C809_I2<C809_I3<C809_I4<C809_I5<C809_I6<C809_I7<C809<T>>>>>>>>, C809_I8<C809_I9<C809<T>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C809<object> c_object = new C809<object>();
        C809<int> c_int = new C809<int>();
        C809<float> c_float = new C809<float>();
    }
}

public class Test810
{
    interface C810_I0<T>
    { }
    interface C810_I1<T>
    { }
    interface C810_I2<T>
    { }
    interface C810_I3<T>
    { }
    interface C810_I4<T>
    { }
    interface C810_I5<T>
    { }
    interface C810_I6<T>
    { }
    interface C810_I7<T>
    { }
    interface C810_I8<T>
    { }
    interface C810_I9<T>
    { }
    interface C810_I10<T>
    { }

    class C810<T> : C810_I0<C810<T>>, C810_I1<C810_I2<C810_I3<C810_I4<C810_I5<C810_I6<C810_I7<C810<T>>>>>>>>, C810_I8<C810_I9<C810_I10<C810<T>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C810<object> c_object = new C810<object>();
        C810<int> c_int = new C810<int>();
        C810<float> c_float = new C810<float>();
    }
}

public class Test811
{
    interface C811_I0<T>
    { }
    interface C811_I1<T>
    { }
    interface C811_I2<T>
    { }
    interface C811_I3<T>
    { }
    interface C811_I4<T>
    { }
    interface C811_I5<T>
    { }
    interface C811_I6<T>
    { }
    interface C811_I7<T>
    { }
    interface C811_I8<T>
    { }
    interface C811_I9<T>
    { }
    interface C811_I10<T>
    { }
    interface C811_I11<T>
    { }

    class C811<T> : C811_I0<C811<T>>, C811_I1<C811_I2<C811_I3<C811_I4<C811_I5<C811_I6<C811_I7<C811<T>>>>>>>>, C811_I8<C811_I9<C811_I10<C811_I11<C811<T>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C811<object> c_object = new C811<object>();
        C811<int> c_int = new C811<int>();
        C811<float> c_float = new C811<float>();
    }
}

public class Test812
{
    interface C812_I0<T>
    { }
    interface C812_I1<T>
    { }
    interface C812_I2<T>
    { }
    interface C812_I3<T>
    { }
    interface C812_I4<T>
    { }
    interface C812_I5<T>
    { }
    interface C812_I6<T>
    { }
    interface C812_I7<T>
    { }
    interface C812_I8<T>
    { }
    interface C812_I9<T>
    { }
    interface C812_I10<T>
    { }
    interface C812_I11<T>
    { }
    interface C812_I12<T>
    { }

    class C812<T> : C812_I0<C812<T>>, C812_I1<C812_I2<C812_I3<C812_I4<C812_I5<C812_I6<C812_I7<C812<T>>>>>>>>, C812_I8<C812_I9<C812_I10<C812_I11<C812_I12<C812<T>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C812<object> c_object = new C812<object>();
        C812<int> c_int = new C812<int>();
        C812<float> c_float = new C812<float>();
    }
}

public class Test813
{
    interface C813_I0<T>
    { }
    interface C813_I1<T>
    { }
    interface C813_I2<T>
    { }
    interface C813_I3<T>
    { }
    interface C813_I4<T>
    { }
    interface C813_I5<T>
    { }
    interface C813_I6<T>
    { }
    interface C813_I7<T>
    { }
    interface C813_I8<T>
    { }
    interface C813_I9<T>
    { }
    interface C813_I10<T>
    { }
    interface C813_I11<T>
    { }
    interface C813_I12<T>
    { }
    interface C813_I13<T>
    { }

    class C813<T> : C813_I0<C813<T>>, C813_I1<C813_I2<C813_I3<C813_I4<C813_I5<C813_I6<C813_I7<C813<T>>>>>>>>, C813_I8<C813_I9<C813_I10<C813_I11<C813_I12<C813_I13<C813<T>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C813<object> c_object = new C813<object>();
        C813<int> c_int = new C813<int>();
        C813<float> c_float = new C813<float>();
    }
}

public class Test814
{
    interface C814_I0<T>
    { }
    interface C814_I1<T>
    { }
    interface C814_I2<T>
    { }
    interface C814_I3<T>
    { }
    interface C814_I4<T>
    { }
    interface C814_I5<T>
    { }
    interface C814_I6<T>
    { }
    interface C814_I7<T>
    { }
    interface C814_I8<T>
    { }

    class C814<T> : C814_I0<C814<T>>, C814_I1<C814<T>>, C814_I2<C814_I3<C814_I4<C814_I5<C814_I6<C814_I7<C814_I8<C814<T>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C814<object> c_object = new C814<object>();
        C814<int> c_int = new C814<int>();
        C814<float> c_float = new C814<float>();
    }
}

public class Test816
{
    interface C816_I0<T>
    { }
    interface C816_I1<T>
    { }
    interface C816_I2<T>
    { }
    interface C816_I3<T>
    { }
    interface C816_I4<T>
    { }
    interface C816_I5<T>
    { }
    interface C816_I6<T>
    { }
    interface C816_I7<T>
    { }
    interface C816_I8<T>
    { }
    interface C816_I9<T>
    { }

    class C816<T> : C816_I0<C816_I1<C816<T>>>, C816_I2<C816_I3<C816_I4<C816_I5<C816_I6<C816_I7<C816_I8<C816<T>>>>>>>>, C816_I9<C816<T>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C816<object> c_object = new C816<object>();
        C816<int> c_int = new C816<int>();
        C816<float> c_float = new C816<float>();
    }
}

public class Test817
{
    interface C817_I0<T>
    { }
    interface C817_I1<T>
    { }
    interface C817_I2<T>
    { }
    interface C817_I3<T>
    { }
    interface C817_I4<T>
    { }
    interface C817_I5<T>
    { }
    interface C817_I6<T>
    { }
    interface C817_I7<T>
    { }
    interface C817_I8<T>
    { }
    interface C817_I9<T>
    { }
    interface C817_I10<T>
    { }

    class C817<T> : C817_I0<C817_I1<C817<T>>>, C817_I2<C817_I3<C817_I4<C817_I5<C817_I6<C817_I7<C817_I8<C817<T>>>>>>>>, C817_I9<C817_I10<C817<T>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C817<object> c_object = new C817<object>();
        C817<int> c_int = new C817<int>();
        C817<float> c_float = new C817<float>();
    }
}

public class Test818
{
    interface C818_I0<T>
    { }
    interface C818_I1<T>
    { }
    interface C818_I2<T>
    { }
    interface C818_I3<T>
    { }
    interface C818_I4<T>
    { }
    interface C818_I5<T>
    { }
    interface C818_I6<T>
    { }
    interface C818_I7<T>
    { }
    interface C818_I8<T>
    { }
    interface C818_I9<T>
    { }
    interface C818_I10<T>
    { }
    interface C818_I11<T>
    { }

    class C818<T> : C818_I0<C818_I1<C818<T>>>, C818_I2<C818_I3<C818_I4<C818_I5<C818_I6<C818_I7<C818_I8<C818<T>>>>>>>>, C818_I9<C818_I10<C818_I11<C818<T>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C818<object> c_object = new C818<object>();
        C818<int> c_int = new C818<int>();
        C818<float> c_float = new C818<float>();
    }
}

public class Test819
{
    interface C819_I0<T>
    { }
    interface C819_I1<T>
    { }
    interface C819_I2<T>
    { }
    interface C819_I3<T>
    { }
    interface C819_I4<T>
    { }
    interface C819_I5<T>
    { }
    interface C819_I6<T>
    { }
    interface C819_I7<T>
    { }
    interface C819_I8<T>
    { }
    interface C819_I9<T>
    { }
    interface C819_I10<T>
    { }
    interface C819_I11<T>
    { }
    interface C819_I12<T>
    { }

    class C819<T> : C819_I0<C819_I1<C819<T>>>, C819_I2<C819_I3<C819_I4<C819_I5<C819_I6<C819_I7<C819_I8<C819<T>>>>>>>>, C819_I9<C819_I10<C819_I11<C819_I12<C819<T>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C819<object> c_object = new C819<object>();
        C819<int> c_int = new C819<int>();
        C819<float> c_float = new C819<float>();
    }
}

public class Test820
{
    interface C820_I0<T>
    { }
    interface C820_I1<T>
    { }
    interface C820_I2<T>
    { }
    interface C820_I3<T>
    { }
    interface C820_I4<T>
    { }
    interface C820_I5<T>
    { }
    interface C820_I6<T>
    { }
    interface C820_I7<T>
    { }
    interface C820_I8<T>
    { }
    interface C820_I9<T>
    { }
    interface C820_I10<T>
    { }
    interface C820_I11<T>
    { }
    interface C820_I12<T>
    { }
    interface C820_I13<T>
    { }

    class C820<T> : C820_I0<C820_I1<C820<T>>>, C820_I2<C820_I3<C820_I4<C820_I5<C820_I6<C820_I7<C820_I8<C820<T>>>>>>>>, C820_I9<C820_I10<C820_I11<C820_I12<C820_I13<C820<T>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C820<object> c_object = new C820<object>();
        C820<int> c_int = new C820<int>();
        C820<float> c_float = new C820<float>();
    }
}

public class Test821
{
    interface C821_I0<T>
    { }
    interface C821_I1<T>
    { }
    interface C821_I2<T>
    { }
    interface C821_I3<T>
    { }
    interface C821_I4<T>
    { }
    interface C821_I5<T>
    { }
    interface C821_I6<T>
    { }
    interface C821_I7<T>
    { }
    interface C821_I8<T>
    { }
    interface C821_I9<T>
    { }
    interface C821_I10<T>
    { }
    interface C821_I11<T>
    { }
    interface C821_I12<T>
    { }
    interface C821_I13<T>
    { }
    interface C821_I14<T>
    { }

    class C821<T> : C821_I0<C821_I1<C821<T>>>, C821_I2<C821_I3<C821_I4<C821_I5<C821_I6<C821_I7<C821_I8<C821<T>>>>>>>>, C821_I9<C821_I10<C821_I11<C821_I12<C821_I13<C821_I14<C821<T>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C821<object> c_object = new C821<object>();
        C821<int> c_int = new C821<int>();
        C821<float> c_float = new C821<float>();
    }
}

public class Test822
{
    interface C822_I0<T>
    { }
    interface C822_I1<T>
    { }
    interface C822_I2<T>
    { }
    interface C822_I3<T>
    { }
    interface C822_I4<T>
    { }
    interface C822_I5<T>
    { }
    interface C822_I6<T>
    { }
    interface C822_I7<T>
    { }
    interface C822_I8<T>
    { }
    interface C822_I9<T>
    { }
    interface C822_I10<T>
    { }

    class C822<T> : C822_I0<C822_I1<C822<T>>>, C822_I2<C822_I3<C822<T>>>, C822_I4<C822_I5<C822_I6<C822_I7<C822_I8<C822_I9<C822_I10<C822<T>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C822<object> c_object = new C822<object>();
        C822<int> c_int = new C822<int>();
        C822<float> c_float = new C822<float>();
    }
}

public class Test824
{
    interface C824_I0<T>
    { }
    interface C824_I1<T>
    { }
    interface C824_I2<T>
    { }
    interface C824_I3<T>
    { }
    interface C824_I4<T>
    { }
    interface C824_I5<T>
    { }
    interface C824_I6<T>
    { }
    interface C824_I7<T>
    { }
    interface C824_I8<T>
    { }
    interface C824_I9<T>
    { }
    interface C824_I10<T>
    { }

    class C824<T> : C824_I0<C824_I1<C824_I2<C824<T>>>>, C824_I3<C824_I4<C824_I5<C824_I6<C824_I7<C824_I8<C824_I9<C824<T>>>>>>>>, C824_I10<C824<T>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C824<object> c_object = new C824<object>();
        C824<int> c_int = new C824<int>();
        C824<float> c_float = new C824<float>();
    }
}

public class Test825
{
    interface C825_I0<T>
    { }
    interface C825_I1<T>
    { }
    interface C825_I2<T>
    { }
    interface C825_I3<T>
    { }
    interface C825_I4<T>
    { }
    interface C825_I5<T>
    { }
    interface C825_I6<T>
    { }
    interface C825_I7<T>
    { }
    interface C825_I8<T>
    { }
    interface C825_I9<T>
    { }
    interface C825_I10<T>
    { }
    interface C825_I11<T>
    { }

    class C825<T> : C825_I0<C825_I1<C825_I2<C825<T>>>>, C825_I3<C825_I4<C825_I5<C825_I6<C825_I7<C825_I8<C825_I9<C825<T>>>>>>>>, C825_I10<C825_I11<C825<T>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C825<object> c_object = new C825<object>();
        C825<int> c_int = new C825<int>();
        C825<float> c_float = new C825<float>();
    }
}

public class Test826
{
    interface C826_I0<T>
    { }
    interface C826_I1<T>
    { }
    interface C826_I2<T>
    { }
    interface C826_I3<T>
    { }
    interface C826_I4<T>
    { }
    interface C826_I5<T>
    { }
    interface C826_I6<T>
    { }
    interface C826_I7<T>
    { }
    interface C826_I8<T>
    { }
    interface C826_I9<T>
    { }
    interface C826_I10<T>
    { }
    interface C826_I11<T>
    { }
    interface C826_I12<T>
    { }

    class C826<T> : C826_I0<C826_I1<C826_I2<C826<T>>>>, C826_I3<C826_I4<C826_I5<C826_I6<C826_I7<C826_I8<C826_I9<C826<T>>>>>>>>, C826_I10<C826_I11<C826_I12<C826<T>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C826<object> c_object = new C826<object>();
        C826<int> c_int = new C826<int>();
        C826<float> c_float = new C826<float>();
    }
}

public class Test827
{
    interface C827_I0<T>
    { }
    interface C827_I1<T>
    { }
    interface C827_I2<T>
    { }
    interface C827_I3<T>
    { }
    interface C827_I4<T>
    { }
    interface C827_I5<T>
    { }
    interface C827_I6<T>
    { }
    interface C827_I7<T>
    { }
    interface C827_I8<T>
    { }
    interface C827_I9<T>
    { }
    interface C827_I10<T>
    { }
    interface C827_I11<T>
    { }
    interface C827_I12<T>
    { }
    interface C827_I13<T>
    { }

    class C827<T> : C827_I0<C827_I1<C827_I2<C827<T>>>>, C827_I3<C827_I4<C827_I5<C827_I6<C827_I7<C827_I8<C827_I9<C827<T>>>>>>>>, C827_I10<C827_I11<C827_I12<C827_I13<C827<T>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C827<object> c_object = new C827<object>();
        C827<int> c_int = new C827<int>();
        C827<float> c_float = new C827<float>();
    }
}

public class Test828
{
    interface C828_I0<T>
    { }
    interface C828_I1<T>
    { }
    interface C828_I2<T>
    { }
    interface C828_I3<T>
    { }
    interface C828_I4<T>
    { }
    interface C828_I5<T>
    { }
    interface C828_I6<T>
    { }
    interface C828_I7<T>
    { }
    interface C828_I8<T>
    { }
    interface C828_I9<T>
    { }
    interface C828_I10<T>
    { }
    interface C828_I11<T>
    { }
    interface C828_I12<T>
    { }
    interface C828_I13<T>
    { }
    interface C828_I14<T>
    { }

    class C828<T> : C828_I0<C828_I1<C828_I2<C828<T>>>>, C828_I3<C828_I4<C828_I5<C828_I6<C828_I7<C828_I8<C828_I9<C828<T>>>>>>>>, C828_I10<C828_I11<C828_I12<C828_I13<C828_I14<C828<T>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C828<object> c_object = new C828<object>();
        C828<int> c_int = new C828<int>();
        C828<float> c_float = new C828<float>();
    }
}

public class Test829
{
    interface C829_I0<T>
    { }
    interface C829_I1<T>
    { }
    interface C829_I2<T>
    { }
    interface C829_I3<T>
    { }
    interface C829_I4<T>
    { }
    interface C829_I5<T>
    { }
    interface C829_I6<T>
    { }
    interface C829_I7<T>
    { }
    interface C829_I8<T>
    { }
    interface C829_I9<T>
    { }
    interface C829_I10<T>
    { }
    interface C829_I11<T>
    { }
    interface C829_I12<T>
    { }
    interface C829_I13<T>
    { }
    interface C829_I14<T>
    { }
    interface C829_I15<T>
    { }

    class C829<T> : C829_I0<C829_I1<C829_I2<C829<T>>>>, C829_I3<C829_I4<C829_I5<C829_I6<C829_I7<C829_I8<C829_I9<C829<T>>>>>>>>, C829_I10<C829_I11<C829_I12<C829_I13<C829_I14<C829_I15<C829<T>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C829<object> c_object = new C829<object>();
        C829<int> c_int = new C829<int>();
        C829<float> c_float = new C829<float>();
    }
}

public class Test830
{
    interface C830_I0<T>
    { }
    interface C830_I1<T>
    { }
    interface C830_I2<T>
    { }
    interface C830_I3<T>
    { }
    interface C830_I4<T>
    { }
    interface C830_I5<T>
    { }
    interface C830_I6<T>
    { }
    interface C830_I7<T>
    { }
    interface C830_I8<T>
    { }
    interface C830_I9<T>
    { }
    interface C830_I10<T>
    { }
    interface C830_I11<T>
    { }
    interface C830_I12<T>
    { }

    class C830<T> : C830_I0<C830_I1<C830_I2<C830<T>>>>, C830_I3<C830_I4<C830_I5<C830<T>>>>, C830_I6<C830_I7<C830_I8<C830_I9<C830_I10<C830_I11<C830_I12<C830<T>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C830<object> c_object = new C830<object>();
        C830<int> c_int = new C830<int>();
        C830<float> c_float = new C830<float>();
    }
}

public class Test832
{
    interface C832_I0<T>
    { }
    interface C832_I1<T>
    { }
    interface C832_I2<T>
    { }
    interface C832_I3<T>
    { }
    interface C832_I4<T>
    { }
    interface C832_I5<T>
    { }
    interface C832_I6<T>
    { }
    interface C832_I7<T>
    { }
    interface C832_I8<T>
    { }
    interface C832_I9<T>
    { }
    interface C832_I10<T>
    { }
    interface C832_I11<T>
    { }

    class C832<T> : C832_I0<C832_I1<C832_I2<C832_I3<C832<T>>>>>, C832_I4<C832_I5<C832_I6<C832_I7<C832_I8<C832_I9<C832_I10<C832<T>>>>>>>>, C832_I11<C832<T>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C832<object> c_object = new C832<object>();
        C832<int> c_int = new C832<int>();
        C832<float> c_float = new C832<float>();
    }
}

public class Test833
{
    interface C833_I0<T>
    { }
    interface C833_I1<T>
    { }
    interface C833_I2<T>
    { }
    interface C833_I3<T>
    { }
    interface C833_I4<T>
    { }
    interface C833_I5<T>
    { }
    interface C833_I6<T>
    { }
    interface C833_I7<T>
    { }
    interface C833_I8<T>
    { }
    interface C833_I9<T>
    { }
    interface C833_I10<T>
    { }
    interface C833_I11<T>
    { }
    interface C833_I12<T>
    { }

    class C833<T> : C833_I0<C833_I1<C833_I2<C833_I3<C833<T>>>>>, C833_I4<C833_I5<C833_I6<C833_I7<C833_I8<C833_I9<C833_I10<C833<T>>>>>>>>, C833_I11<C833_I12<C833<T>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C833<object> c_object = new C833<object>();
        C833<int> c_int = new C833<int>();
        C833<float> c_float = new C833<float>();
    }
}

public class Test834
{
    interface C834_I0<T>
    { }
    interface C834_I1<T>
    { }
    interface C834_I2<T>
    { }
    interface C834_I3<T>
    { }
    interface C834_I4<T>
    { }
    interface C834_I5<T>
    { }
    interface C834_I6<T>
    { }
    interface C834_I7<T>
    { }
    interface C834_I8<T>
    { }
    interface C834_I9<T>
    { }
    interface C834_I10<T>
    { }
    interface C834_I11<T>
    { }
    interface C834_I12<T>
    { }
    interface C834_I13<T>
    { }

    class C834<T> : C834_I0<C834_I1<C834_I2<C834_I3<C834<T>>>>>, C834_I4<C834_I5<C834_I6<C834_I7<C834_I8<C834_I9<C834_I10<C834<T>>>>>>>>, C834_I11<C834_I12<C834_I13<C834<T>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C834<object> c_object = new C834<object>();
        C834<int> c_int = new C834<int>();
        C834<float> c_float = new C834<float>();
    }
}

public class Test835
{
    interface C835_I0<T>
    { }
    interface C835_I1<T>
    { }
    interface C835_I2<T>
    { }
    interface C835_I3<T>
    { }
    interface C835_I4<T>
    { }
    interface C835_I5<T>
    { }
    interface C835_I6<T>
    { }
    interface C835_I7<T>
    { }
    interface C835_I8<T>
    { }
    interface C835_I9<T>
    { }
    interface C835_I10<T>
    { }
    interface C835_I11<T>
    { }
    interface C835_I12<T>
    { }
    interface C835_I13<T>
    { }
    interface C835_I14<T>
    { }

    class C835<T> : C835_I0<C835_I1<C835_I2<C835_I3<C835<T>>>>>, C835_I4<C835_I5<C835_I6<C835_I7<C835_I8<C835_I9<C835_I10<C835<T>>>>>>>>, C835_I11<C835_I12<C835_I13<C835_I14<C835<T>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C835<object> c_object = new C835<object>();
        C835<int> c_int = new C835<int>();
        C835<float> c_float = new C835<float>();
    }
}

public class Test836
{
    interface C836_I0<T>
    { }
    interface C836_I1<T>
    { }
    interface C836_I2<T>
    { }
    interface C836_I3<T>
    { }
    interface C836_I4<T>
    { }
    interface C836_I5<T>
    { }
    interface C836_I6<T>
    { }
    interface C836_I7<T>
    { }
    interface C836_I8<T>
    { }
    interface C836_I9<T>
    { }
    interface C836_I10<T>
    { }
    interface C836_I11<T>
    { }
    interface C836_I12<T>
    { }
    interface C836_I13<T>
    { }
    interface C836_I14<T>
    { }
    interface C836_I15<T>
    { }

    class C836<T> : C836_I0<C836_I1<C836_I2<C836_I3<C836<T>>>>>, C836_I4<C836_I5<C836_I6<C836_I7<C836_I8<C836_I9<C836_I10<C836<T>>>>>>>>, C836_I11<C836_I12<C836_I13<C836_I14<C836_I15<C836<T>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C836<object> c_object = new C836<object>();
        C836<int> c_int = new C836<int>();
        C836<float> c_float = new C836<float>();
    }
}

public class Test837
{
    interface C837_I0<T>
    { }
    interface C837_I1<T>
    { }
    interface C837_I2<T>
    { }
    interface C837_I3<T>
    { }
    interface C837_I4<T>
    { }
    interface C837_I5<T>
    { }
    interface C837_I6<T>
    { }
    interface C837_I7<T>
    { }
    interface C837_I8<T>
    { }
    interface C837_I9<T>
    { }
    interface C837_I10<T>
    { }
    interface C837_I11<T>
    { }
    interface C837_I12<T>
    { }
    interface C837_I13<T>
    { }
    interface C837_I14<T>
    { }
    interface C837_I15<T>
    { }
    interface C837_I16<T>
    { }

    class C837<T> : C837_I0<C837_I1<C837_I2<C837_I3<C837<T>>>>>, C837_I4<C837_I5<C837_I6<C837_I7<C837_I8<C837_I9<C837_I10<C837<T>>>>>>>>, C837_I11<C837_I12<C837_I13<C837_I14<C837_I15<C837_I16<C837<T>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C837<object> c_object = new C837<object>();
        C837<int> c_int = new C837<int>();
        C837<float> c_float = new C837<float>();
    }
}

public class Test838
{
    interface C838_I0<T>
    { }
    interface C838_I1<T>
    { }
    interface C838_I2<T>
    { }
    interface C838_I3<T>
    { }
    interface C838_I4<T>
    { }
    interface C838_I5<T>
    { }
    interface C838_I6<T>
    { }
    interface C838_I7<T>
    { }
    interface C838_I8<T>
    { }
    interface C838_I9<T>
    { }
    interface C838_I10<T>
    { }
    interface C838_I11<T>
    { }
    interface C838_I12<T>
    { }
    interface C838_I13<T>
    { }
    interface C838_I14<T>
    { }

    class C838<T> : C838_I0<C838_I1<C838_I2<C838_I3<C838<T>>>>>, C838_I4<C838_I5<C838_I6<C838_I7<C838<T>>>>>, C838_I8<C838_I9<C838_I10<C838_I11<C838_I12<C838_I13<C838_I14<C838<T>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C838<object> c_object = new C838<object>();
        C838<int> c_int = new C838<int>();
        C838<float> c_float = new C838<float>();
    }
}

public class Test840
{
    interface C840_I0<T>
    { }
    interface C840_I1<T>
    { }
    interface C840_I2<T>
    { }
    interface C840_I3<T>
    { }
    interface C840_I4<T>
    { }
    interface C840_I5<T>
    { }
    interface C840_I6<T>
    { }
    interface C840_I7<T>
    { }
    interface C840_I8<T>
    { }
    interface C840_I9<T>
    { }
    interface C840_I10<T>
    { }
    interface C840_I11<T>
    { }
    interface C840_I12<T>
    { }

    class C840<T> : C840_I0<C840_I1<C840_I2<C840_I3<C840_I4<C840<T>>>>>>, C840_I5<C840_I6<C840_I7<C840_I8<C840_I9<C840_I10<C840_I11<C840<T>>>>>>>>, C840_I12<C840<T>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C840<object> c_object = new C840<object>();
        C840<int> c_int = new C840<int>();
        C840<float> c_float = new C840<float>();
    }
}

public class Test841
{
    interface C841_I0<T>
    { }
    interface C841_I1<T>
    { }
    interface C841_I2<T>
    { }
    interface C841_I3<T>
    { }
    interface C841_I4<T>
    { }
    interface C841_I5<T>
    { }
    interface C841_I6<T>
    { }
    interface C841_I7<T>
    { }
    interface C841_I8<T>
    { }
    interface C841_I9<T>
    { }
    interface C841_I10<T>
    { }
    interface C841_I11<T>
    { }
    interface C841_I12<T>
    { }
    interface C841_I13<T>
    { }

    class C841<T> : C841_I0<C841_I1<C841_I2<C841_I3<C841_I4<C841<T>>>>>>, C841_I5<C841_I6<C841_I7<C841_I8<C841_I9<C841_I10<C841_I11<C841<T>>>>>>>>, C841_I12<C841_I13<C841<T>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C841<object> c_object = new C841<object>();
        C841<int> c_int = new C841<int>();
        C841<float> c_float = new C841<float>();
    }
}

public class Test842
{
    interface C842_I0<T>
    { }
    interface C842_I1<T>
    { }
    interface C842_I2<T>
    { }
    interface C842_I3<T>
    { }
    interface C842_I4<T>
    { }
    interface C842_I5<T>
    { }
    interface C842_I6<T>
    { }
    interface C842_I7<T>
    { }
    interface C842_I8<T>
    { }
    interface C842_I9<T>
    { }
    interface C842_I10<T>
    { }
    interface C842_I11<T>
    { }
    interface C842_I12<T>
    { }
    interface C842_I13<T>
    { }
    interface C842_I14<T>
    { }

    class C842<T> : C842_I0<C842_I1<C842_I2<C842_I3<C842_I4<C842<T>>>>>>, C842_I5<C842_I6<C842_I7<C842_I8<C842_I9<C842_I10<C842_I11<C842<T>>>>>>>>, C842_I12<C842_I13<C842_I14<C842<T>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C842<object> c_object = new C842<object>();
        C842<int> c_int = new C842<int>();
        C842<float> c_float = new C842<float>();
    }
}

public class Test843
{
    interface C843_I0<T>
    { }
    interface C843_I1<T>
    { }
    interface C843_I2<T>
    { }
    interface C843_I3<T>
    { }
    interface C843_I4<T>
    { }
    interface C843_I5<T>
    { }
    interface C843_I6<T>
    { }
    interface C843_I7<T>
    { }
    interface C843_I8<T>
    { }
    interface C843_I9<T>
    { }
    interface C843_I10<T>
    { }
    interface C843_I11<T>
    { }
    interface C843_I12<T>
    { }
    interface C843_I13<T>
    { }
    interface C843_I14<T>
    { }
    interface C843_I15<T>
    { }

    class C843<T> : C843_I0<C843_I1<C843_I2<C843_I3<C843_I4<C843<T>>>>>>, C843_I5<C843_I6<C843_I7<C843_I8<C843_I9<C843_I10<C843_I11<C843<T>>>>>>>>, C843_I12<C843_I13<C843_I14<C843_I15<C843<T>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C843<object> c_object = new C843<object>();
        C843<int> c_int = new C843<int>();
        C843<float> c_float = new C843<float>();
    }
}

public class Test844
{
    interface C844_I0<T>
    { }
    interface C844_I1<T>
    { }
    interface C844_I2<T>
    { }
    interface C844_I3<T>
    { }
    interface C844_I4<T>
    { }
    interface C844_I5<T>
    { }
    interface C844_I6<T>
    { }
    interface C844_I7<T>
    { }
    interface C844_I8<T>
    { }
    interface C844_I9<T>
    { }
    interface C844_I10<T>
    { }
    interface C844_I11<T>
    { }
    interface C844_I12<T>
    { }
    interface C844_I13<T>
    { }
    interface C844_I14<T>
    { }
    interface C844_I15<T>
    { }
    interface C844_I16<T>
    { }

    class C844<T> : C844_I0<C844_I1<C844_I2<C844_I3<C844_I4<C844<T>>>>>>, C844_I5<C844_I6<C844_I7<C844_I8<C844_I9<C844_I10<C844_I11<C844<T>>>>>>>>, C844_I12<C844_I13<C844_I14<C844_I15<C844_I16<C844<T>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C844<object> c_object = new C844<object>();
        C844<int> c_int = new C844<int>();
        C844<float> c_float = new C844<float>();
    }
}

public class Test845
{
    interface C845_I0<T>
    { }
    interface C845_I1<T>
    { }
    interface C845_I2<T>
    { }
    interface C845_I3<T>
    { }
    interface C845_I4<T>
    { }
    interface C845_I5<T>
    { }
    interface C845_I6<T>
    { }
    interface C845_I7<T>
    { }
    interface C845_I8<T>
    { }
    interface C845_I9<T>
    { }
    interface C845_I10<T>
    { }
    interface C845_I11<T>
    { }
    interface C845_I12<T>
    { }
    interface C845_I13<T>
    { }
    interface C845_I14<T>
    { }
    interface C845_I15<T>
    { }
    interface C845_I16<T>
    { }
    interface C845_I17<T>
    { }

    class C845<T> : C845_I0<C845_I1<C845_I2<C845_I3<C845_I4<C845<T>>>>>>, C845_I5<C845_I6<C845_I7<C845_I8<C845_I9<C845_I10<C845_I11<C845<T>>>>>>>>, C845_I12<C845_I13<C845_I14<C845_I15<C845_I16<C845_I17<C845<T>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C845<object> c_object = new C845<object>();
        C845<int> c_int = new C845<int>();
        C845<float> c_float = new C845<float>();
    }
}

public class Test846
{
    interface C846_I0<T>
    { }
    interface C846_I1<T>
    { }
    interface C846_I2<T>
    { }
    interface C846_I3<T>
    { }
    interface C846_I4<T>
    { }
    interface C846_I5<T>
    { }
    interface C846_I6<T>
    { }
    interface C846_I7<T>
    { }
    interface C846_I8<T>
    { }
    interface C846_I9<T>
    { }
    interface C846_I10<T>
    { }
    interface C846_I11<T>
    { }
    interface C846_I12<T>
    { }
    interface C846_I13<T>
    { }
    interface C846_I14<T>
    { }
    interface C846_I15<T>
    { }
    interface C846_I16<T>
    { }

    class C846<T> : C846_I0<C846_I1<C846_I2<C846_I3<C846_I4<C846<T>>>>>>, C846_I5<C846_I6<C846_I7<C846_I8<C846_I9<C846<T>>>>>>, C846_I10<C846_I11<C846_I12<C846_I13<C846_I14<C846_I15<C846_I16<C846<T>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C846<object> c_object = new C846<object>();
        C846<int> c_int = new C846<int>();
        C846<float> c_float = new C846<float>();
    }
}

public class Test848
{
    interface C848_I0<T>
    { }
    interface C848_I1<T>
    { }
    interface C848_I2<T>
    { }
    interface C848_I3<T>
    { }
    interface C848_I4<T>
    { }
    interface C848_I5<T>
    { }
    interface C848_I6<T>
    { }
    interface C848_I7<T>
    { }
    interface C848_I8<T>
    { }
    interface C848_I9<T>
    { }
    interface C848_I10<T>
    { }
    interface C848_I11<T>
    { }
    interface C848_I12<T>
    { }
    interface C848_I13<T>
    { }

    class C848<T> : C848_I0<C848_I1<C848_I2<C848_I3<C848_I4<C848_I5<C848<T>>>>>>>, C848_I6<C848_I7<C848_I8<C848_I9<C848_I10<C848_I11<C848_I12<C848<T>>>>>>>>, C848_I13<C848<T>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C848<object> c_object = new C848<object>();
        C848<int> c_int = new C848<int>();
        C848<float> c_float = new C848<float>();
    }
}

public class Test849
{
    interface C849_I0<T>
    { }
    interface C849_I1<T>
    { }
    interface C849_I2<T>
    { }
    interface C849_I3<T>
    { }
    interface C849_I4<T>
    { }
    interface C849_I5<T>
    { }
    interface C849_I6<T>
    { }
    interface C849_I7<T>
    { }
    interface C849_I8<T>
    { }
    interface C849_I9<T>
    { }
    interface C849_I10<T>
    { }
    interface C849_I11<T>
    { }
    interface C849_I12<T>
    { }
    interface C849_I13<T>
    { }
    interface C849_I14<T>
    { }

    class C849<T> : C849_I0<C849_I1<C849_I2<C849_I3<C849_I4<C849_I5<C849<T>>>>>>>, C849_I6<C849_I7<C849_I8<C849_I9<C849_I10<C849_I11<C849_I12<C849<T>>>>>>>>, C849_I13<C849_I14<C849<T>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C849<object> c_object = new C849<object>();
        C849<int> c_int = new C849<int>();
        C849<float> c_float = new C849<float>();
    }
}



public class Test850
{
    interface C850_I0<T>
    { }
    interface C850_I1<T>
    { }
    interface C850_I2<T>
    { }
    interface C850_I3<T>
    { }
    interface C850_I4<T>
    { }
    interface C850_I5<T>
    { }
    interface C850_I6<T>
    { }
    interface C850_I7<T>
    { }
    interface C850_I8<T>
    { }
    interface C850_I9<T>
    { }
    interface C850_I10<T>
    { }
    interface C850_I11<T>
    { }
    interface C850_I12<T>
    { }
    interface C850_I13<T>
    { }
    interface C850_I14<T>
    { }
    interface C850_I15<T>
    { }

    class C850<T> : C850_I0<C850_I1<C850_I2<C850_I3<C850_I4<C850_I5<C850<T>>>>>>>, C850_I6<C850_I7<C850_I8<C850_I9<C850_I10<C850_I11<C850_I12<C850<T>>>>>>>>, C850_I13<C850_I14<C850_I15<C850<T>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C850<object> c_object = new C850<object>();
        C850<int> c_int = new C850<int>();
        C850<float> c_float = new C850<float>();
    }
}


public class Test851
{
    interface C851_I0<T>
    { }
    interface C851_I1<T>
    { }
    interface C851_I2<T>
    { }
    interface C851_I3<T>
    { }
    interface C851_I4<T>
    { }
    interface C851_I5<T>
    { }
    interface C851_I6<T>
    { }
    interface C851_I7<T>
    { }
    interface C851_I8<T>
    { }
    interface C851_I9<T>
    { }
    interface C851_I10<T>
    { }
    interface C851_I11<T>
    { }
    interface C851_I12<T>
    { }
    interface C851_I13<T>
    { }
    interface C851_I14<T>
    { }
    interface C851_I15<T>
    { }
    interface C851_I16<T>
    { }

    class C851<T> : C851_I0<C851_I1<C851_I2<C851_I3<C851_I4<C851_I5<C851<T>>>>>>>, C851_I6<C851_I7<C851_I8<C851_I9<C851_I10<C851_I11<C851_I12<C851<T>>>>>>>>, C851_I13<C851_I14<C851_I15<C851_I16<C851<T>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C851<object> c_object = new C851<object>();
        C851<int> c_int = new C851<int>();
        C851<float> c_float = new C851<float>();
    }
}

public class Test852
{
    interface C852_I0<T>
    { }
    interface C852_I1<T>
    { }
    interface C852_I2<T>
    { }
    interface C852_I3<T>
    { }
    interface C852_I4<T>
    { }
    interface C852_I5<T>
    { }
    interface C852_I6<T>
    { }
    interface C852_I7<T>
    { }
    interface C852_I8<T>
    { }
    interface C852_I9<T>
    { }
    interface C852_I10<T>
    { }
    interface C852_I11<T>
    { }
    interface C852_I12<T>
    { }
    interface C852_I13<T>
    { }
    interface C852_I14<T>
    { }
    interface C852_I15<T>
    { }
    interface C852_I16<T>
    { }
    interface C852_I17<T>
    { }

    class C852<T> : C852_I0<C852_I1<C852_I2<C852_I3<C852_I4<C852_I5<C852<T>>>>>>>, C852_I6<C852_I7<C852_I8<C852_I9<C852_I10<C852_I11<C852_I12<C852<T>>>>>>>>, C852_I13<C852_I14<C852_I15<C852_I16<C852_I17<C852<T>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C852<object> c_object = new C852<object>();
        C852<int> c_int = new C852<int>();
        C852<float> c_float = new C852<float>();
    }
}


public class Test853
{
    interface C853_I0<T>
    { }
    interface C853_I1<T>
    { }
    interface C853_I2<T>
    { }
    interface C853_I3<T>
    { }
    interface C853_I4<T>
    { }
    interface C853_I5<T>
    { }
    interface C853_I6<T>
    { }
    interface C853_I7<T>
    { }
    interface C853_I8<T>
    { }
    interface C853_I9<T>
    { }
    interface C853_I10<T>
    { }
    interface C853_I11<T>
    { }
    interface C853_I12<T>
    { }
    interface C853_I13<T>
    { }
    interface C853_I14<T>
    { }
    interface C853_I15<T>
    { }
    interface C853_I16<T>
    { }
    interface C853_I17<T>
    { }
    interface C853_I18<T>
    { }

    class C853<T> : C853_I0<C853_I1<C853_I2<C853_I3<C853_I4<C853_I5<C853<T>>>>>>>, C853_I6<C853_I7<C853_I8<C853_I9<C853_I10<C853_I11<C853_I12<C853<T>>>>>>>>, C853_I13<C853_I14<C853_I15<C853_I16<C853_I17<C853_I18<C853<T>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C853<object> c_object = new C853<object>();
        C853<int> c_int = new C853<int>();
        C853<float> c_float = new C853<float>();
    }
}

public class Test854
{
    interface C854_I0<T>
    { }
    interface C854_I1<T>
    { }
    interface C854_I2<T>
    { }
    interface C854_I3<T>
    { }
    interface C854_I4<T>
    { }
    interface C854_I5<T>
    { }
    interface C854_I6<T>
    { }
    interface C854_I7<T>
    { }
    interface C854_I8<T>
    { }
    interface C854_I9<T>
    { }
    interface C854_I10<T>
    { }
    interface C854_I11<T>
    { }
    interface C854_I12<T>
    { }
    interface C854_I13<T>
    { }
    interface C854_I14<T>
    { }
    interface C854_I15<T>
    { }
    interface C854_I16<T>
    { }
    interface C854_I17<T>
    { }
    interface C854_I18<T>
    { }

    class C854<T> : C854_I0<C854_I1<C854_I2<C854_I3<C854_I4<C854_I5<C854<T>>>>>>>, C854_I6<C854_I7<C854_I8<C854_I9<C854_I10<C854_I11<C854<T>>>>>>>, C854_I12<C854_I13<C854_I14<C854_I15<C854_I16<C854_I17<C854_I18<C854<T>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C854<object> c_object = new C854<object>();
        C854<int> c_int = new C854<int>();
        C854<float> c_float = new C854<float>();
    }
}

public class Test862
{
    interface C862_I0<T>
    { }
    interface C862_I1<T>
    { }
    interface C862_I2<T>
    { }
    interface C862_I3<T>
    { }
    interface C862_I4<T>
    { }
    interface C862_I5<T>
    { }
    interface C862_I6<T>
    { }
    interface C862_I7<T>
    { }
    interface C862_I8<T>
    { }
    interface C862_I9<T>
    { }
    interface C862_I10<T>
    { }
    interface C862_I11<T>
    { }
    interface C862_I12<T>
    { }
    interface C862_I13<T>
    { }
    interface C862_I14<T>
    { }
    interface C862_I15<T>
    { }
    interface C862_I16<T>
    { }
    interface C862_I17<T>
    { }
    interface C862_I18<T>
    { }
    interface C862_I19<T>
    { }
    interface C862_I20<T>
    { }

    class C862<T> : C862_I0<C862_I1<C862_I2<C862_I3<C862_I4<C862_I5<C862_I6<C862<T>>>>>>>>, C862_I7<C862_I8<C862_I9<C862_I10<C862_I11<C862_I12<C862_I13<C862<T>>>>>>>>, C862_I14<C862_I15<C862_I16<C862_I17<C862_I18<C862_I19<C862_I20<C862<T>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C862<object> c_object = new C862<object>();
        C862<int> c_int = new C862<int>();
        C862<float> c_float = new C862<float>();
    }
}

public class Test864
{
    interface C864_I0<T>
    { }
    interface C864_I1<T>
    { }
    interface C864_I2<T>
    { }
    interface C864_I3<T>
    { }
    interface C864_I4<T>
    { }
    interface C864_I5<T>
    { }
    interface C864_I6<T>
    { }
    interface C864_I7<T>
    { }
    interface C864_I8<T>
    { }

    class C864<T> : C864_I0<C864_I1<C864_I2<C864_I3<C864_I4<C864_I5<C864_I6<C864<T>>>>>>>>, C864_I7<C864<T>>, C864_I8<C864<T>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C864<object> c_object = new C864<object>();
        C864<int> c_int = new C864<int>();
        C864<float> c_float = new C864<float>();
    }
}

public class Test865
{
    interface C865_I0<T>
    { }
    interface C865_I1<T>
    { }
    interface C865_I2<T>
    { }
    interface C865_I3<T>
    { }
    interface C865_I4<T>
    { }
    interface C865_I5<T>
    { }
    interface C865_I6<T>
    { }
    interface C865_I7<T>
    { }
    interface C865_I8<T>
    { }
    interface C865_I9<T>
    { }

    class C865<T> : C865_I0<C865_I1<C865_I2<C865_I3<C865_I4<C865_I5<C865_I6<C865<T>>>>>>>>, C865_I7<C865<T>>, C865_I8<C865_I9<C865<T>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C865<object> c_object = new C865<object>();
        C865<int> c_int = new C865<int>();
        C865<float> c_float = new C865<float>();
    }
}

public class Test866
{
    interface C866_I0<T>
    { }
    interface C866_I1<T>
    { }
    interface C866_I2<T>
    { }
    interface C866_I3<T>
    { }
    interface C866_I4<T>
    { }
    interface C866_I5<T>
    { }
    interface C866_I6<T>
    { }
    interface C866_I7<T>
    { }
    interface C866_I8<T>
    { }
    interface C866_I9<T>
    { }
    interface C866_I10<T>
    { }

    class C866<T> : C866_I0<C866_I1<C866_I2<C866_I3<C866_I4<C866_I5<C866_I6<C866<T>>>>>>>>, C866_I7<C866<T>>, C866_I8<C866_I9<C866_I10<C866<T>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C866<object> c_object = new C866<object>();
        C866<int> c_int = new C866<int>();
        C866<float> c_float = new C866<float>();
    }
}

public class Test867
{
    interface C867_I0<T>
    { }
    interface C867_I1<T>
    { }
    interface C867_I2<T>
    { }
    interface C867_I3<T>
    { }
    interface C867_I4<T>
    { }
    interface C867_I5<T>
    { }
    interface C867_I6<T>
    { }
    interface C867_I7<T>
    { }
    interface C867_I8<T>
    { }
    interface C867_I9<T>
    { }
    interface C867_I10<T>
    { }
    interface C867_I11<T>
    { }

    class C867<T> : C867_I0<C867_I1<C867_I2<C867_I3<C867_I4<C867_I5<C867_I6<C867<T>>>>>>>>, C867_I7<C867<T>>, C867_I8<C867_I9<C867_I10<C867_I11<C867<T>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C867<object> c_object = new C867<object>();
        C867<int> c_int = new C867<int>();
        C867<float> c_float = new C867<float>();
    }
}

public class Test868
{
    interface C868_I0<T>
    { }
    interface C868_I1<T>
    { }
    interface C868_I2<T>
    { }
    interface C868_I3<T>
    { }
    interface C868_I4<T>
    { }
    interface C868_I5<T>
    { }
    interface C868_I6<T>
    { }
    interface C868_I7<T>
    { }
    interface C868_I8<T>
    { }
    interface C868_I9<T>
    { }
    interface C868_I10<T>
    { }
    interface C868_I11<T>
    { }
    interface C868_I12<T>
    { }

    class C868<T> : C868_I0<C868_I1<C868_I2<C868_I3<C868_I4<C868_I5<C868_I6<C868<T>>>>>>>>, C868_I7<C868<T>>, C868_I8<C868_I9<C868_I10<C868_I11<C868_I12<C868<T>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C868<object> c_object = new C868<object>();
        C868<int> c_int = new C868<int>();
        C868<float> c_float = new C868<float>();
    }
}

public class Test869
{
    interface C869_I0<T>
    { }
    interface C869_I1<T>
    { }
    interface C869_I2<T>
    { }
    interface C869_I3<T>
    { }
    interface C869_I4<T>
    { }
    interface C869_I5<T>
    { }
    interface C869_I6<T>
    { }
    interface C869_I7<T>
    { }
    interface C869_I8<T>
    { }
    interface C869_I9<T>
    { }
    interface C869_I10<T>
    { }
    interface C869_I11<T>
    { }
    interface C869_I12<T>
    { }
    interface C869_I13<T>
    { }

    class C869<T> : C869_I0<C869_I1<C869_I2<C869_I3<C869_I4<C869_I5<C869_I6<C869<T>>>>>>>>, C869_I7<C869<T>>, C869_I8<C869_I9<C869_I10<C869_I11<C869_I12<C869_I13<C869<T>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C869<object> c_object = new C869<object>();
        C869<int> c_int = new C869<int>();
        C869<float> c_float = new C869<float>();
    }
}

public class Test871
{
    interface C871_I0<T>
    { }
    interface C871_I1<T>
    { }
    interface C871_I2<T>
    { }
    interface C871_I3<T>
    { }
    interface C871_I4<T>
    { }
    interface C871_I5<T>
    { }
    interface C871_I6<T>
    { }
    interface C871_I7<T>
    { }
    interface C871_I8<T>
    { }
    interface C871_I9<T>
    { }

    class C871<T> : C871_I0<C871_I1<C871_I2<C871_I3<C871_I4<C871_I5<C871_I6<C871<T>>>>>>>>, C871_I7<C871_I8<C871<T>>>, C871_I9<C871<T>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C871<object> c_object = new C871<object>();
        C871<int> c_int = new C871<int>();
        C871<float> c_float = new C871<float>();
    }
}

public class Test872
{
    interface C872_I0<T>
    { }
    interface C872_I1<T>
    { }
    interface C872_I2<T>
    { }
    interface C872_I3<T>
    { }
    interface C872_I4<T>
    { }
    interface C872_I5<T>
    { }
    interface C872_I6<T>
    { }
    interface C872_I7<T>
    { }
    interface C872_I8<T>
    { }
    interface C872_I9<T>
    { }
    interface C872_I10<T>
    { }

    class C872<T> : C872_I0<C872_I1<C872_I2<C872_I3<C872_I4<C872_I5<C872_I6<C872<T>>>>>>>>, C872_I7<C872_I8<C872<T>>>, C872_I9<C872_I10<C872<T>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C872<object> c_object = new C872<object>();
        C872<int> c_int = new C872<int>();
        C872<float> c_float = new C872<float>();
    }
}

public class Test873
{
    interface C873_I0<T>
    { }
    interface C873_I1<T>
    { }
    interface C873_I2<T>
    { }
    interface C873_I3<T>
    { }
    interface C873_I4<T>
    { }
    interface C873_I5<T>
    { }
    interface C873_I6<T>
    { }
    interface C873_I7<T>
    { }
    interface C873_I8<T>
    { }
    interface C873_I9<T>
    { }
    interface C873_I10<T>
    { }
    interface C873_I11<T>
    { }

    class C873<T> : C873_I0<C873_I1<C873_I2<C873_I3<C873_I4<C873_I5<C873_I6<C873<T>>>>>>>>, C873_I7<C873_I8<C873<T>>>, C873_I9<C873_I10<C873_I11<C873<T>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C873<object> c_object = new C873<object>();
        C873<int> c_int = new C873<int>();
        C873<float> c_float = new C873<float>();
    }
}

public class Test874
{
    interface C874_I0<T>
    { }
    interface C874_I1<T>
    { }
    interface C874_I2<T>
    { }
    interface C874_I3<T>
    { }
    interface C874_I4<T>
    { }
    interface C874_I5<T>
    { }
    interface C874_I6<T>
    { }
    interface C874_I7<T>
    { }
    interface C874_I8<T>
    { }
    interface C874_I9<T>
    { }
    interface C874_I10<T>
    { }
    interface C874_I11<T>
    { }
    interface C874_I12<T>
    { }

    class C874<T> : C874_I0<C874_I1<C874_I2<C874_I3<C874_I4<C874_I5<C874_I6<C874<T>>>>>>>>, C874_I7<C874_I8<C874<T>>>, C874_I9<C874_I10<C874_I11<C874_I12<C874<T>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C874<object> c_object = new C874<object>();
        C874<int> c_int = new C874<int>();
        C874<float> c_float = new C874<float>();
    }
}

public class Test875
{
    interface C875_I0<T>
    { }
    interface C875_I1<T>
    { }
    interface C875_I2<T>
    { }
    interface C875_I3<T>
    { }
    interface C875_I4<T>
    { }
    interface C875_I5<T>
    { }
    interface C875_I6<T>
    { }
    interface C875_I7<T>
    { }
    interface C875_I8<T>
    { }
    interface C875_I9<T>
    { }
    interface C875_I10<T>
    { }
    interface C875_I11<T>
    { }
    interface C875_I12<T>
    { }
    interface C875_I13<T>
    { }

    class C875<T> : C875_I0<C875_I1<C875_I2<C875_I3<C875_I4<C875_I5<C875_I6<C875<T>>>>>>>>, C875_I7<C875_I8<C875<T>>>, C875_I9<C875_I10<C875_I11<C875_I12<C875_I13<C875<T>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C875<object> c_object = new C875<object>();
        C875<int> c_int = new C875<int>();
        C875<float> c_float = new C875<float>();
    }
}

public class Test876
{
    interface C876_I0<T>
    { }
    interface C876_I1<T>
    { }
    interface C876_I2<T>
    { }
    interface C876_I3<T>
    { }
    interface C876_I4<T>
    { }
    interface C876_I5<T>
    { }
    interface C876_I6<T>
    { }
    interface C876_I7<T>
    { }
    interface C876_I8<T>
    { }
    interface C876_I9<T>
    { }
    interface C876_I10<T>
    { }
    interface C876_I11<T>
    { }
    interface C876_I12<T>
    { }
    interface C876_I13<T>
    { }
    interface C876_I14<T>
    { }

    class C876<T> : C876_I0<C876_I1<C876_I2<C876_I3<C876_I4<C876_I5<C876_I6<C876<T>>>>>>>>, C876_I7<C876_I8<C876<T>>>, C876_I9<C876_I10<C876_I11<C876_I12<C876_I13<C876_I14<C876<T>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C876<object> c_object = new C876<object>();
        C876<int> c_int = new C876<int>();
        C876<float> c_float = new C876<float>();
    }
}

public class Test878
{
    interface C878_I0<T>
    { }
    interface C878_I1<T>
    { }
    interface C878_I2<T>
    { }
    interface C878_I3<T>
    { }
    interface C878_I4<T>
    { }
    interface C878_I5<T>
    { }
    interface C878_I6<T>
    { }
    interface C878_I7<T>
    { }
    interface C878_I8<T>
    { }
    interface C878_I9<T>
    { }
    interface C878_I10<T>
    { }

    class C878<T> : C878_I0<C878_I1<C878_I2<C878_I3<C878_I4<C878_I5<C878_I6<C878<T>>>>>>>>, C878_I7<C878_I8<C878_I9<C878<T>>>>, C878_I10<C878<T>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C878<object> c_object = new C878<object>();
        C878<int> c_int = new C878<int>();
        C878<float> c_float = new C878<float>();
    }
}

public class Test879
{
    interface C879_I0<T>
    { }
    interface C879_I1<T>
    { }
    interface C879_I2<T>
    { }
    interface C879_I3<T>
    { }
    interface C879_I4<T>
    { }
    interface C879_I5<T>
    { }
    interface C879_I6<T>
    { }
    interface C879_I7<T>
    { }
    interface C879_I8<T>
    { }
    interface C879_I9<T>
    { }
    interface C879_I10<T>
    { }
    interface C879_I11<T>
    { }

    class C879<T> : C879_I0<C879_I1<C879_I2<C879_I3<C879_I4<C879_I5<C879_I6<C879<T>>>>>>>>, C879_I7<C879_I8<C879_I9<C879<T>>>>, C879_I10<C879_I11<C879<T>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C879<object> c_object = new C879<object>();
        C879<int> c_int = new C879<int>();
        C879<float> c_float = new C879<float>();
    }
}

public class Test880
{
    interface C880_I0<T>
    { }
    interface C880_I1<T>
    { }
    interface C880_I2<T>
    { }
    interface C880_I3<T>
    { }
    interface C880_I4<T>
    { }
    interface C880_I5<T>
    { }
    interface C880_I6<T>
    { }
    interface C880_I7<T>
    { }
    interface C880_I8<T>
    { }
    interface C880_I9<T>
    { }
    interface C880_I10<T>
    { }
    interface C880_I11<T>
    { }
    interface C880_I12<T>
    { }

    class C880<T> : C880_I0<C880_I1<C880_I2<C880_I3<C880_I4<C880_I5<C880_I6<C880<T>>>>>>>>, C880_I7<C880_I8<C880_I9<C880<T>>>>, C880_I10<C880_I11<C880_I12<C880<T>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C880<object> c_object = new C880<object>();
        C880<int> c_int = new C880<int>();
        C880<float> c_float = new C880<float>();
    }
}

public class Test881
{
    interface C881_I0<T>
    { }
    interface C881_I1<T>
    { }
    interface C881_I2<T>
    { }
    interface C881_I3<T>
    { }
    interface C881_I4<T>
    { }
    interface C881_I5<T>
    { }
    interface C881_I6<T>
    { }
    interface C881_I7<T>
    { }
    interface C881_I8<T>
    { }
    interface C881_I9<T>
    { }
    interface C881_I10<T>
    { }
    interface C881_I11<T>
    { }
    interface C881_I12<T>
    { }
    interface C881_I13<T>
    { }

    class C881<T> : C881_I0<C881_I1<C881_I2<C881_I3<C881_I4<C881_I5<C881_I6<C881<T>>>>>>>>, C881_I7<C881_I8<C881_I9<C881<T>>>>, C881_I10<C881_I11<C881_I12<C881_I13<C881<T>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C881<object> c_object = new C881<object>();
        C881<int> c_int = new C881<int>();
        C881<float> c_float = new C881<float>();
    }
}

public class Test882
{
    interface C882_I0<T>
    { }
    interface C882_I1<T>
    { }
    interface C882_I2<T>
    { }
    interface C882_I3<T>
    { }
    interface C882_I4<T>
    { }
    interface C882_I5<T>
    { }
    interface C882_I6<T>
    { }
    interface C882_I7<T>
    { }
    interface C882_I8<T>
    { }
    interface C882_I9<T>
    { }
    interface C882_I10<T>
    { }
    interface C882_I11<T>
    { }
    interface C882_I12<T>
    { }
    interface C882_I13<T>
    { }
    interface C882_I14<T>
    { }

    class C882<T> : C882_I0<C882_I1<C882_I2<C882_I3<C882_I4<C882_I5<C882_I6<C882<T>>>>>>>>, C882_I7<C882_I8<C882_I9<C882<T>>>>, C882_I10<C882_I11<C882_I12<C882_I13<C882_I14<C882<T>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C882<object> c_object = new C882<object>();
        C882<int> c_int = new C882<int>();
        C882<float> c_float = new C882<float>();
    }
}

public class Test883
{
    interface C883_I0<T>
    { }
    interface C883_I1<T>
    { }
    interface C883_I2<T>
    { }
    interface C883_I3<T>
    { }
    interface C883_I4<T>
    { }
    interface C883_I5<T>
    { }
    interface C883_I6<T>
    { }
    interface C883_I7<T>
    { }
    interface C883_I8<T>
    { }
    interface C883_I9<T>
    { }
    interface C883_I10<T>
    { }
    interface C883_I11<T>
    { }
    interface C883_I12<T>
    { }
    interface C883_I13<T>
    { }
    interface C883_I14<T>
    { }
    interface C883_I15<T>
    { }

    class C883<T> : C883_I0<C883_I1<C883_I2<C883_I3<C883_I4<C883_I5<C883_I6<C883<T>>>>>>>>, C883_I7<C883_I8<C883_I9<C883<T>>>>, C883_I10<C883_I11<C883_I12<C883_I13<C883_I14<C883_I15<C883<T>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C883<object> c_object = new C883<object>();
        C883<int> c_int = new C883<int>();
        C883<float> c_float = new C883<float>();
    }
}

public class Test885
{
    interface C885_I0<T>
    { }
    interface C885_I1<T>
    { }
    interface C885_I2<T>
    { }
    interface C885_I3<T>
    { }
    interface C885_I4<T>
    { }
    interface C885_I5<T>
    { }
    interface C885_I6<T>
    { }
    interface C885_I7<T>
    { }
    interface C885_I8<T>
    { }
    interface C885_I9<T>
    { }
    interface C885_I10<T>
    { }
    interface C885_I11<T>
    { }

    class C885<T> : C885_I0<C885_I1<C885_I2<C885_I3<C885_I4<C885_I5<C885_I6<C885<T>>>>>>>>, C885_I7<C885_I8<C885_I9<C885_I10<C885<T>>>>>, C885_I11<C885<T>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C885<object> c_object = new C885<object>();
        C885<int> c_int = new C885<int>();
        C885<float> c_float = new C885<float>();
    }
}

public class Test886
{
    interface C886_I0<T>
    { }
    interface C886_I1<T>
    { }
    interface C886_I2<T>
    { }
    interface C886_I3<T>
    { }
    interface C886_I4<T>
    { }
    interface C886_I5<T>
    { }
    interface C886_I6<T>
    { }
    interface C886_I7<T>
    { }
    interface C886_I8<T>
    { }
    interface C886_I9<T>
    { }
    interface C886_I10<T>
    { }
    interface C886_I11<T>
    { }
    interface C886_I12<T>
    { }

    class C886<T> : C886_I0<C886_I1<C886_I2<C886_I3<C886_I4<C886_I5<C886_I6<C886<T>>>>>>>>, C886_I7<C886_I8<C886_I9<C886_I10<C886<T>>>>>, C886_I11<C886_I12<C886<T>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C886<object> c_object = new C886<object>();
        C886<int> c_int = new C886<int>();
        C886<float> c_float = new C886<float>();
    }
}

public class Test887
{
    interface C887_I0<T>
    { }
    interface C887_I1<T>
    { }
    interface C887_I2<T>
    { }
    interface C887_I3<T>
    { }
    interface C887_I4<T>
    { }
    interface C887_I5<T>
    { }
    interface C887_I6<T>
    { }
    interface C887_I7<T>
    { }
    interface C887_I8<T>
    { }
    interface C887_I9<T>
    { }
    interface C887_I10<T>
    { }
    interface C887_I11<T>
    { }
    interface C887_I12<T>
    { }
    interface C887_I13<T>
    { }

    class C887<T> : C887_I0<C887_I1<C887_I2<C887_I3<C887_I4<C887_I5<C887_I6<C887<T>>>>>>>>, C887_I7<C887_I8<C887_I9<C887_I10<C887<T>>>>>, C887_I11<C887_I12<C887_I13<C887<T>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C887<object> c_object = new C887<object>();
        C887<int> c_int = new C887<int>();
        C887<float> c_float = new C887<float>();
    }
}

public class Test888
{
    interface C888_I0<T>
    { }
    interface C888_I1<T>
    { }
    interface C888_I2<T>
    { }
    interface C888_I3<T>
    { }
    interface C888_I4<T>
    { }
    interface C888_I5<T>
    { }
    interface C888_I6<T>
    { }
    interface C888_I7<T>
    { }
    interface C888_I8<T>
    { }
    interface C888_I9<T>
    { }
    interface C888_I10<T>
    { }
    interface C888_I11<T>
    { }
    interface C888_I12<T>
    { }
    interface C888_I13<T>
    { }
    interface C888_I14<T>
    { }

    class C888<T> : C888_I0<C888_I1<C888_I2<C888_I3<C888_I4<C888_I5<C888_I6<C888<T>>>>>>>>, C888_I7<C888_I8<C888_I9<C888_I10<C888<T>>>>>, C888_I11<C888_I12<C888_I13<C888_I14<C888<T>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C888<object> c_object = new C888<object>();
        C888<int> c_int = new C888<int>();
        C888<float> c_float = new C888<float>();
    }
}

public class Test889
{
    interface C889_I0<T>
    { }
    interface C889_I1<T>
    { }
    interface C889_I2<T>
    { }
    interface C889_I3<T>
    { }
    interface C889_I4<T>
    { }
    interface C889_I5<T>
    { }
    interface C889_I6<T>
    { }
    interface C889_I7<T>
    { }
    interface C889_I8<T>
    { }
    interface C889_I9<T>
    { }
    interface C889_I10<T>
    { }
    interface C889_I11<T>
    { }
    interface C889_I12<T>
    { }
    interface C889_I13<T>
    { }
    interface C889_I14<T>
    { }
    interface C889_I15<T>
    { }

    class C889<T> : C889_I0<C889_I1<C889_I2<C889_I3<C889_I4<C889_I5<C889_I6<C889<T>>>>>>>>, C889_I7<C889_I8<C889_I9<C889_I10<C889<T>>>>>, C889_I11<C889_I12<C889_I13<C889_I14<C889_I15<C889<T>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C889<object> c_object = new C889<object>();
        C889<int> c_int = new C889<int>();
        C889<float> c_float = new C889<float>();
    }
}

public class Test890
{
    interface C890_I0<T>
    { }
    interface C890_I1<T>
    { }
    interface C890_I2<T>
    { }
    interface C890_I3<T>
    { }
    interface C890_I4<T>
    { }
    interface C890_I5<T>
    { }
    interface C890_I6<T>
    { }
    interface C890_I7<T>
    { }
    interface C890_I8<T>
    { }
    interface C890_I9<T>
    { }
    interface C890_I10<T>
    { }
    interface C890_I11<T>
    { }
    interface C890_I12<T>
    { }
    interface C890_I13<T>
    { }
    interface C890_I14<T>
    { }
    interface C890_I15<T>
    { }
    interface C890_I16<T>
    { }

    class C890<T> : C890_I0<C890_I1<C890_I2<C890_I3<C890_I4<C890_I5<C890_I6<C890<T>>>>>>>>, C890_I7<C890_I8<C890_I9<C890_I10<C890<T>>>>>, C890_I11<C890_I12<C890_I13<C890_I14<C890_I15<C890_I16<C890<T>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C890<object> c_object = new C890<object>();
        C890<int> c_int = new C890<int>();
        C890<float> c_float = new C890<float>();
    }
}

public class Test892
{
    interface C892_I0<T>
    { }
    interface C892_I1<T>
    { }
    interface C892_I2<T>
    { }
    interface C892_I3<T>
    { }
    interface C892_I4<T>
    { }
    interface C892_I5<T>
    { }
    interface C892_I6<T>
    { }
    interface C892_I7<T>
    { }
    interface C892_I8<T>
    { }
    interface C892_I9<T>
    { }
    interface C892_I10<T>
    { }
    interface C892_I11<T>
    { }
    interface C892_I12<T>
    { }

    class C892<T> : C892_I0<C892_I1<C892_I2<C892_I3<C892_I4<C892_I5<C892_I6<C892<T>>>>>>>>, C892_I7<C892_I8<C892_I9<C892_I10<C892_I11<C892<T>>>>>>, C892_I12<C892<T>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C892<object> c_object = new C892<object>();
        C892<int> c_int = new C892<int>();
        C892<float> c_float = new C892<float>();
    }
}

public class Test893
{
    interface C893_I0<T>
    { }
    interface C893_I1<T>
    { }
    interface C893_I2<T>
    { }
    interface C893_I3<T>
    { }
    interface C893_I4<T>
    { }
    interface C893_I5<T>
    { }
    interface C893_I6<T>
    { }
    interface C893_I7<T>
    { }
    interface C893_I8<T>
    { }
    interface C893_I9<T>
    { }
    interface C893_I10<T>
    { }
    interface C893_I11<T>
    { }
    interface C893_I12<T>
    { }
    interface C893_I13<T>
    { }

    class C893<T> : C893_I0<C893_I1<C893_I2<C893_I3<C893_I4<C893_I5<C893_I6<C893<T>>>>>>>>, C893_I7<C893_I8<C893_I9<C893_I10<C893_I11<C893<T>>>>>>, C893_I12<C893_I13<C893<T>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C893<object> c_object = new C893<object>();
        C893<int> c_int = new C893<int>();
        C893<float> c_float = new C893<float>();
    }
}

public class Test894
{
    interface C894_I0<T>
    { }
    interface C894_I1<T>
    { }
    interface C894_I2<T>
    { }
    interface C894_I3<T>
    { }
    interface C894_I4<T>
    { }
    interface C894_I5<T>
    { }
    interface C894_I6<T>
    { }
    interface C894_I7<T>
    { }
    interface C894_I8<T>
    { }
    interface C894_I9<T>
    { }
    interface C894_I10<T>
    { }
    interface C894_I11<T>
    { }
    interface C894_I12<T>
    { }
    interface C894_I13<T>
    { }
    interface C894_I14<T>
    { }

    class C894<T> : C894_I0<C894_I1<C894_I2<C894_I3<C894_I4<C894_I5<C894_I6<C894<T>>>>>>>>, C894_I7<C894_I8<C894_I9<C894_I10<C894_I11<C894<T>>>>>>, C894_I12<C894_I13<C894_I14<C894<T>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C894<object> c_object = new C894<object>();
        C894<int> c_int = new C894<int>();
        C894<float> c_float = new C894<float>();
    }
}

public class Test895
{
    interface C895_I0<T>
    { }
    interface C895_I1<T>
    { }
    interface C895_I2<T>
    { }
    interface C895_I3<T>
    { }
    interface C895_I4<T>
    { }
    interface C895_I5<T>
    { }
    interface C895_I6<T>
    { }
    interface C895_I7<T>
    { }
    interface C895_I8<T>
    { }
    interface C895_I9<T>
    { }
    interface C895_I10<T>
    { }
    interface C895_I11<T>
    { }
    interface C895_I12<T>
    { }
    interface C895_I13<T>
    { }
    interface C895_I14<T>
    { }
    interface C895_I15<T>
    { }

    class C895<T> : C895_I0<C895_I1<C895_I2<C895_I3<C895_I4<C895_I5<C895_I6<C895<T>>>>>>>>, C895_I7<C895_I8<C895_I9<C895_I10<C895_I11<C895<T>>>>>>, C895_I12<C895_I13<C895_I14<C895_I15<C895<T>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C895<object> c_object = new C895<object>();
        C895<int> c_int = new C895<int>();
        C895<float> c_float = new C895<float>();
    }
}

public class Test896
{
    interface C896_I0<T>
    { }
    interface C896_I1<T>
    { }
    interface C896_I2<T>
    { }
    interface C896_I3<T>
    { }
    interface C896_I4<T>
    { }
    interface C896_I5<T>
    { }
    interface C896_I6<T>
    { }
    interface C896_I7<T>
    { }
    interface C896_I8<T>
    { }
    interface C896_I9<T>
    { }
    interface C896_I10<T>
    { }
    interface C896_I11<T>
    { }
    interface C896_I12<T>
    { }
    interface C896_I13<T>
    { }
    interface C896_I14<T>
    { }
    interface C896_I15<T>
    { }
    interface C896_I16<T>
    { }

    class C896<T> : C896_I0<C896_I1<C896_I2<C896_I3<C896_I4<C896_I5<C896_I6<C896<T>>>>>>>>, C896_I7<C896_I8<C896_I9<C896_I10<C896_I11<C896<T>>>>>>, C896_I12<C896_I13<C896_I14<C896_I15<C896_I16<C896<T>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C896<object> c_object = new C896<object>();
        C896<int> c_int = new C896<int>();
        C896<float> c_float = new C896<float>();
    }
}

public class Test897
{
    interface C897_I0<T>
    { }
    interface C897_I1<T>
    { }
    interface C897_I2<T>
    { }
    interface C897_I3<T>
    { }
    interface C897_I4<T>
    { }
    interface C897_I5<T>
    { }
    interface C897_I6<T>
    { }
    interface C897_I7<T>
    { }
    interface C897_I8<T>
    { }
    interface C897_I9<T>
    { }
    interface C897_I10<T>
    { }
    interface C897_I11<T>
    { }
    interface C897_I12<T>
    { }
    interface C897_I13<T>
    { }
    interface C897_I14<T>
    { }
    interface C897_I15<T>
    { }
    interface C897_I16<T>
    { }
    interface C897_I17<T>
    { }

    class C897<T> : C897_I0<C897_I1<C897_I2<C897_I3<C897_I4<C897_I5<C897_I6<C897<T>>>>>>>>, C897_I7<C897_I8<C897_I9<C897_I10<C897_I11<C897<T>>>>>>, C897_I12<C897_I13<C897_I14<C897_I15<C897_I16<C897_I17<C897<T>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C897<object> c_object = new C897<object>();
        C897<int> c_int = new C897<int>();
        C897<float> c_float = new C897<float>();
    }
}

public class Test899
{
    interface C899_I0<T>
    { }
    interface C899_I1<T>
    { }
    interface C899_I2<T>
    { }
    interface C899_I3<T>
    { }
    interface C899_I4<T>
    { }
    interface C899_I5<T>
    { }
    interface C899_I6<T>
    { }
    interface C899_I7<T>
    { }
    interface C899_I8<T>
    { }
    interface C899_I9<T>
    { }
    interface C899_I10<T>
    { }
    interface C899_I11<T>
    { }
    interface C899_I12<T>
    { }
    interface C899_I13<T>
    { }

    class C899<T> : C899_I0<C899_I1<C899_I2<C899_I3<C899_I4<C899_I5<C899_I6<C899<T>>>>>>>>, C899_I7<C899_I8<C899_I9<C899_I10<C899_I11<C899_I12<C899<T>>>>>>>, C899_I13<C899<T>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C899<object> c_object = new C899<object>();
        C899<int> c_int = new C899<int>();
        C899<float> c_float = new C899<float>();
    }
}

public class Test900
{
    interface C900_I0<T>
    { }
    interface C900_I1<T>
    { }
    interface C900_I2<T>
    { }
    interface C900_I3<T>
    { }
    interface C900_I4<T>
    { }
    interface C900_I5<T>
    { }
    interface C900_I6<T>
    { }
    interface C900_I7<T>
    { }
    interface C900_I8<T>
    { }
    interface C900_I9<T>
    { }
    interface C900_I10<T>
    { }
    interface C900_I11<T>
    { }
    interface C900_I12<T>
    { }
    interface C900_I13<T>
    { }
    interface C900_I14<T>
    { }

    class C900<T> : C900_I0<C900_I1<C900_I2<C900_I3<C900_I4<C900_I5<C900_I6<C900<T>>>>>>>>, C900_I7<C900_I8<C900_I9<C900_I10<C900_I11<C900_I12<C900<T>>>>>>>, C900_I13<C900_I14<C900<T>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C900<object> c_object = new C900<object>();
        C900<int> c_int = new C900<int>();
        C900<float> c_float = new C900<float>();
    }
}

public class Test901
{
    interface C901_I0<T>
    { }
    interface C901_I1<T>
    { }
    interface C901_I2<T>
    { }
    interface C901_I3<T>
    { }
    interface C901_I4<T>
    { }
    interface C901_I5<T>
    { }
    interface C901_I6<T>
    { }
    interface C901_I7<T>
    { }
    interface C901_I8<T>
    { }
    interface C901_I9<T>
    { }
    interface C901_I10<T>
    { }
    interface C901_I11<T>
    { }
    interface C901_I12<T>
    { }
    interface C901_I13<T>
    { }
    interface C901_I14<T>
    { }
    interface C901_I15<T>
    { }

    class C901<T> : C901_I0<C901_I1<C901_I2<C901_I3<C901_I4<C901_I5<C901_I6<C901<T>>>>>>>>, C901_I7<C901_I8<C901_I9<C901_I10<C901_I11<C901_I12<C901<T>>>>>>>, C901_I13<C901_I14<C901_I15<C901<T>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C901<object> c_object = new C901<object>();
        C901<int> c_int = new C901<int>();
        C901<float> c_float = new C901<float>();
    }
}

public class Test902
{
    interface C902_I0<T>
    { }
    interface C902_I1<T>
    { }
    interface C902_I2<T>
    { }
    interface C902_I3<T>
    { }
    interface C902_I4<T>
    { }
    interface C902_I5<T>
    { }
    interface C902_I6<T>
    { }
    interface C902_I7<T>
    { }
    interface C902_I8<T>
    { }
    interface C902_I9<T>
    { }
    interface C902_I10<T>
    { }
    interface C902_I11<T>
    { }
    interface C902_I12<T>
    { }
    interface C902_I13<T>
    { }
    interface C902_I14<T>
    { }
    interface C902_I15<T>
    { }
    interface C902_I16<T>
    { }

    class C902<T> : C902_I0<C902_I1<C902_I2<C902_I3<C902_I4<C902_I5<C902_I6<C902<T>>>>>>>>, C902_I7<C902_I8<C902_I9<C902_I10<C902_I11<C902_I12<C902<T>>>>>>>, C902_I13<C902_I14<C902_I15<C902_I16<C902<T>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C902<object> c_object = new C902<object>();
        C902<int> c_int = new C902<int>();
        C902<float> c_float = new C902<float>();
    }
}

public class Test903
{
    interface C903_I0<T>
    { }
    interface C903_I1<T>
    { }
    interface C903_I2<T>
    { }
    interface C903_I3<T>
    { }
    interface C903_I4<T>
    { }
    interface C903_I5<T>
    { }
    interface C903_I6<T>
    { }
    interface C903_I7<T>
    { }
    interface C903_I8<T>
    { }
    interface C903_I9<T>
    { }
    interface C903_I10<T>
    { }
    interface C903_I11<T>
    { }
    interface C903_I12<T>
    { }
    interface C903_I13<T>
    { }
    interface C903_I14<T>
    { }
    interface C903_I15<T>
    { }
    interface C903_I16<T>
    { }
    interface C903_I17<T>
    { }

    class C903<T> : C903_I0<C903_I1<C903_I2<C903_I3<C903_I4<C903_I5<C903_I6<C903<T>>>>>>>>, C903_I7<C903_I8<C903_I9<C903_I10<C903_I11<C903_I12<C903<T>>>>>>>, C903_I13<C903_I14<C903_I15<C903_I16<C903_I17<C903<T>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C903<object> c_object = new C903<object>();
        C903<int> c_int = new C903<int>();
        C903<float> c_float = new C903<float>();
    }
}

public class Test904
{
    interface C904_I0<T>
    { }
    interface C904_I1<T>
    { }
    interface C904_I2<T>
    { }
    interface C904_I3<T>
    { }
    interface C904_I4<T>
    { }
    interface C904_I5<T>
    { }
    interface C904_I6<T>
    { }
    interface C904_I7<T>
    { }
    interface C904_I8<T>
    { }
    interface C904_I9<T>
    { }
    interface C904_I10<T>
    { }
    interface C904_I11<T>
    { }
    interface C904_I12<T>
    { }
    interface C904_I13<T>
    { }
    interface C904_I14<T>
    { }
    interface C904_I15<T>
    { }
    interface C904_I16<T>
    { }
    interface C904_I17<T>
    { }
    interface C904_I18<T>
    { }

    class C904<T> : C904_I0<C904_I1<C904_I2<C904_I3<C904_I4<C904_I5<C904_I6<C904<T>>>>>>>>, C904_I7<C904_I8<C904_I9<C904_I10<C904_I11<C904_I12<C904<T>>>>>>>, C904_I13<C904_I14<C904_I15<C904_I16<C904_I17<C904_I18<C904<T>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C904<object> c_object = new C904<object>();
        C904<int> c_int = new C904<int>();
        C904<float> c_float = new C904<float>();
    }
}

public class Test905
{
    interface C905_I0<T>
    { }
    interface C905_I1<T>
    { }
    interface C905_I2<T>
    { }
    interface C905_I3<T>
    { }
    interface C905_I4<T>
    { }
    interface C905_I5<T>
    { }
    interface C905_I6<T>
    { }
    interface C905_I7<T>
    { }

    class C905 : C905_I0<C905_I1<C905_I2<C905_I3<C905_I4<C905_I5<C905_I6<C905_I7<C905>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C905 c = new C905();
    }
}

public class Test907
{
    interface C907_I0<T>
    { }
    interface C907_I1<T>
    { }
    interface C907_I2<T>
    { }
    interface C907_I3<T>
    { }
    interface C907_I4<T>
    { }
    interface C907_I5<T>
    { }
    interface C907_I6<T>
    { }
    interface C907_I7<T>
    { }
    interface C907_I8<T>
    { }

    class C907 : C907_I0<C907>, C907_I1<C907_I2<C907_I3<C907_I4<C907_I5<C907_I6<C907_I7<C907_I8<C907>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C907 c = new C907();
    }
}

public class Test908
{
    interface C908_I0<T>
    { }
    interface C908_I1<T>
    { }
    interface C908_I2<T>
    { }
    interface C908_I3<T>
    { }
    interface C908_I4<T>
    { }
    interface C908_I5<T>
    { }
    interface C908_I6<T>
    { }
    interface C908_I7<T>
    { }
    interface C908_I8<T>
    { }
    interface C908_I9<T>
    { }

    class C908 : C908_I0<C908_I1<C908>>, C908_I2<C908_I3<C908_I4<C908_I5<C908_I6<C908_I7<C908_I8<C908_I9<C908>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C908 c = new C908();
    }
}

public class Test909
{
    interface C909_I0<T>
    { }
    interface C909_I1<T>
    { }
    interface C909_I2<T>
    { }
    interface C909_I3<T>
    { }
    interface C909_I4<T>
    { }
    interface C909_I5<T>
    { }
    interface C909_I6<T>
    { }
    interface C909_I7<T>
    { }
    interface C909_I8<T>
    { }
    interface C909_I9<T>
    { }
    interface C909_I10<T>
    { }

    class C909 : C909_I0<C909_I1<C909_I2<C909>>>, C909_I3<C909_I4<C909_I5<C909_I6<C909_I7<C909_I8<C909_I9<C909_I10<C909>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C909 c = new C909();
    }
}

public class Test910
{
    interface C910_I0<T>
    { }
    interface C910_I1<T>
    { }
    interface C910_I2<T>
    { }
    interface C910_I3<T>
    { }
    interface C910_I4<T>
    { }
    interface C910_I5<T>
    { }
    interface C910_I6<T>
    { }
    interface C910_I7<T>
    { }
    interface C910_I8<T>
    { }
    interface C910_I9<T>
    { }
    interface C910_I10<T>
    { }
    interface C910_I11<T>
    { }

    class C910 : C910_I0<C910_I1<C910_I2<C910_I3<C910>>>>, C910_I4<C910_I5<C910_I6<C910_I7<C910_I8<C910_I9<C910_I10<C910_I11<C910>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C910 c = new C910();
    }
}

public class Test911
{
    interface C911_I0<T>
    { }
    interface C911_I1<T>
    { }
    interface C911_I2<T>
    { }
    interface C911_I3<T>
    { }
    interface C911_I4<T>
    { }
    interface C911_I5<T>
    { }
    interface C911_I6<T>
    { }
    interface C911_I7<T>
    { }
    interface C911_I8<T>
    { }
    interface C911_I9<T>
    { }
    interface C911_I10<T>
    { }
    interface C911_I11<T>
    { }
    interface C911_I12<T>
    { }

    class C911 : C911_I0<C911_I1<C911_I2<C911_I3<C911_I4<C911>>>>>, C911_I5<C911_I6<C911_I7<C911_I8<C911_I9<C911_I10<C911_I11<C911_I12<C911>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C911 c = new C911();
    }
}

public class Test912
{
    interface C912_I0<T>
    { }
    interface C912_I1<T>
    { }
    interface C912_I2<T>
    { }
    interface C912_I3<T>
    { }
    interface C912_I4<T>
    { }
    interface C912_I5<T>
    { }
    interface C912_I6<T>
    { }
    interface C912_I7<T>
    { }
    interface C912_I8<T>
    { }
    interface C912_I9<T>
    { }
    interface C912_I10<T>
    { }
    interface C912_I11<T>
    { }
    interface C912_I12<T>
    { }
    interface C912_I13<T>
    { }

    class C912 : C912_I0<C912_I1<C912_I2<C912_I3<C912_I4<C912_I5<C912>>>>>>, C912_I6<C912_I7<C912_I8<C912_I9<C912_I10<C912_I11<C912_I12<C912_I13<C912>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C912 c = new C912();
    }
}

public class Test913
{
    interface C913_I0<T>
    { }
    interface C913_I1<T>
    { }
    interface C913_I2<T>
    { }
    interface C913_I3<T>
    { }
    interface C913_I4<T>
    { }
    interface C913_I5<T>
    { }
    interface C913_I6<T>
    { }
    interface C913_I7<T>
    { }
    interface C913_I8<T>
    { }
    interface C913_I9<T>
    { }
    interface C913_I10<T>
    { }
    interface C913_I11<T>
    { }
    interface C913_I12<T>
    { }
    interface C913_I13<T>
    { }
    interface C913_I14<T>
    { }

    class C913 : C913_I0<C913_I1<C913_I2<C913_I3<C913_I4<C913_I5<C913_I6<C913>>>>>>>, C913_I7<C913_I8<C913_I9<C913_I10<C913_I11<C913_I12<C913_I13<C913_I14<C913>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C913 c = new C913();
    }
}

public class Test915
{
    interface C915_I0<T>
    { }
    interface C915_I1<T>
    { }
    interface C915_I2<T>
    { }
    interface C915_I3<T>
    { }
    interface C915_I4<T>
    { }
    interface C915_I5<T>
    { }
    interface C915_I6<T>
    { }
    interface C915_I7<T>
    { }
    interface C915_I8<T>
    { }

    class C915 : C915_I0<C915_I1<C915_I2<C915_I3<C915_I4<C915_I5<C915_I6<C915_I7<C915>>>>>>>>, C915_I8<C915>
    { }

    public static void Test_RecursiveInheritance()
    {
        C915 c = new C915();
    }
}

public class Test916
{
    interface C916_I0<T>
    { }
    interface C916_I1<T>
    { }
    interface C916_I2<T>
    { }
    interface C916_I3<T>
    { }
    interface C916_I4<T>
    { }
    interface C916_I5<T>
    { }
    interface C916_I6<T>
    { }
    interface C916_I7<T>
    { }
    interface C916_I8<T>
    { }
    interface C916_I9<T>
    { }

    class C916 : C916_I0<C916_I1<C916_I2<C916_I3<C916_I4<C916_I5<C916_I6<C916_I7<C916>>>>>>>>, C916_I8<C916_I9<C916>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C916 c = new C916();
    }
}

public class Test917
{
    interface C917_I0<T>
    { }
    interface C917_I1<T>
    { }
    interface C917_I2<T>
    { }
    interface C917_I3<T>
    { }
    interface C917_I4<T>
    { }
    interface C917_I5<T>
    { }
    interface C917_I6<T>
    { }
    interface C917_I7<T>
    { }
    interface C917_I8<T>
    { }
    interface C917_I9<T>
    { }
    interface C917_I10<T>
    { }

    class C917 : C917_I0<C917_I1<C917_I2<C917_I3<C917_I4<C917_I5<C917_I6<C917_I7<C917>>>>>>>>, C917_I8<C917_I9<C917_I10<C917>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C917 c = new C917();
    }
}

public class Test918
{
    interface C918_I0<T>
    { }
    interface C918_I1<T>
    { }
    interface C918_I2<T>
    { }
    interface C918_I3<T>
    { }
    interface C918_I4<T>
    { }
    interface C918_I5<T>
    { }
    interface C918_I6<T>
    { }
    interface C918_I7<T>
    { }
    interface C918_I8<T>
    { }
    interface C918_I9<T>
    { }
    interface C918_I10<T>
    { }
    interface C918_I11<T>
    { }

    class C918 : C918_I0<C918_I1<C918_I2<C918_I3<C918_I4<C918_I5<C918_I6<C918_I7<C918>>>>>>>>, C918_I8<C918_I9<C918_I10<C918_I11<C918>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C918 c = new C918();
    }
}

public class Test919
{
    interface C919_I0<T>
    { }
    interface C919_I1<T>
    { }
    interface C919_I2<T>
    { }
    interface C919_I3<T>
    { }
    interface C919_I4<T>
    { }
    interface C919_I5<T>
    { }
    interface C919_I6<T>
    { }
    interface C919_I7<T>
    { }
    interface C919_I8<T>
    { }
    interface C919_I9<T>
    { }
    interface C919_I10<T>
    { }
    interface C919_I11<T>
    { }
    interface C919_I12<T>
    { }

    class C919 : C919_I0<C919_I1<C919_I2<C919_I3<C919_I4<C919_I5<C919_I6<C919_I7<C919>>>>>>>>, C919_I8<C919_I9<C919_I10<C919_I11<C919_I12<C919>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C919 c = new C919();
    }
}

public class Test920
{
    interface C920_I0<T>
    { }
    interface C920_I1<T>
    { }
    interface C920_I2<T>
    { }
    interface C920_I3<T>
    { }
    interface C920_I4<T>
    { }
    interface C920_I5<T>
    { }
    interface C920_I6<T>
    { }
    interface C920_I7<T>
    { }
    interface C920_I8<T>
    { }
    interface C920_I9<T>
    { }
    interface C920_I10<T>
    { }
    interface C920_I11<T>
    { }
    interface C920_I12<T>
    { }
    interface C920_I13<T>
    { }

    class C920 : C920_I0<C920_I1<C920_I2<C920_I3<C920_I4<C920_I5<C920_I6<C920_I7<C920>>>>>>>>, C920_I8<C920_I9<C920_I10<C920_I11<C920_I12<C920_I13<C920>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C920 c = new C920();
    }
}

public class Test921
{
    interface C921_I0<T>
    { }
    interface C921_I1<T>
    { }
    interface C921_I2<T>
    { }
    interface C921_I3<T>
    { }
    interface C921_I4<T>
    { }
    interface C921_I5<T>
    { }
    interface C921_I6<T>
    { }
    interface C921_I7<T>
    { }
    interface C921_I8<T>
    { }
    interface C921_I9<T>
    { }
    interface C921_I10<T>
    { }
    interface C921_I11<T>
    { }
    interface C921_I12<T>
    { }
    interface C921_I13<T>
    { }
    interface C921_I14<T>
    { }

    class C921 : C921_I0<C921_I1<C921_I2<C921_I3<C921_I4<C921_I5<C921_I6<C921_I7<C921>>>>>>>>, C921_I8<C921_I9<C921_I10<C921_I11<C921_I12<C921_I13<C921_I14<C921>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C921 c = new C921();
    }
}

public class Test922
{
    interface C922_I0<T>
    { }
    interface C922_I1<T>
    { }
    interface C922_I2<T>
    { }
    interface C922_I3<T>
    { }
    interface C922_I4<T>
    { }
    interface C922_I5<T>
    { }
    interface C922_I6<T>
    { }
    interface C922_I7<T>
    { }
    interface C922_I8<T>
    { }
    interface C922_I9<T>
    { }
    interface C922_I10<T>
    { }
    interface C922_I11<T>
    { }
    interface C922_I12<T>
    { }
    interface C922_I13<T>
    { }
    interface C922_I14<T>
    { }
    interface C922_I15<T>
    { }

    class C922 : C922_I0<C922_I1<C922_I2<C922_I3<C922_I4<C922_I5<C922_I6<C922_I7<C922>>>>>>>>, C922_I8<C922_I9<C922_I10<C922_I11<C922_I12<C922_I13<C922_I14<C922_I15<C922>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C922 c = new C922();
    }
}

public class Test923
{
    interface C923_I0<T>
    { }
    interface C923_I1<T>
    { }
    interface C923_I2<T>
    { }
    interface C923_I3<T>
    { }
    interface C923_I4<T>
    { }
    interface C923_I5<T>
    { }
    interface C923_I6<T>
    { }
    interface C923_I7<T>
    { }
    interface C923_I8<T>
    { }
    interface C923_I9<T>
    { }
    interface C923_I10<T>
    { }
    interface C923_I11<T>
    { }
    interface C923_I12<T>
    { }
    interface C923_I13<T>
    { }
    interface C923_I14<T>
    { }
    interface C923_I15<T>
    { }
    interface C923_I16<T>
    { }
    interface C923_I17<T>
    { }
    interface C923_I18<T>
    { }
    interface C923_I19<T>
    { }
    interface C923_I20<T>
    { }
    interface C923_I21<T>
    { }

    class C923 : C923_I0<C923_I1<C923_I2<C923_I3<C923_I4<C923_I5<C923_I6<C923_I7<C923>>>>>>>>, C923_I8<C923_I9<C923_I10<C923_I11<C923_I12<C923_I13<C923_I14<C923_I15<C923>>>>>>>>, C923_I16<C923_I17<C923_I18<C923_I19<C923_I20<C923_I21<C923>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C923 c = new C923();
    }
}

public class Test924
{
    interface C924_I0<T>
    { }
    interface C924_I1<T>
    { }
    interface C924_I2<T>
    { }
    interface C924_I3<T>
    { }
    interface C924_I4<T>
    { }
    interface C924_I5<T>
    { }
    interface C924_I6<T>
    { }
    interface C924_I7<T>
    { }
    interface C924_I8<T>
    { }
    interface C924_I9<T>
    { }
    interface C924_I10<T>
    { }
    interface C924_I11<T>
    { }
    interface C924_I12<T>
    { }
    interface C924_I13<T>
    { }
    interface C924_I14<T>
    { }
    interface C924_I15<T>
    { }
    interface C924_I16<T>
    { }
    interface C924_I17<T>
    { }
    interface C924_I18<T>
    { }
    interface C924_I19<T>
    { }

    class C924 : C924_I0<C924_I1<C924_I2<C924_I3<C924_I4<C924_I5<C924_I6<C924_I7<C924>>>>>>>>, C924_I8<C924_I9<C924_I10<C924_I11<C924_I12<C924_I13<C924_I14<C924>>>>>>>, C924_I15<C924_I16<C924_I17<C924_I18<C924_I19<C924>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C924 c = new C924();
    }
}

public class Test925
{
    interface C925_I0<T>
    { }
    interface C925_I1<T>
    { }
    interface C925_I2<T>
    { }
    interface C925_I3<T>
    { }
    interface C925_I4<T>
    { }
    interface C925_I5<T>
    { }
    interface C925_I6<T>
    { }
    interface C925_I7<T>
    { }
    interface C925_I8<T>
    { }
    interface C925_I9<T>
    { }
    interface C925_I10<T>
    { }
    interface C925_I11<T>
    { }
    interface C925_I12<T>
    { }
    interface C925_I13<T>
    { }
    interface C925_I14<T>
    { }
    interface C925_I15<T>
    { }
    interface C925_I16<T>
    { }
    interface C925_I17<T>
    { }
    interface C925_I18<T>
    { }
    interface C925_I19<T>
    { }

    class C925 : C925_I0<C925_I1<C925_I2<C925_I3<C925_I4<C925_I5<C925_I6<C925_I7<C925>>>>>>>>, C925_I8<C925_I9<C925_I10<C925_I11<C925_I12<C925_I13<C925>>>>>>, C925_I14<C925_I15<C925_I16<C925_I17<C925_I18<C925_I19<C925>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C925 c = new C925();
    }
}

public class Test926
{
    interface C926_I0<T>
    { }
    interface C926_I1<T>
    { }
    interface C926_I2<T>
    { }
    interface C926_I3<T>
    { }
    interface C926_I4<T>
    { }
    interface C926_I5<T>
    { }
    interface C926_I6<T>
    { }
    interface C926_I7<T>
    { }
    interface C926_I8<T>
    { }
    interface C926_I9<T>
    { }
    interface C926_I10<T>
    { }
    interface C926_I11<T>
    { }
    interface C926_I12<T>
    { }
    interface C926_I13<T>
    { }

    class C926 : C926_I0<C926_I1<C926_I2<C926_I3<C926_I4<C926_I5<C926_I6<C926_I7<C926>>>>>>>>, C926_I8<C926_I9<C926_I10<C926_I11<C926_I12<C926>>>>>, C926_I13<C926>
    { }

    public static void Test_RecursiveInheritance()
    {
        C926 c = new C926();
    }
}

public class Test928
{
    interface C928_I0<T>
    { }
    interface C928_I1<T>
    { }
    interface C928_I2<T>
    { }
    interface C928_I3<T>
    { }
    interface C928_I4<T>
    { }
    interface C928_I5<T>
    { }
    interface C928_I6<T>
    { }
    interface C928_I7<T>
    { }
    interface C928_I8<T>
    { }
    interface C928_I9<T>
    { }
    interface C928_I10<T>
    { }
    interface C928_I11<T>
    { }
    interface C928_I12<T>
    { }
    interface C928_I13<T>
    { }
    interface C928_I14<T>
    { }
    interface C928_I15<T>
    { }

    class C928 : C928_I0<C928_I1<C928_I2<C928_I3<C928_I4<C928_I5<C928_I6<C928_I7<C928>>>>>>>>, C928_I8<C928_I9<C928_I10<C928>>>, C928_I11<C928_I12<C928_I13<C928_I14<C928_I15<C928>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C928 c = new C928();
    }
}

public class Test929
{
    interface C929_I0<T>
    { }
    interface C929_I1<T>
    { }
    interface C929_I2<T>
    { }
    interface C929_I3<T>
    { }
    interface C929_I4<T>
    { }
    interface C929_I5<T>
    { }
    interface C929_I6<T>
    { }
    interface C929_I7<T>
    { }
    interface C929_I8<T>
    { }
    interface C929_I9<T>
    { }
    interface C929_I10<T>
    { }
    interface C929_I11<T>
    { }
    interface C929_I12<T>
    { }
    interface C929_I13<T>
    { }
    interface C929_I14<T>
    { }
    interface C929_I15<T>
    { }

    class C929 : C929_I0<C929_I1<C929_I2<C929_I3<C929_I4<C929_I5<C929_I6<C929_I7<C929>>>>>>>>, C929_I8<C929_I9<C929>>, C929_I10<C929_I11<C929_I12<C929_I13<C929_I14<C929_I15<C929>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C929 c = new C929();
    }
}

public class Test930
{
    interface C930_I0<T>
    { }
    interface C930_I1<T>
    { }
    interface C930_I2<T>
    { }
    interface C930_I3<T>
    { }
    interface C930_I4<T>
    { }
    interface C930_I5<T>
    { }
    interface C930_I6<T>
    { }
    interface C930_I7<T>
    { }
    interface C930_I8<T>
    { }
    interface C930_I9<T>
    { }
    interface C930_I10<T>
    { }
    interface C930_I11<T>
    { }

    class C930 : C930_I0<C930_I1<C930_I2<C930_I3<C930_I4<C930_I5<C930_I6<C930_I7<C930>>>>>>>>, C930_I8<C930>, C930_I9<C930_I10<C930_I11<C930>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C930 c = new C930();
    }
}

public class Test932
{
    interface C932_I0<T>
    { }
    interface C932_I1<T>
    { }
    interface C932_I2<T>
    { }
    interface C932_I3<T>
    { }
    interface C932_I4<T>
    { }
    interface C932_I5<T>
    { }
    interface C932_I6<T>
    { }
    interface C932_I7<T>
    { }
    interface C932_I8<T>
    { }
    interface C932_I9<T>
    { }
    interface C932_I10<T>
    { }
    interface C932_I11<T>
    { }
    interface C932_I12<T>
    { }
    interface C932_I13<T>
    { }
    interface C932_I14<T>
    { }
    interface C932_I15<T>
    { }
    interface C932_I16<T>
    { }
    interface C932_I17<T>
    { }
    interface C932_I18<T>
    { }

    class C932 : C932_I0<C932_I1<C932_I2<C932_I3<C932_I4<C932_I5<C932_I6<C932>>>>>>>, C932_I7<C932_I8<C932_I9<C932_I10<C932_I11<C932_I12<C932_I13<C932_I14<C932>>>>>>>>, C932_I15<C932_I16<C932_I17<C932_I18<C932>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C932 c = new C932();
    }
}

public class Test933
{
    interface C933_I0<T>
    { }
    interface C933_I1<T>
    { }
    interface C933_I2<T>
    { }
    interface C933_I3<T>
    { }
    interface C933_I4<T>
    { }
    interface C933_I5<T>
    { }
    interface C933_I6<T>
    { }
    interface C933_I7<T>
    { }
    interface C933_I8<T>
    { }
    interface C933_I9<T>
    { }
    interface C933_I10<T>
    { }
    interface C933_I11<T>
    { }
    interface C933_I12<T>
    { }
    interface C933_I13<T>
    { }
    interface C933_I14<T>
    { }
    interface C933_I15<T>
    { }
    interface C933_I16<T>
    { }
    interface C933_I17<T>
    { }
    interface C933_I18<T>
    { }

    class C933 : C933_I0<C933_I1<C933_I2<C933_I3<C933_I4<C933_I5<C933>>>>>>, C933_I6<C933_I7<C933_I8<C933_I9<C933_I10<C933_I11<C933_I12<C933_I13<C933>>>>>>>>, C933_I14<C933_I15<C933_I16<C933_I17<C933_I18<C933>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C933 c = new C933();
    }
}

public class Test934
{
    interface C934_I0<T>
    { }
    interface C934_I1<T>
    { }
    interface C934_I2<T>
    { }
    interface C934_I3<T>
    { }
    interface C934_I4<T>
    { }
    interface C934_I5<T>
    { }
    interface C934_I6<T>
    { }
    interface C934_I7<T>
    { }
    interface C934_I8<T>
    { }
    interface C934_I9<T>
    { }
    interface C934_I10<T>
    { }
    interface C934_I11<T>
    { }
    interface C934_I12<T>
    { }
    interface C934_I13<T>
    { }

    class C934 : C934_I0<C934_I1<C934_I2<C934_I3<C934_I4<C934>>>>>, C934_I5<C934_I6<C934_I7<C934_I8<C934_I9<C934_I10<C934_I11<C934_I12<C934>>>>>>>>, C934_I13<C934>
    { }

    public static void Test_RecursiveInheritance()
    {
        C934 c = new C934();
    }
}

public class Test935
{
    interface C935_I0<T>
    { }
    interface C935_I1<T>
    { }
    interface C935_I2<T>
    { }
    interface C935_I3<T>
    { }
    interface C935_I4<T>
    { }
    interface C935_I5<T>
    { }
    interface C935_I6<T>
    { }
    interface C935_I7<T>
    { }
    interface C935_I8<T>
    { }
    interface C935_I9<T>
    { }
    interface C935_I10<T>
    { }
    interface C935_I11<T>
    { }
    interface C935_I12<T>
    { }
    interface C935_I13<T>
    { }
    interface C935_I14<T>
    { }
    interface C935_I15<T>
    { }
    interface C935_I16<T>
    { }
    interface C935_I17<T>
    { }
    interface C935_I18<T>
    { }

    class C935 : C935_I0<C935_I1<C935_I2<C935_I3<C935>>>>, C935_I4<C935_I5<C935_I6<C935_I7<C935_I8<C935_I9<C935_I10<C935_I11<C935>>>>>>>>, C935_I12<C935_I13<C935_I14<C935_I15<C935_I16<C935_I17<C935_I18<C935>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C935 c = new C935();
    }
}

public class Test936
{
    interface C936_I0<T>
    { }
    interface C936_I1<T>
    { }
    interface C936_I2<T>
    { }
    interface C936_I3<T>
    { }
    interface C936_I4<T>
    { }
    interface C936_I5<T>
    { }
    interface C936_I6<T>
    { }
    interface C936_I7<T>
    { }
    interface C936_I8<T>
    { }
    interface C936_I9<T>
    { }
    interface C936_I10<T>
    { }
    interface C936_I11<T>
    { }
    interface C936_I12<T>
    { }
    interface C936_I13<T>
    { }

    class C936 : C936_I0<C936_I1<C936_I2<C936>>>, C936_I3<C936_I4<C936_I5<C936_I6<C936_I7<C936_I8<C936_I9<C936_I10<C936>>>>>>>>, C936_I11<C936_I12<C936_I13<C936>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C936 c = new C936();
    }
}

public class Test937
{
    interface C937_I0<T>
    { }
    interface C937_I1<T>
    { }
    interface C937_I2<T>
    { }
    interface C937_I3<T>
    { }
    interface C937_I4<T>
    { }
    interface C937_I5<T>
    { }
    interface C937_I6<T>
    { }
    interface C937_I7<T>
    { }
    interface C937_I8<T>
    { }
    interface C937_I9<T>
    { }
    interface C937_I10<T>
    { }
    interface C937_I11<T>
    { }
    interface C937_I12<T>
    { }
    interface C937_I13<T>
    { }

    class C937 : C937_I0<C937_I1<C937>>, C937_I2<C937_I3<C937_I4<C937_I5<C937_I6<C937_I7<C937_I8<C937_I9<C937>>>>>>>>, C937_I10<C937_I11<C937_I12<C937_I13<C937>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C937 c = new C937();
    }
}

public class Test938
{
    interface C938_I0<T>
    { }
    interface C938_I1<T>
    { }
    interface C938_I2<T>
    { }
    interface C938_I3<T>
    { }
    interface C938_I4<T>
    { }
    interface C938_I5<T>
    { }
    interface C938_I6<T>
    { }
    interface C938_I7<T>
    { }
    interface C938_I8<T>
    { }
    interface C938_I9<T>
    { }
    interface C938_I10<T>
    { }

    class C938 : C938_I0<C938>, C938_I1<C938_I2<C938_I3<C938_I4<C938_I5<C938_I6<C938_I7<C938_I8<C938>>>>>>>>, C938_I9<C938_I10<C938>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C938 c = new C938();
    }
}


public class Test950
{
    interface C950_I0<T>
    { }
    interface C950_I1<T>
    { }
    interface C950_I2<T>
    { }
    interface C950_I3<T>
    { }
    interface C950_I4<T>
    { }
    interface C950_I5<T>
    { }
    interface C950_I6<T>
    { }
    interface C950_I7<T>
    { }
    interface C950_I8<T>
    { }
    interface C950_I9<T>
    { }

    class C950 : C950_I0<C950>, C950_I1<C950_I2<C950_I3<C950_I4<C950_I5<C950_I6<C950_I7<C950_I8<C950>>>>>>>>, C950_I9<C950>
    { }

    public static void Test_RecursiveInheritance()
    {
        C950 c = new C950();
    }
}

public class Test951
{
    interface C951_I0<T>
    { }
    interface C951_I1<T>
    { }
    interface C951_I2<T>
    { }
    interface C951_I3<T>
    { }
    interface C951_I4<T>
    { }
    interface C951_I5<T>
    { }
    interface C951_I6<T>
    { }
    interface C951_I7<T>
    { }
    interface C951_I8<T>
    { }
    interface C951_I9<T>
    { }
    interface C951_I10<T>
    { }

    class C951 : C951_I0<C951>, C951_I1<C951_I2<C951_I3<C951_I4<C951_I5<C951_I6<C951_I7<C951_I8<C951>>>>>>>>, C951_I9<C951_I10<C951>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C951 c = new C951();
    }
}

public class Test952
{
    interface C952_I0<T>
    { }
    interface C952_I1<T>
    { }
    interface C952_I2<T>
    { }
    interface C952_I3<T>
    { }
    interface C952_I4<T>
    { }
    interface C952_I5<T>
    { }
    interface C952_I6<T>
    { }
    interface C952_I7<T>
    { }
    interface C952_I8<T>
    { }
    interface C952_I9<T>
    { }
    interface C952_I10<T>
    { }
    interface C952_I11<T>
    { }

    class C952 : C952_I0<C952>, C952_I1<C952_I2<C952_I3<C952_I4<C952_I5<C952_I6<C952_I7<C952_I8<C952>>>>>>>>, C952_I9<C952_I10<C952_I11<C952>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C952 c = new C952();
    }
}

public class Test953
{
    interface C953_I0<T>
    { }
    interface C953_I1<T>
    { }
    interface C953_I2<T>
    { }
    interface C953_I3<T>
    { }
    interface C953_I4<T>
    { }
    interface C953_I5<T>
    { }
    interface C953_I6<T>
    { }
    interface C953_I7<T>
    { }
    interface C953_I8<T>
    { }
    interface C953_I9<T>
    { }
    interface C953_I10<T>
    { }
    interface C953_I11<T>
    { }
    interface C953_I12<T>
    { }

    class C953 : C953_I0<C953>, C953_I1<C953_I2<C953_I3<C953_I4<C953_I5<C953_I6<C953_I7<C953_I8<C953>>>>>>>>, C953_I9<C953_I10<C953_I11<C953_I12<C953>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C953 c = new C953();
    }
}

public class Test954
{
    interface C954_I0<T>
    { }
    interface C954_I1<T>
    { }
    interface C954_I2<T>
    { }
    interface C954_I3<T>
    { }
    interface C954_I4<T>
    { }
    interface C954_I5<T>
    { }
    interface C954_I6<T>
    { }
    interface C954_I7<T>
    { }
    interface C954_I8<T>
    { }
    interface C954_I9<T>
    { }
    interface C954_I10<T>
    { }
    interface C954_I11<T>
    { }
    interface C954_I12<T>
    { }
    interface C954_I13<T>
    { }

    class C954 : C954_I0<C954>, C954_I1<C954_I2<C954_I3<C954_I4<C954_I5<C954_I6<C954_I7<C954_I8<C954>>>>>>>>, C954_I9<C954_I10<C954_I11<C954_I12<C954_I13<C954>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C954 c = new C954();
    }
}

public class Test955
{
    interface C955_I0<T>
    { }
    interface C955_I1<T>
    { }
    interface C955_I2<T>
    { }
    interface C955_I3<T>
    { }
    interface C955_I4<T>
    { }
    interface C955_I5<T>
    { }
    interface C955_I6<T>
    { }
    interface C955_I7<T>
    { }
    interface C955_I8<T>
    { }
    interface C955_I9<T>
    { }
    interface C955_I10<T>
    { }
    interface C955_I11<T>
    { }
    interface C955_I12<T>
    { }
    interface C955_I13<T>
    { }
    interface C955_I14<T>
    { }

    class C955 : C955_I0<C955>, C955_I1<C955_I2<C955_I3<C955_I4<C955_I5<C955_I6<C955_I7<C955_I8<C955>>>>>>>>, C955_I9<C955_I10<C955_I11<C955_I12<C955_I13<C955_I14<C955>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C955 c = new C955();
    }
}

public class Test956
{
    interface C956_I0<T>
    { }
    interface C956_I1<T>
    { }
    interface C956_I2<T>
    { }
    interface C956_I3<T>
    { }
    interface C956_I4<T>
    { }
    interface C956_I5<T>
    { }
    interface C956_I6<T>
    { }
    interface C956_I7<T>
    { }
    interface C956_I8<T>
    { }
    interface C956_I9<T>
    { }
    interface C956_I10<T>
    { }
    interface C956_I11<T>
    { }
    interface C956_I12<T>
    { }
    interface C956_I13<T>
    { }
    interface C956_I14<T>
    { }
    interface C956_I15<T>
    { }

    class C956 : C956_I0<C956>, C956_I1<C956_I2<C956_I3<C956_I4<C956_I5<C956_I6<C956_I7<C956_I8<C956>>>>>>>>, C956_I9<C956_I10<C956_I11<C956_I12<C956_I13<C956_I14<C956_I15<C956>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C956 c = new C956();
    }
}

public class Test957
{
    interface C957_I0<T>
    { }
    interface C957_I1<T>
    { }
    interface C957_I2<T>
    { }
    interface C957_I3<T>
    { }
    interface C957_I4<T>
    { }
    interface C957_I5<T>
    { }
    interface C957_I6<T>
    { }
    interface C957_I7<T>
    { }
    interface C957_I8<T>
    { }
    interface C957_I9<T>
    { }

    class C957 : C957_I0<C957>, C957_I1<C957>, C957_I2<C957_I3<C957_I4<C957_I5<C957_I6<C957_I7<C957_I8<C957_I9<C957>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C957 c = new C957();
    }
}

public class Test959
{
    interface C959_I0<T>
    { }
    interface C959_I1<T>
    { }
    interface C959_I2<T>
    { }
    interface C959_I3<T>
    { }
    interface C959_I4<T>
    { }
    interface C959_I5<T>
    { }
    interface C959_I6<T>
    { }
    interface C959_I7<T>
    { }
    interface C959_I8<T>
    { }
    interface C959_I9<T>
    { }
    interface C959_I10<T>
    { }

    class C959 : C959_I0<C959_I1<C959>>, C959_I2<C959_I3<C959_I4<C959_I5<C959_I6<C959_I7<C959_I8<C959_I9<C959>>>>>>>>, C959_I10<C959>
    { }

    public static void Test_RecursiveInheritance()
    {
        C959 c = new C959();
    }
}

public class Test960
{
    interface C960_I0<T>
    { }
    interface C960_I1<T>
    { }
    interface C960_I2<T>
    { }
    interface C960_I3<T>
    { }
    interface C960_I4<T>
    { }
    interface C960_I5<T>
    { }
    interface C960_I6<T>
    { }
    interface C960_I7<T>
    { }
    interface C960_I8<T>
    { }
    interface C960_I9<T>
    { }
    interface C960_I10<T>
    { }
    interface C960_I11<T>
    { }

    class C960 : C960_I0<C960_I1<C960>>, C960_I2<C960_I3<C960_I4<C960_I5<C960_I6<C960_I7<C960_I8<C960_I9<C960>>>>>>>>, C960_I10<C960_I11<C960>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C960 c = new C960();
    }
}

public class Test961
{
    interface C961_I0<T>
    { }
    interface C961_I1<T>
    { }
    interface C961_I2<T>
    { }
    interface C961_I3<T>
    { }
    interface C961_I4<T>
    { }
    interface C961_I5<T>
    { }
    interface C961_I6<T>
    { }
    interface C961_I7<T>
    { }
    interface C961_I8<T>
    { }
    interface C961_I9<T>
    { }
    interface C961_I10<T>
    { }
    interface C961_I11<T>
    { }
    interface C961_I12<T>
    { }

    class C961 : C961_I0<C961_I1<C961>>, C961_I2<C961_I3<C961_I4<C961_I5<C961_I6<C961_I7<C961_I8<C961_I9<C961>>>>>>>>, C961_I10<C961_I11<C961_I12<C961>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C961 c = new C961();
    }
}

public class Test962
{
    interface C962_I0<T>
    { }
    interface C962_I1<T>
    { }
    interface C962_I2<T>
    { }
    interface C962_I3<T>
    { }
    interface C962_I4<T>
    { }
    interface C962_I5<T>
    { }
    interface C962_I6<T>
    { }
    interface C962_I7<T>
    { }
    interface C962_I8<T>
    { }
    interface C962_I9<T>
    { }
    interface C962_I10<T>
    { }
    interface C962_I11<T>
    { }
    interface C962_I12<T>
    { }
    interface C962_I13<T>
    { }

    class C962 : C962_I0<C962_I1<C962>>, C962_I2<C962_I3<C962_I4<C962_I5<C962_I6<C962_I7<C962_I8<C962_I9<C962>>>>>>>>, C962_I10<C962_I11<C962_I12<C962_I13<C962>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C962 c = new C962();
    }
}

public class Test963
{
    interface C963_I0<T>
    { }
    interface C963_I1<T>
    { }
    interface C963_I2<T>
    { }
    interface C963_I3<T>
    { }
    interface C963_I4<T>
    { }
    interface C963_I5<T>
    { }
    interface C963_I6<T>
    { }
    interface C963_I7<T>
    { }
    interface C963_I8<T>
    { }
    interface C963_I9<T>
    { }
    interface C963_I10<T>
    { }
    interface C963_I11<T>
    { }
    interface C963_I12<T>
    { }
    interface C963_I13<T>
    { }
    interface C963_I14<T>
    { }

    class C963 : C963_I0<C963_I1<C963>>, C963_I2<C963_I3<C963_I4<C963_I5<C963_I6<C963_I7<C963_I8<C963_I9<C963>>>>>>>>, C963_I10<C963_I11<C963_I12<C963_I13<C963_I14<C963>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C963 c = new C963();
    }
}

public class Test964
{
    interface C964_I0<T>
    { }
    interface C964_I1<T>
    { }
    interface C964_I2<T>
    { }
    interface C964_I3<T>
    { }
    interface C964_I4<T>
    { }
    interface C964_I5<T>
    { }
    interface C964_I6<T>
    { }
    interface C964_I7<T>
    { }
    interface C964_I8<T>
    { }
    interface C964_I9<T>
    { }
    interface C964_I10<T>
    { }
    interface C964_I11<T>
    { }
    interface C964_I12<T>
    { }
    interface C964_I13<T>
    { }
    interface C964_I14<T>
    { }
    interface C964_I15<T>
    { }

    class C964 : C964_I0<C964_I1<C964>>, C964_I2<C964_I3<C964_I4<C964_I5<C964_I6<C964_I7<C964_I8<C964_I9<C964>>>>>>>>, C964_I10<C964_I11<C964_I12<C964_I13<C964_I14<C964_I15<C964>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C964 c = new C964();
    }
}

public class Test965
{
    interface C965_I0<T>
    { }
    interface C965_I1<T>
    { }
    interface C965_I2<T>
    { }
    interface C965_I3<T>
    { }
    interface C965_I4<T>
    { }
    interface C965_I5<T>
    { }
    interface C965_I6<T>
    { }
    interface C965_I7<T>
    { }
    interface C965_I8<T>
    { }
    interface C965_I9<T>
    { }
    interface C965_I10<T>
    { }
    interface C965_I11<T>
    { }
    interface C965_I12<T>
    { }
    interface C965_I13<T>
    { }
    interface C965_I14<T>
    { }
    interface C965_I15<T>
    { }
    interface C965_I16<T>
    { }

    class C965 : C965_I0<C965_I1<C965>>, C965_I2<C965_I3<C965_I4<C965_I5<C965_I6<C965_I7<C965_I8<C965_I9<C965>>>>>>>>, C965_I10<C965_I11<C965_I12<C965_I13<C965_I14<C965_I15<C965_I16<C965>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C965 c = new C965();
    }
}

public class Test966
{
    interface C966_I0<T>
    { }
    interface C966_I1<T>
    { }
    interface C966_I2<T>
    { }
    interface C966_I3<T>
    { }
    interface C966_I4<T>
    { }
    interface C966_I5<T>
    { }
    interface C966_I6<T>
    { }
    interface C966_I7<T>
    { }
    interface C966_I8<T>
    { }
    interface C966_I9<T>
    { }
    interface C966_I10<T>
    { }
    interface C966_I11<T>
    { }

    class C966 : C966_I0<C966_I1<C966>>, C966_I2<C966_I3<C966>>, C966_I4<C966_I5<C966_I6<C966_I7<C966_I8<C966_I9<C966_I10<C966_I11<C966>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C966 c = new C966();
    }
}

public class Test968
{
    interface C968_I0<T>
    { }
    interface C968_I1<T>
    { }
    interface C968_I2<T>
    { }
    interface C968_I3<T>
    { }
    interface C968_I4<T>
    { }
    interface C968_I5<T>
    { }
    interface C968_I6<T>
    { }
    interface C968_I7<T>
    { }
    interface C968_I8<T>
    { }
    interface C968_I9<T>
    { }
    interface C968_I10<T>
    { }
    interface C968_I11<T>
    { }

    class C968 : C968_I0<C968_I1<C968_I2<C968>>>, C968_I3<C968_I4<C968_I5<C968_I6<C968_I7<C968_I8<C968_I9<C968_I10<C968>>>>>>>>, C968_I11<C968>
    { }

    public static void Test_RecursiveInheritance()
    {
        C968 c = new C968();
    }
}

public class Test969
{
    interface C969_I0<T>
    { }
    interface C969_I1<T>
    { }
    interface C969_I2<T>
    { }
    interface C969_I3<T>
    { }
    interface C969_I4<T>
    { }
    interface C969_I5<T>
    { }
    interface C969_I6<T>
    { }
    interface C969_I7<T>
    { }
    interface C969_I8<T>
    { }
    interface C969_I9<T>
    { }
    interface C969_I10<T>
    { }
    interface C969_I11<T>
    { }
    interface C969_I12<T>
    { }

    class C969 : C969_I0<C969_I1<C969_I2<C969>>>, C969_I3<C969_I4<C969_I5<C969_I6<C969_I7<C969_I8<C969_I9<C969_I10<C969>>>>>>>>, C969_I11<C969_I12<C969>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C969 c = new C969();
    }
}

public class Test970
{
    interface C970_I0<T>
    { }
    interface C970_I1<T>
    { }
    interface C970_I2<T>
    { }
    interface C970_I3<T>
    { }
    interface C970_I4<T>
    { }
    interface C970_I5<T>
    { }
    interface C970_I6<T>
    { }
    interface C970_I7<T>
    { }
    interface C970_I8<T>
    { }
    interface C970_I9<T>
    { }
    interface C970_I10<T>
    { }
    interface C970_I11<T>
    { }
    interface C970_I12<T>
    { }
    interface C970_I13<T>
    { }

    class C970 : C970_I0<C970_I1<C970_I2<C970>>>, C970_I3<C970_I4<C970_I5<C970_I6<C970_I7<C970_I8<C970_I9<C970_I10<C970>>>>>>>>, C970_I11<C970_I12<C970_I13<C970>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C970 c = new C970();
    }
}

public class Test971
{
    interface C971_I0<T>
    { }
    interface C971_I1<T>
    { }
    interface C971_I2<T>
    { }
    interface C971_I3<T>
    { }
    interface C971_I4<T>
    { }
    interface C971_I5<T>
    { }
    interface C971_I6<T>
    { }
    interface C971_I7<T>
    { }
    interface C971_I8<T>
    { }
    interface C971_I9<T>
    { }
    interface C971_I10<T>
    { }
    interface C971_I11<T>
    { }
    interface C971_I12<T>
    { }
    interface C971_I13<T>
    { }
    interface C971_I14<T>
    { }

    class C971 : C971_I0<C971_I1<C971_I2<C971>>>, C971_I3<C971_I4<C971_I5<C971_I6<C971_I7<C971_I8<C971_I9<C971_I10<C971>>>>>>>>, C971_I11<C971_I12<C971_I13<C971_I14<C971>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C971 c = new C971();
    }
}

public class Test972
{
    interface C972_I0<T>
    { }
    interface C972_I1<T>
    { }
    interface C972_I2<T>
    { }
    interface C972_I3<T>
    { }
    interface C972_I4<T>
    { }
    interface C972_I5<T>
    { }
    interface C972_I6<T>
    { }
    interface C972_I7<T>
    { }
    interface C972_I8<T>
    { }
    interface C972_I9<T>
    { }
    interface C972_I10<T>
    { }
    interface C972_I11<T>
    { }
    interface C972_I12<T>
    { }
    interface C972_I13<T>
    { }
    interface C972_I14<T>
    { }
    interface C972_I15<T>
    { }

    class C972 : C972_I0<C972_I1<C972_I2<C972>>>, C972_I3<C972_I4<C972_I5<C972_I6<C972_I7<C972_I8<C972_I9<C972_I10<C972>>>>>>>>, C972_I11<C972_I12<C972_I13<C972_I14<C972_I15<C972>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C972 c = new C972();
    }
}

public class Test973
{
    interface C973_I0<T>
    { }
    interface C973_I1<T>
    { }
    interface C973_I2<T>
    { }
    interface C973_I3<T>
    { }
    interface C973_I4<T>
    { }
    interface C973_I5<T>
    { }
    interface C973_I6<T>
    { }
    interface C973_I7<T>
    { }
    interface C973_I8<T>
    { }
    interface C973_I9<T>
    { }
    interface C973_I10<T>
    { }
    interface C973_I11<T>
    { }
    interface C973_I12<T>
    { }
    interface C973_I13<T>
    { }
    interface C973_I14<T>
    { }
    interface C973_I15<T>
    { }
    interface C973_I16<T>
    { }

    class C973 : C973_I0<C973_I1<C973_I2<C973>>>, C973_I3<C973_I4<C973_I5<C973_I6<C973_I7<C973_I8<C973_I9<C973_I10<C973>>>>>>>>, C973_I11<C973_I12<C973_I13<C973_I14<C973_I15<C973_I16<C973>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C973 c = new C973();
    }
}

public class Test974
{
    interface C974_I0<T>
    { }
    interface C974_I1<T>
    { }
    interface C974_I2<T>
    { }
    interface C974_I3<T>
    { }
    interface C974_I4<T>
    { }
    interface C974_I5<T>
    { }
    interface C974_I6<T>
    { }
    interface C974_I7<T>
    { }
    interface C974_I8<T>
    { }
    interface C974_I9<T>
    { }
    interface C974_I10<T>
    { }
    interface C974_I11<T>
    { }
    interface C974_I12<T>
    { }
    interface C974_I13<T>
    { }
    interface C974_I14<T>
    { }
    interface C974_I15<T>
    { }
    interface C974_I16<T>
    { }
    interface C974_I17<T>
    { }

    class C974 : C974_I0<C974_I1<C974_I2<C974>>>, C974_I3<C974_I4<C974_I5<C974_I6<C974_I7<C974_I8<C974_I9<C974_I10<C974>>>>>>>>, C974_I11<C974_I12<C974_I13<C974_I14<C974_I15<C974_I16<C974_I17<C974>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C974 c = new C974();
    }
}

public class Test975
{
    interface C975_I0<T>
    { }
    interface C975_I1<T>
    { }
    interface C975_I2<T>
    { }
    interface C975_I3<T>
    { }
    interface C975_I4<T>
    { }
    interface C975_I5<T>
    { }
    interface C975_I6<T>
    { }
    interface C975_I7<T>
    { }
    interface C975_I8<T>
    { }
    interface C975_I9<T>
    { }
    interface C975_I10<T>
    { }
    interface C975_I11<T>
    { }
    interface C975_I12<T>
    { }
    interface C975_I13<T>
    { }

    class C975 : C975_I0<C975_I1<C975_I2<C975>>>, C975_I3<C975_I4<C975_I5<C975>>>, C975_I6<C975_I7<C975_I8<C975_I9<C975_I10<C975_I11<C975_I12<C975_I13<C975>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C975 c = new C975();
    }
}

public class Test977
{
    interface C977_I0<T>
    { }
    interface C977_I1<T>
    { }
    interface C977_I2<T>
    { }
    interface C977_I3<T>
    { }
    interface C977_I4<T>
    { }
    interface C977_I5<T>
    { }
    interface C977_I6<T>
    { }
    interface C977_I7<T>
    { }
    interface C977_I8<T>
    { }
    interface C977_I9<T>
    { }
    interface C977_I10<T>
    { }
    interface C977_I11<T>
    { }
    interface C977_I12<T>
    { }

    class C977 : C977_I0<C977_I1<C977_I2<C977_I3<C977>>>>, C977_I4<C977_I5<C977_I6<C977_I7<C977_I8<C977_I9<C977_I10<C977_I11<C977>>>>>>>>, C977_I12<C977>
    { }

    public static void Test_RecursiveInheritance()
    {
        C977 c = new C977();
    }
}

public class Test978
{
    interface C978_I0<T>
    { }
    interface C978_I1<T>
    { }
    interface C978_I2<T>
    { }
    interface C978_I3<T>
    { }
    interface C978_I4<T>
    { }
    interface C978_I5<T>
    { }
    interface C978_I6<T>
    { }
    interface C978_I7<T>
    { }
    interface C978_I8<T>
    { }
    interface C978_I9<T>
    { }
    interface C978_I10<T>
    { }
    interface C978_I11<T>
    { }
    interface C978_I12<T>
    { }
    interface C978_I13<T>
    { }

    class C978 : C978_I0<C978_I1<C978_I2<C978_I3<C978>>>>, C978_I4<C978_I5<C978_I6<C978_I7<C978_I8<C978_I9<C978_I10<C978_I11<C978>>>>>>>>, C978_I12<C978_I13<C978>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C978 c = new C978();
    }
}

public class Test979
{
    interface C979_I0<T>
    { }
    interface C979_I1<T>
    { }
    interface C979_I2<T>
    { }
    interface C979_I3<T>
    { }
    interface C979_I4<T>
    { }
    interface C979_I5<T>
    { }
    interface C979_I6<T>
    { }
    interface C979_I7<T>
    { }
    interface C979_I8<T>
    { }
    interface C979_I9<T>
    { }
    interface C979_I10<T>
    { }
    interface C979_I11<T>
    { }
    interface C979_I12<T>
    { }
    interface C979_I13<T>
    { }
    interface C979_I14<T>
    { }

    class C979 : C979_I0<C979_I1<C979_I2<C979_I3<C979>>>>, C979_I4<C979_I5<C979_I6<C979_I7<C979_I8<C979_I9<C979_I10<C979_I11<C979>>>>>>>>, C979_I12<C979_I13<C979_I14<C979>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C979 c = new C979();
    }
}

public class Test980
{
    interface C980_I0<T>
    { }
    interface C980_I1<T>
    { }
    interface C980_I2<T>
    { }
    interface C980_I3<T>
    { }
    interface C980_I4<T>
    { }
    interface C980_I5<T>
    { }
    interface C980_I6<T>
    { }
    interface C980_I7<T>
    { }
    interface C980_I8<T>
    { }
    interface C980_I9<T>
    { }
    interface C980_I10<T>
    { }
    interface C980_I11<T>
    { }
    interface C980_I12<T>
    { }
    interface C980_I13<T>
    { }
    interface C980_I14<T>
    { }
    interface C980_I15<T>
    { }

    class C980 : C980_I0<C980_I1<C980_I2<C980_I3<C980>>>>, C980_I4<C980_I5<C980_I6<C980_I7<C980_I8<C980_I9<C980_I10<C980_I11<C980>>>>>>>>, C980_I12<C980_I13<C980_I14<C980_I15<C980>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C980 c = new C980();
    }
}

public class Test981
{
    interface C981_I0<T>
    { }
    interface C981_I1<T>
    { }
    interface C981_I2<T>
    { }
    interface C981_I3<T>
    { }
    interface C981_I4<T>
    { }
    interface C981_I5<T>
    { }
    interface C981_I6<T>
    { }
    interface C981_I7<T>
    { }
    interface C981_I8<T>
    { }
    interface C981_I9<T>
    { }
    interface C981_I10<T>
    { }
    interface C981_I11<T>
    { }
    interface C981_I12<T>
    { }
    interface C981_I13<T>
    { }
    interface C981_I14<T>
    { }
    interface C981_I15<T>
    { }
    interface C981_I16<T>
    { }

    class C981 : C981_I0<C981_I1<C981_I2<C981_I3<C981>>>>, C981_I4<C981_I5<C981_I6<C981_I7<C981_I8<C981_I9<C981_I10<C981_I11<C981>>>>>>>>, C981_I12<C981_I13<C981_I14<C981_I15<C981_I16<C981>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C981 c = new C981();
    }
}

public class Test982
{
    interface C982_I0<T>
    { }
    interface C982_I1<T>
    { }
    interface C982_I2<T>
    { }
    interface C982_I3<T>
    { }
    interface C982_I4<T>
    { }
    interface C982_I5<T>
    { }
    interface C982_I6<T>
    { }
    interface C982_I7<T>
    { }
    interface C982_I8<T>
    { }
    interface C982_I9<T>
    { }
    interface C982_I10<T>
    { }
    interface C982_I11<T>
    { }
    interface C982_I12<T>
    { }
    interface C982_I13<T>
    { }
    interface C982_I14<T>
    { }
    interface C982_I15<T>
    { }
    interface C982_I16<T>
    { }
    interface C982_I17<T>
    { }

    class C982 : C982_I0<C982_I1<C982_I2<C982_I3<C982>>>>, C982_I4<C982_I5<C982_I6<C982_I7<C982_I8<C982_I9<C982_I10<C982_I11<C982>>>>>>>>, C982_I12<C982_I13<C982_I14<C982_I15<C982_I16<C982_I17<C982>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C982 c = new C982();
    }
}

public class Test983
{
    interface C983_I0<T>
    { }
    interface C983_I1<T>
    { }
    interface C983_I2<T>
    { }
    interface C983_I3<T>
    { }
    interface C983_I4<T>
    { }
    interface C983_I5<T>
    { }
    interface C983_I6<T>
    { }
    interface C983_I7<T>
    { }
    interface C983_I8<T>
    { }
    interface C983_I9<T>
    { }
    interface C983_I10<T>
    { }
    interface C983_I11<T>
    { }
    interface C983_I12<T>
    { }
    interface C983_I13<T>
    { }
    interface C983_I14<T>
    { }
    interface C983_I15<T>
    { }
    interface C983_I16<T>
    { }
    interface C983_I17<T>
    { }
    interface C983_I18<T>
    { }

    class C983 : C983_I0<C983_I1<C983_I2<C983_I3<C983>>>>, C983_I4<C983_I5<C983_I6<C983_I7<C983_I8<C983_I9<C983_I10<C983_I11<C983>>>>>>>>, C983_I12<C983_I13<C983_I14<C983_I15<C983_I16<C983_I17<C983_I18<C983>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C983 c = new C983();
    }
}

public class Test984
{
    interface C984_I0<T>
    { }
    interface C984_I1<T>
    { }
    interface C984_I2<T>
    { }
    interface C984_I3<T>
    { }
    interface C984_I4<T>
    { }
    interface C984_I5<T>
    { }
    interface C984_I6<T>
    { }
    interface C984_I7<T>
    { }
    interface C984_I8<T>
    { }
    interface C984_I9<T>
    { }
    interface C984_I10<T>
    { }
    interface C984_I11<T>
    { }
    interface C984_I12<T>
    { }
    interface C984_I13<T>
    { }
    interface C984_I14<T>
    { }
    interface C984_I15<T>
    { }

    class C984 : C984_I0<C984_I1<C984_I2<C984_I3<C984>>>>, C984_I4<C984_I5<C984_I6<C984_I7<C984>>>>, C984_I8<C984_I9<C984_I10<C984_I11<C984_I12<C984_I13<C984_I14<C984_I15<C984>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C984 c = new C984();
    }
}

public class Test986
{
    interface C986_I0<T>
    { }
    interface C986_I1<T>
    { }
    interface C986_I2<T>
    { }
    interface C986_I3<T>
    { }
    interface C986_I4<T>
    { }
    interface C986_I5<T>
    { }
    interface C986_I6<T>
    { }
    interface C986_I7<T>
    { }
    interface C986_I8<T>
    { }
    interface C986_I9<T>
    { }
    interface C986_I10<T>
    { }
    interface C986_I11<T>
    { }
    interface C986_I12<T>
    { }
    interface C986_I13<T>
    { }

    class C986 : C986_I0<C986_I1<C986_I2<C986_I3<C986_I4<C986>>>>>, C986_I5<C986_I6<C986_I7<C986_I8<C986_I9<C986_I10<C986_I11<C986_I12<C986>>>>>>>>, C986_I13<C986>
    { }

    public static void Test_RecursiveInheritance()
    {
        C986 c = new C986();
    }
}

public class Test987
{
    interface C987_I0<T>
    { }
    interface C987_I1<T>
    { }
    interface C987_I2<T>
    { }
    interface C987_I3<T>
    { }
    interface C987_I4<T>
    { }
    interface C987_I5<T>
    { }
    interface C987_I6<T>
    { }
    interface C987_I7<T>
    { }
    interface C987_I8<T>
    { }
    interface C987_I9<T>
    { }
    interface C987_I10<T>
    { }
    interface C987_I11<T>
    { }
    interface C987_I12<T>
    { }
    interface C987_I13<T>
    { }
    interface C987_I14<T>
    { }

    class C987 : C987_I0<C987_I1<C987_I2<C987_I3<C987_I4<C987>>>>>, C987_I5<C987_I6<C987_I7<C987_I8<C987_I9<C987_I10<C987_I11<C987_I12<C987>>>>>>>>, C987_I13<C987_I14<C987>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C987 c = new C987();
    }
}

public class Test988
{
    interface C988_I0<T>
    { }
    interface C988_I1<T>
    { }
    interface C988_I2<T>
    { }
    interface C988_I3<T>
    { }
    interface C988_I4<T>
    { }
    interface C988_I5<T>
    { }
    interface C988_I6<T>
    { }
    interface C988_I7<T>
    { }
    interface C988_I8<T>
    { }
    interface C988_I9<T>
    { }
    interface C988_I10<T>
    { }
    interface C988_I11<T>
    { }
    interface C988_I12<T>
    { }
    interface C988_I13<T>
    { }
    interface C988_I14<T>
    { }
    interface C988_I15<T>
    { }

    class C988 : C988_I0<C988_I1<C988_I2<C988_I3<C988_I4<C988>>>>>, C988_I5<C988_I6<C988_I7<C988_I8<C988_I9<C988_I10<C988_I11<C988_I12<C988>>>>>>>>, C988_I13<C988_I14<C988_I15<C988>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C988 c = new C988();
    }
}

public class Test989
{
    interface C989_I0<T>
    { }
    interface C989_I1<T>
    { }
    interface C989_I2<T>
    { }
    interface C989_I3<T>
    { }
    interface C989_I4<T>
    { }
    interface C989_I5<T>
    { }
    interface C989_I6<T>
    { }
    interface C989_I7<T>
    { }
    interface C989_I8<T>
    { }
    interface C989_I9<T>
    { }
    interface C989_I10<T>
    { }
    interface C989_I11<T>
    { }
    interface C989_I12<T>
    { }
    interface C989_I13<T>
    { }
    interface C989_I14<T>
    { }
    interface C989_I15<T>
    { }
    interface C989_I16<T>
    { }

    class C989 : C989_I0<C989_I1<C989_I2<C989_I3<C989_I4<C989>>>>>, C989_I5<C989_I6<C989_I7<C989_I8<C989_I9<C989_I10<C989_I11<C989_I12<C989>>>>>>>>, C989_I13<C989_I14<C989_I15<C989_I16<C989>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C989 c = new C989();
    }
}

public class Test990
{
    interface C990_I0<T>
    { }
    interface C990_I1<T>
    { }
    interface C990_I2<T>
    { }
    interface C990_I3<T>
    { }
    interface C990_I4<T>
    { }
    interface C990_I5<T>
    { }
    interface C990_I6<T>
    { }
    interface C990_I7<T>
    { }
    interface C990_I8<T>
    { }
    interface C990_I9<T>
    { }
    interface C990_I10<T>
    { }
    interface C990_I11<T>
    { }
    interface C990_I12<T>
    { }
    interface C990_I13<T>
    { }
    interface C990_I14<T>
    { }
    interface C990_I15<T>
    { }
    interface C990_I16<T>
    { }
    interface C990_I17<T>
    { }

    class C990 : C990_I0<C990_I1<C990_I2<C990_I3<C990_I4<C990>>>>>, C990_I5<C990_I6<C990_I7<C990_I8<C990_I9<C990_I10<C990_I11<C990_I12<C990>>>>>>>>, C990_I13<C990_I14<C990_I15<C990_I16<C990_I17<C990>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C990 c = new C990();
    }
}

public class Test991
{
    interface C991_I0<T>
    { }
    interface C991_I1<T>
    { }
    interface C991_I2<T>
    { }
    interface C991_I3<T>
    { }
    interface C991_I4<T>
    { }
    interface C991_I5<T>
    { }
    interface C991_I6<T>
    { }
    interface C991_I7<T>
    { }
    interface C991_I8<T>
    { }
    interface C991_I9<T>
    { }
    interface C991_I10<T>
    { }
    interface C991_I11<T>
    { }
    interface C991_I12<T>
    { }
    interface C991_I13<T>
    { }
    interface C991_I14<T>
    { }
    interface C991_I15<T>
    { }
    interface C991_I16<T>
    { }
    interface C991_I17<T>
    { }
    interface C991_I18<T>
    { }

    class C991 : C991_I0<C991_I1<C991_I2<C991_I3<C991_I4<C991>>>>>, C991_I5<C991_I6<C991_I7<C991_I8<C991_I9<C991_I10<C991_I11<C991_I12<C991>>>>>>>>, C991_I13<C991_I14<C991_I15<C991_I16<C991_I17<C991_I18<C991>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C991 c = new C991();
    }
}

public class Test992
{
    interface C992_I0<T>
    { }
    interface C992_I1<T>
    { }
    interface C992_I2<T>
    { }
    interface C992_I3<T>
    { }
    interface C992_I4<T>
    { }
    interface C992_I5<T>
    { }
    interface C992_I6<T>
    { }
    interface C992_I7<T>
    { }
    interface C992_I8<T>
    { }
    interface C992_I9<T>
    { }
    interface C992_I10<T>
    { }
    interface C992_I11<T>
    { }
    interface C992_I12<T>
    { }
    interface C992_I13<T>
    { }
    interface C992_I14<T>
    { }
    interface C992_I15<T>
    { }
    interface C992_I16<T>
    { }
    interface C992_I17<T>
    { }
    interface C992_I18<T>
    { }
    interface C992_I19<T>
    { }

    class C992 : C992_I0<C992_I1<C992_I2<C992_I3<C992_I4<C992>>>>>, C992_I5<C992_I6<C992_I7<C992_I8<C992_I9<C992_I10<C992_I11<C992_I12<C992>>>>>>>>, C992_I13<C992_I14<C992_I15<C992_I16<C992_I17<C992_I18<C992_I19<C992>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C992 c = new C992();
    }
}

public class Test993
{
    interface C993_I0<T>
    { }
    interface C993_I1<T>
    { }
    interface C993_I2<T>
    { }
    interface C993_I3<T>
    { }
    interface C993_I4<T>
    { }
    interface C993_I5<T>
    { }
    interface C993_I6<T>
    { }
    interface C993_I7<T>
    { }
    interface C993_I8<T>
    { }
    interface C993_I9<T>
    { }
    interface C993_I10<T>
    { }
    interface C993_I11<T>
    { }
    interface C993_I12<T>
    { }
    interface C993_I13<T>
    { }
    interface C993_I14<T>
    { }
    interface C993_I15<T>
    { }
    interface C993_I16<T>
    { }
    interface C993_I17<T>
    { }

    class C993 : C993_I0<C993_I1<C993_I2<C993_I3<C993_I4<C993>>>>>, C993_I5<C993_I6<C993_I7<C993_I8<C993_I9<C993>>>>>, C993_I10<C993_I11<C993_I12<C993_I13<C993_I14<C993_I15<C993_I16<C993_I17<C993>>>>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C993 c = new C993();
    }
}

public class Test995
{
    interface C995_I0<T>
    { }
    interface C995_I1<T>
    { }
    interface C995_I2<T>
    { }
    interface C995_I3<T>
    { }
    interface C995_I4<T>
    { }
    interface C995_I5<T>
    { }
    interface C995_I6<T>
    { }
    interface C995_I7<T>
    { }
    interface C995_I8<T>
    { }
    interface C995_I9<T>
    { }
    interface C995_I10<T>
    { }
    interface C995_I11<T>
    { }
    interface C995_I12<T>
    { }
    interface C995_I13<T>
    { }
    interface C995_I14<T>
    { }

    class C995 : C995_I0<C995_I1<C995_I2<C995_I3<C995_I4<C995_I5<C995>>>>>>, C995_I6<C995_I7<C995_I8<C995_I9<C995_I10<C995_I11<C995_I12<C995_I13<C995>>>>>>>>, C995_I14<C995>
    { }

    public static void Test_RecursiveInheritance()
    {
        C995 c = new C995();
    }
}

public class Test996
{
    interface C996_I0<T>
    { }
    interface C996_I1<T>
    { }
    interface C996_I2<T>
    { }
    interface C996_I3<T>
    { }
    interface C996_I4<T>
    { }
    interface C996_I5<T>
    { }
    interface C996_I6<T>
    { }
    interface C996_I7<T>
    { }
    interface C996_I8<T>
    { }
    interface C996_I9<T>
    { }
    interface C996_I10<T>
    { }
    interface C996_I11<T>
    { }
    interface C996_I12<T>
    { }
    interface C996_I13<T>
    { }
    interface C996_I14<T>
    { }
    interface C996_I15<T>
    { }

    class C996 : C996_I0<C996_I1<C996_I2<C996_I3<C996_I4<C996_I5<C996>>>>>>, C996_I6<C996_I7<C996_I8<C996_I9<C996_I10<C996_I11<C996_I12<C996_I13<C996>>>>>>>>, C996_I14<C996_I15<C996>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C996 c = new C996();
    }
}

public class Test997
{
    interface C997_I0<T>
    { }
    interface C997_I1<T>
    { }
    interface C997_I2<T>
    { }
    interface C997_I3<T>
    { }
    interface C997_I4<T>
    { }
    interface C997_I5<T>
    { }
    interface C997_I6<T>
    { }
    interface C997_I7<T>
    { }
    interface C997_I8<T>
    { }
    interface C997_I9<T>
    { }
    interface C997_I10<T>
    { }
    interface C997_I11<T>
    { }
    interface C997_I12<T>
    { }
    interface C997_I13<T>
    { }
    interface C997_I14<T>
    { }
    interface C997_I15<T>
    { }
    interface C997_I16<T>
    { }

    class C997 : C997_I0<C997_I1<C997_I2<C997_I3<C997_I4<C997_I5<C997>>>>>>, C997_I6<C997_I7<C997_I8<C997_I9<C997_I10<C997_I11<C997_I12<C997_I13<C997>>>>>>>>, C997_I14<C997_I15<C997_I16<C997>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C997 c = new C997();
    }
}

public class Test998
{
    interface C998_I0<T>
    { }
    interface C998_I1<T>
    { }
    interface C998_I2<T>
    { }
    interface C998_I3<T>
    { }
    interface C998_I4<T>
    { }
    interface C998_I5<T>
    { }
    interface C998_I6<T>
    { }
    interface C998_I7<T>
    { }
    interface C998_I8<T>
    { }
    interface C998_I9<T>
    { }
    interface C998_I10<T>
    { }
    interface C998_I11<T>
    { }
    interface C998_I12<T>
    { }
    interface C998_I13<T>
    { }
    interface C998_I14<T>
    { }
    interface C998_I15<T>
    { }
    interface C998_I16<T>
    { }
    interface C998_I17<T>
    { }

    class C998 : C998_I0<C998_I1<C998_I2<C998_I3<C998_I4<C998_I5<C998>>>>>>, C998_I6<C998_I7<C998_I8<C998_I9<C998_I10<C998_I11<C998_I12<C998_I13<C998>>>>>>>>, C998_I14<C998_I15<C998_I16<C998_I17<C998>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C998 c = new C998();
    }
}

public class Test999
{
    interface C999_I0<T>
    { }
    interface C999_I1<T>
    { }
    interface C999_I2<T>
    { }
    interface C999_I3<T>
    { }
    interface C999_I4<T>
    { }
    interface C999_I5<T>
    { }
    interface C999_I6<T>
    { }
    interface C999_I7<T>
    { }
    interface C999_I8<T>
    { }
    interface C999_I9<T>
    { }
    interface C999_I10<T>
    { }
    interface C999_I11<T>
    { }
    interface C999_I12<T>
    { }
    interface C999_I13<T>
    { }
    interface C999_I14<T>
    { }
    interface C999_I15<T>
    { }
    interface C999_I16<T>
    { }
    interface C999_I17<T>
    { }
    interface C999_I18<T>
    { }

    class C999 : C999_I0<C999_I1<C999_I2<C999_I3<C999_I4<C999_I5<C999>>>>>>, C999_I6<C999_I7<C999_I8<C999_I9<C999_I10<C999_I11<C999_I12<C999_I13<C999>>>>>>>>, C999_I14<C999_I15<C999_I16<C999_I17<C999_I18<C999>>>>>
    { }

    public static void Test_RecursiveInheritance()
    {
        C999 c = new C999();
    }
}


public class Test_RecursiveInheritance
{
    [Fact]
    public static void TestEntryPoint()
    {
        Test770.Test_RecursiveInheritance();
        Test771.Test_RecursiveInheritance();
        Test772.Test_RecursiveInheritance();
        Test773.Test_RecursiveInheritance();
        Test774.Test_RecursiveInheritance();
        Test775.Test_RecursiveInheritance();
        Test777.Test_RecursiveInheritance();
        Test778.Test_RecursiveInheritance();
        Test779.Test_RecursiveInheritance();
        Test780.Test_RecursiveInheritance();
        Test781.Test_RecursiveInheritance();
        Test782.Test_RecursiveInheritance();
        Test783.Test_RecursiveInheritance();
        Test784.Test_RecursiveInheritance();
        Test785.Test_RecursiveInheritance();
        Test786.Test_RecursiveInheritance();
        Test787.Test_RecursiveInheritance();
        Test790.Test_RecursiveInheritance();
        Test792.Test_RecursiveInheritance();
        Test793.Test_RecursiveInheritance();
        Test794.Test_RecursiveInheritance();
        Test795.Test_RecursiveInheritance();
        Test796.Test_RecursiveInheritance();

        Test808.Test_RecursiveInheritance();
        Test809.Test_RecursiveInheritance();
        Test810.Test_RecursiveInheritance();
        Test811.Test_RecursiveInheritance();
        Test812.Test_RecursiveInheritance();
        Test813.Test_RecursiveInheritance();
        Test814.Test_RecursiveInheritance();
        Test816.Test_RecursiveInheritance();
        Test817.Test_RecursiveInheritance();
        Test818.Test_RecursiveInheritance();
        Test819.Test_RecursiveInheritance();
        Test820.Test_RecursiveInheritance();
        Test821.Test_RecursiveInheritance();
        Test822.Test_RecursiveInheritance();
        Test824.Test_RecursiveInheritance();
        Test825.Test_RecursiveInheritance();
        Test826.Test_RecursiveInheritance();
        Test827.Test_RecursiveInheritance();
        Test828.Test_RecursiveInheritance();
        Test829.Test_RecursiveInheritance();
        Test830.Test_RecursiveInheritance();
        Test832.Test_RecursiveInheritance();
        Test833.Test_RecursiveInheritance();
        Test834.Test_RecursiveInheritance();
        Test835.Test_RecursiveInheritance();
        Test836.Test_RecursiveInheritance();
        Test837.Test_RecursiveInheritance();
        Test838.Test_RecursiveInheritance();
        Test840.Test_RecursiveInheritance();
        Test841.Test_RecursiveInheritance();
        Test842.Test_RecursiveInheritance();
        Test843.Test_RecursiveInheritance();
        Test844.Test_RecursiveInheritance();
        Test845.Test_RecursiveInheritance();
        Test846.Test_RecursiveInheritance();
        Test848.Test_RecursiveInheritance();
        Test849.Test_RecursiveInheritance();

        Test850.Test_RecursiveInheritance();
        Test851.Test_RecursiveInheritance();
        Test852.Test_RecursiveInheritance();

        Test853.Test_RecursiveInheritance();
        Test854.Test_RecursiveInheritance();
        Test862.Test_RecursiveInheritance();
        Test864.Test_RecursiveInheritance();
        Test865.Test_RecursiveInheritance();
        Test866.Test_RecursiveInheritance();
        Test867.Test_RecursiveInheritance();
        Test868.Test_RecursiveInheritance();
        Test869.Test_RecursiveInheritance();
        Test871.Test_RecursiveInheritance();
        Test872.Test_RecursiveInheritance();
        Test873.Test_RecursiveInheritance();
        Test874.Test_RecursiveInheritance();
        Test875.Test_RecursiveInheritance();
        Test876.Test_RecursiveInheritance();
        Test878.Test_RecursiveInheritance();
        Test879.Test_RecursiveInheritance();
        Test880.Test_RecursiveInheritance();
        Test881.Test_RecursiveInheritance();
        Test882.Test_RecursiveInheritance();
        Test883.Test_RecursiveInheritance();
        Test885.Test_RecursiveInheritance();
        Test886.Test_RecursiveInheritance();
        Test887.Test_RecursiveInheritance();
        Test888.Test_RecursiveInheritance();
        Test889.Test_RecursiveInheritance();
        Test890.Test_RecursiveInheritance();
        Test892.Test_RecursiveInheritance();
        Test893.Test_RecursiveInheritance();
        Test894.Test_RecursiveInheritance();
        Test895.Test_RecursiveInheritance();
        Test896.Test_RecursiveInheritance();
        Test897.Test_RecursiveInheritance();
        Test899.Test_RecursiveInheritance();
        Test900.Test_RecursiveInheritance();
        Test901.Test_RecursiveInheritance();
        Test902.Test_RecursiveInheritance();
        Test903.Test_RecursiveInheritance();
        Test904.Test_RecursiveInheritance();
        Test905.Test_RecursiveInheritance();
        Test907.Test_RecursiveInheritance();
        Test908.Test_RecursiveInheritance();
        Test909.Test_RecursiveInheritance();
        Test910.Test_RecursiveInheritance();
        Test911.Test_RecursiveInheritance();
        Test912.Test_RecursiveInheritance();
        Test913.Test_RecursiveInheritance();
        Test915.Test_RecursiveInheritance();
        Test916.Test_RecursiveInheritance();
        Test917.Test_RecursiveInheritance();
        Test918.Test_RecursiveInheritance();
        Test919.Test_RecursiveInheritance();
        Test920.Test_RecursiveInheritance();
        Test921.Test_RecursiveInheritance();
        Test922.Test_RecursiveInheritance();
        Test923.Test_RecursiveInheritance();
        Test924.Test_RecursiveInheritance();
        Test925.Test_RecursiveInheritance();
        Test926.Test_RecursiveInheritance();
        Test928.Test_RecursiveInheritance();
        Test929.Test_RecursiveInheritance();
        Test930.Test_RecursiveInheritance();
        Test932.Test_RecursiveInheritance();
        Test933.Test_RecursiveInheritance();
        Test934.Test_RecursiveInheritance();
        Test935.Test_RecursiveInheritance();
        Test936.Test_RecursiveInheritance();
        Test937.Test_RecursiveInheritance();
        Test938.Test_RecursiveInheritance();

        Test950.Test_RecursiveInheritance();
        Test951.Test_RecursiveInheritance();
        Test952.Test_RecursiveInheritance();
        Test953.Test_RecursiveInheritance();
        Test954.Test_RecursiveInheritance();
        Test955.Test_RecursiveInheritance();
        Test956.Test_RecursiveInheritance();
        Test957.Test_RecursiveInheritance();
        Test959.Test_RecursiveInheritance();
        Test960.Test_RecursiveInheritance();
        Test961.Test_RecursiveInheritance();
        Test962.Test_RecursiveInheritance();
        Test963.Test_RecursiveInheritance();
        Test964.Test_RecursiveInheritance();
        Test965.Test_RecursiveInheritance();
        Test966.Test_RecursiveInheritance();
        Test968.Test_RecursiveInheritance();
        Test969.Test_RecursiveInheritance();
        Test970.Test_RecursiveInheritance();
        Test971.Test_RecursiveInheritance();
        Test972.Test_RecursiveInheritance();
        Test973.Test_RecursiveInheritance();
        Test974.Test_RecursiveInheritance();
        Test975.Test_RecursiveInheritance();
        Test977.Test_RecursiveInheritance();
        Test978.Test_RecursiveInheritance();
        Test979.Test_RecursiveInheritance();
        Test980.Test_RecursiveInheritance();
        Test981.Test_RecursiveInheritance();
        Test982.Test_RecursiveInheritance();
        Test983.Test_RecursiveInheritance();
        Test984.Test_RecursiveInheritance();
        Test986.Test_RecursiveInheritance();
        Test987.Test_RecursiveInheritance();
        Test988.Test_RecursiveInheritance();
        Test989.Test_RecursiveInheritance();
        Test990.Test_RecursiveInheritance();
        Test991.Test_RecursiveInheritance();
        Test992.Test_RecursiveInheritance();
        Test993.Test_RecursiveInheritance();
        Test995.Test_RecursiveInheritance();
        Test996.Test_RecursiveInheritance();
        Test997.Test_RecursiveInheritance();
        Test998.Test_RecursiveInheritance();
        Test999.Test_RecursiveInheritance();
    }
}
