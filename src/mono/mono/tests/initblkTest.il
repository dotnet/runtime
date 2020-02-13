//tests initblk by allocating and initializing and checking an initialized area
//Prints PASS or FAIL
.assembly extern mscorlib{}
.assembly someTest{}
.module someTest

.class private auto ansi someTest
       extends [mscorlib]System.Object {

  .method public static void  go() cil managed {
    .entrypoint

	.locals init (native int)

//allocate memory
ldc.i4.s 10
localloc
stloc.0

//init memory offset 3 indirectly 
ldloc.0
ldc.i4.0
add		//addr
ldc.i4.0	//value
stind.i1

//initblk
ldloc.0		//addr
ldc.i4.7	//value
ldc.i4.s 10	//size
initblk

//load number from offset 3
ldloc.0
ldc.i4.3
add	
ldind.u1

//compare
ldc.i4.7
beq PASS

FAIL:	ldstr "FAIL"
	call void [mscorlib]System.Console::WriteLine(string)
	br END

PASS:	ldstr "PASS"
	call void [mscorlib]System.Console::WriteLine(string)

END:	ret
  }


} 