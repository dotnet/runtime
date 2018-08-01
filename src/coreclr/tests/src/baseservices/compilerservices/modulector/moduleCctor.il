// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern mscorlib { auto }
.assembly moduleCctor {}
.assembly extern FieldTypes { auto }


 .method private hidebysig specialname rtspecialname static
          void  .cctor() cil managed
  {
    .maxstack  8
    nop
    ldsfld int32 IntHolder::val
    ldc.i4.1
    add
    stsfld int32 IntHolder::val
//    ldstr      "modCctor.txt"
//    ldstr      "inside .cctor"
//    call       void [mscorlib]System.IO.File::WriteAllText(string,
//                                                           string)
    ret
  } // end of method Foo::.cctor


// =============== CLASS MEMBERS DECLARATION ===================
.class public auto ansi beforefieldinit IntHolder
    extends [mscorlib]System.Object
{
    // Fields
    .field public static int32 val

    // Methods
    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        // Method begins at RVA 0x2050
        // Code size 8 (0x8)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    } // end of method IntHolder::.ctor

    .method private hidebysig specialname rtspecialname static
        void .cctor () cil managed
    {
        // Method begins at RVA 0x2059
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldc.i4.0
        IL_0001: stsfld int32 IntHolder::val
        IL_0006: ret
    } // end of method IntHolder::.cctor

        .method public hidebysig static
            void Assign (
                    int32 arg
                    ) cil managed
            {
                // Method begins at RVA 0x2078
                // Code size 8 (0x8)
                .maxstack 8

                    IL_0000: nop
                    IL_0001: ldarg.0
                    IL_0002: stsfld int32 IntHolder::val
                    IL_0007: ret
            } // end of method IntHolder::Assign
        .method public hidebysig static
            void Check (
                    int32 arg
                    ) cil managed
            {
                // Method begins at RVA 0x2050
                // Code size 28 (0x1c)
                .maxstack 2
                    .locals init (
                            [0] bool
                            )

                    IL_0000: nop
                    IL_0001: ldsfld int32 IntHolder::val
                    IL_0006: ldarg.0
                    IL_0007: ceq
                    IL_0009: ldc.i4.0
                    IL_000a: ceq
                    IL_000c: stloc.0
                    IL_000d: ldloc.0
                    IL_000e: brfalse.s IL_001b

                    IL_0010: ldstr "Mod Ctor did not functon correctly"
                    IL_0015: newobj instance void [mscorlib]System.Exception::.ctor(string)
                    IL_001a: throw

                    IL_001b: ret
            } // end of method IntHolder::Check
} // end of class IntHolder
