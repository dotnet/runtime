// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly Test_HndIndex_10_Plain
{
    .custom instance void [mscorlib]System.Security.SecurityTransparentAttribute::.ctor() = ( 01 00 00 00 ) 
}

.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    // ILONLY


.class
    public auto ansi beforefieldinit sealed abstract
    Test.App
        extends [mscorlib]System.Object
{
.class nested public auto ansi Test_Test_HndIndex_10_Plain extends [mscorlib] System.Object {
    .method public hidebysig static int32 Main() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
        .maxstack 8
        .locals init (class Test.DerivedClass derivedInstance, int32 exitCode)

        .try
        {
            ldstr       "Hello World"
            ldstr       "Hello World 2"
            newobj      instance void Test.DerivedClass::.ctor(string, string)
            stloc       derivedInstance

            ldstr       "Value of Test: "
            ldloc       derivedInstance
            callvirt    instance string Test.BaseClass::get_Test()
            call        string [mscorlib]System.String::Concat(string, string)
            call        void [System.Console]System.Console::WriteLine(string)

            ldstr       "FAIL: The double ctor call worked without generating an exception"
            call        void [System.Console]System.Console::WriteLine(string)
            ldc.i4      101
            stloc       exitCode
            leave       RETURN_TO_CALLER
        }
        catch [mscorlib]System.Exception
        {
            callvirt    instance string [mscorlib]System.Exception::get_Message()
            call        void [System.Console]System.Console::WriteLine(string)

            ldstr       "PASS: The double ctor call generated an exception"
            call        void [System.Console]System.Console::WriteLine(string)
            ldc.i4      100
            stloc       exitCode
            leave       RETURN_TO_CALLER
        }

    RETURN_TO_CALLER:
        ldloc       exitCode
        ret
    }
}


    .method public hidebysig static void WriteLine(string format) cil managed noinlining
    {
        .maxstack 8

        ldarg       format
        call        void [System.Console]System.Console::WriteLine(string)
        ret
    }
}


.class
    private auto ansi beforefieldinit
    Test.BaseClass
        extends [mscorlib]System.Object
{
    .field private string test

    .method public hidebysig specialname instance string get_Test() cil managed
    {
        .maxstack 8
        ldarg.0
        ldfld       string Test.BaseClass::test
        ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor(string input) cil managed noinlining
    {
        .maxstack 8

        ldarg.0
        call        instance void [mscorlib]System.Object::.ctor()

        ldstr       "In BaseClass::BaseClass(string) with parameter \"{0}\""
        ldarg       input
        call        void [System.Console]System.Console::WriteLine(string, object)

        ldarg.0
        ldarg       input
        stfld       string Test.BaseClass::test
        ret
    }

    .property instance string Test()
    {
        .get instance string Test.BaseClass::get_Test()
    }
}


.class
    public sequential ansi sealed beforefieldinit
    Test.StackFiller16
        extends System.ValueType
{
    .field public uint64 f01
    .field public uint64 f02
    .field public uint64 f03
    .field public uint64 f04
    .field public uint64 f05
    .field public uint64 f06
    .field public uint64 f07
    .field public uint64 f08
    .field public uint64 f09
    .field public uint64 f10
    .field public uint64 f11
    .field public uint64 f12
    .field public uint64 f13
    .field public uint64 f14
    .field public uint64 f15
    .field public uint64 f16

    .method public hidebysig static void Fill(valuetype Test.StackFiller16& targetRef) cil managed noinlining
    {
        .maxstack 8

        ldarg       targetRef
        ldc.i8      0xabababababababab
        stfld       uint64 Test.StackFiller16::f01

        ldarg       targetRef
        ldc.i8      0xabababababababab
        stfld       uint64 Test.StackFiller16::f02

        ldarg       targetRef
        ldc.i8      0xabababababababab
        stfld       uint64 Test.StackFiller16::f03

        ldarg       targetRef
        ldc.i8      0xabababababababab
        stfld       uint64 Test.StackFiller16::f04

        ldarg       targetRef
        ldc.i8      0xabababababababab
        stfld       uint64 Test.StackFiller16::f05

        ldarg       targetRef
        ldc.i8      0xabababababababab
        stfld       uint64 Test.StackFiller16::f06

        ldarg       targetRef
        ldc.i8      0xabababababababab
        stfld       uint64 Test.StackFiller16::f07

        ldarg       targetRef
        ldc.i8      0xabababababababab
        stfld       uint64 Test.StackFiller16::f08

        ldarg       targetRef
        ldc.i8      0xabababababababab
        stfld       uint64 Test.StackFiller16::f09

        ldarg       targetRef
        ldc.i8      0xabababababababab
        stfld       uint64 Test.StackFiller16::f10

        ldarg       targetRef
        ldc.i8      0xabababababababab
        stfld       uint64 Test.StackFiller16::f11

        ldarg       targetRef
        ldc.i8      0xabababababababab
        stfld       uint64 Test.StackFiller16::f12

        ldarg       targetRef
        ldc.i8      0xabababababababab
        stfld       uint64 Test.StackFiller16::f13

        ldarg       targetRef
        ldc.i8      0xabababababababab
        stfld       uint64 Test.StackFiller16::f14

        ldarg       targetRef
        ldc.i8      0xabababababababab
        stfld       uint64 Test.StackFiller16::f15

        ldarg       targetRef
        ldc.i8      0xabababababababab
        stfld       uint64 Test.StackFiller16::f16
        ret
    }
}


.class
    private auto ansi beforefieldinit
    Test.DerivedClass
        extends Test.BaseClass
{
    .method public hidebysig specialname rtspecialname instance void .ctor(string input, string input2) cil managed noinlining
    {
        .maxstack 8

        ldarg.0
        ldarg       input
        call        instance void Test.BaseClass::.ctor(string)

        call        void Test.DerivedClass::SetExceptionSP()

        ldarg       input
        call        void Test.DerivedClass::FillStackAndCallout(string)
        ret
    }


    .method public hidebysig static void SetExceptionSP() cil managed noinlining
    {
        .maxstack 8

        .try
        {
            ldstr       "In the setup try block."
            call        void Test.App::WriteLine(string)
            call        void Test.DerivedClass::ThrowInvalidOperationException()
            leave       RETURN_TO_CALLER
        }
        catch [mscorlib]System.InvalidOperationException
        {
            ldstr       "In the setup catch block."
            call        void Test.App::WriteLine(string)
            leave       RETURN_TO_CALLER
        }

    RETURN_TO_CALLER:
        ret
    }


    .method public hidebysig static void FillStackAndCallout(string input) cil managed noinlining
    {
        .maxstack 8

        .locals init (valuetype Test.StackFiller16 filler1)

        ldloca      filler1
        call        void Test.StackFiller16::Fill(valuetype Test.StackFiller16&)

        ldarg       input
        ldloca      filler1
        call        void Test.DerivedClass::AccessExceptionSP(string, valuetype Test.StackFiller16&)
        ret
    }


    .method public hidebysig static void AccessExceptionSP(string input, valuetype Test.StackFiller16& filler1) cil managed noinlining
    {
        .maxstack 8

        .try
        {
            ldstr       "In the outer try block."
            call        void Test.App::WriteLine(string)
            leave       RETURN_TO_CALLER
        }
        finally
        {
            ldstr       "In the outer finally block."
            call        void Test.App::WriteLine(string)


            ldarg       input
            brinst      ENTER_INNER_CATCH

            .try
            {
                ldstr       "In the inner try block."
                call        void Test.App::WriteLine(string)
                leave       DONE_WITH_INNER_TRY
            }
            catch [mscorlib]System.InvalidOperationException
            {
                pop


            ENTER_INNER_CATCH:
                ldstr       "In the inner catch block."
                call        void Test.App::WriteLine(string)
                leave       DONE_WITH_INNER_TRY
            }

        DONE_WITH_INNER_TRY:
            endfinally
        }

    RETURN_TO_CALLER:
        ldstr       "In the return block."
        call        void Test.App::WriteLine(string)
        br          REAL_RETURN_BLOCK

    REAL_RETURN_BLOCK:
        ldstr       "In the real return block."
        call        void Test.App::WriteLine(string)
        ret
    }


    .method public hidebysig static void ThrowInvalidOperationException() cil managed noinlining
    {
        .maxstack 8
        newobj      instance void [mscorlib]System.InvalidOperationException::.ctor()
        throw
    }
}

