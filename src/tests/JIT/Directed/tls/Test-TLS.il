.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly  'test-tls'
{
}
.data tls FieldData = int32(51966)
.class public test {

	.field public static int32 TLSFieldData at FieldData
	.method public static int32 main() 
	{
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		ldsfld	int32 test::TLSFieldData  
		stsfld	int32 test::TLSFieldData  
		ldc.i4  0x64
		stsfld	int32 test::TLSFieldData  
		ldsfld	int32 test::TLSFieldData  
		ret
	}
}


