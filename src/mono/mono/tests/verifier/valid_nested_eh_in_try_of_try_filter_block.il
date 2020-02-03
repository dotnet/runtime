.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
}
.assembly 'try_catch_block_inside_try_patch_of_try_filter'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module repro.exe 

.class private auto ansi beforefieldinit MainClass extends [mscorlib]System.Object
{
	.method public static  hidebysig  default int32 Main (string[] args)  cil managed 
	{
		.entrypoint
		.maxstack 1
		.locals init (int32 V_0)
		.try {
			.try {
				ldc.i4.0 
				stloc.0
				leave END
			}
			catch class [mscorlib]System.Exception {
				pop
				ldc.i4.1 
				stloc.0
				leave END
			}
			leave END
		} filter {
			pop
			ldc.i4.1
			endfilter
		} {
			pop
			ldc.i4.3 
			stloc.0
			leave END
		}

		END:
		ldloc.0
		ret 
	}
}

