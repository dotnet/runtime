// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.







.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib
{
                           
  
           
  
}
.assembly 'int32_0_5b'
{
}
.assembly extern xunit.core {}
.namespace JitTest
{
  .class public auto ansi Int32_0_5B
         extends [mscorlib]System.Object
  {
    .method private hidebysig static int32 
            try_index(int32[0...5] arr,
                      int32 idx,
                      int32 expected) il managed
    {
      .maxstack  5
      .locals (int32)
		ldstr     "Trying index "
		call       void [System.Console]System.Console::Write(string)
		ldarga.s   idx
		call       instance string [mscorlib]System.Int32::ToString()
		call       void [System.Console]System.Console::Write(string)
      .try
      {
			ldarg.0
			ldarg.1
			ldarg.1
			stelem.i4
			
			ldarg.0
			ldarg.1
			ldelem.i4
			
			ldarg.1
			ceq
			brtrue aaa
			
        	ldstr      "READ-WRITE FAILED"
        	call       void [System.Console]System.Console::WriteLine(string)
        	newobj		instance void [mscorlib]System.Exception::.ctor()
        	throw
        	
		aaa:
			
        	ldstr      ": OK"
        	call       void [System.Console]System.Console::WriteLine(string)
        	ldc.i4.1
        	stloc.0
        	leave.s    IL_0029

      }  // end .try
      catch [mscorlib]System.Object 
      {
        IL_001c:  pop
        IL_001d:  ldstr      ": exception!"
        IL_0022:  call       void [System.Console]System.Console::WriteLine(string)
        
				ldc.i4.0
				stloc.0
        IL_0027:  leave.s    IL_0029

      }  // end handler
      IL_0029:
      ldloc.0
      ldarg.2
      ceq
      ret
    } // end of method 'Test::try_index'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  4
      .locals (int32[0...5] V_0,
               int32 V_1,
               int32 V_2)
               
      			ldstr     "Creating array int32[0..5]"
      			call       void [System.Console]System.Console::WriteLine(string)
      			
      			ldc.i4.s   0
      			ldc.i4.s   6
      			newobj		instance void int32[0...5]::.ctor(int32, int32)
      			stloc.0

      			ldstr     "Reported array size is "
      			call       void [System.Console]System.Console::Write(string)
      			ldloc.0
      			ldlen
      			call       void [System.Console]System.Console::WriteLine(int32)
      
      			ldstr     "[mscorlib]System.Array::get_Rank returned "
      			call       void [System.Console]System.Console::Write(string)
      			ldloc.0
      			callvirt   instance int32 [mscorlib]System.Array::get_Rank()
      			call       void [System.Console]System.Console::WriteLine(int32)

      			ldstr     "[mscorlib]System.Array::GetLowerBound returned "
      			call       void [System.Console]System.Console::Write(string)
      			ldloc.0
      			ldc.i4.0
      			callvirt   instance int32 [mscorlib]System.Array::GetLowerBound(int32)
      			call       void [System.Console]System.Console::WriteLine(int32)
      			
      			ldstr     "[mscorlib]System.Array::GetUpperBound returned "
      			call       void [System.Console]System.Console::Write(string)
      			ldloc.0
      			ldc.i4.0
      			callvirt   instance int32 [mscorlib]System.Array::GetUpperBound(int32)
      			call       void [System.Console]System.Console::WriteLine(int32)
      			
      			ldloc.0
      			ldc.i4		-10000
      			ldc.i4.0
      			call		int32 JitTest.Int32_0_5B::try_index(int32[0...5], int32, int32)
      			brfalse		Failure
      			
      			ldloc.0
      			ldc.i4.m1
      			ldc.i4.0
      			call		int32 JitTest.Int32_0_5B::try_index(int32[0...5], int32, int32)
      			brfalse		Failure
      			
      			ldloc.0
      			ldc.i4.0
      			ldc.i4.1
      			call		int32 JitTest.Int32_0_5B::try_index(int32[0...5], int32, int32)
      			brfalse		Failure
      			
      			ldloc.0
      			ldc.i4.1
      			ldc.i4.1
      			call		int32 JitTest.Int32_0_5B::try_index(int32[0...5], int32, int32)
      			brfalse		Failure
      			
      			ldloc.0
      			ldc.i4		5
      			ldc.i4.1
      			call		int32 JitTest.Int32_0_5B::try_index(int32[0...5], int32, int32)
      			brfalse		Failure
      			
      			ldloc.0
      			ldc.i4		6
      			ldc.i4.0
      			call		int32 JitTest.Int32_0_5B::try_index(int32[0...5], int32, int32)
      			brfalse		Failure
      			
      			ldloc.0
      			ldc.i4		1000
      			ldc.i4.0
      			call		int32 JitTest.Int32_0_5B::try_index(int32[0...5], int32, int32)
      			brfalse		Failure
      			
      			ldloc.0
      			ldc.i4 1000000
      			ldc.i4.0
      			call       int32 JitTest.Int32_0_5B::try_index(int32[0...5], int32, int32)
      			brfalse		Failure
      			
      			ldloc.0
      			ldc.i4 1300000
      			ldc.i4.0
      			call       int32 JitTest.Int32_0_5B::try_index(int32[0...5], int32, int32)
      			brfalse		Failure
      			
      			ldc.i4    0x64
      			ldstr   "**** PASSED ****"
      			call       void [System.Console]System.Console::WriteLine(string)
      			br 		Escape	
      		Failure:
      			ldc.i4.1
      			ldstr   "**** FAILED ****"
      			call       void [System.Console]System.Console::WriteLine(string)
      		Escape:
      			ret
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'JitTest'

