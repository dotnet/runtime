// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'filter' { }
.namespace JitTest
{
  .class private auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method private hidebysig static int32
            Main() cil managed
    {
      .entrypoint
      // Code size       120 (0x78)
      .maxstack  2
      .locals (bool V_0,
               bool V_1,
               bool V_2,
               object V_3,
               int32 V_4)
		.try {
		      
        TRY_1:
        		newobj instance void [mscorlib]System.Exception::.ctor()
      			throw
          
		FILTER_1:
				isinst [mscorlib]System.Single
				ldstr      "inside 1st filter"
          		call       void [System.Console]System.Console::WriteLine(string)
          
      			unbox float32
      			ldind.r4
      			ldc.r4 1.0
      			ceq
      			pop
      			
				ldc.i4.1
				endfilter
				
		HANDLER_1:
      			stloc.3
      			ldloc.3
      			unbox float32
      			ldind.r4
      			ldc.r4 1.0
      			ceq
      			brfalse NEXT
      			
      			ldc.i4.1
      			stloc.1
          			
        NEXT:
        		leave.s END_1

        END_1:
        
        .try TRY_1 to FILTER_1 filter FILTER_1 handler HANDLER_1 to END_1
         leave EOM_FAIL
       } catch [mscorlib]System.Exception {
         leave EOM_PASS
       }
       EOM_PASS:
		ldc.i4 100
      	ret
       EOM_FAIL:
		ldc.i4 101
      	ret
    } // end of method Test::Main

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test::.ctor

  } // end of class Test

} // end of namespace JitTest
