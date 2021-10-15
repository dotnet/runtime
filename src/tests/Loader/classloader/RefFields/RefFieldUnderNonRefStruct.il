.assembly _
{
    .custom instance void [System.Private.CoreLib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = (
        01 00 08 00 00 00 00 00
    )
    .custom instance void [System.Private.CoreLib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = (
        01 00 01 00 54 02 16 57 72 61 70 4e 6f 6e 45 78
        63 65 70 74 69 6f 6e 54 68 72 6f 77 73 01
    )
    .custom instance void [System.Private.CoreLib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [System.Private.CoreLib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = (
        01 00 07 01 00 00 00 00
    )
    .permissionset reqmin = (
        2e 01 80 92 53 79 73 74 65 6d 2e 53 65 63 75 72
        69 74 79 2e 50 65 72 6d 69 73 73 69 6f 6e 73 2e
        53 65 63 75 72 69 74 79 50 65 72 6d 69 73 73 69
        6f 6e 41 74 74 72 69 62 75 74 65 2c 20 53 79 73
        74 65 6d 2e 50 72 69 76 61 74 65 2e 43 6f 72 65
        4c 69 62 2c 20 56 65 72 73 69 6f 6e 3d 35 2e 30
        2e 30 2e 30 2c 20 43 75 6c 74 75 72 65 3d 6e 65
        75 74 72 61 6c 2c 20 50 75 62 6c 69 63 4b 65 79
        54 6f 6b 65 6e 3d 37 63 65 63 38 35 64 37 62 65
        61 37 37 39 38 65 15 01 54 02 10 53 6b 69 70 56
        65 72 69 66 69 63 61 74 69 6f 6e 01
    )
    .hash algorithm 0x00008004 // SHA1
    .ver 0:0:0:0
}

.class private auto ansi '<Module>'
{
} // end of class <Module>

.class public auto ansi sealed beforefieldinit S
    extends [System.Private.CoreLib]System.ValueType
{
    // Fields
    .field public string& f

    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            string& x
        ) cil managed 
    {
        // Method begins at RVA 0x2050
        // Code size 10 (0xa)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldarg.0
        IL_0002: ldarg.1
        IL_0004: stfld string& S::f
        IL_0009: ret
    } // end of method S::.ctor

} // end of class S

.class public auto ansi abstract sealed beforefieldinit R
    extends [System.Private.CoreLib]System.Object
{
    // Methods
    .method public hidebysig static 
        void CreateStruct (
            string str
        ) cil managed noinlining 
    {
        // Method begins at RVA 0x205c
        // Code size 10 (0xa)
        .maxstack 1
        .locals init (
            [0] valuetype S s
        )

        IL_0000: nop
        IL_0001: ldarga.s str
        IL_0003: newobj instance void S::.ctor(string&)
        IL_0008: stloc.0
        IL_0009: ret
    } // end of method R::CreateStruct

    .method public hidebysig static 
        int32 Main () cil managed 
    {
        // Method begins at RVA 0x2078
        // Code size 28 (0x1c)
        .entrypoint
        .maxstack 1
        .locals init (
            [0] string hi,
            [1] int32
        )

        IL_0000: nop
        IL_0001: ldstr "hello"
        IL_0006: stloc.0
        .try
        {
            IL_0007: nop
            IL_0008: ldloc.0
            IL_0009: call void R::CreateStruct(string)
            IL_000e: nop
            IL_000f: ldc.i4.1
            IL_0010: stloc.1
            IL_0011: leave.s IL_001a
        } // end .try
        catch [System.Private.CoreLib]System.TypeLoadException
        {
            IL_0013: pop
            IL_0014: nop
            IL_0015: ldc.i4.s 100
            IL_0017: stloc.1
            IL_0018: leave.s IL_001a
        } // end handler

        IL_001a: ldloc.1
        IL_001b: ret
    } // end of method R::Main

} // end of class R

