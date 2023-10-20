// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Xunit;

class TestType1<T> { }
class TestType2<T> { }
class TestType3<T> { }
class TestType4<T> { }
class TestType5<T> { }
class TestType6<T> { }
class TestType7<T> { }
class TestType8<T> { }
class TestType9<T> { }
class TestType10<T> { }
class TestType11<T> { }
class TestType12<T> { }
class TestType13<T> { }
class TestType14<T> { }
class TestType15<T> { }
class TestType16<T> { }
class TestType17<T> { }
class TestType18<T> { }
class TestType19<T> { }
class TestType20<T> { }
class TestType21<T> { }
class TestType22<T> { }
class TestType23<T> { }
class TestType24<T> { }
class TestType25<T> { }

public class GenBase
{
    public virtual void VFunc() { }
}

public class GenClass<T> : GenBase
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public Type FuncOnGenClass(int level)
    {
        switch (level)
        {
            case 0: return typeof(T);
            case 1: return typeof(TestType1<T>);
            case 2: return typeof(TestType2<T>);
            case 3: return typeof(TestType3<T>);
            case 4: return typeof(TestType4<T>);
            case 5: return typeof(TestType5<T>);
            case 6: return typeof(TestType6<T>);
            case 7: return typeof(TestType7<T>);
            case 8: return typeof(TestType8<T>);
            case 9: return typeof(TestType9<T>);
            case 10: return typeof(TestType10<T>);
            case 11: return typeof(TestType11<T>);
            case 12: return typeof(TestType12<T>);
            case 13: return typeof(TestType13<T>);
            case 14: return typeof(TestType14<T>);
            case 15: return typeof(TestType15<T>);
            case 16: return typeof(TestType16<T>);
            case 17: return typeof(TestType17<T>);
            case 18: return typeof(TestType18<T>);
            case 19: return typeof(TestType19<T>);
            case 20: return typeof(TestType20<T>);
            case 21: return typeof(TestType21<T>);
            case 22: return typeof(TestType22<T>);
            case 23: return typeof(TestType23<T>);
            case 24: return typeof(TestType24<T>);
            case 25: default: return typeof(TestType25<T>);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Type FuncOnGenClass2(int level)
    {
        switch (level)
        {
            case 0: return typeof(T);
            case 1: return typeof(TestType1<T>);
            case 2: return typeof(TestType2<T>);
            case 3: return typeof(TestType3<T>);
            case 4: return typeof(TestType4<T>);
            case 5: return typeof(TestType5<T>);
            case 6: return typeof(TestType6<T>);
            case 7: return typeof(TestType7<T>);
            case 8: return typeof(TestType8<T>);
            case 9: return typeof(TestType9<T>);
            case 10: return typeof(TestType10<T>);
            case 11: return typeof(TestType11<T>);
            case 12: return typeof(TestType12<T>);
            case 13: return typeof(TestType13<T>);
            case 14: return typeof(TestType14<T>);
            case 15: return typeof(TestType15<T>);
            case 16: return typeof(TestType16<T>);
            case 17: return typeof(TestType17<T>);
            case 18: return typeof(TestType18<T>);
            case 19: return typeof(TestType19<T>);
            case 20: return typeof(TestType20<T>);
            case 21: return typeof(TestType21<T>);
            case 22: return typeof(TestType22<T>);
            case 23: return typeof(TestType23<T>);
            case 24: return typeof(TestType24<T>);
            case 25: default: return typeof(TestType25<T>);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void DoTest_Inner<T1,T2,T3>(int max, GenClass<T1> o1, GenClass<T2> o2, GenClass<T3> o3)
    {
        Console.WriteLine("TEST: FuncOnGenClass<{0}>", typeof(T1).Name);
        for (int i = 0; i < max; i++)
            Assert.Equal(o1.FuncOnGenClass(i).ToString(), i == 0 ? $"{typeof(T1)}" : $"TestType{i}`1[{typeof(T1)}]");

        Console.WriteLine("TEST: FuncOnGenClass<{0}>", typeof(T2).Name);
        for (int i = 0; i < max; i++)
            Assert.Equal(o2.FuncOnGenClass(i).ToString(), i == 0 ? $"{typeof(T2)}" : $"TestType{i}`1[{typeof(T2)}]");

        Console.WriteLine("TEST: FuncOnGenClass2<{0}>", typeof(T2).Name);
        for (int i = 0; i < max; i++)
            Assert.Equal(o2.FuncOnGenClass2(i).ToString(), i == 0 ? $"{typeof(T2)}" : $"TestType{i}`1[{typeof(T2)}]");

        Console.WriteLine("TEST: FuncOnGenClass<{0}>", typeof(T3).Name);
        for (int i = 0; i < max; i++)
            Assert.Equal(o3.FuncOnGenClass(i).ToString(), i == 0 ? $"{typeof(T3)}" : $"TestType{i}`1[{typeof(T3)}]");
    }

    public static void DoTest_GenClass(int max)
    {
        DoTest_Inner<string, object, Test_DictionaryExpansion>(max,
            new GenClass<string>(),
            new GenClass<object>(),
            new GenClass<Test_DictionaryExpansion>());
    }

    public static void DoTest_GenDerived(int max)
    {
        DoTest_Inner<string, object, Test_DictionaryExpansion>(max,
            new GenDerived<string, int>(),
            new GenDerived<object, int>(),
            new GenDerived<Test_DictionaryExpansion, int>());
    }

    public static void DoTest_GenDerived2(int max)
    {
        DoTest_Inner<object, object, object>(max,
            new GenDerived2(),
            new GenDerived2(),
            new GenDerived2());
    }

    public static void DoTest_GenDerived3(int max)
    {
        DoTest_Inner<object, object, object>(max,
            new GenDerived3(),
            new GenDerived3(),
            new GenDerived3());
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void VFunc()
    {
        Assert.Equal("System.Collections.Generic.KeyValuePair`2[System.Object,System.String]", typeof(KeyValuePair<T, string>).ToString());
        Assert.Equal("System.Collections.Generic.KeyValuePair`2[System.Object,System.String]", typeof(KeyValuePair<T, string>).ToString());
    }
}

public class GenDerived<T, U> : GenClass<T>
{
}

public class GenDerived2 : GenDerived<object, string>
{
}

public class GenDerived3 : GenDerived2
{
}

public class GenDerived4 : GenDerived3
{
}

public class Test_DictionaryExpansion
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type GFunc<T>(int level)
    {
        switch(level)
        {
            case 0: return typeof(T);
            case 1: return typeof(TestType1<T>);
            case 2: return typeof(TestType2<T>);
            case 3: return typeof(TestType3<T>);
            case 4: return typeof(TestType4<T>);
            case 5: return typeof(TestType5<T>);
            case 6: return typeof(TestType6<T>);
            case 7: return typeof(TestType7<T>);
            case 8: return typeof(TestType8<T>);
            case 9: return typeof(TestType9<T>);
            case 10: return typeof(TestType10<T>);
            case 11: return typeof(TestType11<T>);
            case 12: return typeof(TestType12<T>);
            case 13: return typeof(TestType13<T>);
            case 14: return typeof(TestType14<T>);
            case 15: return typeof(TestType15<T>);
            case 16: return typeof(TestType16<T>);
            case 17: return typeof(TestType17<T>);
            case 18: return typeof(TestType18<T>);
            case 19: return typeof(TestType19<T>);
            case 20: return typeof(TestType20<T>);
            case 21: return typeof(TestType21<T>);
            case 22: return typeof(TestType22<T>);
            case 23: return typeof(TestType23<T>);
            case 24: return typeof(TestType24<T>);
            case 25: default: return typeof(TestType25<T>);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type GFunc2<T>(int level)
    {
        switch(level)
        {
            case 0: return typeof(T);
            case 1: return typeof(TestType1<T>);
            case 2: return typeof(TestType2<T>);
            case 3: return typeof(TestType3<T>);
            case 4: return typeof(TestType4<T>);
            case 5: return typeof(TestType5<T>);
            case 6: return typeof(TestType6<T>);
            case 7: return typeof(TestType7<T>);
            case 8: return typeof(TestType8<T>);
            case 9: return typeof(TestType9<T>);
            case 10: return typeof(TestType10<T>);
            case 11: return typeof(TestType11<T>);
            case 12: return typeof(TestType12<T>);
            case 13: return typeof(TestType13<T>);
            case 14: return typeof(TestType14<T>);
            case 15: return typeof(TestType15<T>);
            case 16: return typeof(TestType16<T>);
            case 17: return typeof(TestType17<T>);
            case 18: return typeof(TestType18<T>);
            case 19: return typeof(TestType19<T>);
            case 20: return typeof(TestType20<T>);
            case 21: return typeof(TestType21<T>);
            case 22: return typeof(TestType22<T>);
            case 23: return typeof(TestType23<T>);
            case 24: return typeof(TestType24<T>);
            case 25: default: return typeof(TestType25<T>);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void DoTest(int max)
    {
        Console.WriteLine("TEST: GFunc<string>");
        for(int i = 0; i < max; i++)
            Assert.Equal(GFunc<string>(i).ToString(), i == 0 ? "System.String" : $"TestType{i}`1[System.String]");

        Console.WriteLine("TEST: GFunc<object>(i)");
        for (int i = 0; i < max; i++)
            Assert.Equal(GFunc<object>(i).ToString(), i == 0 ? "System.Object" : $"TestType{i}`1[System.Object]");

        Console.WriteLine("TEST: GFunc2<object>(i)");
        for (int i = 0; i < max; i++)
            Assert.Equal(GFunc2<object>(i).ToString(), i == 0 ? "System.Object" : $"TestType{i}`1[System.Object]");

        Console.WriteLine("TEST: GFunc<Test_DictionaryExpansion>(i)");
        for (int i = 0; i < max; i++)
            Assert.Equal(GFunc<Test_DictionaryExpansion>(i).ToString(), i == 0 ? "Test_DictionaryExpansion" : $"TestType{i}`1[Test_DictionaryExpansion]");
    }

    [Fact]
    public static void TestEntryPoint()
    {
        GenBase deriv4 = new GenDerived4();

        for(int i = 5; i <= 25; i += 5)
        {
            // Test for generic classes
            switch(i % 4)
            {
                case 0:
                    GenClass<int>.DoTest_GenClass(i);
                    break;
                case 1:
                    GenClass<int>.DoTest_GenDerived(i);
                    break;
                case 2:
                    GenClass<int>.DoTest_GenDerived2(i);
                    break;
                case 3:
                    GenClass<int>.DoTest_GenDerived3(i);
                    break;

            }

            // Test for generic methods
            DoTest(i);

            {
                AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("CollectibleAsm"+i), AssemblyBuilderAccess.RunAndCollect);
                var tb = ab.DefineDynamicModule("CollectibleMod" + i).DefineType("CollectibleGenDerived"+i, TypeAttributes.Public, typeof(GenDerived2));
                var t = tb.CreateType();
                GenBase col_b = (GenBase)Activator.CreateInstance(t);
                col_b.VFunc();

                ab = null;
                tb = null;
                t = null;
                col_b = null;
                for(int k = 0; k < 5; k++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        }

        // After all expansions to existing dictionaries, use GenDerived4. GenDerived4 was allocated before any of its
        // base type dictionaries were expanded.
        for(int i = 0; i < 5; i++)
            deriv4.VFunc();
    }
}
