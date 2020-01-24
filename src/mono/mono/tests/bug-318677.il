.assembly extern mscorlib
{
  .ver 1:0:5000:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'out'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'out' // GUID = {F21F5CF2-5B7A-4E45-9087-A8A456BB68C7}


  .class private auto ansi abstract _N_AutoModule
        extends [mscorlib]System.Object
  {

    .method private static  hidebysig 
           default int32 Main ()  cil managed 
    {
	.entrypoint
	// Code size 52 (0x34)
	.maxstack 3
	.locals init (
		class [mscorlib]System.Exception	V_0,
		int32	V_1)
	.try { // 0
	  IL_0000:  call void class _N_AutoModule::ThisMethodWillNotReturn()
	  IL_0005:  ldstr "return 1"
	  IL_000a:  call void class [mscorlib]System.Console::WriteLine(string)
	  IL_000f:  ldc.i4.1 
	  IL_0010:  stloc.1 
	  IL_0011:  leave IL_0032

	  IL_0016:  leave IL_0032

	} // end .try 0
	catch [mscorlib]System.Exception { // 0
	  IL_001b:  stloc.0 
	  IL_001c:  ldstr "return 0"
	  IL_0021:  call void class [mscorlib]System.Console::WriteLine(string)
	  IL_0026:  ldc.i4.0 
	  IL_0027:  stloc.1 
	  IL_0028:  leave IL_0032

	  IL_002d:  leave IL_0032

	} // end handler 0
	IL_0032:  ldloc.1 
	IL_0033:  ret 
    } // end of method _N_AutoModule::Main

    .method private static  hidebysig
           default void ThisMethodWillNotReturn ()  cil managed
    {
        // Code size 44 (0x2c)
        .maxstack 4
        .locals init (
                bool    V_0)
        IL_0000:  ldc.i4.2
        IL_0001:  ldc.i4.3
        IL_0002:  bgt.s IL_000a

        IL_0004:  ldc.i4.0
        IL_0005:  br IL_000b

        IL_000a:  ldc.i4.1
        IL_000b:  stloc.0
        IL_000c:  ldloc.0
        IL_000d:  brfalse IL_0017

        IL_0012:  br IL_0021

        IL_0017:  ldstr "ble"
        IL_001c:  call void class [mscorlib]System.Console::WriteLine(string)
        IL_0021:  ldstr ""
        IL_0026:  newobj instance void class
[mscorlib]System.Exception::.ctor(string)
        IL_002b:  throw
    } // end of method _N_AutoModule::default void Main ()

  } // end of class _N_AutoModule
