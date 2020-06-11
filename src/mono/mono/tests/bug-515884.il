.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly ConsoleApplication1
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module ConsoleApplication1.exe


.class private auto ansi beforefieldinit ConsoleApplication1.Program extends [mscorlib]System.Object
{
	.method public hidebysig static int32 Main(string[] args) cil managed
	{
		.entrypoint
		.maxstack 8
		.locals init (class [mscorlib]System.AppDomain V_0)

		ldstr "Test"
		call class [mscorlib]System.AppDomain [mscorlib]System.AppDomain::CreateDomain(string)
		call instance string [mscorlib]System.AppDomain::get_FriendlyName()

		ldstr "Test"
		callvirt instance bool [mscorlib]System.Object::Equals (object)
		brfalse END

		ldc.i4.0
		ret

END:
		ldc.i4.1
		ret
	}
}
