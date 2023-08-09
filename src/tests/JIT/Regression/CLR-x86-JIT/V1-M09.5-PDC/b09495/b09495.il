// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b09495
{
}
.class public SideEffects
{
  .field public static int32 m_i
  .field public static int32 s_i
  .field public static value class [mscorlib]System.Decimal s_d

  .field public static int32 m_exitcode
//----------------------------------------------------------------------------

  .method public static void AliasedLocal() il managed
  {
	.locals(int32 aliasedLocal, int32 & addrOfLocal, int32 temp)

	ldsfld int32 SideEffects::s_i	// aliasedLocal = 0
	stloc aliasedLocal

	ldloca aliasedLocal		// addrOfLocal = &aliasedLocal
	stloc addrOfLocal

	ldloc aliasedLocal		// push aliasedLocal

	ldloc addrOfLocal		// *addrOfLocal = 5
	ldc.i4 5
	stind.i4

	stloc temp			// temp = pushed alisasedLocal

	ldstr "Should print 0 : "
	call void [System.Console]System.Console::Write(class [mscorlib]System.String)

	ldloc temp
	dup
	ldsfld     int32 SideEffects::m_exitcode
	or
	stsfld     int32 SideEffects::m_exitcode
	
	call void [System.Console]System.Console::WriteLine(int32)

	ret
  }


//----------------------------------------------------------------------------

  .method public static void AliasedLocalCall_helper(int32 & addrOfLocal) il managed
  {
	.try // to avoid inlining
	{
		ldarg addrOfLocal	// *addrOfLocal = 5
		ldc.i4 5
		stind.i4
		leave DONE
	}
	catch [mscorlib]System.Exception
	{
		pop
		newobj instance void [mscorlib]System.Exception::.ctor()
		throw
	}
DONE:
	ret
  }

  .method public static void AliasedLocalCall() il managed
  {
	.locals(int32 aliasedLocal, int32 & addrOfLocal, int32 temp)

	ldsfld int32 SideEffects::s_i	// aliasedLocal = 0
	stloc aliasedLocal

	ldloca aliasedLocal		// addrOfLocal = &aliasedLocal
	stloc addrOfLocal

	ldloc aliasedLocal		// push aliasedLocal

	ldloc addrOfLocal		// AliasedLocalCall_helper(addrOfLocal)
	call void SideEffects::AliasedLocalCall_helper(int32&)

	stloc temp			// temp = pushed alisasedLocal

	ldstr "Should print 0 : "
	call void [System.Console]System.Console::Write(class [mscorlib]System.String)

	ldloc temp
	dup
	ldsfld     int32 SideEffects::m_exitcode
	or
	stsfld     int32 SideEffects::m_exitcode
	call void [System.Console]System.Console::WriteLine(int32)

	ret
  }

//----------------------------------------------------------------------------

  .method public static void AliasedLocalIndir() il managed
  {
	.locals(int32 aliasedLocal, int32 & addrOfLocal, int32 temp)

	ldsfld int32 SideEffects::s_i	// aliasedLocal = 0
	stloc aliasedLocal

	ldloca aliasedLocal		// addrOfLocal = &aliasedLocal
	stloc addrOfLocal

	ldloc addrOfLocal		// push *addrOfLocal
	ldind.i4

	ldloc addrOfLocal		// *addrOfLocal = 5
	ldc.i4 5
	stind.i4

	stloc temp			// temp = pushed *addrOfLocal

	ldstr "Should print 0 : "
	call void [System.Console]System.Console::Write(class [mscorlib]System.String)

	ldloc temp
	dup
	ldsfld     int32 SideEffects::m_exitcode
	or
	stsfld     int32 SideEffects::m_exitcode
	call void [System.Console]System.Console::WriteLine(int32)

	ret
  }

//----------------------------------------------------------------------------

  .method public static void AliasedStatic() il managed
  {
	.locals(int32 & addrOfStatic, int32 temp)

	ldsflda int32 SideEffects::s_i	// addrOfStatic = &si
	stloc addrOfStatic

	ldsfld int32 SideEffects::s_i	// push s_i

	ldloc addrOfStatic		// *addrOfStatic = 5
	ldc.i4 5
	stind.i4

	stloc temp			// temp = pushed s_i

	ldstr "Should print 0 : "
	call void [System.Console]System.Console::Write(class [mscorlib]System.String)

	ldloc temp
	dup
	ldsfld     int32 SideEffects::m_exitcode
	or
	stsfld     int32 SideEffects::m_exitcode
	call void [System.Console]System.Console::WriteLine(int32)

	ret
  }

//----------------------------------------------------------------------------

  .method public static void SpillStatic_helper() il managed
  {
	.try // to avoid inlining
	{
		ldc.i4 5		// s_i = 5
		stsfld int32 SideEffects::s_i
		leave DONE
	}
	catch [mscorlib]System.Exception
	{
		pop
		newobj instance void [mscorlib]System.Exception::.ctor()
		throw
	}
DONE:
	ret
  }

  .method public static void SpillStatic() il managed
  {
	.locals(int32 & addrOfStatic, int32 temp)

	ldsfld int32 SideEffects::s_i	// push s_i

	call void SideEffects::SpillStatic_helper()

	stloc temp			// temp = pushed s_i

	ldstr "Should print 0 : "
	call void [System.Console]System.Console::Write(class [mscorlib]System.String)

	ldloc temp
	dup
	ldsfld     int32 SideEffects::m_exitcode
	or
	stsfld     int32 SideEffects::m_exitcode
	call void [System.Console]System.Console::WriteLine(int32)

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

	ldc.i4 0
	stsfld     int32 SideEffects::m_exitcode
	
	ldc.i4 0
	stsfld int32 SideEffects::s_i
	call void SideEffects::AliasedLocal()

	ldc.i4 0
	stsfld int32 SideEffects::s_i
	call void SideEffects::AliasedLocalCall()

	ldc.i4 0
	stsfld int32 SideEffects::s_i
	call void SideEffects::AliasedLocalIndir()

	ldc.i4 0
	stsfld int32 SideEffects::s_i
	call void SideEffects::AliasedStatic()
	
	ldc.i4 0
	stsfld int32 SideEffects::s_i
	call void SideEffects::SpillStatic()

	ldsfld     int32 SideEffects::m_exitcode
	ldc.i4 100
	add
	ret
  }

  .method public specialname rtspecialname instance void .ctor(int32) il managed
  {
	ldarg.0
	call       instance void [mscorlib]System.Object::.ctor()
	ldc.i4 555
	stsfld int32 SideEffects::m_i
	ret
  }

}

