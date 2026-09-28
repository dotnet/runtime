.assembly JmpGenericTest {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 0:0:0:0
}
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.assembly extern xunit.assert {}

.assembly genericjmp { }

.class public auto ansi beforefieldinit JmpGenericToGeneric extends [System.Runtime]System.Object
{
    .method public static void Target<T>() cil managed
    {
        .maxstack 8
        ldstr "Target<T> called"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void Source<T>() cil managed
    {
        jmp void JmpGenericToGeneric::Target<!!0>()
    }

    .method public static void TestEntryPoint() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        call void JmpGenericToGeneric::Source<int32>()
        ret
    }
}

.class public auto ansi beforefieldinit JmpGenericToRegular extends [System.Runtime]System.Object
{
    .method public static void Target() cil managed
    {
        .maxstack 8
        ldstr "Regular Target called"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void Source<T>() cil managed
    {
        jmp void JmpGenericToRegular::Target()
    }

    .method public static void TestEntryPoint() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .try
        {
            call void JmpGenericToRegular::Source<int32>()
            ldstr "JMP from generic to regular method is not allowed"
			call void [xunit.assert]Xunit.Assert::Fail(string)
            leave END
        }
        catch [System.Runtime]System.InvalidProgramException
        {
            leave END
        }
    END:
        ret
    }
}

.class public auto ansi beforefieldinit JmpRegularToGeneric extends [System.Runtime]System.Object
{
    .method public static void Target<T>() cil managed
    {
        .maxstack 8
        ldstr "Generic Target<T> called"
        call void [System.Console]System.Console::WriteLine(string)
        ret
    }

    .method public static void Source() cil managed
    {
        jmp void JmpRegularToGeneric::Target<int32>()
    }

    .method public static void TestEntryPoint() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
         .try
        {
            call void JmpRegularToGeneric::Source()
            ldstr "JMP from regular to generic method is not allowed"
 			call void [xunit.assert]Xunit.Assert::Fail(string)
            leave END
        }
        catch [System.Runtime]System.InvalidProgramException
        {
            leave END
        }
    END:
        ret
    }
}

.method public static int32 Main() il managed
{
    .entrypoint
    call void [genericjmp]JmpGenericToGeneric::TestEntryPoint()
    call void [genericjmp]JmpGenericToRegular::TestEntryPoint()
    call void [genericjmp]JmpRegularToGeneric::TestEntryPoint()
    ldc.i4 100
    ret
}