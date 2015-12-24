// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.module 'gcctor.exe'
.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'valctor'// as "valctor.exe"
{
//This byte field requests that this assembly not be verified at run time and corresponds to this C# declaration:
//[assembly:System.Security.Permissions.SecurityPermissionAttribute( [mscorlib]System.Security.Permissions.SecurityAction.RequestMinimum, Flags=System.Security.Permissions.SecurityPermissionFlag.SkipVerification )]
}
.namespace Test
{
  .class auto ansi sealed value App extends [mscorlib]System.ValueType
  {
    .field public int32 m_nField
    .method public specialname rtspecialname instance vararg void .ctor() il managed
    {
		.maxstack  8
		//--------------------------------------------------
		//	This constructor should execute. But it doesn't.
		//--------------------------------------------------
		ldarg.0
		ldc.i4     5
		stfld      int32 Test.App::m_nField
		ldstr      "In constructor."
		call       void [System.Console]System.Console::WriteLine(class System.String)
		ret
    }

    .method private static int32 Main() il managed
    {
      .entrypoint
      .maxstack  2
      .locals ([0] value class Test.App app)
      //-------------------------------
      //	Calling constructor....
      //-------------------------------
      			ldc.i4.0
      IL_0001:  newobj     instance vararg void Test.App::.ctor(..., int32)
      IL_0006:  stloc.0
      IL_0007:  ldloca 0
      IL_0008:  ldfld      int32 Test.App::m_nField
      IL_000d:  ldc.i4.5
      IL_000e:  beq.s      IL_001c

      IL_0010:  ldstr      "**** TEST FAILED ****"
      IL_0015:  call       void [System.Console]System.Console::WriteLine(class System.String)
      
      IL_001a:  ldc.i4.1
      IL_001b:  ret

      IL_001c:  ldstr      "**** TEST PASSED ****"
      IL_0021:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0026:  ldc.i4    0x64
      IL_0027:  ret
    }
  }
}

