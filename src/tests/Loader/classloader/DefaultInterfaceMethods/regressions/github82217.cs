// The .NET Foundation licenses this file to you under the MIT license.

using System;


class Program
{
    static int Main()
    {
        int ret;
        ret = CallMethod<Class>(); 
        if (ret != 100) return ret;
		ret = CallMethod<IInterface>();
        if (ret != 100) return ret;
        return ret;
    }
    private static int CallMethod<T>() where T : IHaveStaticMethod
    {
        return T.Method();
    }
}
public interface IHaveStaticMethod
{
    static abstract int Method();
}

public interface IInterface : IHaveStaticMethod
{
    static int IHaveStaticMethod.Method()
    {
        return 100;
    }
}

public class Class : IHaveStaticMethod
{
    public static int Method()
    {
        return 100;
    }
}

