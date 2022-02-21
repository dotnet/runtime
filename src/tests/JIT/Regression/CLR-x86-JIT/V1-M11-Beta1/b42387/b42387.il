// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b42387'
{


  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
// MVID: {23E3D49B-6CFD-4DA0-8C9D-A611CE0BA103}
.namespace JitTest
{
  .class private auto ansi Test
         extends [mscorlib]System.Object
  {
    .method private hidebysig static void 
            try_index(int32[0...] arr,
                      int32 idx) il managed
    {
      // Code size       42 (0x2a)
      .maxstack  3
		ldstr     "Trying index "
		call       void [System.Console]System.Console::Write(class [mscorlib]System.String)
		ldarga.s   idx
		call       instance class [mscorlib]System.String [mscorlib]System.Int32::ToString()
		call       void [System.Console]System.Console::Write(class [mscorlib]System.String)
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
        	call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
		aaa:
			
        	ldstr      ": OK"
        	call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        	leave.s    IL_0029

      }  // end .try
      catch [mscorlib]System.Object 
      {
        IL_001c:  pop
        IL_001d:  ldstr      ": exception!"
        IL_0022:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
        IL_0027:  leave.s    IL_0029

      }  // end handler
      IL_0029:  ret
    } // end of method 'Test::try_index'

    .method private hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       38 (0x26)
      .maxstack  2
      .locals (int32[0...] V_0,
               int32 V_1,
               int32 V_2)
               
      			ldstr     "Creating array of 16 elements"
      			call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      			
      			ldc.i4.s   16
      			newobj		instance void int32[0...]::.ctor(int32)
      			stloc.0
      			
      			ldstr     "Reported array size is "
      			call       void [System.Console]System.Console::Write(class [mscorlib]System.String)
      			ldloc.0
      			ldlen
      			call       void [System.Console]System.Console::WriteLine(int32)
      
      			ldloc.0
      			ldc.i4 -10000
      			call       void JitTest.Test::try_index(int32[0...], int32)
      			
      			ldloc.0
      			ldc.i4.m1
      			call       void JitTest.Test::try_index(int32[0...], int32)
      			
      			ldloc.0
      			ldc.i4.0
      			call       void JitTest.Test::try_index(int32[0...], int32)
      			
      			ldloc.0
      			ldc.i4.1
      			call       void JitTest.Test::try_index(int32[0...], int32)
      			
      			ldloc.0
      			ldc.i4 1000
      			call       void JitTest.Test::try_index(int32[0...], int32)
      			
      			ldloc.0
      			ldc.i4 1000000
      			call       void JitTest.Test::try_index(int32[0...], int32)
      			
      			ldloc.0
      			ldc.i4 1300000
      			call       void JitTest.Test::try_index(int32[0...], int32)
      			
      			ldc.i4 100
      			ret
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'JitTest'

//*********** DISASSEMBLY COMPLETE ***********************
