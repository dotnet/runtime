
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern legacy library mscorlib {}
.assembly 'vafilt'
{
  
  
}
.namespace App
{
  .class auto ansi Foo extends [mscorlib]System.Object
  {
    .field private class System.String text
    .method family newslot virtual instance vararg class App.Foo VargFunc() il managed
    {
      .maxstack  2
      .locals ([0] value class [mscorlib]System.ArgIterator args)
      IL_0000:  ldloca.s   args
      IL_0002:  arglist
      IL_0004:  call       instance void [mscorlib]System.ArgIterator::.ctor(value class [mscorlib]System.RuntimeArgumentHandle)
      IL_0009:  ldloca.s   args
      IL_000b:  call       instance typedref [mscorlib]System.ArgIterator::GetNextArg()
      IL_0010:  refanyval  App.Foo
      IL_0015:  ldind.ref
      IL_0016:  ldfld      class System.String App.Foo::text
      IL_001b:  call       void [System.Console]System.Console::WriteLine(class System.String)
      			call       void [mscorlib]System.GC::Collect()
      IL_0020:  newobj     instance void App.Foo::.ctor()
      IL_0025:  ret
    } 

    .method public static int32  Main(class System.String[] args) il managed
    {
      .entrypoint
      .maxstack  3
      .locals ([0] int32 zero)
        try_begin:
        		ldc.i4.0
				stloc.0
				ldc.i4.s   15
				ldloc.0
				div
				call       void [System.Console]System.Console::WriteLine(int32)
				leave.s    handler_end
		filter_begin:
				pop
				newobj     instance void App.Foo::.ctor()
				newobj     instance void App.Foo::.ctor()
				callvirt   instance vararg class App.Foo App.Foo::VargFunc(...,
                                                                             class App.Foo
                                                                             )
        		pop
        		ldc.i4.1
        		endfilter
        handler_begin:
        		pop
      			leave.s    handler_end
        handler_end:
				ldstr      "*** test passed ***"
				call       void [System.Console]System.Console::WriteLine(class System.String)
				ldc.i4    0x64
				ret
      .try try_begin to filter_begin filter filter_begin handler handler_begin to handler_end
    } 

    .method public specialname rtspecialname instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldstr      "Blah Blah"
      IL_0006:  stfld      class System.String App.Foo::text
      IL_000b:  ldarg.0
      IL_000c:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0011:  ret
    } 

  } 

} 
