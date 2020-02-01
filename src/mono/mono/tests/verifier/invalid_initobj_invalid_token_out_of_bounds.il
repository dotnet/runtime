/*
This test using a token that overflows the typedef table
*/
.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
}
.assembly 'initobj_token_out_of_range'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module ld_type.exe

.class public auto ansi beforefieldinit Driver
        extends [mscorlib]System.Object
{
        .method public static void Method ()
        {
                ret
        }

        .method public virtual void VirtMethod ()
        {
                ret
        }

        .method public static int32 Main ()
        {
                .entrypoint
                .maxstack 2
                .locals init (int32& v0, int32 v1)
		ldloca 1
		stloc.0
		ldloc.0
		nop
		.emitbyte 0xFE
		.emitbyte 0x15
		.emitbyte 0xAA
		.emitbyte 0xAA
		.emitbyte 0x00
		.emitbyte 0x02
		ldc.i4.0
		ret
        }
}


