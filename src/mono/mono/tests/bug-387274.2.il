.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
}
.assembly extern Microsoft.VisualC
{
  .ver 8:0:0:0
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
}

.assembly 'PureApp'
{
  .permissionset reqmin = {[mscorlib]System.Security.Permissions.SecurityPermissionAttribute = {property bool 'SkipVerification' = bool(true)}}
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module PureApp.exe // GUID = {DB842872-06AD-4D53-B0A8-D39869A4A91F}


.method assembly static default char& modopt ([mscorlib]System.Runtime.CompilerServices.IsExplicitlyDereferenced)  modopt ([mscorlib]System.Runtime.CompilerServices.IsConst)  PtrToStringChars (string modopt ([mscorlib]System.Runtime.CompilerServices.IsConst)  s)  cil managed 
{
	.maxstack 2
	.locals ( unsigned int8& modopt ([mscorlib]System.Runtime.CompilerServices.IsExplicitlyDereferenced) 	V_0)
	ldarg.0 
	stloc.0 
	ldloc.0 
	brfalse.s END

	call int32 class [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::get_OffsetToStringData()
	ldloc.0 
	add 
	stloc.0 
END:
	ldloc.0 
	ret 
}

.method assembly static default void WriteStringManaged (char modopt ([mscorlib]System.Runtime.CompilerServices.IsConst) * str)  cil managed 
{
	.maxstack 1
	ldarg.0 
	call native int native int::op_Explicit(void*)
	call string class [mscorlib]System.Runtime.InteropServices.Marshal::PtrToStringUni(native int)
	call void class [mscorlib]System.Console::WriteLine(string)
	ret 
}

.method assembly static default int32 main (string[] args)  cil managed 
{
	.entrypoint
	.maxstack 2
	.locals (
		unsigned int8& modopt ([mscorlib]System.Runtime.CompilerServices.IsExplicitlyDereferenced) 	V_0,
		char& pinned modopt ([mscorlib]System.Runtime.CompilerServices.IsExplicitlyDereferenced) modopt ([mscorlib]	System.Runtime.CompilerServices.IsConst) 	V_1)

	ldstr "Pure console application"
	call void class [mscorlib]System.Console::WriteLine(string)
	ldstr "WriteStringManaged"
	stloc.0 
	ldloc.0 
	brfalse.s END

	call int32 class [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::get_OffsetToStringData()
	ldloc.0 
	add 
	stloc.0 
	ldloc.0 
	stloc.1

END:
	ldloc.1 
	call native int native int::op_Explicit(void*)
	call string class [mscorlib]System.Runtime.InteropServices.Marshal::PtrToStringUni(native int)
	call void class [mscorlib]System.Console::WriteLine(string)
	ldc.i4.0 
	ret 
}

.method assembly static  specialname  rtspecialname 
       default void '.cctor' ()  cil managed 
{
	.maxstack 0
	ret 
}

