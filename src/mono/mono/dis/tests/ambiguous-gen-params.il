// Test for type/method having type parameters with identical names
// 
// Roundtrip with ilasm2/monodis should yield same 
//        meth <A,A, B> (!!0 _a1, !!1 _a2, !1 _t, !!B _b)

.assembly extern mscorlib
{
  .ver 2:0:0:0
}
.assembly 'ambiguous-gen-params'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .ver  0:0:0:0
}
.module 'ambiguous-gen-params.dll'


  .class private auto ansi beforefieldinit g`1<T, T>
  	extends [mscorlib]System.Object
  {

    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::.ctor()
	IL_0006:  ret 
    }

    .method public static  hidebysig 
           default void meth <A,A, B> (!!0 _a1, !!1 _a2, !1 _t, !!B _b)  cil managed 
    {
	ret
    }

    .method public static  hidebysig 
           default !!A foo<A,B> (!!B _b, !1 _t, !!A _a)  cil managed 
    {
	.maxstack 1
	.locals init (
		!!A	V_0)
	IL_0000:  ldloca.s 0
	IL_0002:  initobj !!0
	IL_0008:  ldloc.0 
	IL_0009:  ret 
    }

  }

