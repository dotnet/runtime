// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly PrefixTests
{
}

.class public auto ansi beforefieldinit PrefixTestsType
       extends [System.Runtime]System.Object
{
    .method static public hidebysig void StaticMethod() cil managed 
    {
        ret
    }

    .method static public hidebysig void Readonly.Ldelema_Valid() cil managed 
    {
        .locals init (int32[] V_0)

        //int[] array = new int[2];
        ldc.i4.2
        newarr [System.Runtime]System.Int32
        stloc.0
        ldloc.0
        // array[1].ToString();
        ldc.i4.1
        readonly. 
        ldelema    [System.Runtime]System.Int32
        call       instance string [System.Runtime]System.Int32::ToString()
        pop
        ret
    }

    .method static public hidebysig void Readonly.Ldelem_Invalid_ReadOnly() cil managed 
    {
        .locals init (int32[] V_0)

        //int[] array = new int[2];
        ldc.i4.2
        newarr [System.Runtime]System.Int32
        stloc.0
        ldloc.0
        ldc.i4.1
        
        readonly. 
        ldelem    [System.Runtime]System.Int32
        pop
        ret
    }

    .method static public hidebysig void Readonly.ArrayAddress_Valid() cil managed 
    {
        //int[,] array = new int[1, 1];
        ldc.i4.1
        ldc.i4.1
        newobj  instance void int32[0..., 0...]::.ctor(int32, int32)
        ldc.i4.0
        ldc.i4.0
        readonly. 
        call    instance int32& int32[0..., 0...]::Address(int32, int32)
        pop
        ret
    }

    .method static public hidebysig void Readonly.ArrayGet_Invalid_ReadonlyUnexpectedCallee() cil managed 
    {
        //int[,] array = new int[1, 1];
        ldc.i4.1
        ldc.i4.1
        newobj  instance void int32[0..., 0...]::.ctor(int32, int32)
        ldc.i4.0
        ldc.i4.0
        readonly. 
        call    instance int32 int32[0..., 0...]::Get(int32, int32)
        pop
        ret
    }

    .method static public hidebysig void Prefix.UnalignedAndVolatile_Valid() cil managed 
    {
        .locals init (int32& V_0, int32 V_1)
        // ref int x;
        // int y;
        // y = *x;
        ldloc.0
        volatile.
        unaligned. 4
        ldind.i4
        stloc.1
        ret
    }

    .method static public hidebysig void Tail.FromTry_Invalid_TailCallInsideER.TailRet() cil managed 
    {
        .try
        {
            tail.
            call    void PrefixTestsType::StaticMethod()

            leave   MethodEnd
        }
        finally
        {
            endfinally
        }

    MethodEnd:
        ret
    }

    .method static public hidebysig void Tail.FollowedByRet_Valid() cil managed 
    {
        tail.
        call        void PrefixTestsType::StaticMethod()
        ret
    }

    .method static public hidebysig void Tail.NoRet_Invalid_TailRet.MethodFallthrough() cil managed 
    {
        tail.
        call        void PrefixTestsType::StaticMethod()
    }

    .method static public hidebysig void Tail.DoubleCall_Invalid_TailRet() cil managed 
    {
        tail.
        call        void PrefixTestsType::StaticMethod()
        call        void PrefixTestsType::StaticMethod()
        ret
    }

    .method static public hidebysig void Tail.BranchToRet_Valid() cil managed 
    {
        ldc.i4.0
        brfalse     MethodEnd

        tail.
        call        void PrefixTestsType::StaticMethod()

    MethodEnd:
        ret
    }

    .method static public hidebysig void Tail.ComplexBranchToRet_Valid() cil managed 
    {
        br          AfterTail

    TailCall:
        tail.
        call        void PrefixTestsType::StaticMethod()

    MethodEnd:
        ret

    AfterTail:
        ldc.i4.0
        brfalse     TailCall

        br          MethodEnd
    }

    .method static public hidebysig void Tail.BranchToNopRet_Invalid_TailRet() cil managed 
    {
        ldc.i4.0
        brfalse     MethodEnd

        tail.
        call        void PrefixTestsType::StaticMethod()

    MethodEnd:
        nop
        ret
    }

    .method static public hidebysig void Tail.Ret_Invalid_TailCall() cil managed 
    {
        tail.
        ret
    }
}
