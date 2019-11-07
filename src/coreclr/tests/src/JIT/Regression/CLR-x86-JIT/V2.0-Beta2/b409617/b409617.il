// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly asm1{}

.data LONG_DATA = int64(0x0000000100000002)

.class sealed beforefieldinit S extends [mscorlib]System.ValueType
{
	.field int32 m_i1;
	.field int32 m_i2;

.method public specialname rtspecialname instance void .ctor()
{
	ldarg 0
	ldc.i4 101
	stfld int32 S::m_i1

	ldarg 0
	ldc.i4 102
	stfld int32 S::m_i2

	ret
}

.method public instance void Incr()
{
	ldarg 0
	dup
	ldfld int32 S::m_i1
	ldc.i4 1
	add
	stfld int32 S::m_i1

	ldarg 0
	dup
	ldfld int32 S::m_i2
	ldc.i4 1
	add
	stfld int32 S::m_i2

	ret
}

}


.class public asm1
{
.field static int64 s_long at LONG_DATA
.field static int32 s_i

.method public static specialname rtspecialname void .cctor()
{
	ldc.i4 1
	stsfld int32 asm1::s_i
	ret
}

.method public specialname rtspecialname instance void .ctor()
{
	ldarg 0
	call instance void object::.ctor()
	ret
}

.method public static int32 Main(string[] args)
{
	.entrypoint
	.locals init(class asm1 obj, int32 i, valuetype S s)
	.maxstack  8

	ldloca s
	call instance void S::.ctor()

	ldloc s
	ldsfld int32 asm1::s_i
	brfalse L1
L0:
	stloc s
	ldstr "L0"
	call void [System.Console]System.Console::WriteLine(string)
	ldloca s
	call instance void S::Incr()
	ldloc s
	ldsfld int32 asm1::s_i
	brtrue L2
	
L1:
	stloc s
	ldstr "L1"
	call void [System.Console]System.Console::WriteLine(string)
	ldloca s
	call instance void S::Incr()
	ldloc s

L2:
	ldfld int32 S::m_i2
	call void [System.Console]System.Console::WriteLine(int32)

        ldc.i4 100
	ret
}


}
