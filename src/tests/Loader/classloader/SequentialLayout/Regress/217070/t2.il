// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
.assembly extern mscorlib { } 
.assembly typeload { } 
.class sequential sealed public NumberNode 
       extends [mscorlib]System.ValueType 
{ 
    .field public int32 i 
    .field public valuetype NumberNode[] next 
} 

.class public main extends [mscorlib]System.Object 
{

  .method public static int32 Main()
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init (valuetype NumberNode[] V_0,
		  class [mscorlib]System.Exception V_1,                  
                  int32 V_2)

    .try
    {
      ldc.i4.1 
      newarr     NumberNode 
      stloc.0 

      ldstr      "PASS"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s   100
      stloc.2
      leave.s    end
    }
    catch [mscorlib]System.Exception 
    {
      stloc.1
      ldstr      "{0}Caught unexpected exception."
      ldloc.1
      call void [System.Console]System.Console::WriteLine(string,object)
      ldstr      "FAIL"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s   101
      stloc.2
      leave.s    end
    }
    end: ldloc.2
    ret
  }
}
