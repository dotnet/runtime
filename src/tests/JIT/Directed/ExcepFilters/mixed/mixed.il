// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly mixed
{
}
.namespace DD
{
	.class public auto ansi X
	{
	  .field public int32 m_param
	  .method public specialname rtspecialname instance void .ctor(int32 param) il managed
	  {
	    // Code size       7 (0x7)
	    .maxstack  8
		ldarg.0
		call       instance void [mscorlib]System.Object::.ctor()
		ldarg.0
		ldarg.1
		stfld      int32 DD.X::m_param
		ret
	  } // end of method 'X::.ctor'
	
	} // end of class 'X'
	
  .class auto ansi CustomException extends [mscorlib]System.Exception
  {
    .field public int32 m_param
    .method public specialname rtspecialname instance void  .ctor(int32 param) il managed
    {
		// Code size       14 (0xe)
		//.maxstack  8

					ldarg.0
					call       instance void [mscorlib]System.Exception::.ctor()
					ldarg.0
					ldarg.1
					stfld      int32 DD.CustomException::m_param
					ret
    } // end of method 'CustomException::.ctor'

  } // end of class 'CustomException'

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
      .locals (int32 V_0, class DD.X V_1)

				ldc.i4    9
				stloc.0

	  try_begin:
	  			ldc.i4    43
				newobj     instance void DD.CustomException::.ctor(int32)
				throw
			      
	  filter_begin:
				pop
				ldc.i4     11
				newobj     instance void DD.X::.ctor(int32)
				pop
				ldstr      "in filter."
				call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
				ldloc.0
				ldc.i4    2
				sub
				stloc.0
				ldc.i4    1
				endfilter

      int_catch_begin:
				pop
				ldc.i4     19
				newobj     instance void DD.X::.ctor(int32)
				pop
				ldloc.0
				ldc.i4    1
				sub
				stloc.0
				rethrow
				leave.s    try_end

      int_finally_begin:
				ldloc.0
				ldc.i4    2
				sub
				stloc.0
				endfinally
      
      catch1_begin:
				pop
				ldc.i4     13
				newobj     instance void DD.X::.ctor(int32)
				pop
				ldloc.0
				ldc.i4    3
				sub
				stloc.0
				leave.s    try_end

      catch2_begin:
				pop
				ldc.i4     12
				newobj     instance void DD.X::.ctor(int32)
				pop
				ldloc.0
				ldc.i4     1
				add
				stloc.0
				leave.s    try_end

      ext_finally_begin:
				ldloc.0
				ldc.i4     1
				sub
				stloc.0
				endfinally
      
      try_end:
      			ldc.i4     15
				newobj     instance void DD.X::.ctor(int32)
				pop
				call       void [mscorlib]System.GC::Collect()
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

	      			ldloc.0
      				ret

      .try try_begin to filter_begin filter filter_begin handler int_catch_begin to int_finally_begin
      .try try_begin to int_finally_begin finally handler int_finally_begin to catch1_begin
      .try try_begin to catch1_begin catch DD.CustomException handler catch1_begin to catch2_begin
      .try try_begin to catch1_begin catch [mscorlib]System.Exception handler catch2_begin to ext_finally_begin
      .try try_begin to ext_finally_begin finally handler ext_finally_begin to try_end
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
