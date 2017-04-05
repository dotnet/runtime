/* inspired by

    ./mcs/mcs/assign.cs:
	    SimpleAssign:CheckEqualAssign ()

*/

.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'instance_tailrec'
{
  .custom instance void class [mscorlib]System.Diagnostics.DebuggableAttribute::'.ctor'(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) =  (01 00 02 01 00 00 00 00 ) // ........

  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .permissionset reqmin = {[mscorlib]System.Security.Permissions.SecurityPermissionAttribute = {property bool 'SkipVerification' = bool(true)}}
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module instance_tailrec.exe // GUID = {5C65CFE6-9730-430A-BE4D-0F9BAC9C03DA}

.custom instance void class [mscorlib]System.Security.UnverifiableCodeAttribute::'.ctor'() =  (01 00 00 00 ) // ....


.namespace test
{
  .class public auto ansi beforefieldinit TailRec
  	extends [mscorlib]System.Object
  {
    .field  public  int32 counter

    // method line 1
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (int32 a)  cil managed 
    {
        // Method begins at RVA 0x2050
	// Code size 15 (0xf)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  nop 
	IL_0007:  ldarg.0 
	IL_0008:  ldarg.1 
	IL_0009:  stfld int32 test.TailRec::counter
	IL_000e:  ret 
    } // end of method TailRec::.ctor

    // method line 2
    .method public hidebysig 
           instance default bool tailrec (object obj)  cil managed 
    {
        // Method begins at RVA 0x2060
	// Code size 78 (0x4e)
	.maxstack 3
	.locals init (
		bool	V_0)
	IL_0000:  nop 
	IL_0001:  ldarg.1 
	IL_0002:  brtrue IL_003f

	IL_0007:  nop 
	IL_0008:  ldarg.0 
	IL_0009:  dup 
	IL_000a:  ldfld int32 test.TailRec::counter
	IL_000f:  ldc.i4.1 
	IL_0010:  sub 
	IL_0011:  stfld int32 test.TailRec::counter
	IL_0016:  ldarg.0 
	IL_0017:  ldfld int32 test.TailRec::counter
	IL_001c:  ldc.i4.0 
	IL_001d:  bgt IL_0029

	IL_0022:  ldc.i4.0 
	IL_0023:  stloc.0 
	IL_0024:  br IL_004c

	IL_0029:  ldarg.0 
	IL_002a:  brfalse IL_0038

	IL_002f:  ldarg.0 
	IL_0030:  ldnull 
	// here is the tail call
	IL_0031:  call instance bool class test.TailRec::tailrec(object)
	ret

	IL_0038:  ldc.i4.0 
	IL_0039:  stloc.0 
	IL_003a:  br IL_004c

	IL_003f:  ldarg.0 
	IL_0040:  ldarg.1 
	IL_0041:  callvirt instance bool object::Equals(object)
	IL_0046:  stloc.0 
	IL_0047:  br IL_004c

	IL_004c:  ldloc.0 
	IL_004d:  ret 
    } // end of method TailRec::tailrec

    // method line 3
    .method public static hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x20bc
	.entrypoint
	// Code size 112 (0x70)
	.maxstack 2
	.locals init (
		class test.TailRec	V_0,
		bool	V_1,
		int32	V_2,
		int32	V_3,
		int32	V_4,
		int32	V_5)
	IL_0000:  nop 
	IL_0001:  ldnull 
	IL_0002:  stloc.0 
	IL_0003:  ldc.i4.1 
	IL_0004:  stloc.1 
	IL_0005:  ldc.i4 1337
	IL_000a:  stloc.2 
	IL_000b:  ldc.i4 347134
	IL_0010:  stloc.3 
	IL_0011:  ldc.i4.0 
	IL_0012:  stloc.s 4
	IL_0014:  br IL_0031

	IL_0019:  nop 
	IL_001a:  ldloc.s 4
	IL_001c:  newobj instance void class test.TailRec::'.ctor'(int32)
	IL_0021:  stloc.0 
	IL_0022:  ldloc.0 
	IL_0023:  ldnull 
	IL_0024:  callvirt instance bool class test.TailRec::tailrec(object)
	IL_0029:  stloc.1 
	IL_002a:  nop 
	IL_002b:  ldloc.s 4
	IL_002d:  ldc.i4.1 
	IL_002e:  add 
	IL_002f:  stloc.s 4
	IL_0031:  ldloc.s 4
	IL_0033:  ldc.i4 100
	IL_0038:  blt IL_0019

	IL_003d:  ldloc.0 
	IL_003e:  brfalse IL_0065

	IL_0043:  ldloc.1 
	IL_0044:  brtrue IL_0065

	IL_0049:  ldloc.2 
	IL_004a:  ldc.i4 1337
	IL_004f:  bne.un IL_0065

	IL_0054:  ldloc.3 
	IL_0055:  ldc.i4 347134
	IL_005a:  bne.un IL_0065

	IL_005f:  ldc.i4.0 
	IL_0060:  br IL_0066

	IL_0065:  ldc.i4.1 
	IL_0066:  stloc.s 5
	IL_0068:  br IL_006d

	IL_006d:  ldloc.s 5
	IL_006f:  ret 
    } // end of method TailRec::Main

  } // end of class test.TailRec
}

