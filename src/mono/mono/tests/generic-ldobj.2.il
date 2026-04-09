.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'generic-mkrefany.2'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'generic-mkrefany.2.exe' // GUID = {51FFBB77-6AFB-435E-B206-16DB5C8A3730}


  .class public auto ansi beforefieldinit ClassA
  	extends [mscorlib]System.Object
  {

    // method line 1
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method ClassA::.ctor

  } // end of class ClassA

  .class public sequential ansi sealed beforefieldinit GenStruct`1<T>
  	extends [mscorlib]System.ValueType
  {
    .field  public  int32 field1
    .field  public  !0 field2

  } // end of class GenStruct`1

  .class public auto ansi beforefieldinit Gen`1<T>
  	extends [mscorlib]System.Object
  {

    // method line 2
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Gen`1::.ctor

    // method line 3
    .method public hidebysig 
           instance default !T ident (!T t)  cil managed 
    {
        // Method begins at RVA 0x20fc
	// Code size 4 (0x4)
	.maxstack 1
	.locals init (
		!T	V_0)
	IL_0000:  ldarg.1 
	IL_0001:  stloc.0 
		  ldloca 0
		  ldobj		!0
	IL_0003:  ret 
    } // end of method Gen`1::ident

    // method line 4
    .method public hidebysig 
           instance default valuetype GenStruct`1<!T> structIdent (valuetype GenStruct`1<!T> gst)  cil managed 
    {
        // Method begins at RVA 0x210c
	// Code size 4 (0x4)
	.maxstack 1
	.locals init (
		valuetype GenStruct`1<!T>	V_0)
	IL_0000:  ldarg.1 
	IL_0001:  stloc.0 
		  ldloca	0
		  ldobj		valuetype GenStruct`1<!0>
	IL_0003:  ret 
    } // end of method Gen`1::structIdent

  } // end of class Gen`1

  .class public auto ansi beforefieldinit main
  	extends [mscorlib]System.Object
  {

    // method line 5
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x211c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method main::.ctor

    // method line 6
    .method public static  hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x2124
	.entrypoint
	// Code size 94 (0x5e)
	.maxstack 8
	.locals init (
		class ClassA	V_0,
		valuetype GenStruct`1<class ClassA>	V_1,
		class Gen`1<class ClassA>	V_2,
		valuetype GenStruct`1<class ClassA>	V_3)
	IL_0000:  newobj instance void class ClassA::'.ctor'()
	IL_0005:  stloc.0 
	IL_0006:  ldloca.s 1
	IL_0008:  ldc.i4.s 0x7b
	IL_000a:  stfld int32 valuetype GenStruct`1<class ClassA>::field1
	IL_000f:  ldloca.s 1
	IL_0011:  ldloc.0 
	IL_0012:  stfld !0 valuetype GenStruct`1<class ClassA>::field2
	IL_0017:  newobj instance void class Gen`1<class ClassA>::'.ctor'()
	IL_001c:  stloc.2 
	IL_001d:  ldloc.2 
	IL_001e:  ldloc.0 
	IL_001f:  callvirt instance !0 class Gen`1<class ClassA>::ident(!0)
	IL_0024:  ldloc.0 
	IL_0025:  beq IL_002c

	IL_002a:  ldc.i4.1 
	IL_002b:  ret 
	IL_002c:  ldloc.2 
	IL_002d:  ldloc.1 
	IL_002e:  callvirt instance valuetype GenStruct`1<!0> class Gen`1<class ClassA>::structIdent(valuetype GenStruct`1<!0>)
	IL_0033:  stloc.3 
	IL_0034:  ldloca.s 3
	IL_0036:  ldfld int32 valuetype GenStruct`1<class ClassA>::field1
	IL_003b:  ldloca.s 1
	IL_003d:  ldfld int32 valuetype GenStruct`1<class ClassA>::field1
	IL_0042:  bne.un IL_005a

	IL_0047:  ldloca.s 3
	IL_0049:  ldfld !0 valuetype GenStruct`1<class ClassA>::field2
	IL_004e:  ldloca.s 1
	IL_0050:  ldfld !0 valuetype GenStruct`1<class ClassA>::field2
	IL_0055:  beq IL_005c

	IL_005a:  ldc.i4.1 
	IL_005b:  ret 
	IL_005c:  ldc.i4.0 
	IL_005d:  ret 
    } // end of method main::Main

  } // end of class main

