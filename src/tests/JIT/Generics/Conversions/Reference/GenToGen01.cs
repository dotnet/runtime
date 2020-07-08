// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

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


public class GenBase<T>
{
    public virtual Type MyVirtType()
    {
        return typeof(GenBase<T>);
    }

}

public class Gen<T> : GenBase<T>
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

public class Test
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

    public static int Main()
    {
        Eval(new Converter<int>().ToGenOfT(new Gen<int>(), false, typeof(Gen<int>)));
        Eval(new Converter<int>().ToGenBaseOfT(new Gen<int>(), false, typeof(Gen<int>)));
        Eval(new Converter<int>().ToGenBaseOfT(new GenBase<int>(), false, typeof(GenBase<int>)));
        Eval(new Converter<int>().ToGenOfT(new GenBase<int>(), true, null));

        Eval(new Converter<string>().ToGenOfT(new Gen<int>(), true, null));
        Eval(new Converter<string>().ToGenBaseOfT(new Gen<int>(), true, null));
        Eval(new Converter<string>().ToGenBaseOfT(new GenBase<int>(), true, null));
        Eval(new Converter<string>().ToGenOfT(new GenBase<int>(), true, null));


        Eval(new Converter<string>().ToGenOfT(new Gen<string>(), false, typeof(Gen<string>)));
        Eval(new Converter<string>().ToGenBaseOfT(new Gen<string>(), false, typeof(Gen<string>)));
        Eval(new Converter<string>().ToGenBaseOfT(new GenBase<string>(), false, typeof(GenBase<string>)));
        Eval(new Converter<string>().ToGenOfT(new GenBase<string>(), true, null));

        Eval(new Converter<int>().ToGenOfT(new Gen<string>(), true, null));
        Eval(new Converter<int>().ToGenBaseOfT(new Gen<string>(), true, null));
        Eval(new Converter<int>().ToGenBaseOfT(new GenBase<string>(), true, null));
        Eval(new Converter<int>().ToGenOfT(new GenBase<string>(), true, null));

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
