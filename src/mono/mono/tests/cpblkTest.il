.assembly extern mscorlib{}
.assembly cpblkTest{}
.module cpblkTest

.class private auto ansi test
       extends [mscorlib]System.Object {

  .field private static valuetype DataType dataField at theData

  .method public static int32  start() cil managed {
	.entrypoint
	.locals init(native int, unsigned int8)

	//store data address
	ldsflda valuetype DataType test::dataField
	stloc.0

	//copy 7 to first position
	ldloc.0		//dest
	ldloc.0
	ldc.i4.6 
	add		//src
	ldc.i4.1	//size
    	cpblk

	//ldind value
	ldloc.0
	ldind.u1
	
	//compare
	ldc.i4.7
	beq PASS

  FAIL:
        ldc.i4.1	
	ret
  PASS:
        ldc.i4.0	
	ret
  } 

}

.class public explicit ansi sealed DataType 
       extends [mscorlib]System.ValueType
	{.pack 1 .size 10}

.data theData = bytearray (1 2 3 4 5 6 7 8 9 10)