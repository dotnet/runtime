//tests localloc by allocating 4 bytes, storing a value, and loading it
.assembly extern mscorlib{}
.assembly someTest{}
.module someTest

.class private auto ansi someTest
       extends [mscorlib]System.Object {

  .method public static void  go() cil managed {
    .entrypoint

	.locals init (native int)

//allocate
ldc.i4.4
localloc
stloc.0

//store number
ldloc.0
ldc.i4 7777777
stind.i4

//load number
ldloc.0
ldind.i4

//compare
ldc.i4 7777777
beq PASS

FAIL:	ldstr "FAIL"
	call void [mscorlib]System.Console::WriteLine(string)
	br END

PASS:	ldstr "PASS"
	call void [mscorlib]System.Console::WriteLine(string)

END:	ret
  }


} 