.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'test-pattern-01'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'test-pattern-01.exe' // GUID = {9F506173-2872-430E-8B04-E4C828ABCFA1}


  .class private auto ansi beforefieldinit TypePattern
  	extends [mscorlib]System.Object
  {

    // method line 1
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2050
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method TypePattern::.ctor

    // method line 2
    .method public static hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x2058
	.entrypoint
	// Code size 63 (0x3f)
	.maxstack 2
	.locals init (
		object	V_0,
		bool	V_1,
		valuetype [mscorlib]System.Nullable`1<unsigned int8>	V_2)
	IL_0000:  ldc.i4.4 
	IL_0001:  newobj instance void valuetype [mscorlib]System.Nullable`1<int32>::'.ctor'(!0)
	IL_0006:  box valuetype [mscorlib]System.Nullable`1<int32>
	IL_000b:  stloc.0 
	IL_000c:  ldloc.0 
	IL_000d:  isinst valuetype [mscorlib]System.Nullable`1<unsigned int8>
	IL_0012:  unbox.any valuetype [mscorlib]System.Nullable`1<unsigned int8>
	IL_0017:  stloc.2 
	
	IL_0019:  ldnull 
	
	IL_0018:  ldloc.2
	// box valuetype [mscorlib]System.Nullable`1<unsigned int8>


	IL_001a:  cgt.un 
	IL_001c:  stloc.1 
	IL_001d:  ldloca.s 2
	IL_001f:  call instance bool valuetype [mscorlib]System.Nullable`1<unsigned int8>::get_HasValue()
	IL_0024:  brfalse IL_002b

	IL_0029:  ldc.i4.5 
	IL_002a:  ret 
	IL_002b:  ldloc.1 
	IL_002c:  brfalse IL_0033

	IL_0031:  ldc.i4.5 
	IL_0032:  ret 
	IL_0033:  ldstr "ok"
	IL_0038:  call void class [mscorlib]System.Console::WriteLine(string)
	IL_003d:  ldc.i4.0 
	IL_003e:  ret 
    } // end of method TypePattern::Main

  } // end of class TypePattern

