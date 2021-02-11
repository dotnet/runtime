// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib {}
.assembly test {}
.module test.exe

.class Repro
{
    .field private static class [mscorlib]System.Func`2<int32,int32> d1
    .field private static class [mscorlib]System.Func`6<int32,int32,int32,int32,int32,int32> d2
  
    .method private hidebysig static int32 Work1(int32 x) cil managed
    {
        .maxstack  8
        ldarg.0
        brtrue.s   TAILCALL1

        ldc.i4.s   100
        ret

    TAILCALL1:
        ldsfld     class [mscorlib]System.Func`6<int32,int32,int32,int32,int32,int32> Repro::d2
        ldarg.0
        ldc.i4.1
        sub
        ldc.i4.1
        ldc.i4.2
        ldc.i4.3
        ldc.i4.4
        tail. 
        callvirt   instance !5 class [mscorlib]System.Func`6<int32,int32,int32,int32,int32,int32>::Invoke(!0,
                                                                                                          !1,
                                                                                                          !2,
                                                                                                          !3,
                                                                                                          !4)
        ret
    } 

    .method private hidebysig static int32 Work2(int32 x, int32 a, int32 b, int32 c, int32 d) cil managed
    {
        .maxstack  8
        ldarg.0
        brtrue.s   TAILCALL2

        ldc.i4.s   100
        ret
    
    TAILCALL2:
        ldsfld     class [mscorlib]System.Func`2<int32,int32> Repro::d1
        ldarg.0
        ldc.i4.1
        sub
        tail. 
        callvirt   instance !1 class [mscorlib]System.Func`2<int32,int32>::Invoke(!0)
        ret
    } 

    .method private hidebysig static int32  Main() cil managed
    {
        .entrypoint
        .maxstack  8
        ldnull
        ldftn      int32 Repro::Work1(int32)
        newobj     instance void class [mscorlib]System.Func`2<int32,int32>::.ctor(object,
                                                                                   native int)
        stsfld     class [mscorlib]System.Func`2<int32,int32> Repro::d1
        ldnull
        ldftn      int32 Repro::Work2(int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32)
        newobj     instance void class [mscorlib]System.Func`6<int32,int32,int32,int32,int32,int32>::.ctor(object,
                                                                                                           native int)
        stsfld     class [mscorlib]System.Func`6<int32,int32,int32,int32,int32,int32> Repro::d2
        ldsfld     class [mscorlib]System.Func`2<int32,int32> Repro::d1
        ldc.i4     0xf4240
        callvirt   instance !1 class [mscorlib]System.Func`2<int32,int32>::Invoke(!0)
        
        ldstr      "PASS!"
        call       void [System.Console]System.Console::WriteLine(string)
        ret
    } 
}
