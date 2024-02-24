// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly extern System.Runtime.Extensions {}
.assembly BufferMemmoveTailCall {
    // Allow access to private members of System.Private.CoreLib
    .custom instance void System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute::.ctor(string) = (
        01 00 16 53 79 73 74 65 6d 2e 50 72 69 76 61 74
        65 2e 43 6f 72 65 4c 69 62 00 00
    )
}

.class public abstract auto ansi sealed beforefieldinit TailCallBufferMemmove
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .maxstack 8
    .entrypoint

    // C#:
    // byte[] src = new byte[32];
    // Test(ref src[0]);

    ldc.i4.s 32
    newarr [System.Runtime]System.Byte
    ldc.i4.0
    ldelema [System.Runtime]System.Byte
    call void TailCallBufferMemmove::Test(uint8&)
    
    // return 100;
    ldc.i4.s 100
    ret
  }

  .method private hidebysig static void Test (uint8& src) cil managed noinlining 
  {
    .maxstack 3

    // C#:
    // byte* data = stackalloc byte[64]; // to trigger slow helper-based tail calls
    // SpanHelpers.Memmove(ref Unsafe.AsRef<byte>(data), ref src, 64);

    ldc.i4.s 64
    conv.u
    localloc
    call !!0& [System.Runtime]System.Runtime.CompilerServices.Unsafe::AsRef<uint8>(void*)
    ldarg.0
    ldc.i4.s 64
    conv.i
    tail. call void [System.Runtime]System.SpanHelpers::Memmove(uint8&, uint8&, native uint)
    ret
  }
}

// C#:
// namespace System.Runtime.CompilerServices
// {
//     public class IgnoresAccessChecksToAttribute : Attribute
//     {
//         public IgnoresAccessChecksToAttribute(string assemblyName)
//         {
//             AssemblyName = assemblyName;
//         }
//         public string AssemblyName { get; }
//     }
// }
//
.class public auto ansi beforefieldinit System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute
    extends [System.Runtime]System.Attribute
{
  .field private initonly string '<AssemblyName>k__BackingField'
  .method public hidebysig specialname rtspecialname instance void .ctor (string assemblyName) cil managed 
  {
    .maxstack 8
    ldarg.0
    call instance void [System.Runtime]System.Attribute::.ctor()
    ldarg.0
    ldarg.1
    stfld string System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute::'<AssemblyName>k__BackingField'
    ret
  }
  .method public hidebysig specialname instance string get_AssemblyName () cil managed 
  {
    .maxstack 8
    ldarg.0
    ldfld string System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute::'<AssemblyName>k__BackingField'
    ret
  }
  .property instance string AssemblyName()
  {
    .get instance string System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute::get_AssemblyName()
  }
}
