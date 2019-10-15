// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern legacy library mscorlib {}
.assembly pinvoketail
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

.method public static int32 ?callputs1@@YAHXZ() il managed
{
  .maxstack  1

  IL_0000:  ldstr bytearray(48 65 6C 6C 6F 20 57 6F 72 6C 64 21 00 00 00 00) //ldptr      D_00003000
  IL_0005:  tail. call       int32 puts(int8*)
  IL_000a:  ret
} // end of global method '?callputs1@@YAHXZ'

.method public static int32 ?callputs2@@YAHXZ() il managed
{
  .maxstack  1

  IL_0000:  ldstr bytearray(48 65 6C 6C 6F 20 57 6F 72 6C 64 21 00 00 00 00) //"  " //ldptr      D_00003010
  IL_0005:  tail. call       int32 Wrapper::puts(int8*)
  IL_000a:  ret
} // end of global method '?callputs2@@YAHXZ'

.method public static int32 main() il managed
{
  .entrypoint
  .maxstack  2

  IL_0000:  call       int32 ?callputs1@@YAHXZ()
  IL_0005:  ldc.i4     0x0
  IL_000a:  bge        IL_0029

  IL_000f:  	call	    class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
  IL_0014:  ldstr "Puts Failed" //ldptr      D_00003020
  IL_0019:  //newobj     instance void [mscorlib]System.String::.ctor(wchar*)
  IL_001e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  IL_0023:  ldc.i4     0x1
  IL_0028:  ret

  IL_0029:  call       int32 ?callputs2@@YAHXZ()
  IL_002e:  ldc.i4     0x0
  IL_0033:  bge        IL_0052

  IL_0038:  	call	    class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
  IL_003d:  ldstr "wrapper: Puts failed"//ldptr      D_00003040
  IL_0042:  //newobj     instance void [mscorlib]System.String::.ctor(wchar*)
  IL_0047:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  IL_004c:  ldc.i4     0x1
  IL_0051:  ret

  IL_0052:  	call	    class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
  IL_0057:  ldstr "passed"//ldptr      D_00003070
  IL_005c:  //newobj     instance void [mscorlib]System.String::.ctor(wchar*)
  IL_0061:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
  IL_0066:  ldc.i4     0x64
  IL_006b:  ret
} // end of global method 'main'

.method public static int32 _mainMSIL(int32 argc,int8** argv,int8** envp) il managed
{
  .maxstack  1

  IL_0000:  tail. call       int32 main()
  IL_0005:  ret
} // end of global method '_mainMSIL'

.method public static pinvokeimpl("msvcrt.dll" cdecl) int32 puts(int8* A_0) il managed preservesig{ }
.data D_00003000 = bytearray (
                 48 65 6C 6C 6F 20 57 6F 72 6C 64 21 00 00 00 00) // Hello World!....
.data D_00003010 = bytearray (
                 48 65 6C 6C 6F 20 57 6F 72 6C 64 21 00 00 00 00) // Hello World!....
.data D_00003020 = bytearray (
                 70 00 75 00 74 00 73 00 28 00 29 00 20 00 66 00  // p.u.t.s.(.). .f.
                 61 00 69 00 6C 00 65 00 64 00 2E 00 00 00 00 00) // a.i.l.e.d.......
.data D_00003040 = bytearray (
                 57 00 72 00 61 00 70 00 70 00 65 00 72 00 3A 00  // W.r.a.p.p.e.r.:.
                 3A 00 70 00 75 00 74 00 73 00 28 00 29 00 20 00  // :.p.u.t.s.(.). .
                 66 00 61 00 69 00 6C 00 65 00 64 00 2E 00 00 00) // f.a.i.l.e.d.....
.data D_00003070 = bytearray (
                 50 00 61 00 73 00 73 00 65 00 64 00 00 00)       // P.a.s.s.e.d...
