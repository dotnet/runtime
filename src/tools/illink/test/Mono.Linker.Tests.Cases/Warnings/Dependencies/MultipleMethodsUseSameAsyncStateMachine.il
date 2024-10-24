.assembly extern System.Runtime
{
    .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
}

.assembly 'library'
{
    .hash algorithm 0x00008004
    .ver 1:0:0:0
}

.namespace Mono.Linker.Tests.Cases.Warnings.Dependencies
{
    .class public auto ansi beforefieldinit MultipleMethodsUseSameAsyncStateMachine
        extends [System.Runtime]System.Object
    {
        // Nested Types
        .class nested public sequential ansi sealed beforefieldinit '<StateMachine>d'
            extends [System.Runtime]System.ValueType
            implements [System.Runtime]System.Runtime.CompilerServices.IAsyncStateMachine
        {
            // Fields
            .field public static int32 'field'

            // Methods
            .method public final hidebysig newslot virtual 
                instance void MoveNext () cil managed 
            {
                // Method begins at RVA 0x207a
                // Code size 9 (0x9)
                .maxstack 8

                IL_0000: nop
                IL_0001: ldarg.0
                IL_0002: ldc.i4.1
                IL_0003: stfld int32 Mono.Linker.Tests.Cases.Warnings.Dependencies.MultipleMethodsUseSameAsyncStateMachine/'<StateMachine>d'::'field'
                IL_0008: ret
            } // end of method '<StateMachine>d'::MoveNext

            .method private final hidebysig newslot virtual 
                instance void System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine (
                    class [System.Runtime]System.Runtime.CompilerServices.IAsyncStateMachine stateMachine
                ) cil managed 
            {
                .custom instance void [System.Runtime]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
                    01 00 01 00 00
                )
                .override method instance void [System.Runtime]System.Runtime.CompilerServices.IAsyncStateMachine::SetStateMachine(class [System.Runtime]System.Runtime.CompilerServices.IAsyncStateMachine)
                // Method begins at RVA 0x2050
                // Code size 2 (0x2)
                .maxstack 8

                IL_0000: nop
                IL_0001: ret
            } // end of method '<StateMachine>d'::System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine

        } // end of class '<StateMachine>d'


        // Methods
        .method public hidebysig static 
            void M () cil managed 
        {
            .custom instance void [System.Runtime]System.Runtime.CompilerServices.AsyncStateMachineAttribute::.ctor(class [System.Runtime]System.Type) = (
                01 00 65 4D 6F 6E 6F 2E 4C 69 6E 6B 65 72 2E 54
                65 73 74 73 2E 43 61 73 65 73 2E 57 61 72 6E 69
                6E 67 73 2E 44 65 70 65 6E 64 65 6E 63 69 65 73
                2E 4D 75 6C 74 69 70 6C 65 4D 65 74 68 6F 64 73
                55 73 65 53 61 6D 65 41 73 79 6E 63 53 74 61 74
                65 4D 61 63 68 69 6E 65 2B 3C 53 74 61 74 65 4D
                61 63 68 69 6E 65 3E 64 00 00
            )
            .maxstack 8

            IL_0000: call void Mono.Linker.Tests.Cases.Warnings.Dependencies.MultipleMethodsUseSameAsyncStateMachine::RUC();
            IL_0005: call void Mono.Linker.Tests.Cases.Warnings.Dependencies.MultipleMethodsUseSameAsyncStateMachine::'<M>g__LocalFunction'()
            IL_000a: ret
        } // end of method MultipleMethodsUseSameAsyncStateMachine::M

        .method private hidebysig static 
            void RUC () cil managed 
        {
            .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute::.ctor(string) = (
                01 00 03 52 55 43 00 00
            )
            // Method begins at RVA 0x205c
            // Code size 1 (0x1)
            .maxstack 8

            IL_0000: ret
        } // end of method MultipleMethodsUseSameAsyncStateMachine::RUC

        .method public hidebysig static 
            void '<M>g__LocalFunction' () cil managed 
        {
            .custom instance void [System.Runtime]System.Runtime.CompilerServices.AsyncStateMachineAttribute::.ctor(class [System.Runtime]System.Type) = (
                01 00 65 4D 6F 6E 6F 2E 4C 69 6E 6B 65 72 2E 54
                65 73 74 73 2E 43 61 73 65 73 2E 57 61 72 6E 69
                6E 67 73 2E 44 65 70 65 6E 64 65 6E 63 69 65 73
                2E 4D 75 6C 74 69 70 6C 65 4D 65 74 68 6F 64 73
                55 73 65 53 61 6D 65 41 73 79 6E 63 53 74 61 74
                65 4D 61 63 68 69 6E 65 2B 3C 53 74 61 74 65 4D
                61 63 68 69 6E 65 3E 64 00 00
            )
            .maxstack 2
            .locals init (
                [0] valuetype Mono.Linker.Tests.Cases.Warnings.Dependencies.MultipleMethodsUseSameAsyncStateMachine/'<StateMachine>d' s
            )

            IL_0000: nop
            IL_0001: ldloca.s 0
            IL_0003: initobj Mono.Linker.Tests.Cases.Warnings.Dependencies.MultipleMethodsUseSameAsyncStateMachine/'<StateMachine>d'
            IL_0009: ldloca.s 0
            IL_000b: ldc.i4.0
            IL_000c: stfld int32 Mono.Linker.Tests.Cases.Warnings.Dependencies.MultipleMethodsUseSameAsyncStateMachine/'<StateMachine>d'::'field'
            IL_0011: ldloca.s 0
            IL_0013: call instance void Mono.Linker.Tests.Cases.Warnings.Dependencies.MultipleMethodsUseSameAsyncStateMachine/'<StateMachine>d'::MoveNext()
            IL_0018: nop
            IL_0019: ret
        } // end of method MultipleMethodsUseSameAsyncStateMachine::'<M>g__LocalFunction'

        .method public hidebysig specialname rtspecialname 
            instance void .ctor () cil managed 
        {
            // Method begins at RVA 0x2053
            // Code size 8 (0x8)
            .maxstack 8

            IL_0000: ldarg.0
            IL_0001: call instance void [System.Runtime]System.Object::.ctor()
            IL_0006: nop
            IL_0007: ret
        } // end of method MultipleMethodsUseSameAsyncStateMachine::.ctor

    } // end of class Mono.Linker.Tests.Cases.Warnings.Dependencies.MultipleMethodsUseSameAsyncStateMachine

}
