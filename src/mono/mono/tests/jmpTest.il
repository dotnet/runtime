.assembly extern mscorlib{}
.assembly jmpTest{}
.module jmpTest

.class private auto ansi jmpTest
       extends [mscorlib]System.Object {

  .method public static int32 go() cil managed {
    .entrypoint
	jmp int32 [jmpTest]jmpTest::pass()
        ldc.i4.1	
	ret
  }

  .method public static int32 pass() cil managed {
        ldc.i4.0	
	ret
  }
} 