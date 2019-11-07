//
// This .il file was created by ildasming ExchangeTNegSource.exe
// then changing 
//      IL_000d:  ldnull
//      IL_000e:  stloc.1
//      IL_000f:  ldloca.s   V_1
//      IL_0011:  ldstr      "hello world"
// to
//      IL_000d:  ldloc.s 1
//      IL_0011:  ldstr      "hello world"

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .ver 2:0:0:0
}
.assembly ExchangeTNeg {}


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    .custom instance void [mscorlib]System.Security.SecuritySafeCriticalAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       100 (0x64)
    .maxstack  2
    .locals init (int32 V_0,
             object V_1,
             object V_2,
             class [mscorlib]System.Exception V_3,
             int32 V_4,
//Next line was added
             object& V_5)
    IL_0000:  nop
    IL_0001:  ldc.i4.m1
    IL_0002:  stloc.0
    IL_0003:  ldstr      "Start Test"
    IL_0008:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000d:  nop
    .try
    {
      IL_000e:  nop
//Next line was change to
      IL_0010:  ldloc.s 5
//From the following:
//      IL_000f:  ldnull
//      IL_0010:  stloc.1
//      IL_0011:  ldloca.s   V_1
      IL_0013:  ldstr      "hello world"
      IL_0018:  call       !!0 [mscorlib]System.Threading.Interlocked::Exchange<object>(!!0&,
                                                                                        !!0)
      IL_001d:  stloc.2
      IL_001e:  nop
      IL_001f:  leave.s    IL_0044

    }  // end .try
    catch [mscorlib]System.NullReferenceException 
    {
      IL_0021:  pop
      IL_0022:  nop
      IL_0023:  ldc.i4.s   100
      IL_0025:  stloc.0
      IL_0026:  nop
      IL_0027:  leave.s    IL_0044

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_0029:  stloc.3
      IL_002a:  nop
      IL_002b:  ldstr      "Wrong exception thrown: "
      IL_0030:  ldloc.3
      IL_0031:  callvirt   instance string [mscorlib]System.Object::ToString()
      IL_0036:  call       string [mscorlib]System.String::Concat(string,
                                                                  string)
      IL_003b:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0040:  nop
      IL_0041:  nop
      IL_0042:  leave.s    IL_0044

    }  // end handler
    IL_0044:  nop
    IL_0045:  ldc.i4.s   100
    IL_0047:  ldloc.0
    IL_0048:  beq.s      IL_0051

    IL_004a:  ldstr      "Test Failed"
    IL_004f:  br.s       IL_0056

    IL_0051:  ldstr      "Test Passed"
    IL_0056:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_005b:  nop
    IL_005c:  ldloc.0
    IL_005d:  stloc.s    V_4
    IL_005f:  br.s       IL_0061

    IL_0061:  ldloc.s    V_4
    IL_0063:  ret
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

} // end of class Test
