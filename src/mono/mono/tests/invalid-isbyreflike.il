// This is a test that attempts to use BOX on an IsByRefLike struct.

.assembly extern mscorlib
{
  .ver 4:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'invalid-isbyreflike'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::'.ctor'(int32) =  (01 00 08 00 00 00 00 00 ) // ........

  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .custom instance void class [mscorlib]System.Diagnostics.DebuggableAttribute::'.ctor'(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) =  (01 00 07 01 00 00 00 00 ) // ........

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'invalid-isbyreflike.exe'


  .class public sequential ansi sealed beforefieldinit X
  	extends [mscorlib]System.ValueType
  {
    .field  public  int32 i

  } // end of class X

  .class public sequential ansi sealed beforefieldinit R
  	extends [mscorlib]System.ValueType
  {
    .custom instance void class [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .custom instance void class [mscorlib]System.ObsoleteAttribute::'.ctor'(string, bool) =  (
		01 00 52 54 79 70 65 73 20 77 69 74 68 20 65 6D   // ..RTypes with em
		62 65 64 64 65 64 20 72 65 66 65 72 65 6E 63 65   // bedded reference
		73 20 61 72 65 20 6E 6F 74 20 73 75 70 70 6F 72   // s are not suppor
		74 65 64 20 69 6E 20 74 68 69 73 20 76 65 72 73   // ted in this vers
		69 6F 6E 20 6F 66 20 79 6F 75 72 20 63 6F 6D 70   // ion of your comp
		69 6C 65 72 2E 01 00 00                         ) // iler....

    .field  public  int32 i

  } // end of class R

  .class public auto ansi beforefieldinit G`1<T>
       extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
    {
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  nop
      IL_0007:  ret
    } // end of method G`1::.ctor
  } // end of class G`1

  .class public auto ansi beforefieldinit C
  	extends [mscorlib]System.Object
  {

  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       171 (0xab)
    .maxstack  2
    .locals init (bool V_0,
             bool V_1,
             int32 V_2,
             bool V_3,
             bool V_4)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    .try
    {
      IL_0003:  nop
      IL_0004:  call       void C::BadMethod1()
      IL_0009:  nop
      IL_000a:  nop
      IL_000b:  leave.s    IL_001f

    }  // end .try
    catch [mscorlib]System.BadImageFormatException 
    {
      IL_000d:  pop
      IL_000e:  nop
      IL_000f:  ldstr      "good - BadImageFormatException for BadMethod1"
      IL_0014:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0019:  nop
      IL_001a:  ldc.i4.1
      IL_001b:  stloc.0
      IL_001c:  nop
      IL_001d:  leave.s    IL_001f

    }  // end handler
    IL_001f:  ldloc.0
    IL_0020:  ldc.i4.0
    IL_0021:  ceq
    IL_0023:  stloc.1
    IL_0024:  ldloc.1
    IL_0025:  brfalse.s  IL_0037

    IL_0027:  nop
    IL_0028:  ldstr      "bad - no exception when JITing BadMethod1"
    IL_002d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0032:  nop
    IL_0033:  ldc.i4.1
    IL_0034:  stloc.2
    IL_0035:  br.s       IL_00a9

    IL_0037:  ldc.i4.0
    IL_0038:  stloc.0
    .try
    {
      IL_0039:  nop
      IL_003a:  call       void C::BadMethod2()
      IL_003f:  nop
      IL_0040:  nop
      IL_0041:  leave.s    IL_0055

    }  // end .try
    catch [mscorlib]System.TypeLoadException
    {
      IL_0043:  pop
      IL_0044:  nop
      IL_0045:  ldstr      "good - TypeLoadException for BadMethod2"
      IL_004a:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_004f:  nop
      IL_0050:  ldc.i4.1
      IL_0051:  stloc.0
      IL_0052:  nop
      IL_0053:  leave.s    IL_0055

    }  // end handler
    IL_0055:  ldloc.0
    IL_0056:  ldc.i4.0
    IL_0057:  ceq
    IL_0059:  stloc.3
    IL_005a:  ldloc.3
    IL_005b:  brfalse.s  IL_006d

    IL_005d:  nop
    IL_005e:  ldstr      "bad - no exception when JITing BadMethod2"
    IL_0063:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0068:  nop
    IL_0069:  ldc.i4.2
    IL_006a:  stloc.2
    IL_006b:  br.s       IL_00a9

    IL_006d:  ldc.i4.0
    IL_006e:  stloc.0
    .try
    {
      IL_006f:  nop
      IL_0070:  call       void C::BadMethod3()
      IL_0075:  nop
      IL_0076:  nop
      IL_0077:  leave.s    IL_008b

    }  // end .try
    catch [mscorlib]System.BadImageFormatException 
    {
      IL_0079:  pop
      IL_007a:  nop
      IL_007b:  ldstr      "good - BadImageFormatException for BadMethod3"
      IL_0080:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0085:  nop
      IL_0086:  ldc.i4.1
      IL_0087:  stloc.0
      IL_0088:  nop
      IL_0089:  leave.s    IL_008b

    }  // end handler
    IL_008b:  ldloc.0
    IL_008c:  ldc.i4.0
    IL_008d:  ceq
    IL_008f:  stloc.s    V_4
    IL_0091:  ldloc.s    V_4
    IL_0093:  brfalse.s  IL_00a5

    IL_0095:  nop
    IL_0096:  ldstr      "bad - no exception when JITing BadMethod3"
    IL_009b:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_00a0:  nop
    IL_00a1:  ldc.i4.3
    IL_00a2:  stloc.2
    IL_00a3:  br.s       IL_00a9

    IL_00a5:  ldc.i4.0
    IL_00a6:  stloc.2
    IL_00a7:  br.s       IL_00a9

    IL_00a9:  ldloc.2
    IL_00aa:  ret
  } // end of method C::Main

    // method line 2
    // noinling is important - we want the JIT to fail on this method, not on Main().
    .method public static hidebysig 
           default void BadMethod1 ()  cil managed noinlining 
    {
        // Method begins at RVA 0x209c
	// Code size 48 (0x30)
	.maxstack 2
	.locals init (
		valuetype X	V_0,
		valuetype R	V_1,
		object	V_2,
		valuetype X	V_3,
		valuetype R	V_4)
	IL_0000:  nop 
	IL_0001:  ldloca.s 3
	IL_0003:  initobj X
	IL_0009:  ldloca.s 3
	IL_000b:  ldc.i4.s 0x0a
	IL_000d:  stfld int32 X::i
	IL_0012:  ldloc.3 
	IL_0013:  stloc.0 
	IL_0014:  ldloca.s 4
	IL_0016:  initobj R
	IL_001c:  ldloca.s 4
	IL_001e:  ldc.i4.s 0x2a
	IL_0020:  stfld int32 R::i
	IL_0025:  ldloc.s 4
	IL_0027:  stloc.1 
	IL_0028:  ldloc.1 
	IL_0029:  box R    // box of an IsByRefLike type is not allowed
	IL_002e:  stloc.2 
	IL_002f:  ret 
    } // end of method C::BadMethod1

  .method public hidebysig static void  BadMethod2() cil managed noinlining
  {
    // Code size       8 (0x8)
    .maxstack  1
    .locals init (class G`1<valuetype R> V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void class G`1<valuetype R>::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ret
  } // end of method C::BadMethod2

  .method public hidebysig static void  BadMethod3() cil managed noinlining
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  call       void C::GM<valuetype R>()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method C::BadMethod3

  .method public hidebysig static void  GM<T>() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method C::GM

    // method line 3
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20d8
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  nop 
	IL_0007:  ret 
    } // end of method C::.ctor

  } // end of class C

