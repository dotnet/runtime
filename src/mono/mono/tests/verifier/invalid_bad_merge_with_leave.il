.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'bug'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module bug.exe // GUID = {7CDDF4B6-7581-4902-9709-6155199DC325}


.class private auto ansi beforefieldinit Program extends [mscorlib]System.Object
{
	.method public static hidebysig default void Main ()  cil managed 
    {
	.entrypoint
	.maxstack 2
	ret
	}


	.method public static hidebysig default int32 Bla (object arg)  cil managed 
    {
	.locals init ( class [mscorlib]System.Exception V_0)

	nop       
	ldarg.0   
	ldnull    
	ceq       
	brfalse   LB_0

	nop       
	nop       

	leave.s   LB_1

LB_0:
	nop
	nop
	nop       
	ldarg.0   

LB_1:
	isinst    object
	ldnull    
	 ceq       
	ldc.i4.0  
	ceq       
	brfalse   LB_2

	nop       
	ldarg.0   
	castclass object
	callvirt     int32 object::GetHashCode ()
	pop

LB_2:
	nop       
	nop       
	nop       
	ldarg.0   
	isinst    object
	ldnull    
	ceq       
	ldc.i4.0  
	ceq       
	brfalse   LB_3

	nop       
	ldarg.0   
	unbox     int32
	ldobj     int32
	ret   

LB_3:
	nop       
	nop       
	nop       
	ldc.i4.0
		ret 
    }
}

