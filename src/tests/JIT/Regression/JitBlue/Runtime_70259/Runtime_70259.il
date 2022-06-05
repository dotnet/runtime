// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Note: This file is produced from Runtime_70259.cs.


//  Microsoft (R) .NET IL Disassembler.  Version 7.0.0-dev

// Metadata version: v4.0.30319
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 7:0:0:0
}
.assembly playground
{
}
.module playground.dll
// MVID: {D76932D2-B329-4511-BC19-7A4B6B78D720}
.custom instance void [System.Runtime]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 )
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x0000027337C40000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit Runtime_70259
       extends [System.Runtime]System.Object
{
  .class interface abstract auto ansi nested private IFace
  {
    .method public hidebysig newslot abstract virtual
            instance int32  Method(int32 left) cil managed
    {
    } // end of method IFace::Method

  } // end of class IFace

  .class auto ansi nested private beforefieldinit C
         extends [System.Runtime]System.Object
         implements Runtime_70259/IFace
  {
    .class sequential ansi sealed nested private beforefieldinit LargeStruct
           extends [System.Runtime]System.ValueType
    {
      .class sequential ansi sealed nested public beforefieldinit '<Bytes>e__FixedBuffer'
             extends [System.Runtime]System.ValueType
      {
        .pack 0
        .size 1024
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 )
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.UnsafeValueTypeAttribute::.ctor() = ( 01 00 00 00 )
        .field public uint8 FixedElementField
      } // end of class '<Bytes>e__FixedBuffer'

      .field public valuetype Runtime_70259/C/LargeStruct/'<Bytes>e__FixedBuffer' Bytes
      .custom instance void [System.Runtime]System.Runtime.CompilerServices.FixedBufferAttribute::.ctor(class [System.Runtime]System.Type,
                                                                                                        int32) = ( 01 00 5E 53 79 73 74 65 6D 2E 42 79 74 65 2C 20   // ..^System.Byte,
                                                                                                                   53 79 73 74 65 6D 2E 52 75 6E 74 69 6D 65 2C 20   // System.Runtime,
                                                                                                                   56 65 72 73 69 6F 6E 3D 37 2E 30 2E 30 2E 30 2C   // Version=7.0.0.0,
                                                                                                                   20 43 75 6C 74 75 72 65 3D 6E 65 75 74 72 61 6C   //  Culture=neutral
                                                                                                                   2C 20 50 75 62 6C 69 63 4B 65 79 54 6F 6B 65 6E   // , PublicKeyToken
                                                                                                                   3D 62 30 33 66 35 66 37 66 31 31 64 35 30 61 33   // =b03f5f7f11d50a3
                                                                                                                   61 00 04 00 00 00 00 )                            // a......
    } // end of class LargeStruct

    .field public class [System.Runtime]System.Func`3<class Runtime_70259/IFace,int32,int32> Delegate
    .method public hidebysig newslot virtual
            instance int32  Method(int32 left) cil managed
    {
      .custom instance void [System.Runtime]System.Runtime.CompilerServices.SkipLocalsInitAttribute::.ctor() = ( 01 00 00 00 )
      // Code size       32 (0x20)
      .maxstack  4
      .locals (valuetype Runtime_70259/C/LargeStruct V_0)
      IL_0000:  ldarg.1
      IL_0001:  brtrue.s   IL_0006

      IL_0003:  ldc.i4.s   100
      IL_0005:  ret

      IL_0006:  ldarg.0
      IL_0007:  ldloca.s   V_0
      IL_0009:  call       instance void Runtime_70259/C::Consume(valuetype Runtime_70259/C/LargeStruct&)
      IL_000e:  ldarg.0
      IL_000f:  ldfld      class [System.Runtime]System.Func`3<class Runtime_70259/IFace,int32,int32> Runtime_70259/C::Delegate
      IL_0014:  ldarg.0
      IL_0015:  ldarg.1
      IL_0016:  ldc.i4.1
      IL_0017:  sub
      IL_0018:  tail.
      IL_001a:  call       instance !2 class [System.Runtime]System.Func`3<class Runtime_70259/IFace,int32,int32>::Invoke(!0,
                                                                                                                          !1)
      IL_001f:  ret
    } // end of method C::Method

    .method private hidebysig instance void
            Consume([in] valuetype Runtime_70259/C/LargeStruct& s) cil managed noinlining
    {
      .param [1]
      .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
      // Code size       1 (0x1)
      .maxstack  8
      IL_0000:  ret
    } // end of method C::Consume

    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  ret
    } // end of method C::.ctor

  } // end of class C

  .method private hidebysig static int32
          Main() cil managed
  {
    .entrypoint
    // Code size       47 (0x2f)
    .maxstack  4
    IL_0000:  newobj     instance void Runtime_70259/C::.ctor()
    IL_0005:  dup
    IL_0006:  ldtoken    Runtime_70259/IFace
    IL_000b:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0010:  ldstr      "Method"
    IL_0015:  call       instance class [System.Runtime]System.Reflection.MethodInfo [System.Runtime]System.Type::GetMethod(string)
    IL_001a:  callvirt   instance !!0 [System.Runtime]System.Reflection.MethodInfo::CreateDelegate<class [System.Runtime]System.Func`3<class Runtime_70259/IFace,int32,int32>>()
    IL_001f:  stfld      class [System.Runtime]System.Func`3<class Runtime_70259/IFace,int32,int32> Runtime_70259/C::Delegate
    IL_0024:  ldc.i4     0x186a0
    IL_0029:  call       instance int32 Runtime_70259/C::Method(int32)
    IL_002e:  ret
  } // end of method Runtime_70259::Main

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Runtime_70259::.ctor

} // end of class Runtime_70259


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
