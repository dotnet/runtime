.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'repro'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module repro.exe // GUID = {BF27A528-3D7C-4A28-836C-06A36B2B26C9}


  .class interface public auto ansi abstract IFace
  {
  	.field  public static  initonly  int32 X

    // method line 3
    .method private static  specialname  rtspecialname 
           default void '.cctor' ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	// Code size 8 (0x8)
	.maxstack 8
	IL_0000:  ldc.i4.s 0x0a
	IL_0002:  stsfld int32 IFace::X
	IL_0007:  ret 
    } // end of method Foo::.cctor

    // method line 1
    .method public virtual  hidebysig  newslot  abstract 
           instance default int32 Bla ()  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method IFace::Bla

  } // end of class IFace

  .class public auto ansi beforefieldinit Foo
  	extends [mscorlib]System.Object
  	implements IFace  {

    // method line 2
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Foo::.ctor

    // method line 4
    .method public final  virtual  hidebysig  newslot 
           instance default int32 Bla ()  cil managed 
    {
        // Method begins at RVA 0x2100
	// Code size 3 (0x3)
	.maxstack 8
	IL_0000:  ldc.i4.0
	IL_0002:  ret 
    } // end of method Foo::Bla

  } // end of class Foo

  .class private auto ansi beforefieldinit Driver
  	extends [mscorlib]System.Object
  {

    // method line 5
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2104
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method Driver::.ctor

    // method line 6
    .method private static  hidebysig 
           default int32 Main ()  cil managed 
    {
	.entrypoint
	.maxstack 3
	.locals init (class Foo	V_0)
	newobj instance void class Foo::'.ctor'()
	stloc.0 
	ldloc.0 
	callvirt instance int32 class Foo::Bla() 
	ret 
    }
  }

