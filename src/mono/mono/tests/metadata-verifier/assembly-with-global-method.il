.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
}
.assembly 'simple_assembly'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.module simple_assembly.exe

.method public static void Main () cil managed 
{
	.entrypoint
	.maxstack 8
	ret 
}

