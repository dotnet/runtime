// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'fault'
{
}
.namespace DD
{
  .class public auto ansi ZZ
  {
    .method public static int32  Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       63 (0x3f)
      .maxstack  3
      .locals (int32 V_0)

				ldc.i4    3
				stloc.0

	  try_begin:
				ldstr     "message"
				newobj    instance void [mscorlib]System.Exception::.ctor(class [mscorlib]System.String)
				throw
				leave.s   try_end

	  fault_begin:
				ldstr      "in fault block."
				call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	   			ldloc.0
				ldc.i4    1
				sub
				stloc.0
				endfinally

      filter_begin:
      			pop
				ldstr      "in filter."
				call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	   			ldloc.0
				ldc.i4    1
				sub
				stloc.0
      			ldc.i4     1
      			endfilter
      			
      except_begin:
                call       void [mscorlib]System.GC::Collect()
				callvirt   instance class [mscorlib]System.String [mscorlib]System.Exception::get_Message()
				call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
				
				ldstr      "in except block."
				call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	   			ldloc.0
				ldc.i4    1
				sub
				stloc.0
    			leave.s    try_end
      
      try_end:
				ldloc.0
				ldc.i4.0
				bne.un.s   IL_0033
				
				ldstr      "test passed."
				call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
				ldc.i4     0x64
				ret

      IL_0033:
	  			ldstr      "test failed."
	  			call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_003d:
      			ldloc.0
      			ret

      .try try_begin to fault_begin fault handler fault_begin to filter_begin
      .try try_begin to filter_begin filter filter_begin handler except_begin to try_end
      
    } // end of method 'ZZ::Main'

    .method public specialname rtspecialname instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8

      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'ZZ::.ctor'

  } // end of class 'ZZ'

} // end of namespace 'DD'
