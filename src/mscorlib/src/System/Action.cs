// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System {
    using System.Runtime.CompilerServices;

    public delegate void Action<in T>(T obj); 

    // Action/Func delegates first shipped with .NET Framework 3.5 in System.Core.dll as part of LINQ
    // These were type forwarded to mscorlib.dll in .NET Framework 4.0 and in Silverlight 5.0

#if !FEATURE_CORECLR
    [TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
#endif
    public delegate void Action();

#if !FEATURE_CORECLR
    [TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
#endif
    public delegate void Action<in T1,in T2>(T1 arg1, T2 arg2);

#if !FEATURE_CORECLR
    [TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
#endif
    public delegate void Action<in T1,in T2,in T3>(T1 arg1, T2 arg2, T3 arg3);

#if !FEATURE_CORECLR
    [TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
#endif
    public delegate void Action<in T1,in T2,in T3,in T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);

#if !FEATURE_CORECLR
    [TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
#endif
    public delegate TResult Func<out TResult>();

#if !FEATURE_CORECLR
    [TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
#endif
    public delegate TResult Func<in T, out TResult>(T arg);

#if !FEATURE_CORECLR
    [TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
#endif
    public delegate TResult Func<in T1, in T2, out TResult>(T1 arg1, T2 arg2);

#if !FEATURE_CORECLR
    [TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
#endif
    public delegate TResult Func<in T1, in T2, in T3, out TResult>(T1 arg1, T2 arg2, T3 arg3);

#if !FEATURE_CORECLR
    [TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
#endif
    public delegate TResult Func<in T1, in T2, in T3, in T4, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);


    public delegate void Action<in T1,in T2,in T3,in T4,in T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    public delegate void Action<in T1,in T2,in T3,in T4,in T5,in T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    public delegate void Action<in T1,in T2,in T3,in T4,in T5,in T6,in T7>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
    public delegate void Action<in T1,in T2,in T3,in T4,in T5,in T6,in T7,in T8>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);

    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
    public delegate TResult Func<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);

    public delegate int Comparison<in T>(T x, T y);

    public delegate TOutput Converter<in TInput, out TOutput>(TInput input); 
    
    public delegate bool Predicate<in T>(T obj); 

}

