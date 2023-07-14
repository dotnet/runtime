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


public interface GenBase<T>
{
    Type MyVirtType();
}

public class GenInt : GenBase<int>
{
    public virtual Type MyVirtType()
    {
        return typeof(GenInt);
    }
}

public class GenString : GenBase<string>
{
    public virtual Type MyVirtType()
    {
        return typeof(GenString);
    }
}

public class Converter<T>
{
    public bool ToGenBaseOfT(object src, bool invalid, Type t)
    {
        try
        {
            GenBase<T> dst = (GenBase<T>)src;
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

    public bool ToGenInt(object src, bool invalid, Type t)
    {
        try
        {
            GenInt dst = (GenInt)src;
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

    public bool ToGenString(object src, bool invalid, Type t)
    {
        try
        {
            GenString dst = (GenString)src;
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

public class Test_NonGenToGen02
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
        Eval(new Converter<int>().ToGenBaseOfT(new GenInt(), false, typeof(GenInt)));
        Eval(new Converter<string>().ToGenBaseOfT(new GenInt(), true, null));

        Eval(new Converter<string>().ToGenBaseOfT(new GenString(), false, typeof(GenString)));
        Eval(new Converter<int>().ToGenBaseOfT(new GenString(), true, null));

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
