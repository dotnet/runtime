// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public struct ValX0 { }
public struct ValY0 { }
public struct ValX1<T> { }
public struct ValY1<T> { }
public struct ValX2<T, U> { }
public struct ValY2<T, U> { }
public struct ValX3<T, U, V> { }
public struct ValY3<T, U, V> { }
public class RefX0 { }
public class RefY0 { }
public class RefX1<T> { }
public class RefY1<T> { }
public class RefX2<T, U> { }
public class RefY2<T, U> { }
public class RefX3<T, U, V> { }
public class RefY3<T, U, V> { }


public class GenBase
{
    public virtual Type MyVirtType()
    {
        return typeof(GenBase);
    }

}

public class Gen<T> : GenBase
{
    public override Type MyVirtType()
    {
        return typeof(Gen<T>);
    }
}

public class Converter<T>
{
    public bool ToGenBaseOfT(object src, bool invalid, Type t)
    {
        try
        {
            GenBase dst = (GenBase)src;
            if (invalid)
            {
                return false;
            }
            return dst.MyVirtType().Equals(t);
        }
        catch (InvalidCastException)
        {
            return invalid;
        }
        catch
        {
            return false;
        }
    }

    public bool ToGenOfT(object src, bool invalid, Type t)
    {
        try
        {
            Gen<T> dst = (Gen<T>)src;
            if (invalid)
            {
                return false;
            }
            return dst.MyVirtType().Equals(t);
        }
        catch (InvalidCastException)
        {
            return invalid;
        }
        catch
        {
            return false;
        }
    }
}

public class Test_GenToNonGen01
{
    public static int counter = 0;
    public static bool result = true;
    public static void Eval(bool exp)
    {
        counter++;
        if (!exp)
        {
            result = exp;
            Console.WriteLine("Test Failed at location: " + counter);
        }

    }

    [Fact]
    public static int TestEntryPoint()
    {
        Eval(new Converter<int>().ToGenBaseOfT(new Gen<int>(), false, typeof(Gen<int>)));
        Eval(new Converter<int>().ToGenOfT(new GenBase(), true, null));

        Eval(new Converter<string>().ToGenBaseOfT(new Gen<string>(), false, typeof(Gen<string>)));
        Eval(new Converter<string>().ToGenOfT(new GenBase(), true, null));

        if (result)
        {
            Console.WriteLine("Test Passed");
            return 100;
        }
        else
        {
            Console.WriteLine("Test Failed");
            return 1;
        }
    }

}
