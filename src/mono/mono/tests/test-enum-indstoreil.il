.assembly extern mscorlib
{
  .ver 1:0:5000:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'test-enum-indstore'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'test-enum-indstore.exe' // GUID = {EAB30498-E396-428E-877F-1653AAAE4744}


  .class private auto ansi beforefieldinit Test
  	extends [mscorlib]System.Object
  {
    .field  private   valuetype Options 'opt'

    // method line 1
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x20ec
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::.ctor()
	IL_0006:  ret 
    } // end of method Test::instance default void .ctor () 

    // method line 2
    .method private static  hidebysig 
           default void Main ()  cil managed 
    {
        // Method begins at RVA 0x20f4
	.entrypoint
	// Code size 18 (0x12)
	.maxstack 3
	.locals init (
		class Test	V_0,
		valuetype Options&	V_1)
	IL_0000:  newobj instance void class Test::.ctor()
	IL_0005:  stloc.0 
	IL_0006:  ldloc.0 
	IL_0007:  ldflda  valuetype Options Test::'opt'
	IL_000c:  stloc.1 
	IL_000d:  ldloc.1 
		  ldloc.1
		  ldind.u1
	IL_000e:  ldc.i4.1 
	IL_000f:  or 
		  stind.i1
//	IL_0010:  stloc.1 
		  ldloc.0
	IL_0019:  ldfld  valuetype Options Test::'opt'
	IL_001e:  box Options
	IL_0023:  call void class [mscorlib]System.Console::WriteLine(object)
	IL_0011:  ret 
    } // end of method Test::default void Main () 

  } // end of class Test

  .class private auto ansi sealed Options
  	extends [mscorlib]System.Enum
  {
    .field  public specialname  rtspecialname   unsigned int8 value__
    .field public static  literal   valuetype Options Zero = int8(0x00)
    .field public static  literal   valuetype Options One = int8(0x01)

  } // end of class Options

