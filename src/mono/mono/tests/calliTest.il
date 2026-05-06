//Tests ldftn and calli
//Prints PASS if passed, nothing if failed

.assembly extern mscorlib{}
.assembly someTest{}
.module someTest

.class private auto ansi someTest
       extends [mscorlib]System.Object {

  .method public static void  go() cil managed {
    .entrypoint

	ldstr "PASS"
	ldftn void [mscorlib]System.Console::WriteLine(string)
	calli void (string)
	ret
  }


} 