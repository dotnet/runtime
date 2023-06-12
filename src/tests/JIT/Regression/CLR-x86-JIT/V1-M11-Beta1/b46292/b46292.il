// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern Microsoft.VisualBasic
{
}
.assembly 'b46292'
{
}
.assembly extern xunit.core {}
// MVID: {74C704CE-5E25-4072-A5C7-A9DD14614D82}
.class private auto ansi Mod1
       extends [mscorlib]System.Object
{
  .method public static void Main() il managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    // Code size       26 (0x1a)
    .maxstack  1
    .locals init (class c1 V_0)
    .try
    {
      IL_0000:  ldloc.0
      IL_0001:  callvirt   instance void c1::foo()
      IL_0006:  leave.s    IL_0019

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      pop
      IL_000d:  ldstr      "exception"
      IL_0012:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_0017:  leave.s    IL_0019

    }  // end handler
    IL_0019:  
	ret
  } // end of method 'Mod1::Main'

} // end of class 'Mod1'

.class private auto ansi c1
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void .ctor() il managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method 'c1::.ctor'

  .method public instance void foo() il managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "hi"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    IL_000a:  ret
  } // end of method 'c1::foo'

} // end of class 'c1'

.class public auto ansi _vbProject
       extends [mscorlib]System.Object
{
  .method public static int32  _main() il managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       6 (0x6)
    .maxstack  8
    IL_0000:  call       void Mod1::Main()
	ldc.i4 100
    IL_0005:  ret
  } // end of method '_vbProject::_main'

} // end of class '_vbProject'

//*********** DISASSEMBLY COMPLETE ***********************
