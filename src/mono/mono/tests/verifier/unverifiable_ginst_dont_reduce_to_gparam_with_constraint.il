.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'gtest-314'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'gtest-314.exe' // GUID = {EDD5A7C2-306C-4E1F-985D-43A143A3ED18}


.namespace test
{
  .class public auto ansi beforefieldinit App
  	extends [mscorlib]System.Object
  {
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method App::.ctor

    // method line 2
    .method public static  hidebysig 
           default void Main ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	.entrypoint
	// Code size 20 (0x14)
	.maxstack 3
	.locals init (
		class test.Blarg	V_0,
		class test.Bla	V_1)
	IL_0000:  newobj instance void class test.Blarg::'.ctor'()
	IL_0005:  stloc.0 
	IL_0006:  newobj instance void class test.Bla::'.ctor'()
	IL_000b:  stloc.1 
	IL_000c:  ldloc.0 
	IL_000d:  ldloc.1 
	IL_000e:  callvirt instance void class test.OtherClass`2<class test.Blarg, class test.Bla>::Test(!1)
	IL_0013:  ret 
    }
  }
}

.namespace test
{
  .class private auto ansi beforefieldinit Bla
  	extends class test.ThisClass`2<class test.Bla,class test.Blarg>
  {

    // method line 3
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2114
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class test.ThisClass`2<class test.Bla, class test.Blarg>::'.ctor'()
	IL_0006:  ret 
    } // end of method Bla::.ctor

  } // end of class test.Bla
}

.namespace test
{
  .class private auto ansi beforefieldinit Blarg
  	extends class test.OtherClass`2<class test.Blarg,class test.Bla>
  {

    // method line 4
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x211c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class test.OtherClass`2<class test.Blarg, class test.Bla>::'.ctor'()
	IL_0006:  ret 
    } // end of method Blarg::.ctor

  } // end of class test.Blarg
}

.namespace test
{
  .class public auto ansi beforefieldinit ThisClass`2<(class test.ThisClass`2<!T, !O>) T,(class test.OtherClass`2<!O, !T>) O>
  	extends [mscorlib]System.Object
  {
    .field  assembly  int32 dummy

    // method line 5
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2124
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method ThisClass`2::.ctor

  } // end of class test.ThisClass`2
}

.namespace test
{
  .class public auto ansi beforefieldinit OtherClass`2<(class test.OtherClass`2<!O, !T>) O,(class test.ThisClass`2<!T, !O>) T>
  	extends [mscorlib]System.Object
  {

    // method line 6
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x212c
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method OtherClass`2::.ctor

    .method public hidebysig 
           instance default void Test (!T tc)  cil managed 
    {
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  ldc.i4.0
	//XXX ThisClass`2<!1,!0> can't be reduced to T even thou T has a 'ThisClass`2<!1,!0>' constaint.
	IL_0002:  stfld int32 class test.ThisClass`2<!1,!0>::dummy
	IL_0007:  ret 
    }
  }
}

