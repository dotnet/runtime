// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.CompilerServices;

public class RefX1<T>
{
    T _val;
    public RefX1(T t) { _val = t; }

    public override bool Equals(object obj)
    {
        RefX1<T> b = obj as RefX1<T>;
        if (b == null)
        {
            return false;
        }
        return this._val.Equals(b._val);
    }

    public override int GetHashCode()
    {
        return this._val.GetHashCode();
    }

}

public class Test
{
    public static bool result = true;
    public static bool Eval(bool exp)
    {
        return Eval(exp, null);
    }
    public static bool Eval(bool exp, String errorMsg)
    {
        if (!exp)
        {
            //This would never be reset, since we start with true and only set it to false if the Eval fails
            result = exp;
            String err = errorMsg;
            if (err == null)
                err = "Test Failed";
            Console.WriteLine(err);
        }

        return exp;
    }

    public static bool Eval(bool exp, String format, params object[] arg)
    {
        if (!exp)
        {
            return Eval(exp, String.Format(format, arg));
        }

        return true;
    }
}