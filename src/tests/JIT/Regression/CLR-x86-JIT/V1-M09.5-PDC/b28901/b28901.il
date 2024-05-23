// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b28901'
{
}
.class public auto ansi Win32
{
  .method public family static pinvokeimpl("kernel32.dll" ansi winapi) unsigned int32 GetSystemDirectoryA(class [mscorlib]System.Text.StringBuilder lpText,unsigned int32 uSize) il managed preservesig
  { }
  .method public specialname rtspecialname instance void .ctor() il managed
  {
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method 'Win32::.ctor'
} // end of class 'Win32'

.class public auto ansi TailWin32 extends Win32
{
  .method public static vararg unsigned int32 GetSystemDirectoryA(class [mscorlib]System.Text.StringBuilder lpText,unsigned int32 uSize) il managed
  {
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  tail. call unsigned int32 Win32::GetSystemDirectoryA(class [mscorlib]System.Text.StringBuilder,unsigned int32)
    IL_0007:  ret
  }
} // end of class 'TailWin32'

//Global methods
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
.class public auto ansi abstract sealed beforefieldinit b28901
    extends [mscorlib]System.Object
{
  .method public static int32 main() il managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  3
    .locals init (class [mscorlib]System.Text.StringBuilder V_0,
            int32 V_1,
            class TailWin32 V_2,
            int32 V_3)

    IL_0000:  ldc.i4     0x100
    IL_0005:  newobj     instance void [mscorlib]System.Text.StringBuilder::.ctor(int32)
    IL_000a:  stloc.0

    IL_000b:  ldc.i4     0x1
    IL_0010:  stloc.1

    IL_0017:  ldloc.0
    IL_0018:  ldc.i4     0x100
    IL_001d:  call vararg unsigned int32 TailWin32::GetSystemDirectoryA(class [mscorlib]System.Text.StringBuilder,unsigned int32)
    IL_0022:  stloc.3

      IL_00bc:  ldloc.1
    IL_00bd:  ldc.i4     0x0
    IL_00c2:  beq        IL_00dc

    IL_00c7:  ldstr      "Passed"
    IL_00d1:  call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)

    IL_00d6:  ldc.i4     100
    IL_00db:  ret

    IL_00dc:  ldstr      "Failed"
    IL_00e6:  call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)

    IL_00eb:  ldc.i4     0x1
    IL_00f0:  ret
  } // end of global method 'main'
}
