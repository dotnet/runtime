using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C<T>
{
    public static string DefaultTypeOf() => typeof(T).Name;
}

public static class Program
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static int Main()
    {
        var dcs = C<string>.DefaultTypeOf();
        if (dcs != "String") return 200;
        return 100;
    }
}

/*
impDevirtualizeCall: Trying to devirtualize interface call:
    class for 'this' is C [exact] (attrib 20000000)
    base method is I`1[__Canon]::DefaultTypeOf
--- base class is interface
    devirt to I`1[[System.String, System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]][System.String]::DefaultTypeOf -- exact
               [000010] --C-G-------              *  CALLV stub ref    I`1[__Canon][System.__Canon].DefaultTypeOf
               [000009] ------------ this in rcx  \--*  LCL_VAR   ref    V00 loc0
    exact; can devirtualize
... after devirt...
               [000010] --C-G-------              *  CALL nullcheck ref    I`1[[System.String, System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]][System.String].DefaultTypeOf
               [000009] ------------ this in rcx  \--*  LCL_VAR   ref    V00 loc0
Devirtualized interface call to I`1[__Canon]:DefaultTypeOf; now direct call to I`1[[System.String, System.Private.CoreLib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]][System.String]:DefaultTypeOf [exact]
*/
