// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib
{
  .publickeytoken = (7C EC 85 D7 BE A7 79 8E )                         // |.....y.
  .ver 2:0:5:0
}
.assembly ReflectOnField
{


  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 4:0:30513:0
}
.module ReflectOnField.exe
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY



.class public abstract auto ansi sealed beforefieldinit ReflectOnField.Container`1<T>
       extends [mscorlib]System.Object
{
  .field public static int32 StaticField
} // end of class ReflectOnField.Container`1

.class private abstract auto ansi sealed beforefieldinit ReflectOnField.App
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32  Main() cil managed
  {
    .entrypoint
    .maxstack  3
    .locals init (class [mscorlib]System.Reflection.FieldInfo[] V_0)
    IL_0000:  ldtoken    class ReflectOnField.Container`1<int32>
    IL_0005:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000a:  ldc.i4.s   24
    IL_000c:  callvirt   instance class [mscorlib]System.Reflection.FieldInfo[] [mscorlib]System.Type::GetFields(valuetype [mscorlib]System.Reflection.BindingFlags)
    IL_0011:  stloc.0
    IL_0012:  ldstr      "Field0 = `{0}'"
    IL_0017:  ldloc.0
    IL_0018:  ldc.i4.0
    IL_0019:  ldelem.ref
    IL_001a:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_001f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                 object)
    IL_0024:  ldc.i4.s 100 
    IL_0028:  ret
  } // end of method App::Main

} // end of class ReflectOnField.App



