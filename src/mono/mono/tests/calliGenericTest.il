//Tests ldftn and calli with generic return signature

.assembly extern mscorlib{}
.assembly someTest{}
.module someTest

.class public auto ansi beforefieldinit Test`1<T>
  extends [mscorlib]System.Object
{
    // method line 1
    .method public hidebysig
           static default void test (native int ptr)  cil managed 
    {
	.maxstack 8
	ldarg ptr
	calli !T()
	call void class [mscorlib]System.Console::WriteLine(int32)
	ret 
    }
}
	
.class private auto ansi someTest
       extends [mscorlib]System.Object {
	   
  .method public static void  go() cil managed {
    .entrypoint

	ldftn int32 [mscorlib]System.Environment::get_ProcessorCount()
	call void class Test`1<int32>::test(native int)
	ret
  }
} 