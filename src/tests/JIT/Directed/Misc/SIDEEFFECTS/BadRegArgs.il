// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'BadRegArgs'
{
}
// Original test by sborde modified by bwadswor
.class public BadRegArgs
{
  .field public static int32 s_i
  .field public static class [mscorlib]System.Object s_Obj

  .method public static int32 foo1(int32 a, int32 b) il managed
  {
	.locals(int32)

	call void [System.Console]System.Console::WriteLine()

	ldarg a						// a += s_i
	ldsfld int32 BadRegArgs::s_i
	add
	starg a

	ldstr "Should print 100 : "
	call void [System.Console]System.Console::Write(class [mscorlib]System.String)
	ldarg a
	call void [System.Console]System.Console::WriteLine(int32)
	
	ldarg a
	ldc.i4 100
	bne.un ERR_EXIT


	ldsfld int32 BadRegArgs::s_i			// b = s_i; So b is DEAD on entry
	starg b

	ldarg b						// b += s_i
	ldsfld int32 BadRegArgs::s_i
	add
	starg b

	ldstr "Should print 0 : "
	call void [System.Console]System.Console::Write(class [mscorlib]System.String)
	ldarg b
	call void [System.Console]System.Console::WriteLine(int32)
	ldarg b
	ldc.i4 0
	bne.un ERR_EXIT
	ldc.i4 0 
	ret

ERR_EXIT:
	ldc.i4 1
	ret

  }


  .method public static int32 foo2(int32 a, int32 b) il managed
  {
	.locals(int32 temp,
		class [mscorlib]System.Object obj1,
		class [mscorlib]System.Object obj2,
		class [mscorlib]System.Object obj3,
		class [mscorlib]System.Object obj4,
		class [mscorlib]System.Object obj5,
		class [mscorlib]System.Object obj6)

	ldloca obj1
	pop
	ldloc obj1
	stsfld class [mscorlib]System.Object BadRegArgs::s_Obj

	ldloca obj2
	pop
	ldloc obj2
	stsfld class [mscorlib]System.Object BadRegArgs::s_Obj

	ldloca obj3
	pop
	ldloc obj3
	stsfld class [mscorlib]System.Object BadRegArgs::s_Obj

	ldloca obj4
	pop
	ldloc obj4
	stsfld class [mscorlib]System.Object BadRegArgs::s_Obj

	ldloca obj5
	pop
	ldloc obj5
	stsfld class [mscorlib]System.Object BadRegArgs::s_Obj

	ldloca obj6
	pop
	ldloc obj6
	stsfld class [mscorlib]System.Object BadRegArgs::s_Obj

	ldarg a
	ldarg b
	add
	stloc temp

	ldstr "Should print 300 : "
	call void [System.Console]System.Console::Write(class [mscorlib]System.String)
	ldloc temp
	call void [System.Console]System.Console::WriteLine(int32)

	ldloc temp
	ldc.i4 300
	bne.un ERR_EXIT

	ldc.i4 0
	ret

ERR_EXIT:
	ldc.i4 1
	ret

  }

//----------------------------------------------------------------------------


  .method public static int32 main() il managed
  {
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.maxstack 10
	.locals (int32,int32,int32)


	ldc.i4 100
	ldc.i4 200
	call int32 BadRegArgs::foo1(int32, int32)

	ldc.i4 100
	ldc.i4 200
	call int32 BadRegArgs::foo2(int32, int32)

	add

	ldc.i4 0x64
	add

	ret
  }

}

