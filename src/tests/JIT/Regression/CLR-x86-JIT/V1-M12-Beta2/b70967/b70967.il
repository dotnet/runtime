// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly bug
{
}
.module bug.exe
// MVID: {E3F734BC-107A-4058-AF49-D8BBB8B75D93}

.file alignment 512

// Image base: 0x03010000
.namespace JitTest
{
  .class private auto ansi beforefieldinit Test extends [mscorlib]System.Object
  {
    .method private hidebysig static int32 Main() cil managed
    {
      .entrypoint
      .maxstack  8
      .locals (int32 V_0)
      ldc.i4.2
      stloc.0
	  .try {
      AGAIN:
	   ldloc.0
       switch(AGAIN)
       leave END
      } catch [mscorlib]System.Exception {
        pop
        leave END
      }
     END:
     ldc.i4 100
      ret
    } // end of method Test::Main

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    // SIG: 20 00 01
    {
      // Method begins at RVA 0x206d
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  /* 02   |                  */ ldarg.0
      IL_0001:  /* 28   | (0A)000003       */ call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  /* 2A   |                  */ ret
    } // end of method Test::.ctor

  } // end of class Test

} // end of namespace JitTest

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file b.res
