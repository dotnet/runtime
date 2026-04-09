//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.8.3928.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.

// See more_tailcalls.cs for the source of this IL.


// Metadata version: v4.0.30319
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 7:0:0:0
}
.assembly more_tailcalls
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                   63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [System.Runtime]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 02 00 00 00 00 00 ) 

  .custom instance void [System.Runtime]System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(string) = ( 01 00 18 2E 4E 45 54 43 6F 72 65 41 70 70 2C 56   // ....NETCoreApp,V
                                                                                                              65 72 73 69 6F 6E 3D 76 37 2E 30 01 00 54 0E 14   // ersion=v7.0..T..
                                                                                                              46 72 61 6D 65 77 6F 72 6B 44 69 73 70 6C 61 79   // FrameworkDisplay
                                                                                                              4E 61 6D 65 08 2E 4E 45 54 20 37 2E 30 )          // Name..NET 7.0
  .custom instance void [System.Runtime]System.Reflection.AssemblyCompanyAttribute::.ctor(string) = ( 01 00 0E 6D 6F 72 65 5F 74 61 69 6C 63 61 6C 6C   // ...more_tailcall
                                                                                                      73 00 00 )                                        // s..
  .custom instance void [System.Runtime]System.Reflection.AssemblyConfigurationAttribute::.ctor(string) = ( 01 00 07 52 65 6C 65 61 73 65 00 00 )             // ...Release..
  .custom instance void [System.Runtime]System.Reflection.AssemblyFileVersionAttribute::.ctor(string) = ( 01 00 07 31 2E 30 2E 30 2E 30 00 00 )             // ...1.0.0.0..
  .custom instance void [System.Runtime]System.Reflection.AssemblyInformationalVersionAttribute::.ctor(string) = ( 01 00 05 31 2E 30 2E 30 00 00 )                   // ...1.0.0..
  .custom instance void [System.Runtime]System.Reflection.AssemblyProductAttribute::.ctor(string) = ( 01 00 0E 6D 6F 72 65 5F 74 61 69 6C 63 61 6C 6C   // ...more_tailcall
                                                                                                      73 00 00 )                                        // s..
  .custom instance void [System.Runtime]System.Reflection.AssemblyTitleAttribute::.ctor(string) = ( 01 00 0E 6D 6F 72 65 5F 74 61 69 6C 63 61 6C 6C   // ...more_tailcall
                                                                                                    73 00 00 )                                        // s..
  .permissionset reqmin
             = {class 'System.Security.Permissions.SecurityPermissionAttribute, System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' = {property bool 'SkipVerification' = bool(true)}}
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
// MVID: {CAF8B990-D08A-4343-9074-7FE350EEBD80}
.custom instance void [System.Runtime]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.custom instance void System.Runtime.CompilerServices.RefSafetyRulesAttribute::.ctor(int32) = ( 01 00 0B 00 00 00 00 00 ) 
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x000002618BA40000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi sealed beforefieldinit Microsoft.CodeAnalysis.EmbeddedAttribute
       extends [System.Runtime]System.Attribute
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Attribute::.ctor()
    IL_0006:  ret
  } // end of method EmbeddedAttribute::.ctor

} // end of class Microsoft.CodeAnalysis.EmbeddedAttribute

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.NullableAttribute
       extends [System.Runtime]System.Attribute
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Runtime]System.AttributeUsageAttribute::.ctor(valuetype [System.Runtime]System.AttributeTargets) = ( 01 00 84 6B 00 00 02 00 54 02 0D 41 6C 6C 6F 77   // ...k....T..Allow
                                                                                                                                     4D 75 6C 74 69 70 6C 65 00 54 02 09 49 6E 68 65   // Multiple.T..Inhe
                                                                                                                                     72 69 74 65 64 00 )                               // rited.
  .field public initonly uint8[] NullableFlags
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(uint8 A_1) cil managed
  {
    // Code size       23 (0x17)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Attribute::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldc.i4.1
    IL_0008:  newarr     [System.Runtime]System.Byte
    IL_000d:  dup
    IL_000e:  ldc.i4.0
    IL_000f:  ldarg.1
    IL_0010:  stelem.i1
    IL_0011:  stfld      uint8[] System.Runtime.CompilerServices.NullableAttribute::NullableFlags
    IL_0016:  ret
  } // end of method NullableAttribute::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(uint8[] A_1) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Attribute::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      uint8[] System.Runtime.CompilerServices.NullableAttribute::NullableFlags
    IL_000d:  ret
  } // end of method NullableAttribute::.ctor

} // end of class System.Runtime.CompilerServices.NullableAttribute

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.NullableContextAttribute
       extends [System.Runtime]System.Attribute
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Runtime]System.AttributeUsageAttribute::.ctor(valuetype [System.Runtime]System.AttributeTargets) = ( 01 00 4C 14 00 00 02 00 54 02 0D 41 6C 6C 6F 77   // ..L.....T..Allow
                                                                                                                                     4D 75 6C 74 69 70 6C 65 00 54 02 09 49 6E 68 65   // Multiple.T..Inhe
                                                                                                                                     72 69 74 65 64 00 )                               // rited.
  .field public initonly uint8 Flag
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(uint8 A_1) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Attribute::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      uint8 System.Runtime.CompilerServices.NullableContextAttribute::Flag
    IL_000d:  ret
  } // end of method NullableContextAttribute::.ctor

} // end of class System.Runtime.CompilerServices.NullableContextAttribute

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.RefSafetyRulesAttribute
       extends [System.Runtime]System.Attribute
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [System.Runtime]System.AttributeUsageAttribute::.ctor(valuetype [System.Runtime]System.AttributeTargets) = ( 01 00 02 00 00 00 02 00 54 02 0D 41 6C 6C 6F 77   // ........T..Allow
                                                                                                                                     4D 75 6C 74 69 70 6C 65 00 54 02 09 49 6E 68 65   // Multiple.T..Inhe
                                                                                                                                     72 69 74 65 64 00 )                               // rited.
  .field public initonly int32 Version
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 A_1) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Attribute::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      int32 System.Runtime.CompilerServices.RefSafetyRulesAttribute::Version
    IL_000d:  ret
  } // end of method RefSafetyRulesAttribute::.ctor

} // end of class System.Runtime.CompilerServices.RefSafetyRulesAttribute

.class private sequential ansi sealed beforefieldinit S16
       extends [System.Runtime]System.ValueType
{
  .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 ) 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
  .field public int64 A
  .field public int64 B
  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    // Code size       55 (0x37)
    .maxstack  3
    .locals init (valuetype [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  ldc.i4.2
    IL_0003:  ldc.i4.2
    IL_0004:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::.ctor(int32,
                                                                                                                               int32)
    IL_0009:  ldloca.s   V_0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      int64 S16::A
    IL_0011:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int64>(!!0)
    IL_0016:  ldloca.s   V_0
    IL_0018:  ldstr      ", "
    IL_001d:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_0022:  ldloca.s   V_0
    IL_0024:  ldarg.0
    IL_0025:  ldfld      int64 S16::B
    IL_002a:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int64>(!!0)
    IL_002f:  ldloca.s   V_0
    IL_0031:  call       instance string [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::ToStringAndClear()
    IL_0036:  ret
  } // end of method S16::ToString

  .method public hidebysig instance string 
          InstanceMethod() cil managed noinlining
  {
    // Code size       6 (0x6)
    .maxstack  8
    IL_0000:  ldstr      "Instance method"
    IL_0005:  ret
  } // end of method S16::InstanceMethod

} // end of class S16

.class private sequential ansi sealed beforefieldinit S32
       extends [System.Runtime]System.ValueType
{
  .field public int64 A
  .field public int64 B
  .field public int64 C
  .field public int64 D
  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 ) 
    // Code size       105 (0x69)
    .maxstack  3
    .locals init (valuetype [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  ldc.i4.6
    IL_0003:  ldc.i4.4
    IL_0004:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::.ctor(int32,
                                                                                                                               int32)
    IL_0009:  ldloca.s   V_0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      int64 S32::A
    IL_0011:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int64>(!!0)
    IL_0016:  ldloca.s   V_0
    IL_0018:  ldstr      ", "
    IL_001d:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_0022:  ldloca.s   V_0
    IL_0024:  ldarg.0
    IL_0025:  ldfld      int64 S32::B
    IL_002a:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int64>(!!0)
    IL_002f:  ldloca.s   V_0
    IL_0031:  ldstr      ", "
    IL_0036:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_003b:  ldloca.s   V_0
    IL_003d:  ldarg.0
    IL_003e:  ldfld      int64 S32::C
    IL_0043:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int64>(!!0)
    IL_0048:  ldloca.s   V_0
    IL_004a:  ldstr      ", "
    IL_004f:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_0054:  ldloca.s   V_0
    IL_0056:  ldarg.0
    IL_0057:  ldfld      int64 S32::D
    IL_005c:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int64>(!!0)
    IL_0061:  ldloca.s   V_0
    IL_0063:  call       instance string [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::ToStringAndClear()
    IL_0068:  ret
  } // end of method S32::ToString

} // end of class S32

.class private sequential ansi sealed beforefieldinit SGC
       extends [System.Runtime]System.ValueType
{
  .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 ) 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
  .field public object A
  .field public object B
  .field public string C
  .field public string D
  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    // Code size       105 (0x69)
    .maxstack  3
    .locals init (valuetype [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  ldc.i4.6
    IL_0003:  ldc.i4.4
    IL_0004:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::.ctor(int32,
                                                                                                                               int32)
    IL_0009:  ldloca.s   V_0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      object SGC::A
    IL_0011:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<object>(!!0)
    IL_0016:  ldloca.s   V_0
    IL_0018:  ldstr      ", "
    IL_001d:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_0022:  ldloca.s   V_0
    IL_0024:  ldarg.0
    IL_0025:  ldfld      object SGC::B
    IL_002a:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<object>(!!0)
    IL_002f:  ldloca.s   V_0
    IL_0031:  ldstr      ", "
    IL_0036:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_003b:  ldloca.s   V_0
    IL_003d:  ldarg.0
    IL_003e:  ldfld      string SGC::C
    IL_0043:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_0048:  ldloca.s   V_0
    IL_004a:  ldstr      ", "
    IL_004f:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_0054:  ldloca.s   V_0
    IL_0056:  ldarg.0
    IL_0057:  ldfld      string SGC::D
    IL_005c:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_0061:  ldloca.s   V_0
    IL_0063:  call       instance string [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::ToStringAndClear()
    IL_0068:  ret
  } // end of method SGC::ToString

} // end of class SGC

.class private sequential ansi sealed beforefieldinit SGC2
       extends [System.Runtime]System.ValueType
{
  .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 ) 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
  .field public int32 A
  .field public valuetype SGC B
  .field public object C
  .field public int32 D
  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    // Code size       105 (0x69)
    .maxstack  3
    .locals init (valuetype [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  ldc.i4.8
    IL_0003:  ldc.i4.4
    IL_0004:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::.ctor(int32,
                                                                                                                               int32)
    IL_0009:  ldloca.s   V_0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      int32 SGC2::A
    IL_0011:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int32>(!!0)
    IL_0016:  ldloca.s   V_0
    IL_0018:  ldstr      ", ("
    IL_001d:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_0022:  ldloca.s   V_0
    IL_0024:  ldarg.0
    IL_0025:  ldfld      valuetype SGC SGC2::B
    IL_002a:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<valuetype SGC>(!!0)
    IL_002f:  ldloca.s   V_0
    IL_0031:  ldstr      "), "
    IL_0036:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_003b:  ldloca.s   V_0
    IL_003d:  ldarg.0
    IL_003e:  ldfld      object SGC2::C
    IL_0043:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<object>(!!0)
    IL_0048:  ldloca.s   V_0
    IL_004a:  ldstr      ", "
    IL_004f:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_0054:  ldloca.s   V_0
    IL_0056:  ldarg.0
    IL_0057:  ldfld      int32 SGC2::D
    IL_005c:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int32>(!!0)
    IL_0061:  ldloca.s   V_0
    IL_0063:  call       instance string [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::ToStringAndClear()
    IL_0068:  ret
  } // end of method SGC2::ToString

} // end of class SGC2

.class private auto ansi beforefieldinit HeapInt
       extends [System.Runtime]System.Object
{
  .field public int32 Value
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 val) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      int32 HeapInt::Value
    IL_000d:  ret
  } // end of method HeapInt::.ctor

  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 ) 
    // Code size       30 (0x1e)
    .maxstack  3
    .locals init (valuetype [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  ldc.i4.0
    IL_0003:  ldc.i4.1
    IL_0004:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::.ctor(int32,
                                                                                                                               int32)
    IL_0009:  ldloca.s   V_0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      int32 HeapInt::Value
    IL_0011:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int32>(!!0)
    IL_0016:  ldloca.s   V_0
    IL_0018:  call       instance string [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::ToStringAndClear()
    IL_001d:  ret
  } // end of method HeapInt::ToString

} // end of class HeapInt

.class public auto ansi Program
       extends [System.Runtime]System.Object
{
  .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 ) 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
  .class auto ansi nested private beforefieldinit GenericInstance`1<T>
         extends [System.Runtime]System.Object
  {
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .param type T 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    .field private class Program/GenericInstanceFactory factory
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(class Program/GenericInstanceFactory factory) cil managed
    {
      // Code size       14 (0xe)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldarg.1
      IL_0008:  stfld      class Program/GenericInstanceFactory class Program/GenericInstance`1<!T>::factory
      IL_000d:  ret
    } // end of method GenericInstance`1::.ctor

    .method public hidebysig newslot virtual 
            instance int32  NumberOfInstances() cil managed noinlining
    {
      // Code size       12 (0xc)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class Program/GenericInstanceFactory class Program/GenericInstance`1<!T>::factory
      IL_0006:  ldfld      int32 Program/GenericInstanceFactory::counter
      IL_000b:  ret
    } // end of method GenericInstance`1::NumberOfInstances

  } // end of class GenericInstance`1

  .class auto ansi nested private beforefieldinit GenericInstanceFactory
         extends [System.Runtime]System.Object
  {
    .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public int32 counter
    .method public hidebysig instance class Program/GenericInstance`1<string> 
            CreateInstance() cil managed noinlining
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 ) 
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldarg.0
      IL_0002:  ldfld      int32 Program/GenericInstanceFactory::counter
      IL_0007:  ldc.i4.1
      IL_0008:  add
      IL_0009:  stfld      int32 Program/GenericInstanceFactory::counter
      IL_000e:  ldarg.0
      IL_000f:  newobj     instance void class Program/GenericInstance`1<string>::.ctor(class Program/GenericInstanceFactory)
      IL_0014:  ret
    } // end of method GenericInstanceFactory::CreateInstance

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  ret
    } // end of method GenericInstanceFactory::.ctor

  } // end of class GenericInstanceFactory

  .class auto ansi sealed nested private beforefieldinit '<>c__DisplayClass7_0'
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public bool result
    .field public int32 ten
    .field public class GenInstance`2<string,int32> g
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 02 00 00 00 00 01 00 00 ) 
    .field public class IGenInterface`2<string,int32> ig
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 02 00 00 00 00 01 00 00 ) 
    .field public class IGenInterface`2<string,object> ig2
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 03 00 00 00 00 01 01 00 00 ) 
    .field public class GenAbstractImpl`1<string> ga1
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 02 00 00 00 00 01 00 00 ) 
    .field public class GenAbstractImpl`1<int32> ga2
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public int32[] a
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  ret
    } // end of method '<>c__DisplayClass7_0'::.ctor

    .method assembly hidebysig instance void 
            '<Main>g__Test|0'<T>(class [System.Runtime]System.Func`1<!!T> f,
                                 !!T expected,
                                 string name) cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      .param [1]
      .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 02 00 00 00 01 00 00 00 ) 
      .param [3]
      .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 01 00 00 ) 
      // Code size       105 (0x69)
      .maxstack  4
      .locals init (class [System.Runtime]System.Diagnostics.Stopwatch V_0,
               !!T V_1)
      IL_0000:  ldstr      "{0}: "
      IL_0005:  ldarg.3
      IL_0006:  call       void [System.Console]System.Console::Write(string,
                                                                      object)
      IL_000b:  call       class [System.Runtime]System.Diagnostics.Stopwatch [System.Runtime]System.Diagnostics.Stopwatch::StartNew()
      IL_0010:  stloc.0
      IL_0011:  ldarg.1
      IL_0012:  callvirt   instance !0 class [System.Runtime]System.Func`1<!!T>::Invoke()
      IL_0017:  stloc.1
      IL_0018:  ldloc.0
      IL_0019:  callvirt   instance void [System.Runtime]System.Diagnostics.Stopwatch::Stop()
      IL_001e:  ldloca.s   V_1
      IL_0020:  ldarg.2
      IL_0021:  box        !!T
      IL_0026:  constrained. !!T
      IL_002c:  callvirt   instance bool [System.Runtime]System.Object::Equals(object)
      IL_0031:  brfalse.s  IL_004a

      IL_0033:  ldstr      "OK in {1} ms"
      IL_0038:  ldarg.3
      IL_0039:  ldloc.0
      IL_003a:  callvirt   instance int64 [System.Runtime]System.Diagnostics.Stopwatch::get_ElapsedMilliseconds()
      IL_003f:  box        [System.Runtime]System.Int64
      IL_0044:  call       void [System.Console]System.Console::WriteLine(string,
                                                                          object,
                                                                          object)
      IL_0049:  ret

      IL_004a:  ldstr      "FAIL (expected {1}, got {2})"
      IL_004f:  ldarg.3
      IL_0050:  ldarg.2
      IL_0051:  box        !!T
      IL_0056:  ldloc.1
      IL_0057:  box        !!T
      IL_005c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                          object,
                                                                          object,
                                                                          object)
      IL_0061:  ldarg.0
      IL_0062:  ldc.i4.0
      IL_0063:  stfld      bool Program/'<>c__DisplayClass7_0'::result
      IL_0068:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>g__Test|0'

    .method assembly hidebysig instance void 
            '<Main>g__TestCalc|1'<T>(class [System.Runtime]System.Func`3<int32,int32,!!T> f,
                                     !!T expected,
                                     string name) cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      .param [1]
      .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 02 00 00 00 01 00 00 00 ) 
      .param [3]
      .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 01 00 00 ) 
      // Code size       34 (0x22)
      .maxstack  4
      .locals init (class Program/'<>c__DisplayClass7_1`1'<!!T> V_0)
      IL_0000:  newobj     instance void class Program/'<>c__DisplayClass7_1`1'<!!T>::.ctor()
      IL_0005:  stloc.0
      IL_0006:  ldloc.0
      IL_0007:  ldarg.1
      IL_0008:  stfld      class [System.Runtime]System.Func`3<int32,int32,!0> class Program/'<>c__DisplayClass7_1`1'<!!T>::f
      IL_000d:  ldarg.0
      IL_000e:  ldloc.0
      IL_000f:  ldftn      instance !0 class Program/'<>c__DisplayClass7_1`1'<!!T>::'<Main>b__31'()
      IL_0015:  newobj     instance void class [System.Runtime]System.Func`1<!!T>::.ctor(object,
                                                                                         native int)
      IL_001a:  ldarg.2
      IL_001b:  ldarg.3
      IL_001c:  call       instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<!!0>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                !!0,
                                                                                                string)
      IL_0021:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'

    .method assembly hidebysig instance string 
            '<Main>b__7'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       51 (0x33)
      .maxstack  10
      IL_0000:  newobj     instance void Instance::.ctor()
      IL_0005:  ldstr      "2"
      IL_000a:  ldc.i4.3
      IL_000b:  ldstr      "4"
      IL_0010:  ldc.i4.5
      IL_0011:  box        [System.Runtime]System.Int32
      IL_0016:  ldstr      "6"
      IL_001b:  ldstr      "7"
      IL_0020:  ldstr      "8"
      IL_0025:  ldc.i4.s   9
      IL_0027:  ldarg.0
      IL_0028:  ldflda     int32 Program/'<>c__DisplayClass7_0'::ten
      IL_002d:  call       instance string Instance::GC(object,
                                                        int32,
                                                        object,
                                                        object,
                                                        string,
                                                        string,
                                                        object,
                                                        int32,
                                                        int32&)
      IL_0032:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__7'

    .method assembly hidebysig instance string 
            '<Main>b__19'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       28 (0x1c)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class GenInstance`2<string,int32> Program/'<>c__DisplayClass7_0'::g
      IL_0006:  ldstr      "a"
      IL_000b:  ldc.i4.5
      IL_000c:  ldstr      "b"
      IL_0011:  ldstr      "c"
      IL_0016:  callvirt   instance string class GenInstance`2<string,int32>::NonVirtForward<object,string>(!0,
                                                                                                            !1,
                                                                                                            !!0,
                                                                                                            !!1)
      IL_001b:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__19'

    .method assembly hidebysig instance string 
            '<Main>b__20'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       28 (0x1c)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class GenInstance`2<string,int32> Program/'<>c__DisplayClass7_0'::g
      IL_0006:  ldstr      "a"
      IL_000b:  ldc.i4.5
      IL_000c:  ldstr      "b"
      IL_0011:  ldstr      "c"
      IL_0016:  callvirt   instance string class GenInstance`2<string,int32>::VirtForward<object,string>(!0,
                                                                                                         !1,
                                                                                                         !!0,
                                                                                                         !!1)
      IL_001b:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__20'

    .method assembly hidebysig instance string 
            '<Main>b__21'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       28 (0x1c)
      .maxstack  8
      IL_0000:  ldstr      "a"
      IL_0005:  ldc.i4.5
      IL_0006:  ldstr      "c"
      IL_000b:  ldstr      "d"
      IL_0010:  ldarg.0
      IL_0011:  ldfld      class IGenInterface`2<string,int32> Program/'<>c__DisplayClass7_0'::ig
      IL_0016:  call       string Program::GenInterfaceForwardF<string,int32,string,object>(!!0,
                                                                                            !!1,
                                                                                            !!2,
                                                                                            !!3,
                                                                                            class IGenInterface`2<!!0,!!1>)
      IL_001b:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__21'

    .method assembly hidebysig instance string 
            '<Main>b__22'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       18 (0x12)
      .maxstack  8
      IL_0000:  ldstr      "a"
      IL_0005:  ldc.i4.5
      IL_0006:  ldarg.0
      IL_0007:  ldfld      class IGenInterface`2<string,int32> Program/'<>c__DisplayClass7_0'::ig
      IL_000c:  call       string Program::GenInterfaceForwardG<string,int32>(!!0,
                                                                              !!1,
                                                                              class IGenInterface`2<!!0,!!1>)
      IL_0011:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__22'

    .method assembly hidebysig instance string 
            '<Main>b__23'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       28 (0x1c)
      .maxstack  8
      IL_0000:  ldstr      "a"
      IL_0005:  ldstr      "b"
      IL_000a:  ldc.i4.5
      IL_000b:  ldstr      "d"
      IL_0010:  ldarg.0
      IL_0011:  ldfld      class IGenInterface`2<string,object> Program/'<>c__DisplayClass7_0'::ig2
      IL_0016:  call       string Program::GenInterfaceForwardNone(string,
                                                                   object,
                                                                   int32,
                                                                   object,
                                                                   class IGenInterface`2<string,object>)
      IL_001b:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__23'

    .method assembly hidebysig instance string 
            '<Main>b__24'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       22 (0x16)
      .maxstack  8
      IL_0000:  ldstr      "a"
      IL_0005:  ldstr      "b"
      IL_000a:  ldarg.0
      IL_000b:  ldfld      class IGenInterface`2<string,object> Program/'<>c__DisplayClass7_0'::ig2
      IL_0010:  call       string Program::GenInterfaceForward2(string,
                                                                object,
                                                                class IGenInterface`2<string,object>)
      IL_0015:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__24'

    .method assembly hidebysig instance string 
            '<Main>b__25'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       12 (0xc)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class GenAbstractImpl`1<string> Program/'<>c__DisplayClass7_0'::ga1
      IL_0006:  call       string Program::GenAbstractFString(class GenAbstract`1<string>)
      IL_000b:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__25'

    .method assembly hidebysig instance string 
            '<Main>b__26'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       12 (0xc)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class GenAbstractImpl`1<int32> Program/'<>c__DisplayClass7_0'::ga2
      IL_0006:  call       string Program::GenAbstractFInt(class GenAbstract`1<int32>)
      IL_000b:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__26'

    .method assembly hidebysig instance string 
            '<Main>b__27'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       12 (0xc)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class GenAbstractImpl`1<string> Program/'<>c__DisplayClass7_0'::ga1
      IL_0006:  call       string Program::GenAbstractGString(class GenAbstract`1<string>)
      IL_000b:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__27'

    .method assembly hidebysig instance string 
            '<Main>b__28'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       12 (0xc)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class GenAbstractImpl`1<int32> Program/'<>c__DisplayClass7_0'::ga2
      IL_0006:  call       string Program::GenAbstractGInt(class GenAbstract`1<int32>)
      IL_000b:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__28'

    .method assembly hidebysig instance int32 
            '<Main>b__29'() cil managed
    {
      // Code size       24 (0x18)
      .maxstack  8
      IL_0000:  ldc.i4.0
      IL_0001:  ldc.i4.0
      IL_0002:  ldstr      "string"
      IL_0007:  ldarg.0
      IL_0008:  ldfld      int32[] Program/'<>c__DisplayClass7_0'::a
      IL_000d:  call       valuetype [System.Runtime]System.Span`1<!0> valuetype [System.Runtime]System.Span`1<int32>::op_Implicit(!0[])
      IL_0012:  call       int32 Program::InstantiatingStub1<string>(int32,
                                                                     int32,
                                                                     !!0,
                                                                     valuetype [System.Runtime]System.Span`1<int32>)
      IL_0017:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__29'

  } // end of class '<>c__DisplayClass7_0'

  .class auto ansi sealed nested private beforefieldinit '<>c__DisplayClass7_1`1'<T>
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public class [System.Runtime]System.Func`3<int32,int32,!T> f
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  ret
    } // end of method '<>c__DisplayClass7_1`1'::.ctor

    .method assembly hidebysig instance !T 
            '<Main>b__31'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       18 (0x12)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class [System.Runtime]System.Func`3<int32,int32,!0> class Program/'<>c__DisplayClass7_1`1'<!T>::f
      IL_0006:  ldc.i4     0xf4240
      IL_000b:  ldc.i4.0
      IL_000c:  callvirt   instance !2 class [System.Runtime]System.Func`3<int32,int32,!T>::Invoke(!0,
                                                                                                   !1)
      IL_0011:  ret
    } // end of method '<>c__DisplayClass7_1`1'::'<Main>b__31'

  } // end of class '<>c__DisplayClass7_1`1'

  .class abstract auto ansi sealed nested private beforefieldinit '<>O'
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`3<int32,int32,int32> '<0>__CalcStatic'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`3<int32,int32,uint8> '<1>__CalcStaticSmall'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`3<int32,int32,valuetype S32> '<2>__CalcStaticRetbuf'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`3<int32,int32,int64> '<3>__CalcStaticLong'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`3<int32,int32,valuetype S16> '<4>__CalcStaticS16'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`3<int32,int32,int32> '<5>__CalcStaticCalli'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`3<int32,int32,valuetype S32> '<6>__CalcStaticCalliRetbuf'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
  } // end of class '<>O'

  .class auto ansi serializable sealed nested private beforefieldinit '<>c'
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public static initonly class Program/'<>c' '<>9'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`3<int32,int32,int32> '<>9__7_2'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_3'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_4'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_5'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`1<int32> '<>9__7_6'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`1<int32> '<>9__7_8'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`1<int32> '<>9__7_9'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_10'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_11'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_12'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_13'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_14'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_15'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_16'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_17'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_18'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .field public static class [System.Runtime]System.Func`1<int32> '<>9__7_30'
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .method private hidebysig specialname rtspecialname static 
            void  .cctor() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  8
      IL_0000:  newobj     instance void Program/'<>c'::.ctor()
      IL_0005:  stsfld     class Program/'<>c' Program/'<>c'::'<>9'
      IL_000a:  ret
    } // end of method '<>c'::.cctor

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  ret
    } // end of method '<>c'::.ctor

    .method assembly hidebysig instance int32 
            '<Main>b__7_2'(int32 x,
                           int32 s) cil managed
    {
      // Code size       13 (0xd)
      .maxstack  8
      IL_0000:  ldarg.1
      IL_0001:  ldarg.2
      IL_0002:  call       void Program::CalcStaticVoid(int32,
                                                        int32)
      IL_0007:  ldsfld     int32 Program::s_result
      IL_000c:  ret
    } // end of method '<>c'::'<Main>b__7_2'

    .method assembly hidebysig instance string 
            '<Main>b__7_3'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       6 (0x6)
      .maxstack  8
      IL_0000:  call       string Program::EmptyCalli()
      IL_0005:  ret
    } // end of method '<>c'::'<Main>b__7_3'

    .method assembly hidebysig instance string 
            '<Main>b__7_4'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       6 (0x6)
      .maxstack  8
      IL_0000:  call       string Program::ValueTypeInstanceMethodCalli()
      IL_0005:  ret
    } // end of method '<>c'::'<Main>b__7_4'

    .method assembly hidebysig instance string 
            '<Main>b__7_5'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       6 (0x6)
      .maxstack  8
      IL_0000:  call       string Program::ValueTypeExplicitThisInstanceMethodCalli()
      IL_0005:  ret
    } // end of method '<>c'::'<Main>b__7_5'

    .method assembly hidebysig instance int32 
            '<Main>b__7_6'() cil managed
    {
      // Code size       27 (0x1b)
      .maxstack  2
      .locals init (valuetype InstanceValueType V_0)
      IL_0000:  ldloca.s   V_0
      IL_0002:  initobj    InstanceValueType
      IL_0008:  ldloca.s   V_0
      IL_000a:  ldc.i4     0xf4240
      IL_000f:  call       instance void InstanceValueType::CountUp(int32)
      IL_0014:  ldloc.0
      IL_0015:  ldfld      int32 InstanceValueType::Count
      IL_001a:  ret
    } // end of method '<>c'::'<Main>b__7_6'

    .method assembly hidebysig instance int32 
            '<Main>b__7_8'() cil managed
    {
      // Code size       17 (0x11)
      .maxstack  8
      IL_0000:  ldc.i4     0xf4240
      IL_0005:  ldc.i4.0
      IL_0006:  newobj     instance void HeapInt::.ctor(int32)
      IL_000b:  call       int32 Program::CountUpHeap(int32,
                                                      class HeapInt)
      IL_0010:  ret
    } // end of method '<>c'::'<Main>b__7_8'

    .method assembly hidebysig instance int32 
            '<Main>b__7_9'() cil managed
    {
      // Code size       28 (0x1c)
      .maxstack  3
      .locals init (int32[] V_0)
      IL_0000:  ldc.i4.1
      IL_0001:  newarr     [System.Runtime]System.Int32
      IL_0006:  stloc.0
      IL_0007:  ldc.i4     0xf4240
      IL_000c:  ldloc.0
      IL_000d:  ldc.i4.0
      IL_000e:  ldelema    [System.Runtime]System.Int32
      IL_0013:  call       void Program::CountUpRef(int32,
                                                    int32&)
      IL_0018:  ldloc.0
      IL_0019:  ldc.i4.0
      IL_001a:  ldelem.i4
      IL_001b:  ret
    } // end of method '<>c'::'<Main>b__7_9'

    .method assembly hidebysig instance string 
            '<Main>b__7_10'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       11 (0xb)
      .maxstack  8
      IL_0000:  ldstr      "hello"
      IL_0005:  call       string Program::GenName1Forward<string>(!!0)
      IL_000a:  ret
    } // end of method '<>c'::'<Main>b__7_10'

    .method assembly hidebysig instance string 
            '<Main>b__7_11'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       11 (0xb)
      .maxstack  8
      IL_0000:  ldstr      "hello"
      IL_0005:  call       string Program::GenName1Forward<object>(!!0)
      IL_000a:  ret
    } // end of method '<>c'::'<Main>b__7_11'

    .method assembly hidebysig instance string 
            '<Main>b__7_12'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldc.i4.5
      IL_0001:  call       string Program::GenName1Forward<int32>(!!0)
      IL_0006:  ret
    } // end of method '<>c'::'<Main>b__7_12'

    .method assembly hidebysig instance string 
            '<Main>b__7_13'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       16 (0x10)
      .maxstack  8
      IL_0000:  ldstr      "hello"
      IL_0005:  ldstr      "hello2"
      IL_000a:  call       string Program::GenName2ForwardBoth<string,object>(!!0,
                                                                              !!1)
      IL_000f:  ret
    } // end of method '<>c'::'<Main>b__7_13'

    .method assembly hidebysig instance string 
            '<Main>b__7_14'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       12 (0xc)
      .maxstack  8
      IL_0000:  ldstr      "hello"
      IL_0005:  ldc.i4.5
      IL_0006:  call       string Program::GenName2ForwardBoth<string,int32>(!!0,
                                                                             !!1)
      IL_000b:  ret
    } // end of method '<>c'::'<Main>b__7_14'

    .method assembly hidebysig instance string 
            '<Main>b__7_15'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       16 (0x10)
      .maxstack  8
      IL_0000:  ldstr      "hello"
      IL_0005:  ldstr      "hello2"
      IL_000a:  call       string Program::GenName2ForwardOne<string>(!!0,
                                                                      string)
      IL_000f:  ret
    } // end of method '<>c'::'<Main>b__7_15'

    .method assembly hidebysig instance string 
            '<Main>b__7_16'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       16 (0x10)
      .maxstack  8
      IL_0000:  ldstr      "hello"
      IL_0005:  ldstr      "hello2"
      IL_000a:  call       string Program::GenName2ForwardOne<object>(!!0,
                                                                      string)
      IL_000f:  ret
    } // end of method '<>c'::'<Main>b__7_16'

    .method assembly hidebysig instance string 
            '<Main>b__7_17'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       12 (0xc)
      .maxstack  8
      IL_0000:  ldc.i4.5
      IL_0001:  ldstr      "hello2"
      IL_0006:  call       string Program::GenName2ForwardOne<int32>(!!0,
                                                                     string)
      IL_000b:  ret
    } // end of method '<>c'::'<Main>b__7_17'

    .method assembly hidebysig instance string 
            '<Main>b__7_18'() cil managed
    {
      .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
      // Code size       16 (0x10)
      .maxstack  8
      IL_0000:  ldstr      "hello"
      IL_0005:  ldstr      "hello2"
      IL_000a:  call       string Program::GenName2ForwardNone(object,
                                                               string)
      IL_000f:  ret
    } // end of method '<>c'::'<Main>b__7_18'

    .method assembly hidebysig instance int32 
            '<Main>b__7_30'() cil managed
    {
      // Code size       6 (0x6)
      .maxstack  8
      IL_0000:  call       int32 Program::VirtCallThisHasSideEffects()
      IL_0005:  ret
    } // end of method '<>c'::'<Main>b__7_30'

  } // end of class '<>c'

  .field private static initonly native int s_calcStaticCalli
  .field private static initonly native int s_calcStaticCalliOther
  .field private static initonly native int s_calcStaticCalliRetbuf
  .field private static initonly native int s_calcStaticCalliRetbufOther
  .field private static initonly native int s_emptyCalliOther
  .field private static initonly native int s_instanceMethodOnValueType
  .field static assembly int32 s_result
  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       83 (0x53)
    .maxstack  1
    .locals init (native int V_0,
             native int V_1,
             native int V_2,
             native int V_3,
             native int V_4,
             native int V_5)
    IL_0000:  ldftn      int32 Program::CalcStaticCalli(int32,
                                                        int32)
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  stsfld     native int Program::s_calcStaticCalli
    IL_000d:  ldftn      int32 Program::CalcStaticCalliOther(int32,
                                                             valuetype S32,
                                                             int32)
    IL_0013:  stloc.1
    IL_0014:  ldloc.1
    IL_0015:  stsfld     native int Program::s_calcStaticCalliOther
    IL_001a:  ldftn      valuetype S32 Program::CalcStaticCalliRetbuf(int32,
                                                                      int32)
    IL_0020:  stloc.2
    IL_0021:  ldloc.2
    IL_0022:  stsfld     native int Program::s_calcStaticCalliRetbuf
    IL_0027:  ldftn      valuetype S32 Program::CalcStaticCalliRetbufOther(int32,
                                                                           valuetype S32,
                                                                           int32)
    IL_002d:  stloc.3
    IL_002e:  ldloc.3
    IL_002f:  stsfld     native int Program::s_calcStaticCalliRetbufOther
    IL_0034:  ldftn      string Program::EmptyCalliOther()
    IL_003a:  stloc.s    V_4
    IL_003c:  ldloc.s    V_4
    IL_003e:  stsfld     native int Program::s_emptyCalliOther
    IL_0043:  ldftn      instance string S16::InstanceMethod()
    IL_0049:  stloc.s    V_5
    IL_004b:  ldloc.s    V_5
    IL_004d:  stsfld     native int Program::s_instanceMethodOnValueType
    IL_0052:  ret
  } // end of method Program::.cctor

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
    ) 
    .entrypoint
    // Code size       1940 (0x794)
    .maxstack  4
    .locals init (class Program/'<>c__DisplayClass7_0' V_0,
             int32 V_1,
             valuetype S32 V_2,
             int32 V_3,
             class ClassImpl V_4,
             class InterfaceImpl V_5,
             int64 V_6,
             valuetype S16 V_7,
             valuetype S32 V_8,
             valuetype S16 V_9,
             valuetype S32 V_10)
    IL_0000:  newobj     instance void Program/'<>c__DisplayClass7_0'::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldc.i4     0xf4240
    IL_000b:  stloc.1
    IL_000c:  ldloca.s   V_2
    IL_000e:  initobj    S32
    IL_0014:  ldc.i4.0
    IL_0015:  stloc.3
    IL_0016:  br.s       IL_0030

    IL_0018:  ldloc.1
    IL_0019:  ldc.i4.2
    IL_001a:  rem
    IL_001b:  brtrue.s   IL_0025

    IL_001d:  ldloca.s   V_2
    IL_001f:  initobj    S32
    IL_0025:  ldloca.s   V_1
    IL_0027:  ldloca.s   V_2
    IL_0029:  ldloca.s   V_3
    IL_002b:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0030:  ldloc.1
    IL_0031:  brtrue.s   IL_0018

    IL_0033:  ldloc.0
    IL_0034:  ldc.i4.1
    IL_0035:  stfld      bool Program/'<>c__DisplayClass7_0'::result
    IL_003a:  newobj     instance void ClassImpl::.ctor()
    IL_003f:  stloc.s    V_4
    IL_0041:  ldloc.s    V_4
    IL_0043:  ldloc.s    V_4
    IL_0045:  callvirt   instance void ClassImpl::set_Other(class BaseClass)
    IL_004a:  newobj     instance void InterfaceImpl::.ctor()
    IL_004f:  stloc.s    V_5
    IL_0051:  ldloc.s    V_5
    IL_0053:  ldloc.s    V_5
    IL_0055:  callvirt   instance void InterfaceImpl::set_Other(class IInterface)
    IL_005a:  ldloc.0
    IL_005b:  newobj     instance void class GenInstance`2<string,int32>::.ctor()
    IL_0060:  stfld      class GenInstance`2<string,int32> Program/'<>c__DisplayClass7_0'::g
    IL_0065:  ldloc.0
    IL_0066:  newobj     instance void class GenInterfaceImpl`2<string,int32>::.ctor()
    IL_006b:  stfld      class IGenInterface`2<string,int32> Program/'<>c__DisplayClass7_0'::ig
    IL_0070:  ldloc.0
    IL_0071:  newobj     instance void class GenInterfaceImpl`2<string,object>::.ctor()
    IL_0076:  stfld      class IGenInterface`2<string,object> Program/'<>c__DisplayClass7_0'::ig2
    IL_007b:  ldloc.0
    IL_007c:  newobj     instance void class GenAbstractImpl`1<string>::.ctor()
    IL_0081:  stfld      class GenAbstractImpl`1<string> Program/'<>c__DisplayClass7_0'::ga1
    IL_0086:  ldloc.0
    IL_0087:  newobj     instance void class GenAbstractImpl`1<int32>::.ctor()
    IL_008c:  stfld      class GenAbstractImpl`1<int32> Program/'<>c__DisplayClass7_0'::ga2
    IL_0091:  ldloc.3
    IL_0092:  conv.u8
    IL_0093:  ldc.i4.s   32
    IL_0095:  shl
    IL_0096:  ldloc.3
    IL_0097:  conv.u8
    IL_0098:  or
    IL_0099:  stloc.s    V_6
    IL_009b:  ldloca.s   V_9
    IL_009d:  initobj    S16
    IL_00a3:  ldloca.s   V_9
    IL_00a5:  ldloc.3
    IL_00a6:  conv.i8
    IL_00a7:  stfld      int64 S16::A
    IL_00ac:  ldloca.s   V_9
    IL_00ae:  ldloc.3
    IL_00af:  conv.i8
    IL_00b0:  stfld      int64 S16::B
    IL_00b5:  ldloc.s    V_9
    IL_00b7:  stloc.s    V_7
    IL_00b9:  ldloca.s   V_10
    IL_00bb:  initobj    S32
    IL_00c1:  ldloca.s   V_10
    IL_00c3:  ldloc.3
    IL_00c4:  conv.i8
    IL_00c5:  stfld      int64 S32::A
    IL_00ca:  ldloca.s   V_10
    IL_00cc:  ldloc.3
    IL_00cd:  conv.i8
    IL_00ce:  stfld      int64 S32::B
    IL_00d3:  ldloca.s   V_10
    IL_00d5:  ldloc.3
    IL_00d6:  conv.i8
    IL_00d7:  stfld      int64 S32::C
    IL_00dc:  ldloca.s   V_10
    IL_00de:  ldloc.3
    IL_00df:  conv.i8
    IL_00e0:  stfld      int64 S32::D
    IL_00e5:  ldloc.s    V_10
    IL_00e7:  stloc.s    V_8
    IL_00e9:  ldloc.0
    IL_00ea:  ldc.i4.s   10
    IL_00ec:  stfld      int32 Program/'<>c__DisplayClass7_0'::ten
    IL_00f1:  ldloc.0
    IL_00f2:  ldsfld     class [System.Runtime]System.Func`3<int32,int32,int32> Program/'<>O'::'<0>__CalcStatic'
    IL_00f7:  dup
    IL_00f8:  brtrue.s   IL_010d

    IL_00fa:  pop
    IL_00fb:  ldnull
    IL_00fc:  ldftn      int32 Program::CalcStatic(int32,
                                                   int32)
    IL_0102:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,int32>::.ctor(object,
                                                                                                     native int)
    IL_0107:  dup
    IL_0108:  stsfld     class [System.Runtime]System.Func`3<int32,int32,int32> Program/'<>O'::'<0>__CalcStatic'
    IL_010d:  ldloc.3
    IL_010e:  ldstr      "Static non-generic"
    IL_0113:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<int32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                    !!0,
                                                                                                    string)
    IL_0118:  ldloc.0
    IL_0119:  ldsfld     class [System.Runtime]System.Func`3<int32,int32,uint8> Program/'<>O'::'<1>__CalcStaticSmall'
    IL_011e:  dup
    IL_011f:  brtrue.s   IL_0134

    IL_0121:  pop
    IL_0122:  ldnull
    IL_0123:  ldftn      uint8 Program::CalcStaticSmall(int32,
                                                        int32)
    IL_0129:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,uint8>::.ctor(object,
                                                                                                     native int)
    IL_012e:  dup
    IL_012f:  stsfld     class [System.Runtime]System.Func`3<int32,int32,uint8> Program/'<>O'::'<1>__CalcStaticSmall'
    IL_0134:  ldloc.3
    IL_0135:  conv.u1
    IL_0136:  ldstr      "Static non-generic small"
    IL_013b:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<uint8>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                    !!0,
                                                                                                    string)
    IL_0140:  ldloc.0
    IL_0141:  ldsfld     class [System.Runtime]System.Func`3<int32,int32,valuetype S32> Program/'<>O'::'<2>__CalcStaticRetbuf'
    IL_0146:  dup
    IL_0147:  brtrue.s   IL_015c

    IL_0149:  pop
    IL_014a:  ldnull
    IL_014b:  ldftn      valuetype S32 Program::CalcStaticRetbuf(int32,
                                                                 int32)
    IL_0151:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,valuetype S32>::.ctor(object,
                                                                                                             native int)
    IL_0156:  dup
    IL_0157:  stsfld     class [System.Runtime]System.Func`3<int32,int32,valuetype S32> Program/'<>O'::'<2>__CalcStaticRetbuf'
    IL_015c:  ldloc.s    V_8
    IL_015e:  ldstr      "Static non-generic retbuf"
    IL_0163:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<valuetype S32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                            !!0,
                                                                                                            string)
    IL_0168:  ldloc.0
    IL_0169:  ldsfld     class [System.Runtime]System.Func`3<int32,int32,int64> Program/'<>O'::'<3>__CalcStaticLong'
    IL_016e:  dup
    IL_016f:  brtrue.s   IL_0184

    IL_0171:  pop
    IL_0172:  ldnull
    IL_0173:  ldftn      int64 Program::CalcStaticLong(int32,
                                                       int32)
    IL_0179:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,int64>::.ctor(object,
                                                                                                     native int)
    IL_017e:  dup
    IL_017f:  stsfld     class [System.Runtime]System.Func`3<int32,int32,int64> Program/'<>O'::'<3>__CalcStaticLong'
    IL_0184:  ldloc.s    V_6
    IL_0186:  ldstr      "Static non-generic long"
    IL_018b:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<int64>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                    !!0,
                                                                                                    string)
    IL_0190:  ldloc.0
    IL_0191:  ldsfld     class [System.Runtime]System.Func`3<int32,int32,valuetype S16> Program/'<>O'::'<4>__CalcStaticS16'
    IL_0196:  dup
    IL_0197:  brtrue.s   IL_01ac

    IL_0199:  pop
    IL_019a:  ldnull
    IL_019b:  ldftn      valuetype S16 Program::CalcStaticS16(int32,
                                                              int32)
    IL_01a1:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,valuetype S16>::.ctor(object,
                                                                                                             native int)
    IL_01a6:  dup
    IL_01a7:  stsfld     class [System.Runtime]System.Func`3<int32,int32,valuetype S16> Program/'<>O'::'<4>__CalcStaticS16'
    IL_01ac:  ldloc.s    V_7
    IL_01ae:  ldstr      "Static non-generic S16"
    IL_01b3:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<valuetype S16>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                            !!0,
                                                                                                            string)
    IL_01b8:  ldloc.0
    IL_01b9:  ldsfld     class [System.Runtime]System.Func`3<int32,int32,int32> Program/'<>c'::'<>9__7_2'
    IL_01be:  dup
    IL_01bf:  brtrue.s   IL_01d8

    IL_01c1:  pop
    IL_01c2:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_01c7:  ldftn      instance int32 Program/'<>c'::'<Main>b__7_2'(int32,
                                                                      int32)
    IL_01cd:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,int32>::.ctor(object,
                                                                                                     native int)
    IL_01d2:  dup
    IL_01d3:  stsfld     class [System.Runtime]System.Func`3<int32,int32,int32> Program/'<>c'::'<>9__7_2'
    IL_01d8:  ldloc.3
    IL_01d9:  ldstr      "Static void"
    IL_01de:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<int32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                    !!0,
                                                                                                    string)
    IL_01e3:  ldloc.0
    IL_01e4:  newobj     instance void Instance::.ctor()
    IL_01e9:  ldftn      instance int32 Instance::CalcInstance(int32,
                                                               int32)
    IL_01ef:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,int32>::.ctor(object,
                                                                                                     native int)
    IL_01f4:  ldloc.3
    IL_01f5:  ldstr      "Instance non-generic"
    IL_01fa:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<int32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                    !!0,
                                                                                                    string)
    IL_01ff:  ldloc.0
    IL_0200:  newobj     instance void Instance::.ctor()
    IL_0205:  ldftn      instance valuetype S32 Instance::CalcInstanceRetbuf(int32,
                                                                             int32)
    IL_020b:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,valuetype S32>::.ctor(object,
                                                                                                             native int)
    IL_0210:  ldloc.s    V_8
    IL_0212:  ldstr      "Instance non-generic retbuf"
    IL_0217:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<valuetype S32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                            !!0,
                                                                                                            string)
    IL_021c:  ldloc.0
    IL_021d:  ldloc.s    V_4
    IL_021f:  dup
    IL_0220:  ldvirtftn  instance int32 BaseClass::CalcAbstract(int32,
                                                                int32)
    IL_0226:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,int32>::.ctor(object,
                                                                                                     native int)
    IL_022b:  ldloc.3
    IL_022c:  ldstr      "Abstract class non-generic"
    IL_0231:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<int32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                    !!0,
                                                                                                    string)
    IL_0236:  ldloc.0
    IL_0237:  ldloc.s    V_4
    IL_0239:  dup
    IL_023a:  ldvirtftn  instance valuetype S32 BaseClass::CalcAbstractRetbuf(int32,
                                                                              int32)
    IL_0240:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,valuetype S32>::.ctor(object,
                                                                                                             native int)
    IL_0245:  ldloc.s    V_8
    IL_0247:  ldstr      "Abstract class non-generic retbuf"
    IL_024c:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<valuetype S32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                            !!0,
                                                                                                            string)
    IL_0251:  ldloc.0
    IL_0252:  ldloc.s    V_5
    IL_0254:  dup
    IL_0255:  ldvirtftn  instance int32 InterfaceImpl::CalcInterface(int32,
                                                                     int32)
    IL_025b:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,int32>::.ctor(object,
                                                                                                     native int)
    IL_0260:  ldloc.3
    IL_0261:  ldstr      "Interface non-generic"
    IL_0266:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<int32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                    !!0,
                                                                                                    string)
    IL_026b:  ldloc.0
    IL_026c:  ldloc.s    V_5
    IL_026e:  dup
    IL_026f:  ldvirtftn  instance valuetype S32 InterfaceImpl::CalcInterfaceRetbuf(int32,
                                                                                   int32)
    IL_0275:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,valuetype S32>::.ctor(object,
                                                                                                             native int)
    IL_027a:  ldloc.s    V_8
    IL_027c:  ldstr      "Interface non-generic retbuf"
    IL_0281:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<valuetype S32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                            !!0,
                                                                                                            string)
    IL_0286:  ldloc.0
    IL_0287:  ldsfld     class [System.Runtime]System.Func`3<int32,int32,int32> Program/'<>O'::'<5>__CalcStaticCalli'
    IL_028c:  dup
    IL_028d:  brtrue.s   IL_02a2

    IL_028f:  pop
    IL_0290:  ldnull
    IL_0291:  ldftn      int32 Program::CalcStaticCalli(int32,
                                                        int32)
    IL_0297:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,int32>::.ctor(object,
                                                                                                     native int)
    IL_029c:  dup
    IL_029d:  stsfld     class [System.Runtime]System.Func`3<int32,int32,int32> Program/'<>O'::'<5>__CalcStaticCalli'
    IL_02a2:  ldloc.3
    IL_02a3:  ldstr      "Static calli"
    IL_02a8:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<int32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                    !!0,
                                                                                                    string)
    IL_02ad:  ldloc.0
    IL_02ae:  ldsfld     class [System.Runtime]System.Func`3<int32,int32,valuetype S32> Program/'<>O'::'<6>__CalcStaticCalliRetbuf'
    IL_02b3:  dup
    IL_02b4:  brtrue.s   IL_02c9

    IL_02b6:  pop
    IL_02b7:  ldnull
    IL_02b8:  ldftn      valuetype S32 Program::CalcStaticCalliRetbuf(int32,
                                                                      int32)
    IL_02be:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,valuetype S32>::.ctor(object,
                                                                                                             native int)
    IL_02c3:  dup
    IL_02c4:  stsfld     class [System.Runtime]System.Func`3<int32,int32,valuetype S32> Program/'<>O'::'<6>__CalcStaticCalliRetbuf'
    IL_02c9:  ldloc.s    V_8
    IL_02cb:  ldstr      "Static calli retbuf"
    IL_02d0:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<valuetype S32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                            !!0,
                                                                                                            string)
    IL_02d5:  ldloc.0
    IL_02d6:  newobj     instance void Instance::.ctor()
    IL_02db:  ldftn      instance int32 Instance::CalcInstanceCalli(int32,
                                                                    int32)
    IL_02e1:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,int32>::.ctor(object,
                                                                                                     native int)
    IL_02e6:  ldloc.3
    IL_02e7:  ldstr      "Instance calli"
    IL_02ec:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<int32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                    !!0,
                                                                                                    string)
    IL_02f1:  ldloc.0
    IL_02f2:  newobj     instance void Instance::.ctor()
    IL_02f7:  ldftn      instance valuetype S32 Instance::CalcInstanceCalliRetbuf(int32,
                                                                                  int32)
    IL_02fd:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,valuetype S32>::.ctor(object,
                                                                                                             native int)
    IL_0302:  ldloc.s    V_8
    IL_0304:  ldstr      "Instance calli retbuf"
    IL_0309:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<valuetype S32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                            !!0,
                                                                                                            string)
    IL_030e:  ldloc.0
    IL_030f:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_3'
    IL_0314:  dup
    IL_0315:  brtrue.s   IL_032e

    IL_0317:  pop
    IL_0318:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_031d:  ldftn      instance string Program/'<>c'::'<Main>b__7_3'()
    IL_0323:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_0328:  dup
    IL_0329:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_3'
    IL_032e:  ldstr      "Empty calli"
    IL_0333:  ldstr      "Static calli without args"
    IL_0338:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_033d:  ldloc.0
    IL_033e:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_4'
    IL_0343:  dup
    IL_0344:  brtrue.s   IL_035d

    IL_0346:  pop
    IL_0347:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_034c:  ldftn      instance string Program/'<>c'::'<Main>b__7_4'()
    IL_0352:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_0357:  dup
    IL_0358:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_4'
    IL_035d:  ldstr      "Instance method"
    IL_0362:  ldstr      "calli to an instance method on a value type"
    IL_0367:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_036c:  ldloc.0
    IL_036d:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_5'
    IL_0372:  dup
    IL_0373:  brtrue.s   IL_038c

    IL_0375:  pop
    IL_0376:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_037b:  ldftn      instance string Program/'<>c'::'<Main>b__7_5'()
    IL_0381:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_0386:  dup
    IL_0387:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_5'
    IL_038c:  ldstr      "Instance method"
    IL_0391:  ldstr      "calli to an instance method on a value type with e"
    + "xplicit this"
    IL_0396:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_039b:  ldloc.0
    IL_039c:  ldsfld     class [System.Runtime]System.Func`1<int32> Program/'<>c'::'<>9__7_6'
    IL_03a1:  dup
    IL_03a2:  brtrue.s   IL_03bb

    IL_03a4:  pop
    IL_03a5:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_03aa:  ldftn      instance int32 Program/'<>c'::'<Main>b__7_6'()
    IL_03b0:  newobj     instance void class [System.Runtime]System.Func`1<int32>::.ctor(object,
                                                                                         native int)
    IL_03b5:  dup
    IL_03b6:  stsfld     class [System.Runtime]System.Func`1<int32> Program/'<>c'::'<>9__7_6'
    IL_03bb:  ldc.i4     0xf4240
    IL_03c0:  ldstr      "Value type instance call"
    IL_03c5:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<int32>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                !!0,
                                                                                                string)
    IL_03ca:  ldloc.0
    IL_03cb:  ldloc.0
    IL_03cc:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__7'()
    IL_03d2:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_03d7:  ldstr      "2 3 4 5 6 7 8 9 10"
    IL_03dc:  ldstr      "Instance with GC"
    IL_03e1:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_03e6:  ldloc.0
    IL_03e7:  ldsfld     class [System.Runtime]System.Func`1<int32> Program/'<>c'::'<>9__7_8'
    IL_03ec:  dup
    IL_03ed:  brtrue.s   IL_0406

    IL_03ef:  pop
    IL_03f0:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_03f5:  ldftn      instance int32 Program/'<>c'::'<Main>b__7_8'()
    IL_03fb:  newobj     instance void class [System.Runtime]System.Func`1<int32>::.ctor(object,
                                                                                         native int)
    IL_0400:  dup
    IL_0401:  stsfld     class [System.Runtime]System.Func`1<int32> Program/'<>c'::'<>9__7_8'
    IL_0406:  ldc.i4     0xf4240
    IL_040b:  ldstr      "Count up with heap int"
    IL_0410:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<int32>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                !!0,
                                                                                                string)
    IL_0415:  ldloc.0
    IL_0416:  ldsfld     class [System.Runtime]System.Func`1<int32> Program/'<>c'::'<>9__7_9'
    IL_041b:  dup
    IL_041c:  brtrue.s   IL_0435

    IL_041e:  pop
    IL_041f:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_0424:  ldftn      instance int32 Program/'<>c'::'<Main>b__7_9'()
    IL_042a:  newobj     instance void class [System.Runtime]System.Func`1<int32>::.ctor(object,
                                                                                         native int)
    IL_042f:  dup
    IL_0430:  stsfld     class [System.Runtime]System.Func`1<int32> Program/'<>c'::'<>9__7_9'
    IL_0435:  ldc.i4     0xf4240
    IL_043a:  ldstr      "Count up with byref to heap"
    IL_043f:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<int32>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                !!0,
                                                                                                string)
    IL_0444:  ldloc.0
    IL_0445:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_10'
    IL_044a:  dup
    IL_044b:  brtrue.s   IL_0464

    IL_044d:  pop
    IL_044e:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_0453:  ldftn      instance string Program/'<>c'::'<Main>b__7_10'()
    IL_0459:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_045e:  dup
    IL_045f:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_10'
    IL_0464:  ldstr      "System.String hello"
    IL_0469:  ldstr      "Static generic string"
    IL_046e:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_0473:  ldloc.0
    IL_0474:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_11'
    IL_0479:  dup
    IL_047a:  brtrue.s   IL_0493

    IL_047c:  pop
    IL_047d:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_0482:  ldftn      instance string Program/'<>c'::'<Main>b__7_11'()
    IL_0488:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_048d:  dup
    IL_048e:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_11'
    IL_0493:  ldstr      "System.Object hello"
    IL_0498:  ldstr      "Static generic object"
    IL_049d:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_04a2:  ldloc.0
    IL_04a3:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_12'
    IL_04a8:  dup
    IL_04a9:  brtrue.s   IL_04c2

    IL_04ab:  pop
    IL_04ac:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_04b1:  ldftn      instance string Program/'<>c'::'<Main>b__7_12'()
    IL_04b7:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_04bc:  dup
    IL_04bd:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_12'
    IL_04c2:  ldstr      "System.Int32 5"
    IL_04c7:  ldstr      "Static generic int"
    IL_04cc:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_04d1:  ldloc.0
    IL_04d2:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_13'
    IL_04d7:  dup
    IL_04d8:  brtrue.s   IL_04f1

    IL_04da:  pop
    IL_04db:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_04e0:  ldftn      instance string Program/'<>c'::'<Main>b__7_13'()
    IL_04e6:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_04eb:  dup
    IL_04ec:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_13'
    IL_04f1:  ldstr      "System.String System.Object hello hello2"
    IL_04f6:  ldstr      "Static generic 2 string object"
    IL_04fb:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_0500:  ldloc.0
    IL_0501:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_14'
    IL_0506:  dup
    IL_0507:  brtrue.s   IL_0520

    IL_0509:  pop
    IL_050a:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_050f:  ldftn      instance string Program/'<>c'::'<Main>b__7_14'()
    IL_0515:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_051a:  dup
    IL_051b:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_14'
    IL_0520:  ldstr      "System.String System.Int32 hello 5"
    IL_0525:  ldstr      "Static generic 2 string int"
    IL_052a:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_052f:  ldloc.0
    IL_0530:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_15'
    IL_0535:  dup
    IL_0536:  brtrue.s   IL_054f

    IL_0538:  pop
    IL_0539:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_053e:  ldftn      instance string Program/'<>c'::'<Main>b__7_15'()
    IL_0544:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_0549:  dup
    IL_054a:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_15'
    IL_054f:  ldstr      "System.String System.String hello hello2"
    IL_0554:  ldstr      "Static generic 1 string"
    IL_0559:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_055e:  ldloc.0
    IL_055f:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_16'
    IL_0564:  dup
    IL_0565:  brtrue.s   IL_057e

    IL_0567:  pop
    IL_0568:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_056d:  ldftn      instance string Program/'<>c'::'<Main>b__7_16'()
    IL_0573:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_0578:  dup
    IL_0579:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_16'
    IL_057e:  ldstr      "System.Object System.String hello hello2"
    IL_0583:  ldstr      "Static generic 1 object"
    IL_0588:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_058d:  ldloc.0
    IL_058e:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_17'
    IL_0593:  dup
    IL_0594:  brtrue.s   IL_05ad

    IL_0596:  pop
    IL_0597:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_059c:  ldftn      instance string Program/'<>c'::'<Main>b__7_17'()
    IL_05a2:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_05a7:  dup
    IL_05a8:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_17'
    IL_05ad:  ldstr      "System.Int32 System.String 5 hello2"
    IL_05b2:  ldstr      "Static generic 1 int"
    IL_05b7:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_05bc:  ldloc.0
    IL_05bd:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_18'
    IL_05c2:  dup
    IL_05c3:  brtrue.s   IL_05dc

    IL_05c5:  pop
    IL_05c6:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_05cb:  ldftn      instance string Program/'<>c'::'<Main>b__7_18'()
    IL_05d1:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_05d6:  dup
    IL_05d7:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_18'
    IL_05dc:  ldstr      "System.Object System.String hello hello2"
    IL_05e1:  ldstr      "Static generic 0"
    IL_05e6:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_05eb:  ldloc.0
    IL_05ec:  ldloc.0
    IL_05ed:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__19'()
    IL_05f3:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_05f8:  ldstr      "System.String System.Int32 System.Object System.St"
    + "ring a 5 b c"
    IL_05fd:  ldstr      "Instance generic 4"
    IL_0602:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_0607:  ldloc.0
    IL_0608:  ldloc.0
    IL_0609:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__20'()
    IL_060f:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_0614:  ldstr      "System.String System.Int32 System.Object System.St"
    + "ring a 5 b c"
    IL_0619:  ldstr      "Virtual instance generic 4"
    IL_061e:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_0623:  ldloc.0
    IL_0624:  ldloc.0
    IL_0625:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__21'()
    IL_062b:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_0630:  ldstr      "System.String System.Int32 System.String System.Ob"
    + "ject a 5 c d"
    IL_0635:  ldstr      "Interface generic 4"
    IL_063a:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_063f:  ldloc.0
    IL_0640:  ldloc.0
    IL_0641:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__22'()
    IL_0647:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_064c:  ldstr      "System.String System.Int32 a 5"
    IL_0651:  ldstr      "Interface generic forward G"
    IL_0656:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_065b:  ldloc.0
    IL_065c:  ldloc.0
    IL_065d:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__23'()
    IL_0663:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_0668:  ldstr      "System.String System.Object System.Int32 System.Ob"
    + "ject a b 5 d"
    IL_066d:  ldstr      "Interface generic 0"
    IL_0672:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_0677:  ldloc.0
    IL_0678:  ldloc.0
    IL_0679:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__24'()
    IL_067f:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_0684:  ldstr      "System.String System.Object a b"
    IL_0689:  ldstr      "Interface generic without generics on method"
    IL_068e:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_0693:  ldloc.0
    IL_0694:  ldloc.0
    IL_0695:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__25'()
    IL_069b:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_06a0:  ldstr      "System.String System.Object"
    IL_06a5:  ldstr      "Abstract generic with generic on method 1"
    IL_06aa:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_06af:  ldloc.0
    IL_06b0:  ldloc.0
    IL_06b1:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__26'()
    IL_06b7:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_06bc:  ldstr      "System.Int32 System.Object"
    IL_06c1:  ldstr      "Abstract generic with generic on method 2"
    IL_06c6:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_06cb:  ldloc.0
    IL_06cc:  ldloc.0
    IL_06cd:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__27'()
    IL_06d3:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_06d8:  ldstr      "System.String"
    IL_06dd:  ldstr      "Abstract generic without generic on method 1"
    IL_06e2:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_06e7:  ldloc.0
    IL_06e8:  ldloc.0
    IL_06e9:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__28'()
    IL_06ef:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_06f4:  ldstr      "System.Int32"
    IL_06f9:  ldstr      "Abstract generic without generic on method 2"
    IL_06fe:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_0703:  ldloc.0
    IL_0704:  ldc.i4     0xf4240
    IL_0709:  newarr     [System.Runtime]System.Int32
    IL_070e:  stfld      int32[] Program/'<>c__DisplayClass7_0'::a
    IL_0713:  ldloc.0
    IL_0714:  ldfld      int32[] Program/'<>c__DisplayClass7_0'::a
    IL_0719:  ldc.i4.s   99
    IL_071b:  ldc.i4.1
    IL_071c:  stelem.i4
    IL_071d:  ldloc.0
    IL_071e:  ldloc.0
    IL_071f:  ldftn      instance int32 Program/'<>c__DisplayClass7_0'::'<Main>b__29'()
    IL_0725:  newobj     instance void class [System.Runtime]System.Func`1<int32>::.ctor(object,
                                                                                         native int)
    IL_072a:  ldloc.0
    IL_072b:  ldfld      int32[] Program/'<>c__DisplayClass7_0'::a
    IL_0730:  ldlen
    IL_0731:  conv.i4
    IL_0732:  ldc.i4.1
    IL_0733:  add
    IL_0734:  ldstr      "Instantiating stub direct"
    IL_0739:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<int32>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                !!0,
                                                                                                string)
    IL_073e:  ldloc.0
    IL_073f:  ldsfld     class [System.Runtime]System.Func`1<int32> Program/'<>c'::'<>9__7_30'
    IL_0744:  dup
    IL_0745:  brtrue.s   IL_075e

    IL_0747:  pop
    IL_0748:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_074d:  ldftn      instance int32 Program/'<>c'::'<Main>b__7_30'()
    IL_0753:  newobj     instance void class [System.Runtime]System.Func`1<int32>::.ctor(object,
                                                                                         native int)
    IL_0758:  dup
    IL_0759:  stsfld     class [System.Runtime]System.Func`1<int32> Program/'<>c'::'<>9__7_30'
    IL_075e:  ldc.i4.1
    IL_075f:  ldstr      "Virtual call where computing \"this\" has side effects"
    IL_0764:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<int32>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                !!0,
                                                                                                string)
    IL_0769:  ldloc.0
    IL_076a:  ldfld      bool Program/'<>c__DisplayClass7_0'::result
    IL_076f:  brfalse.s  IL_077d

    IL_0771:  ldstr      "All tailcall-via-help succeeded"
    IL_0776:  call       void [System.Console]System.Console::WriteLine(string)
    IL_077b:  br.s       IL_0787

    IL_077d:  ldstr      "One or more failures in tailcall-via-help test"
    IL_0782:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0787:  ldloc.0
    IL_0788:  ldfld      bool Program/'<>c__DisplayClass7_0'::result
    IL_078d:  brtrue.s   IL_0791

    IL_078f:  ldc.i4.1
    IL_0790:  ret

    IL_0791:  ldc.i4.s   100
    IL_0793:  ret
  } // end of method Program::Main

  .method public hidebysig static void  Calc(int32& x,
                                             valuetype S32& s,
                                             int32& acc) cil managed
  {
    // Code size       166 (0xa6)
    .maxstack  5
    IL_0000:  ldarg.0
    IL_0001:  ldind.i4
    IL_0002:  ldc.i4.2
    IL_0003:  rem
    IL_0004:  brtrue.s   IL_003d

    IL_0006:  ldarg.2
    IL_0007:  ldarg.2
    IL_0008:  ldind.i4
    IL_0009:  ldarg.0
    IL_000a:  ldind.i4
    IL_000b:  ldc.i4.3
    IL_000c:  mul
    IL_000d:  conv.i8
    IL_000e:  ldarg.1
    IL_000f:  ldfld      int64 S32::A
    IL_0014:  ldc.i4.7
    IL_0015:  conv.i8
    IL_0016:  mul
    IL_0017:  add
    IL_0018:  ldarg.1
    IL_0019:  ldfld      int64 S32::B
    IL_001e:  ldc.i4.s   9
    IL_0020:  conv.i8
    IL_0021:  mul
    IL_0022:  add
    IL_0023:  ldarg.1
    IL_0024:  ldfld      int64 S32::C
    IL_0029:  ldc.i4.s   -3
    IL_002b:  conv.i8
    IL_002c:  mul
    IL_002d:  add
    IL_002e:  ldarg.1
    IL_002f:  ldfld      int64 S32::D
    IL_0034:  ldc.i4.4
    IL_0035:  conv.i8
    IL_0036:  mul
    IL_0037:  add
    IL_0038:  conv.i4
    IL_0039:  add
    IL_003a:  stind.i4
    IL_003b:  br.s       IL_0070

    IL_003d:  ldarg.2
    IL_003e:  ldarg.2
    IL_003f:  ldind.i4
    IL_0040:  ldarg.0
    IL_0041:  ldind.i4
    IL_0042:  conv.i8
    IL_0043:  ldarg.1
    IL_0044:  ldfld      int64 S32::A
    IL_0049:  ldc.i4.s   9
    IL_004b:  conv.i8
    IL_004c:  mul
    IL_004d:  add
    IL_004e:  ldarg.1
    IL_004f:  ldfld      int64 S32::B
    IL_0054:  ldc.i4.3
    IL_0055:  conv.i8
    IL_0056:  mul
    IL_0057:  add
    IL_0058:  ldarg.1
    IL_0059:  ldfld      int64 S32::C
    IL_005e:  ldc.i4.s   -4
    IL_0060:  conv.i8
    IL_0061:  mul
    IL_0062:  add
    IL_0063:  ldarg.1
    IL_0064:  ldfld      int64 S32::D
    IL_0069:  ldc.i4.5
    IL_006a:  conv.i8
    IL_006b:  mul
    IL_006c:  add
    IL_006d:  conv.i4
    IL_006e:  add
    IL_006f:  stind.i4
    IL_0070:  ldarg.0
    IL_0071:  ldarg.0
    IL_0072:  ldind.i4
    IL_0073:  ldc.i4.1
    IL_0074:  sub
    IL_0075:  stind.i4
    IL_0076:  ldarg.1
    IL_0077:  ldc.i4.s   11
    IL_0079:  ldarg.0
    IL_007a:  ldind.i4
    IL_007b:  mul
    IL_007c:  conv.i8
    IL_007d:  stfld      int64 S32::A
    IL_0082:  ldarg.1
    IL_0083:  ldc.i4.s   14
    IL_0085:  ldarg.0
    IL_0086:  ldind.i4
    IL_0087:  mul
    IL_0088:  conv.i8
    IL_0089:  stfld      int64 S32::B
    IL_008e:  ldarg.1
    IL_008f:  ldc.i4.s   -14
    IL_0091:  ldarg.0
    IL_0092:  ldind.i4
    IL_0093:  mul
    IL_0094:  conv.i8
    IL_0095:  stfld      int64 S32::C
    IL_009a:  ldarg.1
    IL_009b:  ldc.i4.3
    IL_009c:  ldarg.0
    IL_009d:  ldind.i4
    IL_009e:  mul
    IL_009f:  conv.i8
    IL_00a0:  stfld      int64 S32::D
    IL_00a5:  ret
  } // end of method Program::Calc

  .method private hidebysig static int32 
          CalcStatic(int32 x,
                     int32 acc) cil managed noinlining
  {
    // Code size       35 (0x23)
    .maxstack  3
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0005

    IL_0003:  ldarg.1
    IL_0004:  ret

    IL_0005:  ldloca.s   V_0
    IL_0007:  initobj    S32
    IL_000d:  ldarga.s   x
    IL_000f:  ldloca.s   V_0
    IL_0011:  ldarga.s   acc
    IL_0013:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0018:  ldarg.0
    IL_0019:  ldloc.0
    IL_001a:  ldarg.1
    IL_001b:  tail.
    IL_001d:  call       int32 Program::CalcStaticOther(int32,
                                                        valuetype S32,
                                                        int32)
    IL_0022:  ret
  } // end of method Program::CalcStatic

  .method private hidebysig static int32 
          CalcStaticOther(int32 x,
                          valuetype S32 large,
                          int32 acc) cil managed noinlining
  {
    // Code size       21 (0x15)
    .maxstack  3
    IL_0000:  ldarga.s   x
    IL_0002:  ldarga.s   large
    IL_0004:  ldarga.s   acc
    IL_0006:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000b:  ldarg.0
    IL_000c:  ldarg.2
    IL_000d:  tail.
    IL_000f:  call       int32 Program::CalcStatic(int32,
                                                   int32)
    IL_0014:  ret
  } // end of method Program::CalcStaticOther

  .method private hidebysig static uint8 
          CalcStaticSmall(int32 x,
                          int32 acc) cil managed noinlining
  {
    // Code size       36 (0x24)
    .maxstack  3
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0006

    IL_0003:  ldarg.1
    IL_0004:  conv.u1
    IL_0005:  ret

    IL_0006:  ldloca.s   V_0
    IL_0008:  initobj    S32
    IL_000e:  ldarga.s   x
    IL_0010:  ldloca.s   V_0
    IL_0012:  ldarga.s   acc
    IL_0014:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0019:  ldarg.0
    IL_001a:  ldloc.0
    IL_001b:  ldarg.1
    IL_001c:  tail.
    IL_001e:  call       uint8 Program::CalcStaticSmallOther(int32,
                                                             valuetype S32,
                                                             int32)
    IL_0023:  ret
  } // end of method Program::CalcStaticSmall

  .method private hidebysig static uint8 
          CalcStaticSmallOther(int32 x,
                               valuetype S32 large,
                               int32 acc) cil managed noinlining
  {
    // Code size       21 (0x15)
    .maxstack  3
    IL_0000:  ldarga.s   x
    IL_0002:  ldarga.s   large
    IL_0004:  ldarga.s   acc
    IL_0006:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000b:  ldarg.0
    IL_000c:  ldarg.2
    IL_000d:  tail.
    IL_000f:  call       uint8 Program::CalcStaticSmall(int32,
                                                        int32)
    IL_0014:  ret
  } // end of method Program::CalcStaticSmallOther

  .method private hidebysig static valuetype S32 
          CalcStaticRetbuf(int32 x,
                           int32 acc) cil managed noinlining
  {
    // Code size       79 (0x4f)
    .maxstack  3
    .locals init (valuetype S32 V_0,
             valuetype S32 V_1)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0031

    IL_0003:  ldloca.s   V_1
    IL_0005:  initobj    S32
    IL_000b:  ldloca.s   V_1
    IL_000d:  ldarg.1
    IL_000e:  conv.i8
    IL_000f:  stfld      int64 S32::A
    IL_0014:  ldloca.s   V_1
    IL_0016:  ldarg.1
    IL_0017:  conv.i8
    IL_0018:  stfld      int64 S32::B
    IL_001d:  ldloca.s   V_1
    IL_001f:  ldarg.1
    IL_0020:  conv.i8
    IL_0021:  stfld      int64 S32::C
    IL_0026:  ldloca.s   V_1
    IL_0028:  ldarg.1
    IL_0029:  conv.i8
    IL_002a:  stfld      int64 S32::D
    IL_002f:  ldloc.1
    IL_0030:  ret

    IL_0031:  ldloca.s   V_0
    IL_0033:  initobj    S32
    IL_0039:  ldarga.s   x
    IL_003b:  ldloca.s   V_0
    IL_003d:  ldarga.s   acc
    IL_003f:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0044:  ldarg.0
    IL_0045:  ldloc.0
    IL_0046:  ldarg.1
    IL_0047:  tail.
    IL_0049:  call       valuetype S32 Program::CalcStaticRetbufOther(int32,
                                                                      valuetype S32,
                                                                      int32)
    IL_004e:  ret
  } // end of method Program::CalcStaticRetbuf

  .method private hidebysig static valuetype S32 
          CalcStaticRetbufOther(int32 x,
                                valuetype S32 large,
                                int32 acc) cil managed noinlining
  {
    // Code size       21 (0x15)
    .maxstack  3
    IL_0000:  ldarga.s   x
    IL_0002:  ldarga.s   large
    IL_0004:  ldarga.s   acc
    IL_0006:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000b:  ldarg.0
    IL_000c:  ldarg.2
    IL_000d:  tail.
    IL_000f:  call       valuetype S32 Program::CalcStaticRetbuf(int32,
                                                                 int32)
    IL_0014:  ret
  } // end of method Program::CalcStaticRetbufOther

  .method private hidebysig static int64 
          CalcStaticLong(int32 x,
                         int32 acc) cil managed noinlining
  {
    // Code size       42 (0x2a)
    .maxstack  3
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_000c

    IL_0003:  ldarg.1
    IL_0004:  conv.u8
    IL_0005:  ldc.i4.s   32
    IL_0007:  shl
    IL_0008:  ldarg.1
    IL_0009:  conv.u8
    IL_000a:  or
    IL_000b:  ret

    IL_000c:  ldloca.s   V_0
    IL_000e:  initobj    S32
    IL_0014:  ldarga.s   x
    IL_0016:  ldloca.s   V_0
    IL_0018:  ldarga.s   acc
    IL_001a:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_001f:  ldarg.0
    IL_0020:  ldloc.0
    IL_0021:  ldarg.1
    IL_0022:  tail.
    IL_0024:  call       int64 Program::CalcStaticLongOther(int32,
                                                            valuetype S32,
                                                            int32)
    IL_0029:  ret
  } // end of method Program::CalcStaticLong

  .method private hidebysig static int64 
          CalcStaticLongOther(int32 x,
                              valuetype S32 large,
                              int32 acc) cil managed noinlining
  {
    // Code size       21 (0x15)
    .maxstack  3
    IL_0000:  ldarga.s   x
    IL_0002:  ldarga.s   large
    IL_0004:  ldarga.s   acc
    IL_0006:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000b:  ldarg.0
    IL_000c:  ldarg.2
    IL_000d:  tail.
    IL_000f:  call       int64 Program::CalcStaticLong(int32,
                                                       int32)
    IL_0014:  ret
  } // end of method Program::CalcStaticLongOther

  .method private hidebysig static valuetype S16 
          CalcStaticS16(int32 x,
                        int32 acc) cil managed noinlining
  {
    // Code size       61 (0x3d)
    .maxstack  3
    .locals init (valuetype S32 V_0,
             valuetype S16 V_1)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_001f

    IL_0003:  ldloca.s   V_1
    IL_0005:  initobj    S16
    IL_000b:  ldloca.s   V_1
    IL_000d:  ldarg.1
    IL_000e:  conv.i8
    IL_000f:  stfld      int64 S16::A
    IL_0014:  ldloca.s   V_1
    IL_0016:  ldarg.1
    IL_0017:  conv.i8
    IL_0018:  stfld      int64 S16::B
    IL_001d:  ldloc.1
    IL_001e:  ret

    IL_001f:  ldloca.s   V_0
    IL_0021:  initobj    S32
    IL_0027:  ldarga.s   x
    IL_0029:  ldloca.s   V_0
    IL_002b:  ldarga.s   acc
    IL_002d:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0032:  ldarg.0
    IL_0033:  ldloc.0
    IL_0034:  ldarg.1
    IL_0035:  tail.
    IL_0037:  call       valuetype S16 Program::CalcStaticS16Other(int32,
                                                                   valuetype S32,
                                                                   int32)
    IL_003c:  ret
  } // end of method Program::CalcStaticS16

  .method private hidebysig static valuetype S16 
          CalcStaticS16Other(int32 x,
                             valuetype S32 large,
                             int32 acc) cil managed noinlining
  {
    // Code size       21 (0x15)
    .maxstack  3
    IL_0000:  ldarga.s   x
    IL_0002:  ldarga.s   large
    IL_0004:  ldarga.s   acc
    IL_0006:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000b:  ldarg.0
    IL_000c:  ldarg.2
    IL_000d:  tail.
    IL_000f:  call       valuetype S16 Program::CalcStaticS16(int32,
                                                              int32)
    IL_0014:  ret
  } // end of method Program::CalcStaticS16Other

  .method private hidebysig static int32 
          CalcStaticCalli(int32 x,
                          int32 acc) cil managed noinlining
  {
    // Code size       40 (0x28)
    .maxstack  4
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0005

    IL_0003:  ldarg.1
    IL_0004:  ret

    IL_0005:  ldloca.s   V_0
    IL_0007:  initobj    S32
    IL_000d:  ldarga.s   x
    IL_000f:  ldloca.s   V_0
    IL_0011:  ldarga.s   acc
    IL_0013:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0018:  ldarg.0
    IL_0019:  ldloc.0
    IL_001a:  ldarg.1
    IL_001b:  ldsfld     native int Program::s_calcStaticCalliOther
    IL_0020:  tail.
    IL_0022:  calli      int32(int32,valuetype S32,int32)
    IL_0027:  ret
  } // end of method Program::CalcStaticCalli

  .method private hidebysig static int32 
          CalcStaticCalliOther(int32 x,
                               valuetype S32 large,
                               int32 acc) cil managed noinlining
  {
    // Code size       26 (0x1a)
    .maxstack  3
    IL_0000:  ldarga.s   x
    IL_0002:  ldarga.s   large
    IL_0004:  ldarga.s   acc
    IL_0006:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000b:  ldarg.0
    IL_000c:  ldarg.2
    IL_000d:  ldsfld     native int Program::s_calcStaticCalli
    IL_0012:  tail.
    IL_0014:  calli      int32(int32,int32)
    IL_0019:  ret
  } // end of method Program::CalcStaticCalliOther

  .method private hidebysig static string 
          EmptyCalli() cil managed noinlining
  {
    // Code size       41 (0x29)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  call       int32 [System.Runtime]System.Environment::get_TickCount()
    IL_0005:  ldc.i4.0
    IL_0006:  blt.s      IL_000c

    IL_0008:  ldc.i4.s   40
    IL_000a:  br.s       IL_000e

    IL_000c:  ldc.i4.s   30
    IL_000e:  stloc.0
    IL_000f:  ldloc.0
    IL_0010:  conv.u
    IL_0011:  ldc.i4.4
    IL_0012:  mul.ovf.un
    IL_0013:  localloc
    IL_0015:  ldloc.0
    IL_0016:  newobj     instance void valuetype [System.Runtime]System.Span`1<int32>::.ctor(void*,
                                                                                             int32)
    IL_001b:  pop
    IL_001c:  ldsfld     native int Program::s_emptyCalliOther
    IL_0021:  tail.
    IL_0023:  calli      string()
    IL_0028:  ret
  } // end of method Program::EmptyCalli

  .method private hidebysig static string 
          ValueTypeInstanceMethodCalli() cil managed noinlining
  {
    // Code size       51 (0x33)
    .maxstack  2
    .locals init (valuetype S16 V_0,
             int32 V_1)
    IL_0000:  call       int32 [System.Runtime]System.Environment::get_TickCount()
    IL_0005:  ldc.i4.0
    IL_0006:  blt.s      IL_000c

    IL_0008:  ldc.i4.s   40
    IL_000a:  br.s       IL_000e

    IL_000c:  ldc.i4.s   30
    IL_000e:  stloc.1
    IL_000f:  ldloc.1
    IL_0010:  conv.u
    IL_0011:  ldc.i4.4
    IL_0012:  mul.ovf.un
    IL_0013:  localloc
    IL_0015:  ldloc.1
    IL_0016:  newobj     instance void valuetype [System.Runtime]System.Span`1<int32>::.ctor(void*,
                                                                                             int32)
    IL_001b:  pop
    IL_001c:  ldloca.s   V_0
    IL_001e:  initobj    S16
    IL_0024:  ldloca.s   V_0
    IL_0026:  ldsfld     native int Program::s_instanceMethodOnValueType
    IL_002b:  tail.
    IL_002d:  calli      instance string()
    IL_0032:  ret
  } // end of method Program::ValueTypeInstanceMethodCalli

  .method private hidebysig static string 
          ValueTypeExplicitThisInstanceMethodCalli() cil managed noinlining
  {
    // Code size       51 (0x33)
    .maxstack  2
    .locals init (valuetype S16 V_0,
             int32 V_1)
    IL_0000:  call       int32 [System.Runtime]System.Environment::get_TickCount()
    IL_0005:  ldc.i4.0
    IL_0006:  blt.s      IL_000c

    IL_0008:  ldc.i4.s   40
    IL_000a:  br.s       IL_000e

    IL_000c:  ldc.i4.s   30
    IL_000e:  stloc.1
    IL_000f:  ldloc.1
    IL_0010:  conv.u
    IL_0011:  ldc.i4.4
    IL_0012:  mul.ovf.un
    IL_0013:  localloc
    IL_0015:  ldloc.1
    IL_0016:  newobj     instance void valuetype [System.Runtime]System.Span`1<int32>::.ctor(void*,
                                                                                             int32)
    IL_001b:  pop
    IL_001c:  ldloca.s   V_0
    IL_001e:  initobj    S16
    IL_0024:  ldloca.s   V_0
    IL_0026:  ldsfld     native int Program::s_instanceMethodOnValueType
    IL_002b:  tail.
    IL_002d:  calli      explicit instance string(valuetype S16&)
    IL_0032:  ret
  } // end of method Program::ValueTypeExplicitThisInstanceMethodCalli

  .method private hidebysig static string 
          EmptyCalliOther() cil managed noinlining
  {
    // Code size       6 (0x6)
    .maxstack  8
    IL_0000:  ldstr      "Empty calli"
    IL_0005:  ret
  } // end of method Program::EmptyCalliOther

  .method private hidebysig static valuetype S32 
          CalcStaticCalliRetbuf(int32 x,
                                int32 acc) cil managed noinlining
  {
    // Code size       84 (0x54)
    .maxstack  4
    .locals init (valuetype S32 V_0,
             valuetype S32 V_1)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0031

    IL_0003:  ldloca.s   V_1
    IL_0005:  initobj    S32
    IL_000b:  ldloca.s   V_1
    IL_000d:  ldarg.1
    IL_000e:  conv.i8
    IL_000f:  stfld      int64 S32::A
    IL_0014:  ldloca.s   V_1
    IL_0016:  ldarg.1
    IL_0017:  conv.i8
    IL_0018:  stfld      int64 S32::B
    IL_001d:  ldloca.s   V_1
    IL_001f:  ldarg.1
    IL_0020:  conv.i8
    IL_0021:  stfld      int64 S32::C
    IL_0026:  ldloca.s   V_1
    IL_0028:  ldarg.1
    IL_0029:  conv.i8
    IL_002a:  stfld      int64 S32::D
    IL_002f:  ldloc.1
    IL_0030:  ret

    IL_0031:  ldloca.s   V_0
    IL_0033:  initobj    S32
    IL_0039:  ldarga.s   x
    IL_003b:  ldloca.s   V_0
    IL_003d:  ldarga.s   acc
    IL_003f:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0044:  ldarg.0
    IL_0045:  ldloc.0
    IL_0046:  ldarg.1
    IL_0047:  ldsfld     native int Program::s_calcStaticCalliRetbufOther
    IL_004c:  tail.
    IL_004e:  calli      valuetype S32(int32,valuetype S32,int32)
    IL_0053:  ret
  } // end of method Program::CalcStaticCalliRetbuf

  .method private hidebysig static valuetype S32 
          CalcStaticCalliRetbufOther(int32 x,
                                     valuetype S32 large,
                                     int32 acc) cil managed noinlining
  {
    // Code size       26 (0x1a)
    .maxstack  3
    IL_0000:  ldarga.s   x
    IL_0002:  ldarga.s   large
    IL_0004:  ldarga.s   acc
    IL_0006:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000b:  ldarg.0
    IL_000c:  ldarg.2
    IL_000d:  ldsfld     native int Program::s_calcStaticCalliRetbuf
    IL_0012:  tail.
    IL_0014:  calli      valuetype S32(int32,int32)
    IL_0019:  ret
  } // end of method Program::CalcStaticCalliRetbufOther

  .method private hidebysig static void  CalcStaticVoid(int32 x,
                                                        int32 acc) cil managed noinlining
  {
    // Code size       40 (0x28)
    .maxstack  3
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_000a

    IL_0003:  ldarg.1
    IL_0004:  stsfld     int32 Program::s_result
    IL_0009:  ret

    IL_000a:  ldloca.s   V_0
    IL_000c:  initobj    S32
    IL_0012:  ldarga.s   x
    IL_0014:  ldloca.s   V_0
    IL_0016:  ldarga.s   acc
    IL_0018:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_001d:  ldarg.0
    IL_001e:  ldloc.0
    IL_001f:  ldarg.1
    IL_0020:  tail.
    IL_0022:  call       void Program::CalcStaticVoidOther(int32,
                                                           valuetype S32,
                                                           int32)
    IL_0027:  ret
  } // end of method Program::CalcStaticVoid

  .method private hidebysig static void  CalcStaticVoidOther(int32 x,
                                                             valuetype S32 large,
                                                             int32 acc) cil managed noinlining
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarga.s   x
    IL_0002:  ldarga.s   large
    IL_0004:  ldarga.s   acc
    IL_0006:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000b:  ldarg.0
    IL_000c:  ldarg.2
    IL_000d:  tail.
    IL_000f:  call       void Program::CalcStaticVoid(int32,
                                                      int32)
    IL_0014:  ret
  } // end of method Program::CalcStaticVoidOther

  .method private hidebysig static int32 
          CountUpHeap(int32 left,
                      class HeapInt counter) cil managed noinlining
  {
    // Code size       43 (0x2b)
    .maxstack  4
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_000a

    IL_0003:  ldarg.1
    IL_0004:  ldfld      int32 HeapInt::Value
    IL_0009:  ret

    IL_000a:  ldarg.0
    IL_000b:  ldc.i4.1
    IL_000c:  sub
    IL_000d:  ldloca.s   V_0
    IL_000f:  initobj    S32
    IL_0015:  ldloc.0
    IL_0016:  ldarg.1
    IL_0017:  ldfld      int32 HeapInt::Value
    IL_001c:  ldc.i4.1
    IL_001d:  add
    IL_001e:  newobj     instance void HeapInt::.ctor(int32)
    IL_0023:  tail.
    IL_0025:  call       int32 Program::CountUpHeapOther(int32,
                                                         valuetype S32,
                                                         class HeapInt)
    IL_002a:  ret
  } // end of method Program::CountUpHeap

  .method private hidebysig static int32 
          CountUpHeapOther(int32 left,
                           valuetype S32 s,
                           class HeapInt counter) cil managed noinlining
  {
    // Code size       34 (0x22)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_000a

    IL_0003:  ldarg.2
    IL_0004:  ldfld      int32 HeapInt::Value
    IL_0009:  ret

    IL_000a:  ldarg.0
    IL_000b:  ldc.i4.1
    IL_000c:  sub
    IL_000d:  ldarg.2
    IL_000e:  ldfld      int32 HeapInt::Value
    IL_0013:  ldc.i4.1
    IL_0014:  add
    IL_0015:  newobj     instance void HeapInt::.ctor(int32)
    IL_001a:  tail.
    IL_001c:  call       int32 Program::CountUpHeap(int32,
                                                    class HeapInt)
    IL_0021:  ret
  } // end of method Program::CountUpHeapOther

  .method private hidebysig static void  CountUpRef(int32 left,
                                                    int32& counter) cil managed
  {
    // Code size       31 (0x1f)
    .maxstack  3
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0004

    IL_0003:  ret

    IL_0004:  ldarg.1
    IL_0005:  ldarg.1
    IL_0006:  ldind.i4
    IL_0007:  ldc.i4.1
    IL_0008:  add
    IL_0009:  stind.i4
    IL_000a:  ldarg.0
    IL_000b:  ldc.i4.1
    IL_000c:  sub
    IL_000d:  ldloca.s   V_0
    IL_000f:  initobj    S32
    IL_0015:  ldloc.0
    IL_0016:  ldarg.1
    IL_0017:  tail.
    IL_0019:  call       void Program::CountUpRefOther(int32,
                                                       valuetype S32,
                                                       int32&)
    IL_001e:  ret
  } // end of method Program::CountUpRef

  .method private hidebysig static void  CountUpRefOther(int32 left,
                                                         valuetype S32 s,
                                                         int32& counter) cil managed
  {
    // Code size       22 (0x16)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0004

    IL_0003:  ret

    IL_0004:  ldarg.2
    IL_0005:  ldarg.2
    IL_0006:  ldind.i4
    IL_0007:  ldc.i4.1
    IL_0008:  add
    IL_0009:  stind.i4
    IL_000a:  ldarg.0
    IL_000b:  ldc.i4.1
    IL_000c:  sub
    IL_000d:  ldarg.2
    IL_000e:  tail.
    IL_0010:  call       void Program::CountUpRef(int32,
                                                  int32&)
    IL_0015:  ret
  } // end of method Program::CountUpRefOther

  .method private hidebysig static string 
          GenName1Forward<T>(!!T x) cil managed noinlining
  {
    .param type T 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    // Code size       18 (0x12)
    .maxstack  2
    .locals init (valuetype S32 V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    S32
    IL_0008:  ldloc.0
    IL_0009:  ldarg.0
    IL_000a:  tail.
    IL_000c:  call       string Program::GenName1<!!0>(valuetype S32,
                                                       !!0)
    IL_0011:  ret
  } // end of method Program::GenName1Forward

  .method private hidebysig static string 
          GenName1<T>(valuetype S32 s,
                      !!T x) cil managed noinlining
  {
    .param type T 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    // Code size       59 (0x3b)
    .maxstack  3
    .locals init (valuetype [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  ldc.i4.1
    IL_0003:  ldc.i4.2
    IL_0004:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::.ctor(int32,
                                                                                                                               int32)
    IL_0009:  ldloca.s   V_0
    IL_000b:  ldtoken    !!T
    IL_0010:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0015:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_001a:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_001f:  ldloca.s   V_0
    IL_0021:  ldstr      " "
    IL_0026:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_002b:  ldloca.s   V_0
    IL_002d:  ldarg.1
    IL_002e:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<!!0>(!!0)
    IL_0033:  ldloca.s   V_0
    IL_0035:  call       instance string [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::ToStringAndClear()
    IL_003a:  ret
  } // end of method Program::GenName1

  .method private hidebysig static string 
          GenName2ForwardBoth<T1,T2>(!!T1 x,
                                     !!T2 y) cil managed noinlining
  {
    .param type T1 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    .param type T2 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    // Code size       19 (0x13)
    .maxstack  3
    .locals init (valuetype S32 V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    S32
    IL_0008:  ldloc.0
    IL_0009:  ldarg.0
    IL_000a:  ldarg.1
    IL_000b:  tail.
    IL_000d:  call       string Program::GenName2<!!0,!!1>(valuetype S32,
                                                           !!0,
                                                           !!1)
    IL_0012:  ret
  } // end of method Program::GenName2ForwardBoth

  .method private hidebysig static string 
          GenName2ForwardOne<T>(!!T x,
                                string y) cil managed noinlining
  {
    .param type T 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    // Code size       19 (0x13)
    .maxstack  3
    .locals init (valuetype S32 V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    S32
    IL_0008:  ldloc.0
    IL_0009:  ldarg.0
    IL_000a:  ldarg.1
    IL_000b:  tail.
    IL_000d:  call       string Program::GenName2<!!0,string>(valuetype S32,
                                                              !!0,
                                                              !!1)
    IL_0012:  ret
  } // end of method Program::GenName2ForwardOne

  .method private hidebysig static string 
          GenName2ForwardNone(object x,
                              string y) cil managed noinlining
  {
    // Code size       19 (0x13)
    .maxstack  3
    .locals init (valuetype S32 V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    S32
    IL_0008:  ldloc.0
    IL_0009:  ldarg.0
    IL_000a:  ldarg.1
    IL_000b:  tail.
    IL_000d:  call       string Program::GenName2<object,string>(valuetype S32,
                                                                 !!0,
                                                                 !!1)
    IL_0012:  ret
  } // end of method Program::GenName2ForwardNone

  .method private hidebysig static string 
          GenName2<T1,T2>(valuetype S32 s,
                          !!T1 a,
                          !!T2 b) cil managed noinlining
  {
    .param type T1 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    .param type T2 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    // Code size       113 (0x71)
    .maxstack  3
    .locals init (valuetype [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  ldc.i4.3
    IL_0003:  ldc.i4.4
    IL_0004:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::.ctor(int32,
                                                                                                                               int32)
    IL_0009:  ldloca.s   V_0
    IL_000b:  ldtoken    !!T1
    IL_0010:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0015:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_001a:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_001f:  ldloca.s   V_0
    IL_0021:  ldstr      " "
    IL_0026:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_002b:  ldloca.s   V_0
    IL_002d:  ldtoken    !!T2
    IL_0032:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0037:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_003c:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_0041:  ldloca.s   V_0
    IL_0043:  ldstr      " "
    IL_0048:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_004d:  ldloca.s   V_0
    IL_004f:  ldarg.1
    IL_0050:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<!!0>(!!0)
    IL_0055:  ldloca.s   V_0
    IL_0057:  ldstr      " "
    IL_005c:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_0061:  ldloca.s   V_0
    IL_0063:  ldarg.2
    IL_0064:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<!!1>(!!0)
    IL_0069:  ldloca.s   V_0
    IL_006b:  call       instance string [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::ToStringAndClear()
    IL_0070:  ret
  } // end of method Program::GenName2

  .method private hidebysig static string 
          GenInterfaceForwardF<T1,T2,T3,T4>(!!T1 a,
                                            !!T2 b,
                                            !!T3 c,
                                            !!T4 d,
                                            class IGenInterface`2<!!T1,!!T2> igen) cil managed noinlining
  {
    .param type T1 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    .param type T2 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    .param type T3 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    .param type T4 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    // Code size       23 (0x17)
    .maxstack  6
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.s    igen
    IL_0002:  ldloca.s   V_0
    IL_0004:  initobj    S32
    IL_000a:  ldloc.0
    IL_000b:  ldarg.0
    IL_000c:  ldarg.1
    IL_000d:  ldarg.2
    IL_000e:  ldarg.3
    IL_000f:  tail.
    IL_0011:  callvirt   instance string class IGenInterface`2<!!0,!!1>::F<!!2,!!3>(valuetype S32,
                                                                                    !0,
                                                                                    !1,
                                                                                    !!0,
                                                                                    !!1)
    IL_0016:  ret
  } // end of method Program::GenInterfaceForwardF

  .method private hidebysig static string 
          GenInterfaceForwardG<T1,T2>(!!T1 a,
                                      !!T2 b,
                                      class IGenInterface`2<!!T1,!!T2> igen) cil managed noinlining
  {
    .param type T1 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    .param type T2 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    // Code size       20 (0x14)
    .maxstack  4
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.2
    IL_0001:  ldloca.s   V_0
    IL_0003:  initobj    S32
    IL_0009:  ldloc.0
    IL_000a:  ldarg.0
    IL_000b:  ldarg.1
    IL_000c:  tail.
    IL_000e:  callvirt   instance string class IGenInterface`2<!!T1,!!T2>::G(valuetype S32,
                                                                             !0,
                                                                             !1)
    IL_0013:  ret
  } // end of method Program::GenInterfaceForwardG

  .method private hidebysig static string 
          GenInterfaceForwardNone(string a,
                                  object b,
                                  int32 c,
                                  object d,
                                  class IGenInterface`2<string,object> igen) cil managed noinlining
  {
    // Code size       23 (0x17)
    .maxstack  6
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.s    igen
    IL_0002:  ldloca.s   V_0
    IL_0004:  initobj    S32
    IL_000a:  ldloc.0
    IL_000b:  ldarg.0
    IL_000c:  ldarg.1
    IL_000d:  ldarg.2
    IL_000e:  ldarg.3
    IL_000f:  tail.
    IL_0011:  callvirt   instance string class IGenInterface`2<string,object>::F<int32,object>(valuetype S32,
                                                                                               !0,
                                                                                               !1,
                                                                                               !!0,
                                                                                               !!1)
    IL_0016:  ret
  } // end of method Program::GenInterfaceForwardNone

  .method private hidebysig static string 
          GenInterfaceForward2(string a,
                               object b,
                               class IGenInterface`2<string,object> igen) cil managed noinlining
  {
    // Code size       20 (0x14)
    .maxstack  4
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.2
    IL_0001:  ldloca.s   V_0
    IL_0003:  initobj    S32
    IL_0009:  ldloc.0
    IL_000a:  ldarg.0
    IL_000b:  ldarg.1
    IL_000c:  tail.
    IL_000e:  callvirt   instance string class IGenInterface`2<string,object>::G(valuetype S32,
                                                                                 !0,
                                                                                 !1)
    IL_0013:  ret
  } // end of method Program::GenInterfaceForward2

  .method private hidebysig static string 
          GenAbstractFString(class GenAbstract`1<string> ga) cil managed noinlining
  {
    // Code size       9 (0x9)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  tail.
    IL_0003:  callvirt   instance string class GenAbstract`1<string>::F<object>()
    IL_0008:  ret
  } // end of method Program::GenAbstractFString

  .method private hidebysig static string 
          GenAbstractGString(class GenAbstract`1<string> ga) cil managed noinlining
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  tail.
    IL_0003:  callvirt   instance string class GenAbstract`1<string>::G()
    IL_0008:  ret
  } // end of method Program::GenAbstractGString

  .method private hidebysig static string 
          GenAbstractFInt(class GenAbstract`1<int32> ga) cil managed noinlining
  {
    // Code size       9 (0x9)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  tail.
    IL_0003:  callvirt   instance string class GenAbstract`1<int32>::F<object>()
    IL_0008:  ret
  } // end of method Program::GenAbstractFInt

  .method private hidebysig static string 
          GenAbstractGInt(class GenAbstract`1<int32> ga) cil managed noinlining
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  tail.
    IL_0003:  callvirt   instance string class GenAbstract`1<int32>::G()
    IL_0008:  ret
  } // end of method Program::GenAbstractGInt

  .method private hidebysig static int32 
          InstantiatingStub1<T>(int32 a,
                                int32 r,
                                !!T c,
                                valuetype [System.Runtime]System.Span`1<int32> d) cil managed noinlining
  {
    .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .param type T 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    .param [3]
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 01 00 00 ) 
    // Code size       31 (0x1f)
    .maxstack  14
    IL_0000:  ldarg.2
    IL_0001:  ldarg.2
    IL_0002:  ldarg.2
    IL_0003:  ldarg.2
    IL_0004:  ldarg.2
    IL_0005:  ldarg.2
    IL_0006:  ldarg.2
    IL_0007:  ldarg.2
    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  ldarg.3
    IL_000b:  ldarg.1
    IL_000c:  ldarga.s   d
    IL_000e:  ldc.i4.s   99
    IL_0010:  call       instance !0& valuetype [System.Runtime]System.Span`1<int32>::get_Item(int32)
    IL_0015:  ldind.i4
    IL_0016:  add
    IL_0017:  tail.
    IL_0019:  call       int32 Program::InstantiatingStub1Other<!!0>(!!0,
                                                                     !!0,
                                                                     !!0,
                                                                     !!0,
                                                                     !!0,
                                                                     !!0,
                                                                     !!0,
                                                                     !!0,
                                                                     int32,
                                                                     int32,
                                                                     valuetype [System.Runtime]System.Span`1<int32>,
                                                                     int32)
    IL_001e:  ret
  } // end of method Program::InstantiatingStub1

  .method private hidebysig static int32 
          InstantiatingStub1Other<T>(!!T c0,
                                     !!T c1,
                                     !!T c2,
                                     !!T c3,
                                     !!T c4,
                                     !!T c5,
                                     !!T c6,
                                     !!T c7,
                                     int32 a,
                                     int32 r,
                                     valuetype [System.Runtime]System.Span`1<int32> d,
                                     int32 result) cil managed noinlining
  {
    .param type T 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    .param [11]
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    // Code size       31 (0x1f)
    .maxstack  4
    IL_0000:  ldarg.s    a
    IL_0002:  ldarga.s   d
    IL_0004:  call       instance int32 valuetype [System.Runtime]System.Span`1<int32>::get_Length()
    IL_0009:  bne.un.s   IL_000e

    IL_000b:  ldarg.s    result
    IL_000d:  ret

    IL_000e:  ldarg.s    a
    IL_0010:  ldc.i4.1
    IL_0011:  add
    IL_0012:  ldarg.s    result
    IL_0014:  ldarg.0
    IL_0015:  ldarg.s    d
    IL_0017:  tail.
    IL_0019:  call       int32 Program::InstantiatingStub1<!!0>(int32,
                                                                int32,
                                                                !!0,
                                                                valuetype [System.Runtime]System.Span`1<int32>)
    IL_001e:  ret
  } // end of method Program::InstantiatingStub1Other

  .method private hidebysig static int32 
          VirtCallThisHasSideEffects() cil managed noinlining
  {
    // Code size       26 (0x1a)
    .maxstack  1
    IL_0000:  ldc.i4     0x3e8
    IL_0005:  localloc
    IL_0007:  pop
    IL_0008:  newobj     instance void Program/GenericInstanceFactory::.ctor()
    IL_000d:  call       instance class Program/GenericInstance`1<string> Program/GenericInstanceFactory::CreateInstance()
    IL_0012:  tail.
    IL_0014:  callvirt   instance int32 class Program/GenericInstance`1<string>::NumberOfInstances()
    IL_0019:  ret
  } // end of method Program::VirtCallThisHasSideEffects

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Program::.ctor

} // end of class Program

.class private auto ansi Instance
       extends [System.Runtime]System.Object
{
  .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 ) 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
  .field private static initonly native int s_calcInstanceCalli
  .field private static initonly native int s_calcInstanceCalliOther
  .field private static initonly native int s_calcInstanceCalliRetbuf
  .field private static initonly native int s_calcInstanceCalliRetbufOther
  .field private int32 _x
  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       53 (0x35)
    .maxstack  1
    .locals init (native int V_0,
             native int V_1,
             native int V_2,
             native int V_3)
    IL_0000:  ldftn      instance int32 Instance::CalcInstanceCalli(int32,
                                                                    int32)
    IL_0006:  stloc.0
    IL_0007:  ldftn      instance int32 Instance::CalcInstanceCalliOther(int32,
                                                                         valuetype S32,
                                                                         int32)
    IL_000d:  stloc.1
    IL_000e:  ldftn      instance valuetype S32 Instance::CalcInstanceCalliRetbuf(int32,
                                                                                  int32)
    IL_0014:  stloc.2
    IL_0015:  ldftn      instance valuetype S32 Instance::CalcInstanceCalliRetbufOther(int32,
                                                                                       valuetype S32,
                                                                                       int32)
    IL_001b:  stloc.3
    IL_001c:  ldloc.0
    IL_001d:  stsfld     native int Instance::s_calcInstanceCalli
    IL_0022:  ldloc.1
    IL_0023:  stsfld     native int Instance::s_calcInstanceCalliOther
    IL_0028:  ldloc.2
    IL_0029:  stsfld     native int Instance::s_calcInstanceCalliRetbuf
    IL_002e:  ldloc.3
    IL_002f:  stsfld     native int Instance::s_calcInstanceCalliRetbufOther
    IL_0034:  ret
  } // end of method Instance::.cctor

  .method public hidebysig instance int32 
          CalcInstance(int32 x,
                       int32 acc) cil managed noinlining
  {
    // Code size       64 (0x40)
    .maxstack  4
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4     0xffed2979
    IL_0006:  beq.s      IL_000f

    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      int32 Instance::_x
    IL_000f:  ldarg.0
    IL_0010:  ldfld      int32 Instance::_x
    IL_0015:  brtrue.s   IL_0019

    IL_0017:  ldarg.2
    IL_0018:  ret

    IL_0019:  ldloca.s   V_0
    IL_001b:  initobj    S32
    IL_0021:  ldarg.0
    IL_0022:  ldflda     int32 Instance::_x
    IL_0027:  ldloca.s   V_0
    IL_0029:  ldarga.s   acc
    IL_002b:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0030:  ldarg.0
    IL_0031:  ldc.i4     0xffed2979
    IL_0036:  ldloc.0
    IL_0037:  ldarg.2
    IL_0038:  tail.
    IL_003a:  callvirt   instance int32 Instance::CalcInstanceOther(int32,
                                                                    valuetype S32,
                                                                    int32)
    IL_003f:  ret
  } // end of method Instance::CalcInstance

  .method public hidebysig instance int32 
          CalcInstanceOther(int32 x,
                            valuetype S32 large,
                            int32 acc) cil managed noinlining
  {
    // Code size       30 (0x1e)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 Instance::_x
    IL_0006:  ldarga.s   large
    IL_0008:  ldarga.s   acc
    IL_000a:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000f:  ldarg.0
    IL_0010:  ldc.i4     0xffed2979
    IL_0015:  ldarg.3
    IL_0016:  tail.
    IL_0018:  callvirt   instance int32 Instance::CalcInstance(int32,
                                                               int32)
    IL_001d:  ret
  } // end of method Instance::CalcInstanceOther

  .method public hidebysig instance valuetype S32 
          CalcInstanceRetbuf(int32 x,
                             int32 acc) cil managed noinlining
  {
    // Code size       108 (0x6c)
    .maxstack  4
    .locals init (valuetype S32 V_0,
             valuetype S32 V_1)
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4     0xffed2979
    IL_0006:  beq.s      IL_000f

    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      int32 Instance::_x
    IL_000f:  ldarg.0
    IL_0010:  ldfld      int32 Instance::_x
    IL_0015:  brtrue.s   IL_0045

    IL_0017:  ldloca.s   V_1
    IL_0019:  initobj    S32
    IL_001f:  ldloca.s   V_1
    IL_0021:  ldarg.2
    IL_0022:  conv.i8
    IL_0023:  stfld      int64 S32::A
    IL_0028:  ldloca.s   V_1
    IL_002a:  ldarg.2
    IL_002b:  conv.i8
    IL_002c:  stfld      int64 S32::B
    IL_0031:  ldloca.s   V_1
    IL_0033:  ldarg.2
    IL_0034:  conv.i8
    IL_0035:  stfld      int64 S32::C
    IL_003a:  ldloca.s   V_1
    IL_003c:  ldarg.2
    IL_003d:  conv.i8
    IL_003e:  stfld      int64 S32::D
    IL_0043:  ldloc.1
    IL_0044:  ret

    IL_0045:  ldloca.s   V_0
    IL_0047:  initobj    S32
    IL_004d:  ldarg.0
    IL_004e:  ldflda     int32 Instance::_x
    IL_0053:  ldloca.s   V_0
    IL_0055:  ldarga.s   acc
    IL_0057:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4     0xffed2979
    IL_0062:  ldloc.0
    IL_0063:  ldarg.2
    IL_0064:  tail.
    IL_0066:  callvirt   instance valuetype S32 Instance::CalcInstanceRetbufOther(int32,
                                                                                  valuetype S32,
                                                                                  int32)
    IL_006b:  ret
  } // end of method Instance::CalcInstanceRetbuf

  .method public hidebysig instance valuetype S32 
          CalcInstanceRetbufOther(int32 x,
                                  valuetype S32 large,
                                  int32 acc) cil managed noinlining
  {
    // Code size       30 (0x1e)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 Instance::_x
    IL_0006:  ldarga.s   large
    IL_0008:  ldarga.s   acc
    IL_000a:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000f:  ldarg.0
    IL_0010:  ldc.i4     0xffed2979
    IL_0015:  ldarg.3
    IL_0016:  tail.
    IL_0018:  callvirt   instance valuetype S32 Instance::CalcInstanceRetbuf(int32,
                                                                             int32)
    IL_001d:  ret
  } // end of method Instance::CalcInstanceRetbufOther

  .method public hidebysig instance int32 
          CalcInstanceCalli(int32 x,
                            int32 acc) cil managed noinlining
  {
    // Code size       69 (0x45)
    .maxstack  5
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4     0xffed2979
    IL_0006:  beq.s      IL_000f

    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      int32 Instance::_x
    IL_000f:  ldarg.0
    IL_0010:  ldfld      int32 Instance::_x
    IL_0015:  brtrue.s   IL_0019

    IL_0017:  ldarg.2
    IL_0018:  ret

    IL_0019:  ldloca.s   V_0
    IL_001b:  initobj    S32
    IL_0021:  ldarg.0
    IL_0022:  ldflda     int32 Instance::_x
    IL_0027:  ldloca.s   V_0
    IL_0029:  ldarga.s   acc
    IL_002b:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0030:  ldarg.0
    IL_0031:  ldc.i4     0xffed2979
    IL_0036:  ldloc.0
    IL_0037:  ldarg.2
    IL_0038:  ldsfld     native int Instance::s_calcInstanceCalliOther
    IL_003d:  tail.
    IL_003f:  calli      instance int32(int32,valuetype S32,int32)
    IL_0044:  ret
  } // end of method Instance::CalcInstanceCalli

  .method public hidebysig instance int32 
          CalcInstanceCalliOther(int32 x,
                                 valuetype S32 large,
                                 int32 acc) cil managed noinlining
  {
    // Code size       35 (0x23)
    .maxstack  4
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 Instance::_x
    IL_0006:  ldarga.s   large
    IL_0008:  ldarga.s   acc
    IL_000a:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000f:  ldarg.0
    IL_0010:  ldc.i4     0xffed2979
    IL_0015:  ldarg.3
    IL_0016:  ldsfld     native int Instance::s_calcInstanceCalli
    IL_001b:  tail.
    IL_001d:  calli      instance int32(int32,int32)
    IL_0022:  ret
  } // end of method Instance::CalcInstanceCalliOther

  .method public hidebysig instance valuetype S32 
          CalcInstanceCalliRetbuf(int32 x,
                                  int32 acc) cil managed noinlining
  {
    // Code size       113 (0x71)
    .maxstack  5
    .locals init (valuetype S32 V_0,
             valuetype S32 V_1)
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4     0xffed2979
    IL_0006:  beq.s      IL_000f

    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      int32 Instance::_x
    IL_000f:  ldarg.0
    IL_0010:  ldfld      int32 Instance::_x
    IL_0015:  brtrue.s   IL_0045

    IL_0017:  ldloca.s   V_1
    IL_0019:  initobj    S32
    IL_001f:  ldloca.s   V_1
    IL_0021:  ldarg.2
    IL_0022:  conv.i8
    IL_0023:  stfld      int64 S32::A
    IL_0028:  ldloca.s   V_1
    IL_002a:  ldarg.2
    IL_002b:  conv.i8
    IL_002c:  stfld      int64 S32::B
    IL_0031:  ldloca.s   V_1
    IL_0033:  ldarg.2
    IL_0034:  conv.i8
    IL_0035:  stfld      int64 S32::C
    IL_003a:  ldloca.s   V_1
    IL_003c:  ldarg.2
    IL_003d:  conv.i8
    IL_003e:  stfld      int64 S32::D
    IL_0043:  ldloc.1
    IL_0044:  ret

    IL_0045:  ldloca.s   V_0
    IL_0047:  initobj    S32
    IL_004d:  ldarg.0
    IL_004e:  ldflda     int32 Instance::_x
    IL_0053:  ldloca.s   V_0
    IL_0055:  ldarga.s   acc
    IL_0057:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4     0xffed2979
    IL_0062:  ldloc.0
    IL_0063:  ldarg.2
    IL_0064:  ldsfld     native int Instance::s_calcInstanceCalliRetbufOther
    IL_0069:  tail.
    IL_006b:  calli      instance valuetype S32(int32,valuetype S32,int32)
    IL_0070:  ret
  } // end of method Instance::CalcInstanceCalliRetbuf

  .method public hidebysig instance valuetype S32 
          CalcInstanceCalliRetbufOther(int32 x,
                                       valuetype S32 large,
                                       int32 acc) cil managed noinlining
  {
    // Code size       35 (0x23)
    .maxstack  4
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 Instance::_x
    IL_0006:  ldarga.s   large
    IL_0008:  ldarga.s   acc
    IL_000a:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000f:  ldarg.0
    IL_0010:  ldc.i4     0xffed2979
    IL_0015:  ldarg.3
    IL_0016:  ldsfld     native int Instance::s_calcInstanceCalliRetbuf
    IL_001b:  tail.
    IL_001d:  calli      instance valuetype S32(int32,int32)
    IL_0022:  ret
  } // end of method Instance::CalcInstanceCalliRetbufOther

  .method public hidebysig instance string 
          GC(object a,
             int32 b,
             object c,
             object d,
             string e,
             string f,
             object g,
             int32 h,
             int32& interior) cil managed noinlining
  {
    // Code size       111 (0x6f)
    .maxstack  5
    .locals init (valuetype S32 V_0,
             valuetype SGC2 V_1)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldloca.s   V_0
    IL_0004:  initobj    S32
    IL_000a:  ldloc.0
    IL_000b:  ldloca.s   V_1
    IL_000d:  initobj    SGC2
    IL_0013:  ldloca.s   V_1
    IL_0015:  ldarg.2
    IL_0016:  stfld      int32 SGC2::A
    IL_001b:  ldloca.s   V_1
    IL_001d:  ldflda     valuetype SGC SGC2::B
    IL_0022:  ldarg.3
    IL_0023:  stfld      object SGC::A
    IL_0028:  ldloca.s   V_1
    IL_002a:  ldflda     valuetype SGC SGC2::B
    IL_002f:  ldarg.s    d
    IL_0031:  stfld      object SGC::B
    IL_0036:  ldloca.s   V_1
    IL_0038:  ldflda     valuetype SGC SGC2::B
    IL_003d:  ldarg.s    e
    IL_003f:  stfld      string SGC::C
    IL_0044:  ldloca.s   V_1
    IL_0046:  ldflda     valuetype SGC SGC2::B
    IL_004b:  ldarg.s    f
    IL_004d:  stfld      string SGC::D
    IL_0052:  ldloca.s   V_1
    IL_0054:  ldarg.s    g
    IL_0056:  stfld      object SGC2::C
    IL_005b:  ldloca.s   V_1
    IL_005d:  ldarg.s    h
    IL_005f:  stfld      int32 SGC2::D
    IL_0064:  ldloc.1
    IL_0065:  ldarg.s    interior
    IL_0067:  tail.
    IL_0069:  call       instance string Instance::GCOther(object,
                                                           valuetype S32,
                                                           valuetype SGC2,
                                                           int32&)
    IL_006e:  ret
  } // end of method Instance::GC

  .method private hidebysig instance string 
          GCOther(object a,
                  valuetype S32 s,
                  valuetype SGC2 gc,
                  int32& interior) cil managed noinlining
  {
    // Code size       243 (0xf3)
    .maxstack  3
    .locals init (valuetype [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  ldc.i4.8
    IL_0003:  ldc.i4.s   9
    IL_0005:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::.ctor(int32,
                                                                                                                               int32)
    IL_000a:  ldloca.s   V_0
    IL_000c:  ldarg.1
    IL_000d:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<object>(!!0)
    IL_0012:  ldloca.s   V_0
    IL_0014:  ldstr      " "
    IL_0019:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_001e:  ldloca.s   V_0
    IL_0020:  ldarg.3
    IL_0021:  ldfld      int32 SGC2::A
    IL_0026:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int32>(!!0)
    IL_002b:  ldloca.s   V_0
    IL_002d:  ldstr      " "
    IL_0032:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_0037:  ldloca.s   V_0
    IL_0039:  ldarg.3
    IL_003a:  ldfld      valuetype SGC SGC2::B
    IL_003f:  ldfld      object SGC::A
    IL_0044:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<object>(!!0)
    IL_0049:  ldloca.s   V_0
    IL_004b:  ldstr      " "
    IL_0050:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_0055:  ldloca.s   V_0
    IL_0057:  ldarg.3
    IL_0058:  ldfld      valuetype SGC SGC2::B
    IL_005d:  ldfld      object SGC::B
    IL_0062:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<object>(!!0)
    IL_0067:  ldloca.s   V_0
    IL_0069:  ldstr      " "
    IL_006e:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_0073:  ldloca.s   V_0
    IL_0075:  ldarg.3
    IL_0076:  ldfld      valuetype SGC SGC2::B
    IL_007b:  ldfld      string SGC::C
    IL_0080:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_0085:  ldloca.s   V_0
    IL_0087:  ldstr      " "
    IL_008c:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_0091:  ldloca.s   V_0
    IL_0093:  ldarg.3
    IL_0094:  ldfld      valuetype SGC SGC2::B
    IL_0099:  ldfld      string SGC::D
    IL_009e:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_00a3:  ldloca.s   V_0
    IL_00a5:  ldstr      " "
    IL_00aa:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_00af:  ldloca.s   V_0
    IL_00b1:  ldarg.3
    IL_00b2:  ldfld      object SGC2::C
    IL_00b7:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<object>(!!0)
    IL_00bc:  ldloca.s   V_0
    IL_00be:  ldstr      " "
    IL_00c3:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_00c8:  ldloca.s   V_0
    IL_00ca:  ldarg.3
    IL_00cb:  ldfld      int32 SGC2::D
    IL_00d0:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int32>(!!0)
    IL_00d5:  ldloca.s   V_0
    IL_00d7:  ldstr      " "
    IL_00dc:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_00e1:  ldloca.s   V_0
    IL_00e3:  ldarg.s    interior
    IL_00e5:  ldind.i4
    IL_00e6:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int32>(!!0)
    IL_00eb:  ldloca.s   V_0
    IL_00ed:  call       instance string [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::ToStringAndClear()
    IL_00f2:  ret
  } // end of method Instance::GCOther

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Instance::.ctor

} // end of class Instance

.class private sequential ansi sealed beforefieldinit InstanceValueType
       extends [System.Runtime]System.ValueType
{
  .field public int32 Count
  .method public hidebysig instance void 
          CountUp(int32 left) cil managed
  {
    // Code size       39 (0x27)
    .maxstack  3
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0004

    IL_0003:  ret

    IL_0004:  ldarg.0
    IL_0005:  ldarg.0
    IL_0006:  ldfld      int32 InstanceValueType::Count
    IL_000b:  ldc.i4.1
    IL_000c:  add
    IL_000d:  stfld      int32 InstanceValueType::Count
    IL_0012:  ldarg.0
    IL_0013:  ldarg.1
    IL_0014:  ldc.i4.1
    IL_0015:  sub
    IL_0016:  ldloca.s   V_0
    IL_0018:  initobj    S32
    IL_001e:  ldloc.0
    IL_001f:  tail.
    IL_0021:  call       instance void InstanceValueType::CountUpOther(int32,
                                                                       valuetype S32)
    IL_0026:  ret
  } // end of method InstanceValueType::CountUp

  .method private hidebysig instance void 
          CountUpOther(int32 left,
                       valuetype S32 s) cil managed
  {
    // Code size       30 (0x1e)
    .maxstack  3
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0004

    IL_0003:  ret

    IL_0004:  ldarg.0
    IL_0005:  ldarg.0
    IL_0006:  ldfld      int32 InstanceValueType::Count
    IL_000b:  ldc.i4.1
    IL_000c:  add
    IL_000d:  stfld      int32 InstanceValueType::Count
    IL_0012:  ldarg.0
    IL_0013:  ldarg.1
    IL_0014:  ldc.i4.1
    IL_0015:  sub
    IL_0016:  tail.
    IL_0018:  call       instance void InstanceValueType::CountUp(int32)
    IL_001d:  ret
  } // end of method InstanceValueType::CountUpOther

} // end of class InstanceValueType

.class private abstract auto ansi beforefieldinit BaseClass
       extends [System.Runtime]System.Object
{
  .method public hidebysig newslot abstract virtual 
          instance int32  CalcAbstract(int32 x,
                                       int32 acc) cil managed
  {
  } // end of method BaseClass::CalcAbstract

  .method public hidebysig newslot abstract virtual 
          instance int32  CalcAbstractOther(int32 x,
                                            valuetype S32 large,
                                            int32 acc) cil managed
  {
  } // end of method BaseClass::CalcAbstractOther

  .method public hidebysig newslot abstract virtual 
          instance valuetype S32  CalcAbstractRetbuf(int32 x,
                                                     int32 acc) cil managed
  {
  } // end of method BaseClass::CalcAbstractRetbuf

  .method public hidebysig newslot abstract virtual 
          instance valuetype S32  CalcAbstractRetbufOther(int32 x,
                                                          valuetype S32 large,
                                                          int32 acc) cil managed
  {
  } // end of method BaseClass::CalcAbstractRetbufOther

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method BaseClass::.ctor

} // end of class BaseClass

.class private auto ansi beforefieldinit ClassImpl
       extends BaseClass
{
  .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 ) 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
  .field private int32 _x
  .field private class BaseClass '<Other>k__BackingField'
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig specialname instance class BaseClass 
          get_Other() cil managed
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class BaseClass ClassImpl::'<Other>k__BackingField'
    IL_0006:  ret
  } // end of method ClassImpl::get_Other

  .method public hidebysig specialname instance void 
          set_Other(class BaseClass 'value') cil managed
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      class BaseClass ClassImpl::'<Other>k__BackingField'
    IL_0007:  ret
  } // end of method ClassImpl::set_Other

  .method public hidebysig virtual instance int32 
          CalcAbstract(int32 x,
                       int32 acc) cil managed noinlining
  {
    // Code size       69 (0x45)
    .maxstack  4
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4     0xffed2979
    IL_0006:  beq.s      IL_000f

    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      int32 ClassImpl::_x
    IL_000f:  ldarg.0
    IL_0010:  ldfld      int32 ClassImpl::_x
    IL_0015:  brtrue.s   IL_0019

    IL_0017:  ldarg.2
    IL_0018:  ret

    IL_0019:  ldloca.s   V_0
    IL_001b:  initobj    S32
    IL_0021:  ldarg.0
    IL_0022:  ldflda     int32 ClassImpl::_x
    IL_0027:  ldloca.s   V_0
    IL_0029:  ldarga.s   acc
    IL_002b:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0030:  ldarg.0
    IL_0031:  call       instance class BaseClass ClassImpl::get_Other()
    IL_0036:  ldc.i4     0xffed2979
    IL_003b:  ldloc.0
    IL_003c:  ldarg.2
    IL_003d:  tail.
    IL_003f:  callvirt   instance int32 ClassImpl::CalcAbstractOther(int32,
                                                                     valuetype S32,
                                                                     int32)
    IL_0044:  ret
  } // end of method ClassImpl::CalcAbstract

  .method public hidebysig virtual instance int32 
          CalcAbstractOther(int32 x,
                            valuetype S32 large,
                            int32 acc) cil managed noinlining
  {
    // Code size       35 (0x23)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 ClassImpl::_x
    IL_0006:  ldarga.s   large
    IL_0008:  ldarga.s   acc
    IL_000a:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000f:  ldarg.0
    IL_0010:  call       instance class BaseClass ClassImpl::get_Other()
    IL_0015:  ldc.i4     0xffed2979
    IL_001a:  ldarg.3
    IL_001b:  tail.
    IL_001d:  callvirt   instance int32 ClassImpl::CalcAbstract(int32,
                                                                int32)
    IL_0022:  ret
  } // end of method ClassImpl::CalcAbstractOther

  .method public hidebysig virtual instance valuetype S32 
          CalcAbstractRetbuf(int32 x,
                             int32 acc) cil managed noinlining
  {
    // Code size       113 (0x71)
    .maxstack  4
    .locals init (valuetype S32 V_0,
             valuetype S32 V_1)
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4     0xffed2979
    IL_0006:  beq.s      IL_000f

    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      int32 ClassImpl::_x
    IL_000f:  ldarg.0
    IL_0010:  ldfld      int32 ClassImpl::_x
    IL_0015:  brtrue.s   IL_0045

    IL_0017:  ldloca.s   V_1
    IL_0019:  initobj    S32
    IL_001f:  ldloca.s   V_1
    IL_0021:  ldarg.2
    IL_0022:  conv.i8
    IL_0023:  stfld      int64 S32::A
    IL_0028:  ldloca.s   V_1
    IL_002a:  ldarg.2
    IL_002b:  conv.i8
    IL_002c:  stfld      int64 S32::B
    IL_0031:  ldloca.s   V_1
    IL_0033:  ldarg.2
    IL_0034:  conv.i8
    IL_0035:  stfld      int64 S32::C
    IL_003a:  ldloca.s   V_1
    IL_003c:  ldarg.2
    IL_003d:  conv.i8
    IL_003e:  stfld      int64 S32::D
    IL_0043:  ldloc.1
    IL_0044:  ret

    IL_0045:  ldloca.s   V_0
    IL_0047:  initobj    S32
    IL_004d:  ldarg.0
    IL_004e:  ldflda     int32 ClassImpl::_x
    IL_0053:  ldloca.s   V_0
    IL_0055:  ldarga.s   acc
    IL_0057:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_005c:  ldarg.0
    IL_005d:  call       instance class BaseClass ClassImpl::get_Other()
    IL_0062:  ldc.i4     0xffed2979
    IL_0067:  ldloc.0
    IL_0068:  ldarg.2
    IL_0069:  tail.
    IL_006b:  callvirt   instance valuetype S32 ClassImpl::CalcAbstractRetbufOther(int32,
                                                                                   valuetype S32,
                                                                                   int32)
    IL_0070:  ret
  } // end of method ClassImpl::CalcAbstractRetbuf

  .method public hidebysig virtual instance valuetype S32 
          CalcAbstractRetbufOther(int32 x,
                                  valuetype S32 large,
                                  int32 acc) cil managed noinlining
  {
    // Code size       35 (0x23)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 ClassImpl::_x
    IL_0006:  ldarga.s   large
    IL_0008:  ldarga.s   acc
    IL_000a:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000f:  ldarg.0
    IL_0010:  call       instance class BaseClass ClassImpl::get_Other()
    IL_0015:  ldc.i4     0xffed2979
    IL_001a:  ldarg.3
    IL_001b:  tail.
    IL_001d:  callvirt   instance valuetype S32 ClassImpl::CalcAbstractRetbuf(int32,
                                                                              int32)
    IL_0022:  ret
  } // end of method ClassImpl::CalcAbstractRetbufOther

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void BaseClass::.ctor()
    IL_0006:  ret
  } // end of method ClassImpl::.ctor

  .property instance class BaseClass Other()
  {
    .get instance class BaseClass ClassImpl::get_Other()
    .set instance void ClassImpl::set_Other(class BaseClass)
  } // end of property ClassImpl::Other
} // end of class ClassImpl

.class interface private abstract auto ansi IInterface
{
  .method public hidebysig newslot abstract virtual 
          instance int32  CalcInterface(int32 x,
                                        int32 acc) cil managed
  {
  } // end of method IInterface::CalcInterface

  .method public hidebysig newslot abstract virtual 
          instance int32  CalcInterfaceOther(int32 x,
                                             valuetype S32 large,
                                             int32 acc) cil managed
  {
  } // end of method IInterface::CalcInterfaceOther

  .method public hidebysig newslot abstract virtual 
          instance valuetype S32  CalcInterfaceRetbuf(int32 x,
                                                      int32 acc) cil managed
  {
  } // end of method IInterface::CalcInterfaceRetbuf

  .method public hidebysig newslot abstract virtual 
          instance valuetype S32  CalcInterfaceRetbufOther(int32 x,
                                                           valuetype S32 large,
                                                           int32 acc) cil managed
  {
  } // end of method IInterface::CalcInterfaceRetbufOther

} // end of class IInterface

.class private auto ansi beforefieldinit InterfaceImpl
       extends [System.Runtime]System.Object
       implements IInterface
{
  .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 ) 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
  .interfaceimpl type IInterface
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
  .field private int32 _x
  .field private class IInterface '<Other>k__BackingField'
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig specialname instance class IInterface 
          get_Other() cil managed
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class IInterface InterfaceImpl::'<Other>k__BackingField'
    IL_0006:  ret
  } // end of method InterfaceImpl::get_Other

  .method public hidebysig specialname instance void 
          set_Other(class IInterface 'value') cil managed
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      class IInterface InterfaceImpl::'<Other>k__BackingField'
    IL_0007:  ret
  } // end of method InterfaceImpl::set_Other

  .method public hidebysig newslot virtual final 
          instance int32  CalcInterface(int32 x,
                                        int32 acc) cil managed noinlining
  {
    // Code size       69 (0x45)
    .maxstack  4
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4     0xffed2979
    IL_0006:  beq.s      IL_000f

    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      int32 InterfaceImpl::_x
    IL_000f:  ldarg.0
    IL_0010:  ldfld      int32 InterfaceImpl::_x
    IL_0015:  brtrue.s   IL_0019

    IL_0017:  ldarg.2
    IL_0018:  ret

    IL_0019:  ldloca.s   V_0
    IL_001b:  initobj    S32
    IL_0021:  ldarg.0
    IL_0022:  ldflda     int32 InterfaceImpl::_x
    IL_0027:  ldloca.s   V_0
    IL_0029:  ldarga.s   acc
    IL_002b:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0030:  ldarg.0
    IL_0031:  call       instance class IInterface InterfaceImpl::get_Other()
    IL_0036:  ldc.i4     0xffed2979
    IL_003b:  ldloc.0
    IL_003c:  ldarg.2
    IL_003d:  tail.
    IL_003f:  callvirt   instance int32 IInterface::CalcInterfaceOther(int32,
                                                                       valuetype S32,
                                                                       int32)
    IL_0044:  ret
  } // end of method InterfaceImpl::CalcInterface

  .method public hidebysig newslot virtual final 
          instance int32  CalcInterfaceOther(int32 x,
                                             valuetype S32 large,
                                             int32 acc) cil managed noinlining
  {
    // Code size       35 (0x23)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 InterfaceImpl::_x
    IL_0006:  ldarga.s   large
    IL_0008:  ldarga.s   acc
    IL_000a:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000f:  ldarg.0
    IL_0010:  call       instance class IInterface InterfaceImpl::get_Other()
    IL_0015:  ldc.i4     0xffed2979
    IL_001a:  ldarg.3
    IL_001b:  tail.
    IL_001d:  callvirt   instance int32 IInterface::CalcInterface(int32,
                                                                  int32)
    IL_0022:  ret
  } // end of method InterfaceImpl::CalcInterfaceOther

  .method public hidebysig newslot virtual final 
          instance valuetype S32  CalcInterfaceRetbuf(int32 x,
                                                      int32 acc) cil managed noinlining
  {
    // Code size       113 (0x71)
    .maxstack  4
    .locals init (valuetype S32 V_0,
             valuetype S32 V_1)
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4     0xffed2979
    IL_0006:  beq.s      IL_000f

    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      int32 InterfaceImpl::_x
    IL_000f:  ldarg.0
    IL_0010:  ldfld      int32 InterfaceImpl::_x
    IL_0015:  brtrue.s   IL_0045

    IL_0017:  ldloca.s   V_1
    IL_0019:  initobj    S32
    IL_001f:  ldloca.s   V_1
    IL_0021:  ldarg.2
    IL_0022:  conv.i8
    IL_0023:  stfld      int64 S32::A
    IL_0028:  ldloca.s   V_1
    IL_002a:  ldarg.2
    IL_002b:  conv.i8
    IL_002c:  stfld      int64 S32::B
    IL_0031:  ldloca.s   V_1
    IL_0033:  ldarg.2
    IL_0034:  conv.i8
    IL_0035:  stfld      int64 S32::C
    IL_003a:  ldloca.s   V_1
    IL_003c:  ldarg.2
    IL_003d:  conv.i8
    IL_003e:  stfld      int64 S32::D
    IL_0043:  ldloc.1
    IL_0044:  ret

    IL_0045:  ldloca.s   V_0
    IL_0047:  initobj    S32
    IL_004d:  ldarg.0
    IL_004e:  ldflda     int32 InterfaceImpl::_x
    IL_0053:  ldloca.s   V_0
    IL_0055:  ldarga.s   acc
    IL_0057:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_005c:  ldarg.0
    IL_005d:  call       instance class IInterface InterfaceImpl::get_Other()
    IL_0062:  ldc.i4     0xffed2979
    IL_0067:  ldloc.0
    IL_0068:  ldarg.2
    IL_0069:  tail.
    IL_006b:  callvirt   instance valuetype S32 IInterface::CalcInterfaceRetbufOther(int32,
                                                                                     valuetype S32,
                                                                                     int32)
    IL_0070:  ret
  } // end of method InterfaceImpl::CalcInterfaceRetbuf

  .method public hidebysig newslot virtual final 
          instance valuetype S32  CalcInterfaceRetbufOther(int32 x,
                                                           valuetype S32 large,
                                                           int32 acc) cil managed noinlining
  {
    // Code size       35 (0x23)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 InterfaceImpl::_x
    IL_0006:  ldarga.s   large
    IL_0008:  ldarga.s   acc
    IL_000a:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000f:  ldarg.0
    IL_0010:  call       instance class IInterface InterfaceImpl::get_Other()
    IL_0015:  ldc.i4     0xffed2979
    IL_001a:  ldarg.3
    IL_001b:  tail.
    IL_001d:  callvirt   instance valuetype S32 IInterface::CalcInterfaceRetbuf(int32,
                                                                                int32)
    IL_0022:  ret
  } // end of method InterfaceImpl::CalcInterfaceRetbufOther

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method InterfaceImpl::.ctor

  .property instance class IInterface Other()
  {
    .get instance class IInterface InterfaceImpl::get_Other()
    .set instance void InterfaceImpl::set_Other(class IInterface)
  } // end of property InterfaceImpl::Other
} // end of class InterfaceImpl

.class private auto ansi beforefieldinit GenInstance`2<T1,T2>
       extends [System.Runtime]System.Object
{
  .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 ) 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
  .param type T1 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
  .param type T2 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
  .method public hidebysig instance string 
          NonVirtForward<T3,T4>(!T1 a,
                                !T2 b,
                                !!T3 c,
                                !!T4 d) cil managed noinlining
  {
    .param type T3 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    .param type T4 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    // Code size       23 (0x17)
    .maxstack  6
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldloca.s   V_0
    IL_0003:  initobj    S32
    IL_0009:  ldloc.0
    IL_000a:  ldarg.1
    IL_000b:  ldarg.2
    IL_000c:  ldarg.3
    IL_000d:  ldarg.s    d
    IL_000f:  tail.
    IL_0011:  callvirt   instance string class GenInstance`2<!T1,!T2>::NonVirt<!!0,!!1>(valuetype S32,
                                                                                        !0,
                                                                                        !1,
                                                                                        !!0,
                                                                                        !!1)
    IL_0016:  ret
  } // end of method GenInstance`2::NonVirtForward

  .method public hidebysig instance string 
          VirtForward<T3,T4>(!T1 a,
                             !T2 b,
                             !!T3 c,
                             !!T4 d) cil managed noinlining
  {
    .param type T3 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    .param type T4 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    // Code size       23 (0x17)
    .maxstack  6
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldloca.s   V_0
    IL_0003:  initobj    S32
    IL_0009:  ldloc.0
    IL_000a:  ldarg.1
    IL_000b:  ldarg.2
    IL_000c:  ldarg.3
    IL_000d:  ldarg.s    d
    IL_000f:  tail.
    IL_0011:  callvirt   instance string class GenInstance`2<!T1,!T2>::Virt<!!0,!!1>(valuetype S32,
                                                                                     !0,
                                                                                     !1,
                                                                                     !!0,
                                                                                     !!1)
    IL_0016:  ret
  } // end of method GenInstance`2::VirtForward

  .method public hidebysig instance string 
          NonVirt<T3,T4>(valuetype S32 s,
                         !T1 a,
                         !T2 b,
                         !!T3 c,
                         !!T4 d) cil managed
  {
    .param type T3 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    .param type T4 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    // Code size       223 (0xdf)
    .maxstack  3
    .locals init (valuetype [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  ldc.i4.7
    IL_0003:  ldc.i4.8
    IL_0004:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::.ctor(int32,
                                                                                                                               int32)
    IL_0009:  ldloca.s   V_0
    IL_000b:  ldtoken    !T1
    IL_0010:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0015:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_001a:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_001f:  ldloca.s   V_0
    IL_0021:  ldstr      " "
    IL_0026:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_002b:  ldloca.s   V_0
    IL_002d:  ldtoken    !T2
    IL_0032:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0037:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_003c:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_0041:  ldloca.s   V_0
    IL_0043:  ldstr      " "
    IL_0048:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_004d:  ldloca.s   V_0
    IL_004f:  ldtoken    !!T3
    IL_0054:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0059:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_005e:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_0063:  ldloca.s   V_0
    IL_0065:  ldstr      " "
    IL_006a:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_006f:  ldloca.s   V_0
    IL_0071:  ldtoken    !!T4
    IL_0076:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_007b:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_0080:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_0085:  ldloca.s   V_0
    IL_0087:  ldstr      " "
    IL_008c:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_0091:  ldloca.s   V_0
    IL_0093:  ldarg.2
    IL_0094:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<!T1>(!!0)
    IL_0099:  ldloca.s   V_0
    IL_009b:  ldstr      " "
    IL_00a0:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_00a5:  ldloca.s   V_0
    IL_00a7:  ldarg.3
    IL_00a8:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<!T2>(!!0)
    IL_00ad:  ldloca.s   V_0
    IL_00af:  ldstr      " "
    IL_00b4:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_00b9:  ldloca.s   V_0
    IL_00bb:  ldarg.s    c
    IL_00bd:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<!!0>(!!0)
    IL_00c2:  ldloca.s   V_0
    IL_00c4:  ldstr      " "
    IL_00c9:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_00ce:  ldloca.s   V_0
    IL_00d0:  ldarg.s    d
    IL_00d2:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<!!1>(!!0)
    IL_00d7:  ldloca.s   V_0
    IL_00d9:  call       instance string [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::ToStringAndClear()
    IL_00de:  ret
  } // end of method GenInstance`2::NonVirt

  .method public hidebysig newslot virtual 
          instance string  Virt<T3,T4>(valuetype S32 s,
                                       !T1 a,
                                       !T2 b,
                                       !!T3 c,
                                       !!T4 d) cil managed
  {
    .param type T3 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    .param type T4 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    // Code size       223 (0xdf)
    .maxstack  3
    .locals init (valuetype [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  ldc.i4.7
    IL_0003:  ldc.i4.8
    IL_0004:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::.ctor(int32,
                                                                                                                               int32)
    IL_0009:  ldloca.s   V_0
    IL_000b:  ldtoken    !T1
    IL_0010:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0015:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_001a:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_001f:  ldloca.s   V_0
    IL_0021:  ldstr      " "
    IL_0026:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_002b:  ldloca.s   V_0
    IL_002d:  ldtoken    !T2
    IL_0032:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0037:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_003c:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_0041:  ldloca.s   V_0
    IL_0043:  ldstr      " "
    IL_0048:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_004d:  ldloca.s   V_0
    IL_004f:  ldtoken    !!T3
    IL_0054:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0059:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_005e:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_0063:  ldloca.s   V_0
    IL_0065:  ldstr      " "
    IL_006a:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_006f:  ldloca.s   V_0
    IL_0071:  ldtoken    !!T4
    IL_0076:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_007b:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_0080:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_0085:  ldloca.s   V_0
    IL_0087:  ldstr      " "
    IL_008c:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_0091:  ldloca.s   V_0
    IL_0093:  ldarg.2
    IL_0094:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<!T1>(!!0)
    IL_0099:  ldloca.s   V_0
    IL_009b:  ldstr      " "
    IL_00a0:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_00a5:  ldloca.s   V_0
    IL_00a7:  ldarg.3
    IL_00a8:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<!T2>(!!0)
    IL_00ad:  ldloca.s   V_0
    IL_00af:  ldstr      " "
    IL_00b4:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_00b9:  ldloca.s   V_0
    IL_00bb:  ldarg.s    c
    IL_00bd:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<!!0>(!!0)
    IL_00c2:  ldloca.s   V_0
    IL_00c4:  ldstr      " "
    IL_00c9:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_00ce:  ldloca.s   V_0
    IL_00d0:  ldarg.s    d
    IL_00d2:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<!!1>(!!0)
    IL_00d7:  ldloca.s   V_0
    IL_00d9:  call       instance string [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::ToStringAndClear()
    IL_00de:  ret
  } // end of method GenInstance`2::Virt

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method GenInstance`2::.ctor

} // end of class GenInstance`2

.class interface private abstract auto ansi IGenInterface`2<T1,T2>
{
  .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 ) 
  .param type T1 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
  .param type T2 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance string  F<T3,T4>(valuetype S32 s,
                                    !T1 a,
                                    !T2 b,
                                    !!T3 c,
                                    !!T4 d) cil managed
  {
    .param type T3 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    .param type T4 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
  } // end of method IGenInterface`2::F

  .method public hidebysig newslot abstract virtual 
          instance string  G(valuetype S32 s,
                             !T1 a,
                             !T2 b) cil managed
  {
  } // end of method IGenInterface`2::G

} // end of class IGenInterface`2

.class private auto ansi beforefieldinit GenInterfaceImpl`2<T1,T2>
       extends [System.Runtime]System.Object
       implements class IGenInterface`2<!T1,!T2>
{
  .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 ) 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
  .interfaceimpl type class IGenInterface`2<!T1,!T2>
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 03 00 00 00 00 01 01 00 00 ) 
  .param type T1 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
  .param type T2 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
  .method public hidebysig newslot virtual final 
          instance string  F<T3,T4>(valuetype S32 s,
                                    !T1 a,
                                    !T2 b,
                                    !!T3 c,
                                    !!T4 d) cil managed
  {
    .param type T3 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    .param type T4 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    // Code size       223 (0xdf)
    .maxstack  3
    .locals init (valuetype [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  ldc.i4.7
    IL_0003:  ldc.i4.8
    IL_0004:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::.ctor(int32,
                                                                                                                               int32)
    IL_0009:  ldloca.s   V_0
    IL_000b:  ldtoken    !T1
    IL_0010:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0015:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_001a:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_001f:  ldloca.s   V_0
    IL_0021:  ldstr      " "
    IL_0026:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_002b:  ldloca.s   V_0
    IL_002d:  ldtoken    !T2
    IL_0032:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0037:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_003c:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_0041:  ldloca.s   V_0
    IL_0043:  ldstr      " "
    IL_0048:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_004d:  ldloca.s   V_0
    IL_004f:  ldtoken    !!T3
    IL_0054:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0059:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_005e:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_0063:  ldloca.s   V_0
    IL_0065:  ldstr      " "
    IL_006a:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_006f:  ldloca.s   V_0
    IL_0071:  ldtoken    !!T4
    IL_0076:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_007b:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_0080:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_0085:  ldloca.s   V_0
    IL_0087:  ldstr      " "
    IL_008c:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_0091:  ldloca.s   V_0
    IL_0093:  ldarg.2
    IL_0094:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<!T1>(!!0)
    IL_0099:  ldloca.s   V_0
    IL_009b:  ldstr      " "
    IL_00a0:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_00a5:  ldloca.s   V_0
    IL_00a7:  ldarg.3
    IL_00a8:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<!T2>(!!0)
    IL_00ad:  ldloca.s   V_0
    IL_00af:  ldstr      " "
    IL_00b4:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_00b9:  ldloca.s   V_0
    IL_00bb:  ldarg.s    c
    IL_00bd:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<!!0>(!!0)
    IL_00c2:  ldloca.s   V_0
    IL_00c4:  ldstr      " "
    IL_00c9:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_00ce:  ldloca.s   V_0
    IL_00d0:  ldarg.s    d
    IL_00d2:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<!!1>(!!0)
    IL_00d7:  ldloca.s   V_0
    IL_00d9:  call       instance string [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::ToStringAndClear()
    IL_00de:  ret
  } // end of method GenInterfaceImpl`2::F

  .method public hidebysig newslot virtual final 
          instance string  G(valuetype S32 s,
                             !T1 a,
                             !T2 b) cil managed
  {
    // Code size       113 (0x71)
    .maxstack  3
    .locals init (valuetype [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  ldc.i4.3
    IL_0003:  ldc.i4.4
    IL_0004:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::.ctor(int32,
                                                                                                                               int32)
    IL_0009:  ldloca.s   V_0
    IL_000b:  ldtoken    !T1
    IL_0010:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0015:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_001a:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_001f:  ldloca.s   V_0
    IL_0021:  ldstr      " "
    IL_0026:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_002b:  ldloca.s   V_0
    IL_002d:  ldtoken    !T2
    IL_0032:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0037:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_003c:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted(string)
    IL_0041:  ldloca.s   V_0
    IL_0043:  ldstr      " "
    IL_0048:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_004d:  ldloca.s   V_0
    IL_004f:  ldarg.2
    IL_0050:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<!T1>(!!0)
    IL_0055:  ldloca.s   V_0
    IL_0057:  ldstr      " "
    IL_005c:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendLiteral(string)
    IL_0061:  ldloca.s   V_0
    IL_0063:  ldarg.3
    IL_0064:  call       instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<!T2>(!!0)
    IL_0069:  ldloca.s   V_0
    IL_006b:  call       instance string [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::ToStringAndClear()
    IL_0070:  ret
  } // end of method GenInterfaceImpl`2::G

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method GenInterfaceImpl`2::.ctor

} // end of class GenInterfaceImpl`2

.class private abstract auto ansi beforefieldinit GenAbstract`1<T1>
       extends [System.Runtime]System.Object
{
  .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 ) 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
  .param type T1 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
  .method public hidebysig newslot abstract virtual 
          instance string  F<T2>() cil managed
  {
    .param type T2 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
  } // end of method GenAbstract`1::F

  .method public hidebysig newslot abstract virtual 
          instance string  G() cil managed
  {
  } // end of method GenAbstract`1::G

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method GenAbstract`1::.ctor

} // end of class GenAbstract`1

.class private auto ansi beforefieldinit GenAbstractImpl`1<T1>
       extends class GenAbstract`1<!T1>
{
  .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 ) 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 02 00 00 00 00 01 00 00 ) 
  .param type T1 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
  .method public hidebysig virtual instance string 
          F<T2>() cil managed
  {
    .param type T2 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    // Code size       41 (0x29)
    .maxstack  8
    IL_0000:  ldtoken    !T1
    IL_0005:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_000a:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_000f:  ldstr      " "
    IL_0014:  ldtoken    !!T2
    IL_0019:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_001e:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_0023:  call       string [System.Runtime]System.String::Concat(string,
                                                                      string,
                                                                      string)
    IL_0028:  ret
  } // end of method GenAbstractImpl`1::F

  .method public hidebysig virtual instance string 
          G() cil managed
  {
    // Code size       25 (0x19)
    .maxstack  8
    IL_0000:  ldtoken    !T1
    IL_0005:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_000a:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_000f:  dup
    IL_0010:  brtrue.s   IL_0018

    IL_0012:  pop
    IL_0013:  ldstr      ""
    IL_0018:  ret
  } // end of method GenAbstractImpl`1::G

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class GenAbstract`1<!T1>::.ctor()
    IL_0006:  ret
  } // end of method GenAbstractImpl`1::.ctor

} // end of class GenAbstractImpl`1

.class private auto ansi more_tailcalls_ProcessedByFody
       extends [System.Runtime]System.Object
{
  .field static assembly literal string FodyVersion = "6.6.4.0"
  .field static assembly literal string InlineIL = "1.7.4.0"
} // end of class more_tailcalls_ProcessedByFody


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
