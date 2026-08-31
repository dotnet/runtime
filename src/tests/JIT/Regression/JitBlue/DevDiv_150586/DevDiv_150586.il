// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This il tests importation of unbox[_any] when there are pending evaluations on the stack that side-effect
// the arguments of the unbox.

.assembly extern mscorlib { }

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly DevDiv_150586
{
}
.assembly extern xunit.core {}


.class public auto ansi beforefieldinit Bug.Program
       extends [System.Runtime]System.Object
{
  .method private hidebysig static string 
          GetResourceString(string category,
                            [out] object& foundObj) cil managed
  {
    // Code size       20 (0x14)
    .maxstack  8
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4.0
    IL_0002:  box        [System.Runtime]System.Boolean
    IL_0007:  stind.ref
    IL_0008:  ldstr      "Hello world"
    IL_000d:  ldarg.0
    IL_000e:  call       string [System.Runtime]System.String::Concat(string,
                                                                      string)
    IL_0013:  ret
  } // end of method Program::GetResourceString

  .method private hidebysig static string 
          RString() cil managed
  {
    // Code size       38 (0x26)
    .maxstack  2
    .locals init ([0] object foundObj,
             [1] bool V_1)
    IL_0000:  ldstr      ""
    IL_0005:  ldloca.s   foundObj
    IL_0007:  call       string Bug.Program::GetResourceString(string,
                                                               object&)
    IL_000c:  ldloc.0
    IL_000d:  unbox.any  [System.Runtime]System.Boolean
    IL_0012:  stloc.1
    IL_0013:  ldloca.s   V_1
    IL_0015:  constrained. [System.Runtime]System.Boolean
    IL_001b:  callvirt   instance string [System.Runtime]System.Object::ToString()
    IL_0020:  call       string [System.Runtime]System.String::Concat(string,
                                                                      string)
    IL_0025:  ret
  } // end of method Program::RString

  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       39 (0x27)
    .maxstack  1
    .locals init ([0] int32 V_0)
    .try
    {
      IL_0000:  call       string Bug.Program::RString()
      IL_0005:  pop
      IL_0006:  leave.s    IL_0018

    }  // end .try
    catch [System.Runtime]System.Exception 
    {
      IL_0008:  pop
      IL_0009:  ldstr      "Fail"
      IL_000e:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0013:  ldc.i4.s   101
      IL_0015:  stloc.0
      IL_0016:  leave.s    IL_0025

    }  // end handler
    IL_0018:  ldstr      "Pass"
    IL_001d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0022:  ldc.i4.s   100
    IL_0024:  ret

    IL_0025:  ldloc.0
    IL_0026:  ret
  } // end of method Program::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Program::.ctor

} // end of class Bug.Program
