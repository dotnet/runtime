// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}

.assembly extern legacy library mscorlib {}
.assembly 'PInvokeTail'
{
}

.class public auto ansi Wrapper
{
  .method public static pinvokeimpl("msvcrt.dll" cdecl) int32 puts(int8* A_0) il managed preservesig{ }
  .method public specialname rtspecialname instance void .ctor() il managed
  {
    .maxstack  1

    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method 'Wrapper::.ctor'

} // end of class 'Wrapper'

.method public static pinvokeimpl("msvcrt.dll" cdecl) int32 puts(int8* A_0) il managed preservesig{ }

.method public static int32 callputs1(char&) il managed
{
    .maxstack 1

    ldarg.0
    conv.i
    tail. call int32 puts(int8*)
    ret
} // end of global method 'callputs1'

.method public static int32 callputs2(char&) il managed
{
    .maxstack 1

    ldarg.0
    conv.i
    tail. call int32 Wrapper::puts(int8*)
    ret
} // end of global method 'callputs2'

.class public auto ansi abstract sealed beforefieldinit PInvokeTail
    extends [mscorlib]System.Object
{
  .method public static int32 main() il managed
  {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack 4
      .locals init (char& pinned)

      ldstr bytearray(48 65 6C 6C 6F 20 57 6F 72 6C 64 21 00 00 00 00) // "Hello World!"
      call instance char& modreq([mscorlib]System.Runtime.InteropServices.InAttribute) [mscorlib]System.String::GetPinnableReference()
      stloc.0

      ldloc.0
      call        int32 callputs1(char&)
      ldc.i4      0x0
      bge         PASS_1

      call        class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
      ldstr       "puts() failed"
      callvirt    instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      ldc.i4      0x1
      ret

      PASS_1:
      ldloc.0
      call        int32 callputs2(char&)
      ldc.i4      0x0
      bge         PASS_2

      call        class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
      ldstr       "puts() failed"
      callvirt    instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      ldc.i4      0x1
      ret

      PASS_2:
      call        class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
      ldstr       "Passed"
      callvirt    instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      ldc.i4      0x64
      ret
  } // end of global method 'main'
}
