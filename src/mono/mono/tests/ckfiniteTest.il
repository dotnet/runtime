//Tests the ckfinite instruction
//First test on an infinite (finite / 0)
//Second test on NaN (0 / 0)
//Third test sees if value is left on stack if finite
//Both print either PASS or FAIL
//Assumes div works as per specifications

.assembly extern mscorlib{}
.assembly someTest{}
.module someTest

.class private auto ansi someTest
       extends [mscorlib]System.Object {

  .method public static void  go() cil managed {
    .entrypoint

//TEST 1 - infinite

.try {
	ldc.r8 1
	ldc.r8 0
	div
	ckfinite
	ldstr "FAIL"
	call void [mscorlib]System.Console::WriteLine(string)
	leave test2
}
catch [mscorlib]System.ArithmeticException {
	pop
	ldstr "PASS"
	call void [mscorlib]System.Console::WriteLine(string)
	leave test2
}
catch [mscorlib]System.Exception {
	pop
	ldstr "EFAIL"
	call void [mscorlib]System.Console::WriteLine(string)
	leave test2
}

//TEST 2 - NaN
test2: nop

.try {
	ldc.r8 0
	ldc.r8 0
	div
	ckfinite
	ldstr "FAIL"
	call void [mscorlib]System.Console::WriteLine(string)
	leave test3
}
catch [mscorlib]System.ArithmeticException {
	ldstr "PASS"
	call void [mscorlib]System.Console::WriteLine(string)
	leave test3
}
catch [mscorlib]System.Exception {
	ldstr "FAIL"
	call void [mscorlib]System.Console::WriteLine(string)
	leave test3
}

//TEST 3
test3: nop

.try {
	ldc.r8 3.777
	ckfinite
	ldc.r8 3.777
	bne.un FAIL
	ldstr "PASS"
	call void [mscorlib]System.Console::WriteLine(string)
	leave end
				
FAIL:	ldstr "FAIL"
	call void [mscorlib]System.Console::WriteLine(string)
	leave end
}
catch [mscorlib]System.Exception {
	ldstr "FAIL"
	call void [mscorlib]System.Console::WriteLine(string)
	leave end
}

end:	ret
  }


} 