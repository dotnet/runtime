.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'generic-valuetype-newobj2.2'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'generic-valuetype-newobj2.2.exe' // GUID = {E84A2C06-0CD3-4155-9334-02045F2698FF}


  .class public sequential ansi sealed beforefieldinit Gen
  	extends [mscorlib]System.ValueType
  {
    .field  public  object arr

    // method line 1
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor'<T> (int32 i)  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  ldarg.1 
	IL_0002:  newarr !!0
	IL_0007:  stfld object Gen::arr
	IL_000c:  ret 
    } // end of method Gen::.ctor

  } // end of class Gen

  .class public auto ansi beforefieldinit main
  	extends [mscorlib]System.Object
  {

    // method line 3
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2108
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method main::.ctor

    // method line 4
    .method public static  hidebysig 
           default valuetype Gen newGen ()  cil managed 
    {
        // Method begins at RVA 0x2110
	// Code size 10 (0xa)
	.maxstack 3
	IL_0002:  ldc.i4.3 
		  newobj instance void valuetype Gen::'.ctor'<string>(int32)
	IL_0009:  ret 
    } // end of method main::newGen

    // method line 5
    .method public static  hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x2128
	.entrypoint
	// Code size 37 (0x25)
	.maxstack 6
	.locals init (
		valuetype Gen	V_0)
	IL_0000:  call valuetype Gen class main::newGen()
	IL_0005:  stloc.0 
	IL_0006:  ldloca.s 0
	IL_0008:  ldfld object Gen::arr
	IL_000d:  callvirt instance class [mscorlib]System.Type object::GetType()
	IL_0012:  ldtoken string[]
	IL_0017:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_001c:  beq IL_0023

	IL_0021:  ldc.i4.1 
	IL_0022:  ret 
	IL_0023:  ldc.i4.0 
	IL_0024:  ret 
    } // end of method main::Main

  } // end of class main

