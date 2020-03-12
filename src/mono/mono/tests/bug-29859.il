.assembly extern mscorlib
{
  .ver 0:0:0:0
}
.assembly 'vtable'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0 
}

.class private auto ansi beforefieldinit X
  	extends [mscorlib]System.Object
{

    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor()  cil managed 
    {
	.maxstack 8

	 ldarg.0 
	 call instance void valuetype [mscorlib]System.Object::.ctor()
	 ret 
    }

    .method private static 
           default int32 foo()  cil managed 
    {
	.maxstack 4
	.locals init (
		object	V_0)

	 	ldc.i4.5 
	 	box [mscorlib]System.Int32
	 	stloc.0 
	 	ldloc.0 
	 	ldloc.0 
		callvirt instance bool valuetype [mscorlib]System.Object::Equals(object)
		ldc.i4.0
		ceq
		ret 
    }

    .method private static 
           default int32 Main()  cil managed 
    {
	.entrypoint
	.maxstack 8
    	
		ldftn instance bool [mscorlib]System.Int32::Equals(object)
	    pop
		call int32 class X::foo()
		ret 
    }
}
