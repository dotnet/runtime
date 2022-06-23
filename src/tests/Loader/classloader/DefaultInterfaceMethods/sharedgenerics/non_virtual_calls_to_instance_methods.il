// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 4:2:1:0
}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 4:1:1:0
}
.assembly non_virtual_calls_to_instance_methods
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                   63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [System.Runtime]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {6E650FE8-EE99-4C1A-8206-71823E127FC2}
.imagebase 0x0000000140000000
.file alignment 0x00000200
.stackreserve 0x0000000000400000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x0000029BB7DA0000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface public abstract auto ansi I1`1<T>
{
  .method public hidebysig instance class [System.Runtime]System.Type 
          GetItemType() cil managed noinlining
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldtoken    !T
    IL_0005:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_000a:  ret
  } // end of method I1`1::GetItemType

  .method public hidebysig instance class [System.Runtime]System.Type 
          GetItemTypeMethod<U>() cil managed noinlining
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldtoken    !!U
    IL_0005:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_000a:  ret
  } // end of method I1`1::GetItemTypeMethod

} // end of class I1`1

.class interface public abstract auto ansi I2
{
  .method public hidebysig instance class [System.Runtime]System.Type 
          GetItemTypeNonGeneric(class [System.Runtime]System.Type t) cil managed noinlining
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldarg.1
    IL_0001:  ret
  } // end of method I2::GetItemTypeNonGeneric

  .method public hidebysig instance class [System.Runtime]System.Type 
          GetItemTypeGeneric<U>() cil managed noinlining
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldtoken    !!U
    IL_0005:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_000a:  ret
  } // end of method I2::GetItemTypeGeneric

} // end of class I2

.class public auto ansi sealed beforefieldinit Sample.C1
       extends [System.Runtime]System.Object
       implements class I1`1<string>,
                  I2
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method C1::.ctor

} // end of class Sample.C1

.class private auto ansi beforefieldinit Sample.Program
       extends [System.Runtime]System.Object
{
  .method private hidebysig static int32 
          Main(string[] args) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       56 (0x38)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  call       void Sample.Program::CallOpcodeNonGenericInterface()
    IL_0006:  nop
    IL_0007:  call       void Sample.Program::CallOpcodeNonGenericInterfaceGenericMethod()
    IL_000c:  nop
    IL_000d:  call       void Sample.Program::CallOpcodeGenericInterface()
    IL_0012:  nop
    IL_0013:  call       void Sample.Program::CallOpcodeGenericInterfaceGenericMethod()
    IL_0018:  nop
    IL_0019:  call       void Sample.Program::CallVirtOpcodeNonGenericInterface()
    IL_001e:  nop
    IL_001f:  call       void Sample.Program::CallVirtOpcodeNonGenericInterfaceGenericMethod()
    IL_0024:  nop
    IL_0025:  call       void Sample.Program::CallVirtOpcodeGenericInterface()
    IL_002a:  nop
    IL_002b:  call       void Sample.Program::CallVirtOpcodeGenericInterfaceGenericMethod()
    IL_0030:  nop
    IL_0031:  ldc.i4.s   100
    IL_0033:  stloc.0
    IL_0034:  br.s       IL_0036

    IL_0036:  ldloc.0
    IL_0037:  ret
  } // end of method Program::Main

  .method private hidebysig static void  CallOpcodeNonGenericInterface() cil managed
  {
    // Code size       63 (0x3f)
    .maxstack  2
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Testing call opcode for calling DIM on non-generic"
    + " interface non-generic method"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  newobj     instance void Sample.C1::.ctor()
    IL_0011:  ldtoken    [System.Runtime]System.String
    IL_0016:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_001b:  call       instance class [System.Runtime]System.Type I2::GetItemTypeNonGeneric(class [System.Runtime]System.Type)
    IL_0020:  ldtoken    [System.Runtime]System.String
    IL_0025:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_002a:  call       bool [System.Runtime]System.Type::op_Inequality(class [System.Runtime]System.Type,
                                                                         class [System.Runtime]System.Type)
    IL_002f:  stloc.0
    IL_0030:  ldloc.0
    IL_0031:  brfalse.s  IL_003e

    IL_0033:  ldstr      "CallOpcodeGenericInterface failed"
    IL_0038:  newobj     instance void [System.Runtime]System.Exception::.ctor(string)
    IL_003d:  throw

    IL_003e:  ret
  } // end of method Program::CallOpcodeNonGenericInterface

  .method private hidebysig static void  CallOpcodeNonGenericInterfaceGenericMethod() cil managed
  {
    // Code size       53 (0x35)
    .maxstack  2
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Testing call opcode for calling DIM on non-generic"
    + " interface non-generic method"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  newobj     instance void Sample.C1::.ctor()
    IL_0011:  call       instance class [System.Runtime]System.Type I2::GetItemTypeGeneric<object>()
    IL_0016:  ldtoken    [System.Runtime]System.Object
    IL_001b:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0020:  call       bool [System.Runtime]System.Type::op_Inequality(class [System.Runtime]System.Type,
                                                                         class [System.Runtime]System.Type)
    IL_0025:  stloc.0
    IL_0026:  ldloc.0
    IL_0027:  brfalse.s  IL_0034

    IL_0029:  ldstr      "CallOpcodeGenericInterface failed"
    IL_002e:  newobj     instance void [System.Runtime]System.Exception::.ctor(string)
    IL_0033:  throw

    IL_0034:  ret
  } // end of method Program::CallOpcodeNonGenericInterfaceGenericMethod

  .method private hidebysig static void  CallOpcodeGenericInterface() cil managed
  {
    // Code size       53 (0x35)
    .maxstack  2
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Testing call opcode for calling DIM on generic int"
    + "erface"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  newobj     instance void Sample.C1::.ctor()
    IL_0011:  call       instance class [System.Runtime]System.Type class I1`1<string>::GetItemType()
    IL_0016:  ldtoken    [System.Runtime]System.String
    IL_001b:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0020:  call       bool [System.Runtime]System.Type::op_Inequality(class [System.Runtime]System.Type,
                                                                         class [System.Runtime]System.Type)
    IL_0025:  stloc.0
    IL_0026:  ldloc.0
    IL_0027:  brfalse.s  IL_0034

    IL_0029:  ldstr      "CallOpcodeGenericInterface failed"
    IL_002e:  newobj     instance void [System.Runtime]System.Exception::.ctor(string)
    IL_0033:  throw

    IL_0034:  ret
  } // end of method Program::CallOpcodeGenericInterface

  .method private hidebysig static void  CallOpcodeGenericInterfaceGenericMethod() cil managed
  {
    // Code size       53 (0x35)
    .maxstack  2
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Testing call opcode for calling generic method on "
    + "DIM on generic interface"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  newobj     instance void Sample.C1::.ctor()
    IL_0011:  call       instance class [System.Runtime]System.Type class I1`1<string>::GetItemTypeMethod<object>()
    IL_0016:  ldtoken    [System.Runtime]System.Object
    IL_001b:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0020:  call       bool [System.Runtime]System.Type::op_Inequality(class [System.Runtime]System.Type,
                                                                         class [System.Runtime]System.Type)
    IL_0025:  stloc.0
    IL_0026:  ldloc.0
    IL_0027:  brfalse.s  IL_0034

    IL_0029:  ldstr      "CallOpcodeGenericInterface failed"
    IL_002e:  newobj     instance void [System.Runtime]System.Exception::.ctor(string)
    IL_0033:  throw

    IL_0034:  ret
  } // end of method Program::CallOpcodeGenericInterfaceGenericMethod

  .method private hidebysig static void  CallVirtOpcodeNonGenericInterface() cil managed
  {
    // Code size       65 (0x41)
    .maxstack  2
    .locals init (class I2 V_0,
             bool V_1)
    IL_0000:  nop
    IL_0001:  ldstr      "Testing callvirt opcode for calling DIM on non-gen"
    + "eric interface non-generic method"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  newobj     instance void Sample.C1::.ctor()
    IL_0011:  stloc.0
    IL_0012:  ldloc.0
    IL_0013:  ldtoken    [System.Runtime]System.String
    IL_0018:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_001d:  callvirt   instance class [System.Runtime]System.Type I2::GetItemTypeNonGeneric(class [System.Runtime]System.Type)
    IL_0022:  ldtoken    [System.Runtime]System.String
    IL_0027:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_002c:  call       bool [System.Runtime]System.Type::op_Inequality(class [System.Runtime]System.Type,
                                                                         class [System.Runtime]System.Type)
    IL_0031:  stloc.1
    IL_0032:  ldloc.1
    IL_0033:  brfalse.s  IL_0040

    IL_0035:  ldstr      "CallOpcodeGenericInterface failed"
    IL_003a:  newobj     instance void [System.Runtime]System.Exception::.ctor(string)
    IL_003f:  throw

    IL_0040:  ret
  } // end of method Program::CallVirtOpcodeNonGenericInterface

  .method private hidebysig static void  CallVirtOpcodeNonGenericInterfaceGenericMethod() cil managed
  {
    // Code size       55 (0x37)
    .maxstack  2
    .locals init (class I2 V_0,
             bool V_1)
    IL_0000:  nop
    IL_0001:  ldstr      "Testing callvirt opcode for calling DIM on non-gen"
    + "eric interface non-generic method"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  newobj     instance void Sample.C1::.ctor()
    IL_0011:  stloc.0
    IL_0012:  ldloc.0
    IL_0013:  callvirt   instance class [System.Runtime]System.Type I2::GetItemTypeGeneric<object>()
    IL_0018:  ldtoken    [System.Runtime]System.Object
    IL_001d:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0022:  call       bool [System.Runtime]System.Type::op_Inequality(class [System.Runtime]System.Type,
                                                                         class [System.Runtime]System.Type)
    IL_0027:  stloc.1
    IL_0028:  ldloc.1
    IL_0029:  brfalse.s  IL_0036

    IL_002b:  ldstr      "CallOpcodeGenericInterface failed"
    IL_0030:  newobj     instance void [System.Runtime]System.Exception::.ctor(string)
    IL_0035:  throw

    IL_0036:  ret
  } // end of method Program::CallVirtOpcodeNonGenericInterfaceGenericMethod

  .method private hidebysig static void  CallVirtOpcodeGenericInterface() cil managed
  {
    // Code size       55 (0x37)
    .maxstack  2
    .locals init (class I1`1<string> V_0,
             bool V_1)
    IL_0000:  nop
    IL_0001:  ldstr      "Testing callvirt opcode for calling DIM on generic"
    + " interface"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  newobj     instance void Sample.C1::.ctor()
    IL_0011:  stloc.0
    IL_0012:  ldloc.0
    IL_0013:  callvirt   instance class [System.Runtime]System.Type class I1`1<string>::GetItemType()
    IL_0018:  ldtoken    [System.Runtime]System.String
    IL_001d:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0022:  call       bool [System.Runtime]System.Type::op_Inequality(class [System.Runtime]System.Type,
                                                                         class [System.Runtime]System.Type)
    IL_0027:  stloc.1
    IL_0028:  ldloc.1
    IL_0029:  brfalse.s  IL_0036

    IL_002b:  ldstr      "CallVirtOpcodeGenericInterface failed"
    IL_0030:  newobj     instance void [System.Runtime]System.Exception::.ctor(string)
    IL_0035:  throw

    IL_0036:  ret
  } // end of method Program::CallVirtOpcodeGenericInterface

  .method private hidebysig static void  CallVirtOpcodeGenericInterfaceGenericMethod() cil managed
  {
    // Code size       55 (0x37)
    .maxstack  2
    .locals init (class I1`1<string> V_0,
             bool V_1)
    IL_0000:  nop
    IL_0001:  ldstr      "Testing callvirt opcode for calling generic method"
    + " on DIM on generic interface"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  newobj     instance void Sample.C1::.ctor()
    IL_0011:  stloc.0
    IL_0012:  ldloc.0
    IL_0013:  callvirt   instance class [System.Runtime]System.Type class I1`1<string>::GetItemTypeMethod<object>()
    IL_0018:  ldtoken    [System.Runtime]System.Object
    IL_001d:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0022:  call       bool [System.Runtime]System.Type::op_Inequality(class [System.Runtime]System.Type,
                                                                         class [System.Runtime]System.Type)
    IL_0027:  stloc.1
    IL_0028:  ldloc.1
    IL_0029:  brfalse.s  IL_0036

    IL_002b:  ldstr      "CallVirtOpcodeGenericInterfaceGenericMethod failed"
    IL_0030:  newobj     instance void [System.Runtime]System.Exception::.ctor(string)
    IL_0035:  throw

    IL_0036:  ret
  } // end of method Program::CallVirtOpcodeGenericInterfaceGenericMethod

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Program::.ctor

} // end of class Sample.Program


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
