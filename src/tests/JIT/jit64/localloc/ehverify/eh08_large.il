// Test localloc in fault

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib{}
.assembly extern localloc_common{}
.assembly 'eh08_large'{}

.class public auto ansi beforefieldinit LocallocTest
       extends [mscorlib]System.Object
{
  .method private hidebysig static void  func() cil managed
  {
    .maxstack  2
    .locals init (int32* intArray1)
    .try{
    IL_000a:  ldstr      "Test Exception"
    IL_000f:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
    IL_0014:  throw
	}
	fault
	{
    IL_0000:  ldc.i4.4
    IL_0001:  ldc.i4	0x1000
    IL_0006:  mul
    IL_0007:  localloc
    IL_0009:  stloc.0
    IL_0015:  endfault
    }
    ret
  } 

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  1
    .locals init (int32 retValue)
    .try
    {
      IL_0000:  call       void LocallocTest::func()
      IL_0005:  leave.s    IL_002d

    }  
    catch [mscorlib]System.InvalidProgramException 
    {
      IL_0007:  pop
      IL_0008:  ldstr      "InvalidProgramException was caught."
      IL_000d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0012:  leave.s    IL_003b

    }  
    catch [mscorlib]System.Security.VerificationException 
    {
      IL_0014:  pop
      IL_0015:  ldstr      "Security.VerificationException was caught."
      IL_001a:  call       void [System.Console]System.Console::WriteLine(string)
      IL_001f:  leave.s    IL_003b

    }  
    catch [mscorlib]System.Exception 
    {
      IL_0021:  callvirt   instance string [mscorlib]System.Exception::ToString()
      IL_0026:  call       void [System.Console]System.Console::WriteLine(string)
      IL_002b:  leave.s    IL_002d

    }  
    
    IL_002d:  ldstr      "Failed"
    IL_0032:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0037:  ldc.i4.1
    IL_0038:  stloc.0
    IL_0039:  br.s       IL_004a

    IL_003b:  ldstr      "Passed"
    IL_0040:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0045:  ldc.i4.s   100
    IL_0047:  stloc.0
    IL_0048:  br.s       IL_004a

    IL_004a:  ldloc.0
    IL_004b:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 
