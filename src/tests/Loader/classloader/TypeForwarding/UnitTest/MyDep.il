// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern MyDep2 { }

.assembly MyDep
{
	.ver 1:0:0:0
}


.class extern forwarder MyDep2.Enclosing1   { .assembly extern MyDep2 }
.class extern Nested1                       { .class extern MyDep2.Enclosing1 }
.class extern SubNested1                    { .class extern MyDep2.Enclosing1/Nested1 }
.class extern SubNested2                    { .class extern MyDep2.Enclosing1/Nested1 }
.class extern Nested2                       { .class extern MyDep2.Enclosing1 }
.class extern SubNested1                    { .class extern MyDep2.Enclosing1/Nested2 }
.class extern SubNested2                    { .class extern MyDep2.Enclosing1/Nested2 }
.class extern forwarder MyDep2.Enclosing2   { .assembly extern MyDep2 }
.class extern Nested1                       { .class extern MyDep2.Enclosing2 }
.class extern SubNested1                    { .class extern MyDep2.Enclosing2/Nested1 }
.class extern SubNested2                    { .class extern MyDep2.Enclosing2/Nested1 }
.class extern Nested2                       { .class extern MyDep2.Enclosing2 }
.class extern SubNested1                    { .class extern MyDep2.Enclosing2/Nested2 }
.class extern SubNested2                    { .class extern MyDep2.Enclosing2/Nested2 }


.class extern forwarder MyDep3.Enclosing1   { .assembly extern MyDep2 }
.class extern Nested1                       { .class extern MyDep3.Enclosing1 }
.class extern SubNested1                    { .class extern MyDep3.Enclosing1/Nested1 }
.class extern SubNested2                    { .class extern MyDep3.Enclosing1/Nested1 }
.class extern Nested2                       { .class extern MyDep3.Enclosing1 }
.class extern SubNested1                    { .class extern MyDep3.Enclosing1/Nested2 }
.class extern SubNested2                    { .class extern MyDep3.Enclosing1/Nested2 }
.class extern forwarder MyDep3.Enclosing2   { .assembly extern MyDep2 }
.class extern Nested1                       { .class extern MyDep3.Enclosing2 }
.class extern SubNested1                    { .class extern MyDep3.Enclosing2/Nested1 }
.class extern SubNested2                    { .class extern MyDep3.Enclosing2/Nested1 }
.class extern Nested2                       { .class extern MyDep3.Enclosing2 }
.class extern SubNested1                    { .class extern MyDep3.Enclosing2/Nested2 }
.class extern SubNested2                    { .class extern MyDep3.Enclosing2/Nested2 }


.class public auto ansi beforefieldinit MyDep.Enclosing1
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi beforefieldinit Nested1
        extends [mscorlib]System.Object
    {
        // Nested Types
        .class nested public auto ansi beforefieldinit SubNested1
            extends [mscorlib]System.Object
        {
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
            } // end of method SubNested1::.ctor

        } // end of class SubNested1

        .class nested public auto ansi beforefieldinit SubNested2
            extends [mscorlib]System.Object
        {
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
            } // end of method SubNested2::.ctor

        } // end of class SubNested2


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
        } // end of method Nested1::.ctor

    } // end of class Nested1

    .class nested public auto ansi beforefieldinit Nested2
        extends [mscorlib]System.Object
    {
        // Nested Types
        .class nested public auto ansi beforefieldinit SubNested1
            extends [mscorlib]System.Object
        {
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
            } // end of method SubNested1::.ctor

        } // end of class SubNested1

        .class nested public auto ansi beforefieldinit SubNested2
            extends [mscorlib]System.Object
        {
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
            } // end of method SubNested2::.ctor

        } // end of class SubNested2


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
        } // end of method Nested2::.ctor

    } // end of class Nested2


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
    } // end of method Enclosing1::.ctor

} // end of class MyDep.Enclosing1

.class public auto ansi beforefieldinit MyDep.Enclosing2
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi beforefieldinit Nested1
        extends [mscorlib]System.Object
    {
        // Nested Types
        .class nested public auto ansi beforefieldinit SubNested1
            extends [mscorlib]System.Object
        {
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
            } // end of method SubNested1::.ctor

        } // end of class SubNested1

        .class nested public auto ansi beforefieldinit SubNested2
            extends [mscorlib]System.Object
        {
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
            } // end of method SubNested2::.ctor

        } // end of class SubNested2


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
        } // end of method Nested1::.ctor

    } // end of class Nested1

    .class nested public auto ansi beforefieldinit Nested2
        extends [mscorlib]System.Object
    {
        // Nested Types
        .class nested public auto ansi beforefieldinit SubNested1
            extends [mscorlib]System.Object
        {
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
            } // end of method SubNested1::.ctor

        } // end of class SubNested1

        .class nested public auto ansi beforefieldinit SubNested2
            extends [mscorlib]System.Object
        {
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
            } // end of method SubNested2::.ctor

        } // end of class SubNested2


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
        } // end of method Nested2::.ctor

    } // end of class Nested2


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
    } // end of method Enclosing2::.ctor

} // end of class MyDep.Enclosing2
