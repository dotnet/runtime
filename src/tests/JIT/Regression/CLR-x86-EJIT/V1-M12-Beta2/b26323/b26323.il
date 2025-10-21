// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b26323 { }
.assembly extern xunit.core {}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary {}
.namespace Test
{
  .class public auto ansi App extends [mscorlib]System.Object
  {
    .field public int32 m_nField
    .method public specialname rtspecialname instance vararg void .ctor() il managed
    {
		.maxstack  8
		ldarg.0
		call       instance void [mscorlib]System.Object::.ctor()
		ldarg.0
		ldc.i4     5
		stfld      int32 Test.App::m_nField
		ldstr      "In constructor."
		call       void [System.Console]System.Console::WriteLine(class System.String)
		ret
    }

    .method public static int32 Main() il managed
    {
      // [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsVarArgSupported))]
      .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ConditionalFactAttribute::.ctor(class [System.Runtime]System.Type,
                                                                                                    string[]) = ( 01 00 61 54 65 73 74 4C 69 62 72 61 72 79 2E 50   // ..aTestLibrary.P
                                                                                                                  6C 61 74 66 6F 72 6D 44 65 74 65 63 74 69 6F 6E   // latformDetection
                                                                                                                  2C 20 54 65 73 74 4C 69 62 72 61 72 79 2C 20 56   // , TestLibrary, V
                                                                                                                  65 72 73 69 6F 6E 3D 30 2E 30 2E 30 2E 30 2C 20   // ersion=0.0.0.0, 
                                                                                                                  43 75 6C 74 75 72 65 3D 6E 65 75 74 72 61 6C 2C   // Culture=neutral,
                                                                                                                  20 50 75 62 6C 69 63 4B 65 79 54 6F 6B 65 6E 3D   //  PublicKeyToken=
                                                                                                                  6E 75 6C 6C 01 00 00 00 11 49 73 56 61 72 41 72   // null.....IsVarAr
                                                                                                                  67 53 75 70 70 6F 72 74 65 64 00 00 )             // gSupported..
      .entrypoint
      .maxstack  2
      .locals ([0] class Test.App app)
      IL_0001:  newobj     instance vararg void Test.App::.ctor(...)
      IL_0006:  stloc.0
      IL_0007:  ldloc.0
      IL_0008:  ldfld      int32 Test.App::m_nField
      IL_000d:  ldc.i4.5
      IL_000e:  beq.s      IL_001c

      IL_0010:  ldstr      "**** TEST FAILED ****"
      IL_0015:  call       void [System.Console]System.Console::WriteLine(class System.String)
      
      IL_001a:  ldc.i4.1
      IL_001b:  ret

      IL_001c:  ldstr      "**** TEST PASSED ****"
      IL_0021:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0026:  ldc.i4 100
      IL_0027:  ret
    }
  }
}

