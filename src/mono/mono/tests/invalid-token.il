/*
This test an invalid methoddef token
*/
.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
}
.assembly 'bla'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module ld_type.exe

.class public auto ansi beforefieldinit Driver
        extends [mscorlib]System.Object
{
	.method public static void Misc() {
		ret
	}

        .method public static void InvalidMethodDef ()
        {
               .maxstack 2
                .locals init (int32 bla)
		nop
		call void Driver::Misc()
		/*Token is 0x00AAAA, which is well beyond the numbers of methods here.*/
		.emitbyte 0xFE
		.emitbyte 0x06
		.emitbyte 0xAA
		.emitbyte 0xAA
		.emitbyte 0x00
		.emitbyte 0x06
		pop
		ret

        }

        .method public static int32 Main ()
        {
		.entrypoint
		.maxstack 8
		.locals init (int32 V0)
		.try {
			call void Driver::InvalidMethodDef()
			//mini.c inline_method doesn't clean loader errors, meaning that System.Console fails with BadImageException
			//ldstr "InvalidMethodDef() should throw BadImageException"
			//call void class [mscorlib]System.Console::WriteLine(string)
			ldc.i4.1
			stloc.0
			leave END
		} catch [mscorlib]System.BadImageFormatException { 
			pop 
			ldc.i4.0 
			stloc.0
			leave END
		}
END:
		ldloc.0
		ret
	}
}


