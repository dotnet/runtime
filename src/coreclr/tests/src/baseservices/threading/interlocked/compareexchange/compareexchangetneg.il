.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .ver 2:0:0:0
}
.assembly CompareExchangeTNeg{}

.class Test
{
  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    .custom instance void [mscorlib]System.Security.SecuritySafeCriticalAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       101 (0x65)
    .maxstack  3
    .locals init (int32 V_0,
             object V_1,
             object V_2,
             class [mscorlib]System.Exception V_3,
             int32 V_4,
//Next line was added
             object& V_5)
    IL_0000:  ldc.i4.m1
    IL_0001:  stloc.0
    IL_0002:  ldstr      "Start Test"
    IL_0007:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000c:  nop
    .try
    {
//Next line was changed to
      IL_000d:  ldloc.s 5
//From
//    IL_000d:  ldloc.s 1
      IL_0011:  ldstr      "hello world"
      IL_0016:  ldstr      "goodbye world"
      IL_001b:  call       !!0 [mscorlib]System.Threading.Interlocked::CompareExchange<object>(!!0&,
                                                                                               !!0,
                                                                                               !!0)
      IL_0020:  stloc.2
      IL_0021:  nop
      IL_0022:  leave.s    IL_0045

    }  // end .try
    catch [mscorlib]System.NullReferenceException 
    {
      IL_0024:  pop
      IL_0025:  ldc.i4.s   100
      IL_0027:  stloc.0
      IL_0028:  nop
      IL_0029:  leave.s    IL_0045

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_002b:  stloc.3
      IL_002c:  ldstr      "Wrong exception thrown: "
      IL_0031:  ldloc.3
      IL_0032:  callvirt   instance string [mscorlib]System.Object::ToString()
      IL_0037:  call       string [mscorlib]System.String::Concat(string,
                                                                  string)
      IL_003c:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0041:  nop
      IL_0042:  nop
      IL_0043:  leave.s    IL_0045

    }  // end handler
    IL_0045:  nop
    IL_0046:  ldc.i4.s   100
    IL_0048:  ldloc.0
    IL_0049:  beq.s      IL_0052

    IL_004b:  ldstr      "Test Failed"
    IL_0050:  br.s       IL_0057

    IL_0052:  ldstr      "Test Passed"
    IL_0057:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_005c:  nop
    IL_005d:  ldloc.0
    IL_005e:  stloc.s    V_4
    IL_0060:  br.s       IL_0062

    IL_0062:  ldloc.s    V_4
    IL_0064:  ret
  } // end of method Test::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor
}
