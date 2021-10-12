// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// Metadata version: v4.0.30319
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 6:0:0:0
}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 6:0:0:0
}
.assembly UnconstrainedCallToConstrainedMethod
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                   63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [System.Runtime]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 

  .custom instance void [System.Runtime]System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(string) = ( 01 00 18 2E 4E 45 54 43 6F 72 65 41 70 70 2C 56   // ....NETCoreApp,V
                                                                                                              65 72 73 69 6F 6E 3D 76 36 2E 30 01 00 54 0E 14   // ersion=v6.0..T..
                                                                                                              46 72 61 6D 65 77 6F 72 6B 44 69 73 70 6C 61 79   // FrameworkDisplay
                                                                                                              4E 61 6D 65 00 )                                  // Name.
  .custom instance void [System.Runtime]System.Reflection.AssemblyCompanyAttribute::.ctor(string) = ( 01 00 24 55 6E 63 6F 6E 73 74 72 61 69 6E 65 64   // ..$Unconstrained
                                                                                                      43 61 6C 6C 54 6F 43 6F 6E 73 74 72 61 69 6E 65   // CallToConstraine
                                                                                                      64 4D 65 74 68 6F 64 00 00 )                      // dMethod..
  .custom instance void [System.Runtime]System.Reflection.AssemblyConfigurationAttribute::.ctor(string) = ( 01 00 05 44 65 62 75 67 00 00 )                   // ...Debug..
  .custom instance void [System.Runtime]System.Reflection.AssemblyFileVersionAttribute::.ctor(string) = ( 01 00 07 31 2E 30 2E 30 2E 30 00 00 )             // ...1.0.0.0..
  .custom instance void [System.Runtime]System.Reflection.AssemblyInformationalVersionAttribute::.ctor(string) = ( 01 00 05 31 2E 30 2E 30 00 00 )                   // ...1.0.0..
  .custom instance void [System.Runtime]System.Reflection.AssemblyProductAttribute::.ctor(string) = ( 01 00 24 55 6E 63 6F 6E 73 74 72 61 69 6E 65 64   // ..$Unconstrained
                                                                                                      43 61 6C 6C 54 6F 43 6F 6E 73 74 72 61 69 6E 65   // CallToConstraine
                                                                                                      64 4D 65 74 68 6F 64 00 00 )                      // dMethod..
  .custom instance void [System.Runtime]System.Reflection.AssemblyTitleAttribute::.ctor(string) = ( 01 00 24 55 6E 63 6F 6E 73 74 72 61 69 6E 65 64   // ..$Unconstrained
                                                                                                    43 61 6C 6C 54 6F 43 6F 6E 73 74 72 61 69 6E 65   // CallToConstraine
                                                                                                    64 4D 65 74 68 6F 64 00 00 )                      // dMethod..
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
.module UnconstrainedCallToConstrainedMethod.dll
// MVID: {A27E6A78-61EA-45D7-BFDB-750E8567BFCC}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x07220000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi sealed beforefieldinit Microsoft.CodeAnalysis.EmbeddedAttribute
       extends [System.Runtime]System.Attribute
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  ret
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
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  ldarg.0
    IL_0008:  ldc.i4.1
    IL_0009:  newarr     [System.Runtime]System.Byte
    IL_000e:  dup
    IL_000f:  ldc.i4.0
    IL_0010:  ldarg.1
    IL_0011:  stelem.i1
    IL_0012:  stfld      uint8[] System.Runtime.CompilerServices.NullableAttribute::NullableFlags
    IL_0017:  ret
  } // end of method NullableAttribute::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(uint8[] A_1) cil managed
  {
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  ldarg.0
    IL_0008:  ldarg.1
    IL_0009:  stfld      uint8[] System.Runtime.CompilerServices.NullableAttribute::NullableFlags
    IL_000e:  ret
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
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  ldarg.0
    IL_0008:  ldarg.1
    IL_0009:  stfld      uint8 System.Runtime.CompilerServices.NullableContextAttribute::Flag
    IL_000e:  ret
  } // end of method NullableContextAttribute::.ctor

} // end of class System.Runtime.CompilerServices.NullableContextAttribute

.class interface private abstract auto ansi UnconstrainedCallToConstrainedMethod.IConstraint`1<T>
{
  .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
} // end of class UnconstrainedCallToConstrainedMethod.IConstraint`1

.class interface private abstract auto ansi UnconstrainedCallToConstrainedMethod.IConstraintCurious`1<(class UnconstrainedCallToConstrainedMethod.IConstraintCurious`1<!T>) T>
{
} // end of class UnconstrainedCallToConstrainedMethod.IConstraintCurious`1

.class private auto ansi beforefieldinit UnconstrainedCallToConstrainedMethod.ClassThatImplementsConstraintCurious
       extends [System.Runtime]System.Object
       implements class UnconstrainedCallToConstrainedMethod.IConstraintCurious`1<class UnconstrainedCallToConstrainedMethod.ClassThatImplementsConstraintCurious>
{
  .interfaceimpl type class UnconstrainedCallToConstrainedMethod.IConstraintCurious`1<class UnconstrainedCallToConstrainedMethod.ClassThatImplementsConstraintCurious>
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 02 00 00 00 00 01 00 00 ) 
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method ClassThatImplementsConstraintCurious::.ctor

} // end of class UnconstrainedCallToConstrainedMethod.ClassThatImplementsConstraintCurious

.class private auto ansi beforefieldinit UnconstrainedCallToConstrainedMethod.ClassThatImplementsConstraint
       extends [System.Runtime]System.Object
       implements class UnconstrainedCallToConstrainedMethod.IConstraint`1<object>
{
  .interfaceimpl type class UnconstrainedCallToConstrainedMethod.IConstraint`1<object>
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 02 00 00 00 00 01 00 00 ) 
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method ClassThatImplementsConstraint::.ctor

} // end of class UnconstrainedCallToConstrainedMethod.ClassThatImplementsConstraint

.class private sequential ansi sealed beforefieldinit UnconstrainedCallToConstrainedMethod.StructThatImplementsConstraint
       extends [System.Runtime]System.ValueType
       implements class UnconstrainedCallToConstrainedMethod.IConstraint`1<int32>,
                  class UnconstrainedCallToConstrainedMethod.IConstraint`1<object>
{
  .pack 0
  .size 1
  .interfaceimpl type class UnconstrainedCallToConstrainedMethod.IConstraint`1<object>
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8[]) = ( 01 00 02 00 00 00 00 01 00 00 ) 
} // end of class UnconstrainedCallToConstrainedMethod.StructThatImplementsConstraint

.class private sequential ansi sealed beforefieldinit UnconstrainedCallToConstrainedMethod.StructThatImplementsConstraintCurious
       extends [System.Runtime]System.ValueType
       implements class UnconstrainedCallToConstrainedMethod.IConstraintCurious`1<valuetype UnconstrainedCallToConstrainedMethod.StructThatImplementsConstraintCurious>
{
  .pack 0
  .size 1
} // end of class UnconstrainedCallToConstrainedMethod.StructThatImplementsConstraintCurious

.class private auto ansi beforefieldinit UnconstrainedCallToConstrainedMethod.TestClass
       extends [System.Runtime]System.Object
{
  .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
  .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
  .field private static int32 s_failures
  .method private hidebysig static void  TestHelperMethodRequiresConstraint_TEMPORARY<T,U>(bool expectSuccess) cil managed
  {
    // Code size       91 (0x5b)
    .maxstack  4
    IL_0000:  nop
    IL_0001:  ldsfld     int32 UnconstrainedCallToConstrainedMethod.TestClass::s_failures
    IL_0006:  ldc.i4.1
    IL_0007:  add
    IL_0008:  stsfld     int32 UnconstrainedCallToConstrainedMethod.TestClass::s_failures
    IL_000d:  ldc.i4.5
    IL_000e:  newarr     [System.Runtime]System.String
    IL_0013:  dup
    IL_0014:  ldc.i4.0
    IL_0015:  ldstr      "FAILED Called TestHelperMethodRequiresConstraint_T"
    + "EMPORARY<"
    IL_001a:  stelem.ref
    IL_001b:  dup
    IL_001c:  ldc.i4.1
    IL_001d:  ldtoken    !!T
    IL_0022:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0027:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_002c:  stelem.ref
    IL_002d:  dup
    IL_002e:  ldc.i4.2
    IL_002f:  ldstr      ", "
    IL_0034:  stelem.ref
    IL_0035:  dup
    IL_0036:  ldc.i4.3
    IL_0037:  ldtoken    !!U
    IL_003c:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0041:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_0046:  stelem.ref
    IL_0047:  dup
    IL_0048:  ldc.i4.4
    IL_0049:  ldstr      ">"
    IL_004e:  stelem.ref
    IL_004f:  call       string [System.Runtime]System.String::Concat(string[])
    IL_0054:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0059:  nop
    IL_005a:  ret
  } // end of method TestClass::TestHelperMethodRequiresConstraint_TEMPORARY

  .method private hidebysig static void  TestHelperMethodRequiresConstraintCurious_TEMPORARY<T>(bool expectSuccess) cil managed
  {
    // Code size       50 (0x32)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldsfld     int32 UnconstrainedCallToConstrainedMethod.TestClass::s_failures
    IL_0006:  ldc.i4.1
    IL_0007:  add
    IL_0008:  stsfld     int32 UnconstrainedCallToConstrainedMethod.TestClass::s_failures
    IL_000d:  ldstr      "FAILED Called TestHelperMethodRequiresConstraintCu"
    + "rious_TEMPORARY<"
    IL_0012:  ldtoken    !!T
    IL_0017:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_001c:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_0021:  ldstr      ">"
    IL_0026:  call       string [System.Runtime]System.String::Concat(string,
                                                                      string,
                                                                      string)
    IL_002b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0030:  nop
    IL_0031:  ret
  } // end of method TestClass::TestHelperMethodRequiresConstraintCurious_TEMPORARY

  .method private hidebysig static void  TestHelperMethodRequiresConstraint<(class UnconstrainedCallToConstrainedMethod.IConstraint`1<!!U>) T,U>(bool expectSuccess) cil managed
  {
    .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .custom instance void System.Runtime.CompilerServices.ConvertUnconstrainedCallsToThrowInvalidProgramExceptionAttribute::.ctor() = ( 01 00 00 00 ) 
    .param type U 
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = ( 01 00 02 00 00 ) 
    // Code size       177 (0xb1)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  ldloc.0
    IL_0004:  brfalse.s  IL_0055

    IL_0006:  ldc.i4.5
    IL_0007:  newarr     [System.Runtime]System.String
    IL_000c:  dup
    IL_000d:  ldc.i4.0
    IL_000e:  ldstr      "SUCCESS Called TesHelpertMethodRequiresConstraint<"
    IL_0013:  stelem.ref
    IL_0014:  dup
    IL_0015:  ldc.i4.1
    IL_0016:  ldtoken    !!T
    IL_001b:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0020:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_0025:  stelem.ref
    IL_0026:  dup
    IL_0027:  ldc.i4.2
    IL_0028:  ldstr      ", "
    IL_002d:  stelem.ref
    IL_002e:  dup
    IL_002f:  ldc.i4.3
    IL_0030:  ldtoken    !!U
    IL_0035:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_003a:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_003f:  stelem.ref
    IL_0040:  dup
    IL_0041:  ldc.i4.4
    IL_0042:  ldstr      ">"
    IL_0047:  stelem.ref
    IL_0048:  call       string [System.Runtime]System.String::Concat(string[])
    IL_004d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0052:  nop
    IL_0053:  br.s       IL_00b0

    IL_0055:  nop
    IL_0056:  ldsfld     int32 UnconstrainedCallToConstrainedMethod.TestClass::s_failures
    IL_005b:  ldc.i4.1
    IL_005c:  add
    IL_005d:  stsfld     int32 UnconstrainedCallToConstrainedMethod.TestClass::s_failures
    IL_0062:  ldc.i4.5
    IL_0063:  newarr     [System.Runtime]System.String
    IL_0068:  dup
    IL_0069:  ldc.i4.0
    IL_006a:  ldstr      "FAILED Called TestHelperMethodRequiresConstraint<"
    IL_006f:  stelem.ref
    IL_0070:  dup
    IL_0071:  ldc.i4.1
    IL_0072:  ldtoken    !!T
    IL_0077:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_007c:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_0081:  stelem.ref
    IL_0082:  dup
    IL_0083:  ldc.i4.2
    IL_0084:  ldstr      ", "
    IL_0089:  stelem.ref
    IL_008a:  dup
    IL_008b:  ldc.i4.3
    IL_008c:  ldtoken    !!U
    IL_0091:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0096:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_009b:  stelem.ref
    IL_009c:  dup
    IL_009d:  ldc.i4.4
    IL_009e:  ldstr      ">"
    IL_00a3:  stelem.ref
    IL_00a4:  call       string [System.Runtime]System.String::Concat(string[])
    IL_00a9:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00ae:  nop
    IL_00af:  nop
    IL_00b0:  ret
  } // end of method TestClass::TestHelperMethodRequiresConstraint

  .method private hidebysig static void  TestHelperMethodRequiresConstraintCurious<(class UnconstrainedCallToConstrainedMethod.IConstraintCurious`1<!!T>) T>(bool expectSuccess) cil managed
  {
    .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 00 00 00 ) 
    .custom instance void System.Runtime.CompilerServices.ConvertUnconstrainedCallsToThrowInvalidProgramExceptionAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       95 (0x5f)
    .maxstack  3
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  ldloc.0
    IL_0004:  brfalse.s  IL_002c

    IL_0006:  ldstr      "SUCCESS Called TestHelperMethodRequiresConstraintC"
    + "urious<"
    IL_000b:  ldtoken    !!T
    IL_0010:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0015:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_001a:  ldstr      ">"
    IL_001f:  call       string [System.Runtime]System.String::Concat(string,
                                                                      string,
                                                                      string)
    IL_0024:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0029:  nop
    IL_002a:  br.s       IL_005e

    IL_002c:  nop
    IL_002d:  ldsfld     int32 UnconstrainedCallToConstrainedMethod.TestClass::s_failures
    IL_0032:  ldc.i4.1
    IL_0033:  add
    IL_0034:  stsfld     int32 UnconstrainedCallToConstrainedMethod.TestClass::s_failures
    IL_0039:  ldstr      "FAILED Called TestHelperMethodRequiresConstraintCu"
    + "rious<"
    IL_003e:  ldtoken    !!T
    IL_0043:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0048:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_004d:  ldstr      ">"
    IL_0052:  call       string [System.Runtime]System.String::Concat(string,
                                                                      string,
                                                                      string)
    IL_0057:  call       void [System.Console]System.Console::WriteLine(string)
    IL_005c:  nop
    IL_005d:  nop
    IL_005e:  ret
  } // end of method TestClass::TestHelperMethodRequiresConstraintCurious

  .method private hidebysig static void  ConstraintTest<T,U>(bool expectSuccess) cil managed noinlining
  {
    // Code size       198 (0xc6)
    .maxstack  4
    .locals init (bool V_0)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  ldarg.0
      IL_0003:  call       void UnconstrainedCallToConstrainedMethod.TestClass::TestHelperMethodRequiresConstraint<!!0,!!1>(bool)
      IL_0008:  nop
      IL_0009:  nop
      IL_000a:  leave      IL_00c5

    }  // end .try
    catch [System.Runtime]System.InvalidProgramException 
    {
      IL_000f:  pop
      IL_0010:  nop
      IL_0011:  ldarg.0
      IL_0012:  stloc.0
      IL_0013:  ldloc.0
      IL_0014:  brfalse.s  IL_0073

      IL_0016:  nop
      IL_0017:  ldsfld     int32 UnconstrainedCallToConstrainedMethod.TestClass::s_failures
      IL_001c:  ldc.i4.1
      IL_001d:  add
      IL_001e:  stsfld     int32 UnconstrainedCallToConstrainedMethod.TestClass::s_failures
      IL_0023:  ldc.i4.5
      IL_0024:  newarr     [System.Runtime]System.String
      IL_0029:  dup
      IL_002a:  ldc.i4.0
      IL_002b:  ldstr      "FAILED Threw Exception ConstraintTest<"
      IL_0030:  stelem.ref
      IL_0031:  dup
      IL_0032:  ldc.i4.1
      IL_0033:  ldtoken    !!T
      IL_0038:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
      IL_003d:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
      IL_0042:  stelem.ref
      IL_0043:  dup
      IL_0044:  ldc.i4.2
      IL_0045:  ldstr      ", "
      IL_004a:  stelem.ref
      IL_004b:  dup
      IL_004c:  ldc.i4.3
      IL_004d:  ldtoken    !!U
      IL_0052:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
      IL_0057:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
      IL_005c:  stelem.ref
      IL_005d:  dup
      IL_005e:  ldc.i4.4
      IL_005f:  ldstr      ">"
      IL_0064:  stelem.ref
      IL_0065:  call       string [System.Runtime]System.String::Concat(string[])
      IL_006a:  call       void [System.Console]System.Console::WriteLine(string)
      IL_006f:  nop
      IL_0070:  nop
      IL_0071:  br.s       IL_00c2

      IL_0073:  nop
      IL_0074:  ldc.i4.5
      IL_0075:  newarr     [System.Runtime]System.String
      IL_007a:  dup
      IL_007b:  ldc.i4.0
      IL_007c:  ldstr      "SUCCESS Threw Exception ConstraintTest<"
      IL_0081:  stelem.ref
      IL_0082:  dup
      IL_0083:  ldc.i4.1
      IL_0084:  ldtoken    !!T
      IL_0089:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
      IL_008e:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
      IL_0093:  stelem.ref
      IL_0094:  dup
      IL_0095:  ldc.i4.2
      IL_0096:  ldstr      ", "
      IL_009b:  stelem.ref
      IL_009c:  dup
      IL_009d:  ldc.i4.3
      IL_009e:  ldtoken    !!U
      IL_00a3:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
      IL_00a8:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
      IL_00ad:  stelem.ref
      IL_00ae:  dup
      IL_00af:  ldc.i4.4
      IL_00b0:  ldstr      ">"
      IL_00b5:  stelem.ref
      IL_00b6:  call       string [System.Runtime]System.String::Concat(string[])
      IL_00bb:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00c0:  nop
      IL_00c1:  nop
      IL_00c2:  nop
      IL_00c3:  leave.s    IL_00c5

    }  // end handler
    IL_00c5:  ret
  } // end of method TestClass::ConstraintTest

  .method private hidebysig static void  ConstraintCuriousTest<T>(bool expectSuccess) cil managed noinlining
  {
    // Code size       113 (0x71)
    .maxstack  3
    .locals init (bool V_0)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  ldarg.0
      IL_0003:  call       void UnconstrainedCallToConstrainedMethod.TestClass::TestHelperMethodRequiresConstraintCurious<!!0>(bool)
      IL_0008:  nop
      IL_0009:  nop
      IL_000a:  leave.s    IL_0070

    }  // end .try
    catch [System.Runtime]System.InvalidProgramException 
    {
      IL_000c:  pop
      IL_000d:  nop
      IL_000e:  ldarg.0
      IL_000f:  stloc.0
      IL_0010:  ldloc.0
      IL_0011:  brfalse.s  IL_0047

      IL_0013:  nop
      IL_0014:  ldsfld     int32 UnconstrainedCallToConstrainedMethod.TestClass::s_failures
      IL_0019:  ldc.i4.1
      IL_001a:  add
      IL_001b:  stsfld     int32 UnconstrainedCallToConstrainedMethod.TestClass::s_failures
      IL_0020:  ldstr      "FAILED Threw Exception ConstraintCuriousTest<"
      IL_0025:  ldtoken    !!T
      IL_002a:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
      IL_002f:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
      IL_0034:  ldstr      ">"
      IL_0039:  call       string [System.Runtime]System.String::Concat(string,
                                                                        string,
                                                                        string)
      IL_003e:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0043:  nop
      IL_0044:  nop
      IL_0045:  br.s       IL_006d

      IL_0047:  nop
      IL_0048:  ldstr      "SUCCESS Threw Exception ConstraintCuriousTest<"
      IL_004d:  ldtoken    !!T
      IL_0052:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
      IL_0057:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
      IL_005c:  ldstr      ">"
      IL_0061:  call       string [System.Runtime]System.String::Concat(string,
                                                                        string,
                                                                        string)
      IL_0066:  call       void [System.Console]System.Console::WriteLine(string)
      IL_006b:  nop
      IL_006c:  nop
      IL_006d:  nop
      IL_006e:  leave.s    IL_0070

    }  // end handler
    IL_0070:  ret
  } // end of method TestClass::ConstraintCuriousTest

  .method private hidebysig static int32 
          Main() cil managed
  {
    .entrypoint
    // Code size       114 (0x72)
    .maxstack  2
    .locals init (bool V_0,
             int32 V_1)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  call       void UnconstrainedCallToConstrainedMethod.TestClass::ConstraintTest<class UnconstrainedCallToConstrainedMethod.ClassThatImplementsConstraint,object>(bool)
    IL_0007:  nop
    IL_0008:  ldc.i4.0
    IL_0009:  call       void UnconstrainedCallToConstrainedMethod.TestClass::ConstraintTest<class UnconstrainedCallToConstrainedMethod.ClassThatImplementsConstraint,int32>(bool)
    IL_000e:  nop
    IL_000f:  ldc.i4.1
    IL_0010:  call       void UnconstrainedCallToConstrainedMethod.TestClass::ConstraintTest<valuetype UnconstrainedCallToConstrainedMethod.StructThatImplementsConstraint,int32>(bool)
    IL_0015:  nop
    IL_0016:  ldc.i4.1
    IL_0017:  call       void UnconstrainedCallToConstrainedMethod.TestClass::ConstraintTest<valuetype UnconstrainedCallToConstrainedMethod.StructThatImplementsConstraint,object>(bool)
    IL_001c:  nop
    IL_001d:  ldc.i4.0
    IL_001e:  call       void UnconstrainedCallToConstrainedMethod.TestClass::ConstraintTest<valuetype UnconstrainedCallToConstrainedMethod.StructThatImplementsConstraint,int16>(bool)
    IL_0023:  nop
    IL_0024:  ldc.i4.0
    IL_0025:  call       void UnconstrainedCallToConstrainedMethod.TestClass::ConstraintCuriousTest<class UnconstrainedCallToConstrainedMethod.ClassThatImplementsConstraint>(bool)
    IL_002a:  nop
    IL_002b:  ldc.i4.1
    IL_002c:  call       void UnconstrainedCallToConstrainedMethod.TestClass::ConstraintCuriousTest<class UnconstrainedCallToConstrainedMethod.ClassThatImplementsConstraintCurious>(bool)
    IL_0031:  nop
    IL_0032:  ldc.i4.0
    IL_0033:  call       void UnconstrainedCallToConstrainedMethod.TestClass::ConstraintCuriousTest<valuetype UnconstrainedCallToConstrainedMethod.StructThatImplementsConstraint>(bool)
    IL_0038:  nop
    IL_0039:  ldc.i4.1
    IL_003a:  call       void UnconstrainedCallToConstrainedMethod.TestClass::ConstraintCuriousTest<valuetype UnconstrainedCallToConstrainedMethod.StructThatImplementsConstraintCurious>(bool)
    IL_003f:  nop
    IL_0040:  ldsfld     int32 UnconstrainedCallToConstrainedMethod.TestClass::s_failures
    IL_0045:  ldc.i4.0
    IL_0046:  cgt.un
    IL_0048:  stloc.0
    IL_0049:  ldloc.0
    IL_004a:  brfalse.s  IL_006b

    IL_004c:  nop
    IL_004d:  ldstr      "{0} Failures"
    IL_0052:  ldsfld     int32 UnconstrainedCallToConstrainedMethod.TestClass::s_failures
    IL_0057:  box        [System.Runtime]System.Int32
    IL_005c:  call       string [System.Runtime]System.String::Format(string,
                                                                      object)
    IL_0061:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0066:  nop
    IL_0067:  ldc.i4.1
    IL_0068:  stloc.1
    IL_0069:  br.s       IL_0070

    IL_006b:  ldc.i4.s   100
    IL_006d:  stloc.1
    IL_006e:  br.s       IL_0070

    IL_0070:  ldloc.1
    IL_0071:  ret
  } // end of method TestClass::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method TestClass::.ctor

} // end of class UnconstrainedCallToConstrainedMethod.TestClass

.class private auto ansi beforefieldinit System.Runtime.CompilerServices.ConvertUnconstrainedCallsToThrowInvalidProgramExceptionAttribute
       extends [System.Runtime]System.Attribute
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method ConvertUnconstrainedCallsToThrowInvalidProgramExceptionAttribute::.ctor

} // end of class System.Runtime.CompilerServices.ConvertUnconstrainedCallsToThrowInvalidProgramExceptionAttribute

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file UnconstrainedCallToConstrainedMethod.res
