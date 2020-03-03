.assembly TestDll { }
.assembly extern mscorlib { }

.method assembly specialname rtspecialname static 
        void  .cctor() cil managed
{
	ldc.i4 1
	stsfld int32 NS.TestClass::TestField
	ldstr "Module contructor executed"
	call void [mscorlib]System.Console::WriteLine(string)
	ret
}

.namespace NS
{
	.class public TestClass extends [mscorlib]System.Object
	{
		.field public static int32 TestField
	
		.method public static void  TestMethod() cil managed
		{
		  ldstr "TestMethod executed"
		  call void [mscorlib]System.Console::WriteLine(string)
		  ret
		}
		.method public static void  Main() cil managed
		{
		  .entrypoint
		  ret
		}
	}
}
