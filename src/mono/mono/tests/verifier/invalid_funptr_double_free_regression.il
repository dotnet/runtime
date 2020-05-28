.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly extern System.Core
{
  .ver 3:5:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'test-anon-01'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'test-anon-01.exe' // GUID = {38A511CD-E5D7-4DA2-88C0-A137887BAFBC}


  .class public auto ansi beforefieldinit Test
  	extends [mscorlib]System.Object
  {
    .field  private static  class [System.Core]System.Func`1<!!0> '<>f__am$cache0'
    .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::'.ctor'() =  (01 00 00 00 ) // ....


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
    } // end of method Test::.ctor

    // method line 2
    .method private static  hidebysig 
           default class [System.Core]System.Func`1<!!T> XX<T> ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 32 (0x20)
	.maxstack 4
	.locals init (
		class [System.Core]System.Func`1<!!T>	V_0)
	IL_0000:  ldsfld class [System.Core]System.Func`1<!!0> Test::'<>f__am$cache0'
	IL_0005:  brtrue.s IL_0018

	IL_0007:  ldnull 
	IL_0008:  ldftn !!0 class Test::'<XX>m__0'<!!0> ()
	IL_000e:  newobj instance void class [System.Core]System.Func`1<!!T>::'.ctor'(object, native int)
	IL_0013:  stsfld class [System.Core]System.Func`1<!!0> Test::'<>f__am$cache0'
	IL_0018:  ldsfld class [System.Core]System.Func`1<!!0> Test::'<>f__am$cache0'
	IL_001d:  stloc.0 
	IL_001e:  ldloc.0 
	IL_001f:  ret 
    } // end of method Test::XX

    // method line 3
    .method public static  hidebysig 
           default int32 Main ()  cil managed 
    {
        // Method begins at RVA 0x2120
	.entrypoint
	// Code size 103 (0x67)
	.maxstack 20
	IL_0000:  call class [System.Core]System.Func`1<!!0> class Test::XX<bool> ()
	IL_0005:  callvirt instance !0 class [System.Core]System.Func`1<bool>::Invoke()
	IL_000a:  box [mscorlib]System.Boolean
	IL_000f:  call instance class [mscorlib]System.Type object::GetType()
	IL_0014:  ldtoken [mscorlib]System.Boolean
	IL_0019:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_001e:  beq IL_0025

	IL_0023:  ldc.i4.1 
	IL_0024:  ret 
	IL_0025:  call class [System.Core]System.Func`1<!!0> class Test::XX<int32> ()
	IL_002a:  callvirt instance !0 class [System.Core]System.Func`1<int32>::Invoke()
	IL_002f:  box [mscorlib]System.Int32
	IL_0034:  call instance class [mscorlib]System.Type object::GetType()
	IL_0039:  ldtoken [mscorlib]System.Int32
	IL_003e:  call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
	IL_0043:  beq IL_004a

	IL_0048:  ldc.i4.1 
	IL_0049:  ret 
	IL_004a:  call class [System.Core]System.Func`1<!!0> class Test::XX<object> ()
	IL_004f:  callvirt instance !0 class [System.Core]System.Func`1<object>::Invoke()
	IL_0054:  brfalse IL_005b

	IL_0059:  ldc.i4.2 
	IL_005a:  ret 
	IL_005b:  ldstr "OK"
	IL_0060:  call void class [mscorlib]System.Console::WriteLine(string)
	IL_0065:  ldc.i4.0 
	IL_0066:  ret 
    } // end of method Test::Main

    // method line 4
    .method private static  hidebysig 
           default !!T '<XX>m__0'<T> ()  cil managed 
    {
        .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::'.ctor'() =  (01 00 00 00 ) // ....

        // Method begins at RVA 0x2194
	// Code size 12 (0xc)
	.maxstack 2
	.locals init (
		!!T	V_0,
		!!T	V_1)
	IL_0000:  ldloca.s 0
	IL_0002:  initobj !!0
	IL_0008:  ldloc.0 
	IL_0009:  ret 
	IL_000a:  ldloc.1 
	IL_000b:  ret 
    } // end of method Test::<XX>m__0

  } // end of class Test

