// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly extern 'lib-219037'
{
  .ver 0:0:0:0
}
.assembly 'exe-219037'
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.class public auto ansi beforefieldinit ClientType
       extends [mscorlib]System.Object
{
  .method public static void  Access() cil managed
  {
    .maxstack  8
    IL_0000:  ldsfld     int32 ['lib-219037']LibType::f
    IL_0005:  pop
    IL_0006:  ldsfld     int32 ['lib-219037']LibType::f
    IL_000b:  pop
    IL_000c:  ret
  } 

  .method public static int32  Main() cil managed
  {
    .entrypoint
    .maxstack  100
    IL_0000:  ldstr      "Main..."
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  call       void ClientType::Access()
    IL_000f:  ldc.i4.1
    IL_0010:  ldsfld     int32 ['lib-219037']Util::count
    IL_0015:  ldc.i4.1
    IL_0016:  ceq
    IL_0018:  sub
    IL_0019:  dup
    IL_001a:  ldstr      "Return code: "
    IL_001f:  call       void [System.Console]System.Console::Write(string)
    IL_0024:  box        [mscorlib]System.Int32
    IL_0029:  call       void [System.Console]System.Console::Write(object)
    IL_002e:  ldstr      " (non-zero indicates failure)"
    IL_0033:  call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4 0
	ceq
	brtrue.s PASS

FAIL:
	ldstr      "FAIL"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.0
	br.s       END

PASS:
	ldstr      "PASS"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.s   100
	br.s       END

END:
    IL_0038:  ret
  } 
}
