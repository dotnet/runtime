// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Testing calli with pinvoke methods
.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME
{
}


.class public auto ansi Win32
{

  .method family static pinvokeimpl("kernel32.dll" ansi winapi) unsigned int32 GetSystemDirectoryA(class [mscorlib]System.Text.StringBuilder lpText,unsigned int32 uSize) il managed preservesig {}
  .method family static pinvokeimpl("kernel32.dll" unicode winapi) unsigned int32 GetSystemDirectoryW(class [mscorlib]System.Text.StringBuilder lpText,unsigned int32 uSize) il managed preservesig {}
  .method family static pinvokeimpl("kernel32.dll" as "GetSystemDirectory" autochar winapi) unsigned int32 GetSystemDirectoryAuto(class [mscorlib]System.Text.StringBuilder pText,unsigned int32 uSize) il managed preservesig {}
  .method family static pinvokeimpl("kernel32.dll" as "GetSystemDirectoryA" ansi winapi) unsigned int32 GetSystemDirectory(class [mscorlib]System.Text.StringBuilder lpText,unsigned int32 uSize) il managed preservesig {}
  .method public specialname rtspecialname instance void .ctor() il managed
  {
    // Method begins at RVA 0x1000
    // Code size       7 (0x7)
    .maxstack  1

    IL_0000:  ldarg.0
    IL_0001:  tail. call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method 'Win32::.ctor'

} // end of class 'Win32'

.class public auto ansi TailWin32 extends Win32
{
  .method public static unsigned int32 GetSystemDirectoryA(class [mscorlib]System.Text.StringBuilder lpText,unsigned int32 uSize) il managed
  {
    // Method begins at RVA 0x1020
    // Code size       8 (0x8)
    .maxstack  3

    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    ldftn     unsigned int32 Win32::GetSystemDirectoryA(class [mscorlib]System.Text.StringBuilder,unsigned int32)
    calli     unsigned int32 (class [mscorlib]System.Text.StringBuilder,unsigned int32)
    IL_0007:  ret
  } // end of method 'TailWin32::GetSystemDirectoryA'

  .method public instance unsigned int32 GetSystemDirectoryW(class [mscorlib]System.Text.StringBuilder lpText,unsigned int32 uSize) il managed
  {
    // Method begins at RVA 0x1040
    // Code size       8 (0x8)
    .maxstack  3

    IL_0000:  ldarg.1
    IL_0001:  ldarg.2
    ldftn     unsigned int32 Win32::GetSystemDirectoryW(class [mscorlib]System.Text.StringBuilder,unsigned int32)
    calli     unsigned int32 (class [mscorlib]System.Text.StringBuilder,unsigned int32)
    IL_0007:  ret
  } // end of method 'TailWin32::GetSystemDirectoryW'

  .method public static unsigned int32 GetSystemDirectoryAuto(class [mscorlib]System.Text.StringBuilder lpText,unsigned int32 uSize) il managed
  {
    // Method begins at RVA 0x1060
    // Code size       8 (0x8)
    .maxstack  3

    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    ldftn     unsigned int32 Win32::GetSystemDirectoryAuto(class [mscorlib]System.Text.StringBuilder,unsigned int32)
    calli     unsigned int32 (class [mscorlib]System.Text.StringBuilder,unsigned int32)
    IL_0007:  ret
  } // end of method 'TailWin32::GetSystemDirectoryAuto'

  .method public instance unsigned int32 GetSystemDirectory(class [mscorlib]System.Text.StringBuilder lpText,unsigned int32 uSize) il managed
  {
    // Method begins at RVA 0x1080
    // Code size       8 (0x8)
    .maxstack  3

    IL_0000:  ldarg.1
    IL_0001:  ldarg.2
    ldftn     unsigned int32 Win32::GetSystemDirectory(class [mscorlib]System.Text.StringBuilder,unsigned int32)
    calli     unsigned int32 (class [mscorlib]System.Text.StringBuilder,unsigned int32)
    IL_0007:  ret
  } // end of method 'TailWin32::GetSystemDirectory'

  .method public specialname rtspecialname instance void .ctor() il managed
  {
    // Method begins at RVA 0x10a0
    // Code size       7 (0x7)
    .maxstack  1

    IL_0000:  ldarg.0
    IL_0001:  tail. call       instance void Win32::.ctor()
    IL_0006:  ret
  } // end of method 'TailWin32::.ctor'

} // end of class 'TailWin32'

//Global methods
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
.class public auto ansi abstract sealed beforefieldinit callipinvoke2
    extends [mscorlib]System.Object
{
  .method public static int32 main() il managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Method begins at RVA 0x10c0
    // Code size       241 (0xf1)
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
    IL_0011:  newobj     instance void TailWin32::.ctor()
    IL_0016:  stloc.2
    IL_0017:  ldloc.0
    IL_0018:  ldc.i4     0x100
    IL_001d:  call       unsigned int32 TailWin32::GetSystemDirectoryA(class [mscorlib]System.Text.StringBuilder,unsigned int32)
    IL_0022:  stloc.3
    IL_0023:  ldloc.0
    IL_0024:  castclass  [mscorlib]System.Object
    IL_0029:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.Object)
    IL_002e:  ldloc.3
    IL_002f:  ldc.i4     0x0
    IL_0034:  bne.un     IL_004e

    IL_0039:  ldstr "Call ansi API failed"//ldptr      D_00003000
    IL_003e:  //newobj     instance void [mscorlib]System.String::.ctor(wchar*)
    IL_0043:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    IL_0048:  ldc.i4     0x0
    IL_004d:  stloc.1
    IL_004e:  ldloc.2
    IL_004f:  ldloc.0
    IL_0050:  ldc.i4     0x100
    IL_0055:  call       instance unsigned int32 TailWin32::GetSystemDirectory(class [mscorlib]System.Text.StringBuilder,unsigned int32)
    IL_005a:  stloc.3
    IL_005b:  ldloc.0
    IL_005c:  castclass  [mscorlib]System.Object
    IL_0061:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.Object)
    IL_0066:  ldloc.3
    IL_0067:  ldc.i4     0x0
    IL_006c:  bne.un     IL_0086

    IL_0071:  ldstr "Called unicode/ansi based." //ldptr      D_00003030
    IL_0076:  //newobj     instance void [mscorlib]System.String::.ctor(wchar*)
    IL_007b:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    IL_0080:  ldc.i4     0x0
    IL_0085:  stloc.1
    IL_0086:  ldloc.0
    IL_0087:  ldc.i4     0x100
    IL_008c:  call       unsigned int32 TailWin32::GetSystemDirectoryAuto(class [mscorlib]System.Text.StringBuilder,unsigned int32)
    IL_0091:  stloc.3
    IL_0092:  ldloc.0
    IL_0093:  castclass  [mscorlib]System.Object
    IL_0098:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.Object)
    IL_009d:  ldloc.3
    IL_009e:  ldc.i4     0x0
    IL_00a3:  bne.un     IL_00bc

    IL_00a8:  ldstr "Call unicode/ansi" //ldptr      D_000030A0
    IL_00ad:  //newobj     instance void [mscorlib]System.String::.ctor(wchar*)
    IL_00b2:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    IL_00b7:  br         IL_00dc

    IL_00bc:  ldloc.1
    IL_00bd:  ldc.i4     0x0
    IL_00c2:  beq        IL_00dc

    IL_00c7:  ldstr "Passed" //ldptr      D_000030EC
    IL_00cc:  //newobj     instance void [mscorlib]System.String::.ctor(wchar*)
    IL_00d1:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    IL_00d6:  ldc.i4     100
    IL_00db:  ret

    IL_00dc:  ldstr "failed"//ldptr      D_00003100
    IL_00e1:  //newobj     instance void [mscorlib]System.String::.ctor(wchar*)
    IL_00e6:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    IL_00eb:  ldc.i4     1
    IL_00f0:  ret
  } // end of global method 'main'

  .method public static int32 _mainMSIL(int32 argc,int8** argv,int8** envp) il managed
  {
    // Method begins at RVA 0x11c0
    // Code size       6 (0x6)
    .maxstack  1

    IL_0000:  call       int32 callipinvoke2::main()
    IL_0005:  ret
  } // end of global method '_mainMSIL'
}
