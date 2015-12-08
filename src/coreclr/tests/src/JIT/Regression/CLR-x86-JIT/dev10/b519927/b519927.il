// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.




.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly EmitTest
{
  .custom instance void [mscorlib]System.Reflection.AssemblyTitleAttribute::.ctor(string) = ( 01 00 08 45 6D 69 74 54 65 73 74 00 00 )          // ...EmitTest..
  .custom instance void [mscorlib]System.Reflection.AssemblyDescriptionAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.AssemblyConfigurationAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.AssemblyCompanyAttribute::.ctor(string) = ( 01 00 09 4D 69 63 72 6F 73 6F 66 74 00 00 )       // ...Microsoft..
  .custom instance void [mscorlib]System.Reflection.AssemblyProductAttribute::.ctor(string) = ( 01 00 08 45 6D 69 74 54 65 73 74 00 00 )          // ...EmitTest..
  .custom instance void [mscorlib]System.Reflection.AssemblyCopyrightAttribute::.ctor(string) = ( 01 00 1B 43 6F 70 79 72 69 67 68 74 20 C2 A9 20   // ...Copyright .. 
                                                                                                  4D 69 63 72 6F 73 6F 66 74 20 32 30 30 38 00 00 ) // Microsoft 2008..
  .custom instance void [mscorlib]System.Reflection.AssemblyTrademarkAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 66 66 37 66 30 37 64 33 2D 37 63 35 39   // ..$ff7f07d3-7c59
                                                                                                  2D 34 38 62 30 2D 39 34 63 63 2D 37 37 63 61 65   // -48b0-94cc-77cae
                                                                                                  39 37 34 65 63 66 62 00 00 )                      // 974ecfb..
  .custom instance void [mscorlib]System.Reflection.AssemblyFileVersionAttribute::.ctor(string) = ( 01 00 07 31 2E 30 2E 30 2E 30 00 00 )             // ...1.0.0.0..


  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
.module EmitTest.exe
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI



.class interface public abstract auto ansi IServiceManager
{
} // end of class IServiceManager

.class public auto ansi beforefieldinit Services
       extends [mscorlib]System.Object
       implements IServiceManager
{
  .field public static initonly class IScheduler Scheduler
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Services::.ctor

} // end of class Services

.class interface public abstract auto ansi IService
{
} // end of class IService

.class interface public abstract auto ansi IScheduler
       implements IService
{
  .method public hidebysig newslot abstract virtual 
          instance void  Start() cil managed
  {
  } // end of method IScheduler::Start

} // end of class IScheduler

.class public auto ansi beforefieldinit Scheduler
       extends [mscorlib]System.Object
       implements IScheduler,
                  IService
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldstr      "Creating Scheduler"
    IL_000d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  nop
    IL_0014:  ret
  } // end of method Scheduler::.ctor

  .method public hidebysig newslot virtual final 
          instance void  Start() cil managed
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Starting Scheduler"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Scheduler::Start

} // end of class Scheduler

.class private abstract auto ansi sealed beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          Main() cil managed
  {
    .entrypoint
    .maxstack  1
    .locals init ([0] int32 CS$1$0000)
    IL_0000:  nop
    IL_0001:  call       void ServiceLoader::Initialize()
    IL_0006:  nop
    .try
    {
      IL_0007:  nop
      IL_0008:  ldsfld     class IScheduler Services::Scheduler
      IL_000d:  callvirt   instance void IScheduler::Start()
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  leave.s    IL_001d

    }  // end .try
    catch [mscorlib]System.MissingMethodException 
    {
      IL_0016:  pop
      IL_0017:  nop
      IL_0018:  ldc.i4.s   100
      IL_001a:  stloc.0
      IL_001b:  leave.s    IL_0023

    }  // end handler
    IL_001d:  nop
    IL_001e:  ldc.i4.s   101
    IL_0020:  stloc.0
    IL_0021:  br.s       IL_0023

    IL_0023:  nop
    IL_0024:  ldloc.0
    IL_0025:  ret
  } // end of method Program::Main

} // end of class Program

.class public auto ansi beforefieldinit ServiceLoader
       extends [mscorlib]System.Object
{
  .field private static class Manager manager
  .field private static class [mscorlib]System.Type serviceType
  .method private hidebysig static void  LoadManagers(class [mscorlib]System.Reflection.Assembly 'assembly') cil managed
  {
    .maxstack  2
    .locals init ([0] class [mscorlib]System.Type 'type',
             [1] class [mscorlib]System.Type[] CS$6$0000,
             [2] int32 CS$7$0001,
             [3] bool CS$4$0002)
    IL_0000:  nop
    IL_0001:  nop
    IL_0002:  ldarg.0
    IL_0003:  callvirt   instance class [mscorlib]System.Type[] [mscorlib]System.Reflection.Assembly::GetTypes()
    IL_0008:  stloc.1
    IL_0009:  ldc.i4.0
    IL_000a:  stloc.2
    IL_000b:  br.s       IL_005d

    IL_000d:  ldloc.1
    IL_000e:  ldloc.2
    IL_000f:  ldelem.ref
    IL_0010:  stloc.0
    IL_0011:  nop
    IL_0012:  ldloc.0
    IL_0013:  callvirt   instance bool [mscorlib]System.Type::get_IsPublic()
    IL_0018:  brfalse.s  IL_003b

    IL_001a:  ldtoken    IServiceManager
    IL_001f:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0024:  ldloc.0
    IL_0025:  callvirt   instance bool [mscorlib]System.Type::IsAssignableFrom(class [mscorlib]System.Type)
    IL_002a:  brfalse.s  IL_003b

    IL_002c:  ldloc.0
    IL_002d:  ldtoken    IServiceManager
    IL_0032:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0037:  ceq
    IL_0039:  br.s       IL_003c

    IL_003b:  ldc.i4.1
    IL_003c:  stloc.3
    IL_003d:  ldloc.3
    IL_003e:  brtrue.s   IL_0058

    IL_0040:  nop
    IL_0041:  ldloc.0
    IL_0042:  newobj     instance void Manager::.ctor(class [mscorlib]System.Type)
    IL_0047:  stsfld     class Manager ServiceLoader::manager
    IL_004c:  ldsfld     class Manager ServiceLoader::manager
    IL_0051:  callvirt   instance void Manager::Initialize()
    IL_0056:  nop
    IL_0057:  nop
    IL_0058:  nop
    IL_0059:  ldloc.2
    IL_005a:  ldc.i4.1
    IL_005b:  add
    IL_005c:  stloc.2
    IL_005d:  ldloc.2
    IL_005e:  ldloc.1
    IL_005f:  ldlen
    IL_0060:  conv.i4
    IL_0061:  clt
    IL_0063:  stloc.3
    IL_0064:  ldloc.3
    IL_0065:  brtrue.s   IL_000d

    IL_0067:  ret
  } // end of method ServiceLoader::LoadManagers

  .method private hidebysig static void  LoadServices(class [mscorlib]System.Reflection.Assembly 'assembly') cil managed
  {
    .maxstack  2
    .locals init ([0] class [mscorlib]System.Type 'type',
             [1] class [mscorlib]System.Type[] interfaces,
             [2] class [mscorlib]System.Type 'interface',
             [3] class [mscorlib]System.Type inter,
             [4] class [mscorlib]System.Type[] CS$6$0000,
             [5] int32 CS$7$0001,
             [6] bool CS$4$0002,
             [7] class [mscorlib]System.Type[] CS$6$0003,
             [8] int32 CS$7$0004,
             [9] class [mscorlib]System.Type[] CS$6$0005,
             [10] int32 CS$7$0006)
    IL_0000:  nop
    IL_0001:  nop
    IL_0002:  ldarg.0
    IL_0003:  callvirt   instance class [mscorlib]System.Type[] [mscorlib]System.Reflection.Assembly::GetTypes()
    IL_0008:  stloc.s    CS$6$0000
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.s    CS$7$0001
    IL_000d:  br         IL_00e0

    IL_0012:  ldloc.s    CS$6$0000
    IL_0014:  ldloc.s    CS$7$0001
    IL_0016:  ldelem.ref
    IL_0017:  stloc.0
    IL_0018:  nop
    IL_0019:  ldloc.0
    IL_001a:  callvirt   instance bool [mscorlib]System.Type::get_IsPublic()
    IL_001f:  brfalse.s  IL_0043

    IL_0021:  ldtoken    IService
    IL_0026:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_002b:  ldloc.0
    IL_002c:  callvirt   instance bool [mscorlib]System.Type::IsAssignableFrom(class [mscorlib]System.Type)
    IL_0031:  brfalse.s  IL_0043

    IL_0033:  ldloc.0
    IL_0034:  callvirt   instance bool [mscorlib]System.Type::get_IsClass()
    IL_0039:  brfalse.s  IL_0043

    IL_003b:  ldloc.0
    IL_003c:  callvirt   instance bool [mscorlib]System.Type::get_IsAbstract()
    IL_0041:  br.s       IL_0044

    IL_0043:  ldc.i4.1
    IL_0044:  stloc.s    CS$4$0002
    IL_0046:  ldloc.s    CS$4$0002
    IL_0048:  brtrue     IL_00d9

    IL_004d:  nop
    IL_004e:  ldloc.0
    IL_004f:  callvirt   instance class [mscorlib]System.Type[] [mscorlib]System.Type::GetInterfaces()
    IL_0054:  stloc.1
    IL_0055:  nop
    IL_0056:  ldloc.1
    IL_0057:  stloc.s    CS$6$0003
    IL_0059:  ldc.i4.0
    IL_005a:  stloc.s    CS$7$0004
    IL_005c:  br.s       IL_00ca

    IL_005e:  ldloc.s    CS$6$0003
    IL_0060:  ldloc.s    CS$7$0004
    IL_0062:  ldelem.ref
    IL_0063:  stloc.2
    IL_0064:  nop
    IL_0065:  ldloc.2
    IL_0066:  ldtoken    IService
    IL_006b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0070:  ceq
    IL_0072:  stloc.s    CS$4$0002
    IL_0074:  ldloc.s    CS$4$0002
    IL_0076:  brtrue.s   IL_00c3

    IL_0078:  nop
    IL_0079:  nop
    IL_007a:  ldloc.2
    IL_007b:  callvirt   instance class [mscorlib]System.Type[] [mscorlib]System.Type::GetInterfaces()
    IL_0080:  stloc.s    CS$6$0005
    IL_0082:  ldc.i4.0
    IL_0083:  stloc.s    CS$7$0006
    IL_0085:  br.s       IL_00b4

    IL_0087:  ldloc.s    CS$6$0005
    IL_0089:  ldloc.s    CS$7$0006
    IL_008b:  ldelem.ref
    IL_008c:  stloc.3
    IL_008d:  nop
    IL_008e:  ldloc.3
    IL_008f:  ldtoken    IService
    IL_0094:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0099:  ceq
    IL_009b:  ldc.i4.0
    IL_009c:  ceq
    IL_009e:  stloc.s    CS$4$0002
    IL_00a0:  ldloc.s    CS$4$0002
    IL_00a2:  brtrue.s   IL_00ad

    IL_00a4:  nop
    IL_00a5:  ldloc.0
    IL_00a6:  stsfld     class [mscorlib]System.Type ServiceLoader::serviceType
    IL_00ab:  br.s       IL_00c2

    IL_00ad:  nop
    IL_00ae:  ldloc.s    CS$7$0006
    IL_00b0:  ldc.i4.1
    IL_00b1:  add
    IL_00b2:  stloc.s    CS$7$0006
    IL_00b4:  ldloc.s    CS$7$0006
    IL_00b6:  ldloc.s    CS$6$0005
    IL_00b8:  ldlen
    IL_00b9:  conv.i4
    IL_00ba:  clt
    IL_00bc:  stloc.s    CS$4$0002
    IL_00be:  ldloc.s    CS$4$0002
    IL_00c0:  brtrue.s   IL_0087

    IL_00c2:  nop
    IL_00c3:  nop
    IL_00c4:  ldloc.s    CS$7$0004
    IL_00c6:  ldc.i4.1
    IL_00c7:  add
    IL_00c8:  stloc.s    CS$7$0004
    IL_00ca:  ldloc.s    CS$7$0004
    IL_00cc:  ldloc.s    CS$6$0003
    IL_00ce:  ldlen
    IL_00cf:  conv.i4
    IL_00d0:  clt
    IL_00d2:  stloc.s    CS$4$0002
    IL_00d4:  ldloc.s    CS$4$0002
    IL_00d6:  brtrue.s   IL_005e

    IL_00d8:  nop
    IL_00d9:  nop
    IL_00da:  ldloc.s    CS$7$0001
    IL_00dc:  ldc.i4.1
    IL_00dd:  add
    IL_00de:  stloc.s    CS$7$0001
    IL_00e0:  ldloc.s    CS$7$0001
    IL_00e2:  ldloc.s    CS$6$0000
    IL_00e4:  ldlen
    IL_00e5:  conv.i4
    IL_00e6:  clt
    IL_00e8:  stloc.s    CS$4$0002
    IL_00ea:  ldloc.s    CS$4$0002
    IL_00ec:  brtrue     IL_0012

    IL_00f1:  ret
  } // end of method ServiceLoader::LoadServices

  .method public hidebysig static void  Initialize() cil managed
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  call       class [mscorlib]System.Reflection.Assembly [mscorlib]System.Reflection.Assembly::GetExecutingAssembly()
    IL_0006:  call       void ServiceLoader::LoadManagers(class [mscorlib]System.Reflection.Assembly)
    IL_000b:  nop
    IL_000c:  call       class [mscorlib]System.Reflection.Assembly [mscorlib]System.Reflection.Assembly::GetExecutingAssembly()
    IL_0011:  call       void ServiceLoader::LoadServices(class [mscorlib]System.Reflection.Assembly)
    IL_0016:  nop
    IL_0017:  ret
  } // end of method ServiceLoader::Initialize

  .method public hidebysig static object 
          Create(class [mscorlib]System.Type 'type') cil managed
  {
    .maxstack  2
    .locals init ([0] object CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldstr      "Trying to create "
    IL_0006:  ldarg.0
    IL_0007:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_000c:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0011:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0016:  nop
    IL_0017:  ldnull
    IL_0018:  stloc.0
    IL_0019:  br.s       IL_001b

    IL_001b:  ldloc.0
    IL_001c:  ret
  } // end of method ServiceLoader::Create

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method ServiceLoader::.ctor

} // end of class ServiceLoader

.class private auto ansi beforefieldinit Manager
       extends [mscorlib]System.Object
{
  .field private class [mscorlib]System.Type 'type'
  .method assembly hidebysig specialname rtspecialname 
          instance void  .ctor(class [mscorlib]System.Type 'type') cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      class [mscorlib]System.Type Manager::'type'
    IL_000f:  nop
    IL_0010:  ret
  } // end of method Manager::.ctor

  .method private hidebysig instance void 
          CreateConstructor(class [mscorlib]System.Reflection.Emit.TypeBuilder myType,
                            class [mscorlib]System.Reflection.Emit.FieldBuilder mt) cil managed
  {
    .maxstack  6
    .locals init ([0] class [mscorlib]System.Reflection.Emit.ConstructorBuilder cb,
             [1] class [mscorlib]System.Reflection.Emit.ILGenerator ilgen,
             [2] class [mscorlib]System.Type[] CS$0$0000)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldc.i4.6
    IL_0003:  ldc.i4.1
    IL_0004:  ldc.i4.1
    IL_0005:  newarr     [mscorlib]System.Type
    IL_000a:  stloc.2
    IL_000b:  ldloc.2
    IL_000c:  ldc.i4.0
    IL_000d:  ldtoken    [mscorlib]System.Type
    IL_0012:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0017:  stelem.ref
    IL_0018:  ldloc.2
    IL_0019:  callvirt   instance class [mscorlib]System.Reflection.Emit.ConstructorBuilder [mscorlib]System.Reflection.Emit.TypeBuilder::DefineConstructor(valuetype [mscorlib]System.Reflection.MethodAttributes,
                                                                                                                                                            valuetype [mscorlib]System.Reflection.CallingConventions,
                                                                                                                                                            class [mscorlib]System.Type[])
    IL_001e:  stloc.0
    IL_001f:  ldloc.0
    IL_0020:  callvirt   instance class [mscorlib]System.Reflection.Emit.ILGenerator [mscorlib]System.Reflection.Emit.ConstructorBuilder::GetILGenerator()
    IL_0025:  stloc.1
    IL_0026:  ldloc.1
    IL_0027:  ldsfld     valuetype [mscorlib]System.Reflection.Emit.OpCode [mscorlib]System.Reflection.Emit.OpCodes::Ldarg_0
    IL_002c:  callvirt   instance void [mscorlib]System.Reflection.Emit.ILGenerator::Emit(valuetype [mscorlib]System.Reflection.Emit.OpCode)
    IL_0031:  nop
    IL_0032:  ldloc.1
    IL_0033:  ldsfld     valuetype [mscorlib]System.Reflection.Emit.OpCode [mscorlib]System.Reflection.Emit.OpCodes::Ldarg_1
    IL_0038:  callvirt   instance void [mscorlib]System.Reflection.Emit.ILGenerator::Emit(valuetype [mscorlib]System.Reflection.Emit.OpCode)
    IL_003d:  nop
    IL_003e:  ldloc.1
    IL_003f:  ldsfld     valuetype [mscorlib]System.Reflection.Emit.OpCode [mscorlib]System.Reflection.Emit.OpCodes::Stfld
    IL_0044:  ldarg.2
    IL_0045:  callvirt   instance void [mscorlib]System.Reflection.Emit.ILGenerator::Emit(valuetype [mscorlib]System.Reflection.Emit.OpCode,
                                                                                          class [mscorlib]System.Reflection.FieldInfo)
    IL_004a:  nop
    IL_004b:  ldloc.1
    IL_004c:  ldsfld     valuetype [mscorlib]System.Reflection.Emit.OpCode [mscorlib]System.Reflection.Emit.OpCodes::Ret
    IL_0051:  callvirt   instance void [mscorlib]System.Reflection.Emit.ILGenerator::Emit(valuetype [mscorlib]System.Reflection.Emit.OpCode)
    IL_0056:  nop
    IL_0057:  ret
  } // end of method Manager::CreateConstructor

  .method private hidebysig instance void 
          CreateMethod(class [mscorlib]System.Reflection.Emit.TypeBuilder myType,
                       class [mscorlib]System.Reflection.MethodInfo m,
                       class [mscorlib]System.Reflection.Emit.FieldBuilder mt,
                       class [mscorlib]System.Reflection.FieldInfo 'field',
                       class [mscorlib]System.Type[] paramTypes) cil managed
  {
    .maxstack  5
    .locals init ([0] class [mscorlib]System.Reflection.Emit.MethodBuilder simpleMethod,
             [1] class [mscorlib]System.Reflection.Emit.ILGenerator 'il')
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0008:  ldc.i4.s   70
    IL_000a:  ldarg.2
    IL_000b:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Reflection.MethodInfo::get_ReturnType()
    IL_0010:  ldarg.s    paramTypes
    IL_0012:  callvirt   instance class [mscorlib]System.Reflection.Emit.MethodBuilder [mscorlib]System.Reflection.Emit.TypeBuilder::DefineMethod(string,
                                                                                                                                                  valuetype [mscorlib]System.Reflection.MethodAttributes,
                                                                                                                                                  class [mscorlib]System.Type,
                                                                                                                                                  class [mscorlib]System.Type[])
    IL_0017:  stloc.0
    IL_0018:  ldloc.0
    IL_0019:  callvirt   instance class [mscorlib]System.Reflection.Emit.ILGenerator [mscorlib]System.Reflection.Emit.MethodBuilder::GetILGenerator()
    IL_001e:  stloc.1
    IL_001f:  ldloc.1
    IL_0020:  ldstr      "Created method called - start"
    IL_0025:  callvirt   instance void [mscorlib]System.Reflection.Emit.ILGenerator::EmitWriteLine(string)
    IL_002a:  nop
    IL_002b:  ldloc.1
    IL_002c:  ldsfld     valuetype [mscorlib]System.Reflection.Emit.OpCode [mscorlib]System.Reflection.Emit.OpCodes::Ldarg_0
    IL_0031:  callvirt   instance void [mscorlib]System.Reflection.Emit.ILGenerator::Emit(valuetype [mscorlib]System.Reflection.Emit.OpCode)
    IL_0036:  nop
    IL_0037:  ldloc.1
    IL_0038:  ldsfld     valuetype [mscorlib]System.Reflection.Emit.OpCode [mscorlib]System.Reflection.Emit.OpCodes::Ldfld
    IL_003d:  ldarg.3
    IL_003e:  callvirt   instance void [mscorlib]System.Reflection.Emit.ILGenerator::Emit(valuetype [mscorlib]System.Reflection.Emit.OpCode,
                                                                                          class [mscorlib]System.Reflection.FieldInfo)
    IL_0043:  nop
    IL_0044:  ldloc.1
    IL_0045:  ldsfld     valuetype [mscorlib]System.Reflection.Emit.OpCode [mscorlib]System.Reflection.Emit.OpCodes::Callvirt
    IL_004a:  ldtoken    ServiceLoader
    IL_004f:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0054:  ldstr      "Create"
    IL_0059:  call       instance class [mscorlib]System.Reflection.MethodInfo [mscorlib]System.Type::GetMethod(string)
    IL_005e:  callvirt   instance void [mscorlib]System.Reflection.Emit.ILGenerator::Emit(valuetype [mscorlib]System.Reflection.Emit.OpCode,
                                                                                          class [mscorlib]System.Reflection.MethodInfo)
    IL_0063:  nop
    IL_0064:  ldloc.1
    IL_0065:  ldsfld     valuetype [mscorlib]System.Reflection.Emit.OpCode [mscorlib]System.Reflection.Emit.OpCodes::Pop
    IL_006a:  callvirt   instance void [mscorlib]System.Reflection.Emit.ILGenerator::Emit(valuetype [mscorlib]System.Reflection.Emit.OpCode)
    IL_006f:  nop
    IL_0070:  ldloc.1
    IL_0071:  ldstr      "Created method called - end"
    IL_0076:  callvirt   instance void [mscorlib]System.Reflection.Emit.ILGenerator::EmitWriteLine(string)
    IL_007b:  nop
    IL_007c:  ldloc.1
    IL_007d:  ldsfld     valuetype [mscorlib]System.Reflection.Emit.OpCode [mscorlib]System.Reflection.Emit.OpCodes::Ret
    IL_0082:  callvirt   instance void [mscorlib]System.Reflection.Emit.ILGenerator::Emit(valuetype [mscorlib]System.Reflection.Emit.OpCode)
    IL_0087:  nop
    IL_0088:  ldarg.1
    IL_0089:  ldloc.0
    IL_008a:  ldarg.2
    IL_008b:  callvirt   instance void [mscorlib]System.Reflection.Emit.TypeBuilder::DefineMethodOverride(class [mscorlib]System.Reflection.MethodInfo,
                                                                                                          class [mscorlib]System.Reflection.MethodInfo)
    IL_0090:  nop
    IL_0091:  ret
  } // end of method Manager::CreateMethod

  .method assembly hidebysig instance void 
          Initialize() cil managed
  {
    .maxstack  11
    .locals init ([0] class [mscorlib]System.Reflection.FieldInfo[] fields,
             [1] class [mscorlib]System.Reflection.FieldInfo 'field',
             [2] class [mscorlib]System.Reflection.AssemblyName assemblyName,
             [3] class [mscorlib]System.Reflection.Emit.AssemblyBuilder newAssembly,
             [4] class [mscorlib]System.Reflection.Emit.ModuleBuilder newModule,
             [5] class [mscorlib]System.Reflection.Emit.TypeBuilder myType,
             [6] class [mscorlib]System.Reflection.Emit.FieldBuilder mt,
             [7] class [mscorlib]System.Reflection.MethodInfo m,
             [8] class [mscorlib]System.Type[] paramTypes,
             [9] int32 i,
             [10] class [mscorlib]System.Reflection.FieldInfo[] CS$6$0000,
             [11] int32 CS$7$0001,
             [12] bool CS$4$0002,
             [13] class [mscorlib]System.Reflection.MethodInfo[] CS$6$0003,
             [14] int32 CS$7$0004,
             [15] object[] CS$0$0005,
             [16] object[] CS$0$0006)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class [mscorlib]System.Type Manager::'type'
    IL_0007:  callvirt   instance class [mscorlib]System.Reflection.FieldInfo[] [mscorlib]System.Type::GetFields()
    IL_000c:  stloc.0
    IL_000d:  nop
    IL_000e:  ldloc.0
    IL_000f:  stloc.s    CS$6$0000
    IL_0011:  ldc.i4.0
    IL_0012:  stloc.s    CS$7$0001
    IL_0014:  br         IL_0190

    IL_0019:  ldloc.s    CS$6$0000
    IL_001b:  ldloc.s    CS$7$0001
    IL_001d:  ldelem.ref
    IL_001e:  stloc.1
    IL_001f:  nop
    IL_0020:  ldtoken    IService
    IL_0025:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_002a:  ldloc.1
    IL_002b:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Reflection.FieldInfo::get_FieldType()
    IL_0030:  callvirt   instance bool [mscorlib]System.Type::IsAssignableFrom(class [mscorlib]System.Type)
    IL_0035:  ldc.i4.0
    IL_0036:  ceq
    IL_0038:  stloc.s    CS$4$0002
    IL_003a:  ldloc.s    CS$4$0002
    IL_003c:  brtrue     IL_0189

    IL_0041:  nop
    IL_0042:  newobj     instance void [mscorlib]System.Reflection.AssemblyName::.ctor()
    IL_0047:  stloc.2
    IL_0048:  ldloc.2
    IL_0049:  ldstr      "PseudoServices"
    IL_004e:  callvirt   instance void [mscorlib]System.Reflection.AssemblyName::set_Name(string)
    IL_0053:  nop
    IL_0054:  call       class [mscorlib]System.AppDomain [mscorlib]System.Threading.Thread::GetDomain()
    IL_0059:  ldloc.2
    IL_005a:  ldc.i4.1
    IL_005b:  callvirt   instance class [mscorlib]System.Reflection.Emit.AssemblyBuilder [mscorlib]System.AppDomain::DefineDynamicAssembly(class [mscorlib]System.Reflection.AssemblyName,
                                                                                                                                           valuetype [mscorlib]System.Reflection.Emit.AssemblyBuilderAccess)
    IL_0060:  stloc.3
    IL_0061:  ldloc.3
    IL_0062:  ldstr      "Pseudo"
    IL_0067:  callvirt   instance class [mscorlib]System.Reflection.Emit.ModuleBuilder [mscorlib]System.Reflection.Emit.AssemblyBuilder::DefineDynamicModule(string)
    IL_006c:  stloc.s    newModule
    IL_006e:  ldloc.s    newModule
    IL_0070:  ldstr      "Pseudo"
    IL_0075:  ldloc.1
    IL_0076:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Reflection.FieldInfo::get_FieldType()
    IL_007b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0080:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0085:  ldc.i4.1
    IL_0086:  callvirt   instance class [mscorlib]System.Reflection.Emit.TypeBuilder [mscorlib]System.Reflection.Emit.ModuleBuilder::DefineType(string,
                                                                                                                                                valuetype [mscorlib]System.Reflection.TypeAttributes)
    IL_008b:  stloc.s    myType
    IL_008d:  ldloc.s    myType
    IL_008f:  ldloc.1
    IL_0090:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Reflection.FieldInfo::get_FieldType()
    IL_0095:  callvirt   instance void [mscorlib]System.Reflection.Emit.TypeBuilder::AddInterfaceImplementation(class [mscorlib]System.Type)
    IL_009a:  nop
    IL_009b:  ldloc.s    myType
    IL_009d:  ldstr      "mt"
    IL_00a2:  ldtoken    [mscorlib]System.Type
    IL_00a7:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_00ac:  ldc.i4.1
    IL_00ad:  callvirt   instance class [mscorlib]System.Reflection.Emit.FieldBuilder [mscorlib]System.Reflection.Emit.TypeBuilder::DefineField(string,
                                                                                                                                                class [mscorlib]System.Type,
                                                                                                                                                valuetype [mscorlib]System.Reflection.FieldAttributes)
    IL_00b2:  stloc.s    mt
    IL_00b4:  ldarg.0
    IL_00b5:  ldloc.s    myType
    IL_00b7:  ldloc.s    mt
    IL_00b9:  call       instance void Manager::CreateConstructor(class [mscorlib]System.Reflection.Emit.TypeBuilder,
                                                                  class [mscorlib]System.Reflection.Emit.FieldBuilder)
    IL_00be:  nop
    IL_00bf:  nop
    IL_00c0:  ldloc.1
    IL_00c1:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Reflection.FieldInfo::get_FieldType()
    IL_00c6:  callvirt   instance class [mscorlib]System.Reflection.MethodInfo[] [mscorlib]System.Type::GetMethods()
    IL_00cb:  stloc.s    CS$6$0003
    IL_00cd:  ldc.i4.0
    IL_00ce:  stloc.s    CS$7$0004
    IL_00d0:  br.s       IL_0133

    IL_00d2:  ldloc.s    CS$6$0003
    IL_00d4:  ldloc.s    CS$7$0004
    IL_00d6:  ldelem.ref
    IL_00d7:  stloc.s    m
    IL_00d9:  nop
    IL_00da:  ldloc.s    m
    IL_00dc:  callvirt   instance class [mscorlib]System.Reflection.ParameterInfo[] [mscorlib]System.Reflection.MethodBase::GetParameters()
    IL_00e1:  ldlen
    IL_00e2:  conv.i4
    IL_00e3:  newarr     [mscorlib]System.Type
    IL_00e8:  stloc.s    paramTypes
    IL_00ea:  ldc.i4.0
    IL_00eb:  stloc.s    i
    IL_00ed:  br.s       IL_0109

    IL_00ef:  ldloc.s    paramTypes
    IL_00f1:  ldloc.s    i
    IL_00f3:  ldloc.s    m
    IL_00f5:  callvirt   instance class [mscorlib]System.Reflection.ParameterInfo[] [mscorlib]System.Reflection.MethodBase::GetParameters()
    IL_00fa:  ldloc.s    i
    IL_00fc:  ldelem.ref
    IL_00fd:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Reflection.ParameterInfo::get_ParameterType()
    IL_0102:  stelem.ref
    IL_0103:  ldloc.s    i
    IL_0105:  ldc.i4.1
    IL_0106:  add
    IL_0107:  stloc.s    i
    IL_0109:  ldloc.s    i
    IL_010b:  ldloc.s    m
    IL_010d:  callvirt   instance class [mscorlib]System.Reflection.ParameterInfo[] [mscorlib]System.Reflection.MethodBase::GetParameters()
    IL_0112:  ldlen
    IL_0113:  conv.i4
    IL_0114:  clt
    IL_0116:  stloc.s    CS$4$0002
    IL_0118:  ldloc.s    CS$4$0002
    IL_011a:  brtrue.s   IL_00ef

    IL_011c:  ldarg.0
    IL_011d:  ldloc.s    myType
    IL_011f:  ldloc.s    m
    IL_0121:  ldloc.s    mt
    IL_0123:  ldloc.1
    IL_0124:  ldloc.s    paramTypes
    IL_0126:  call       instance void Manager::CreateMethod(class [mscorlib]System.Reflection.Emit.TypeBuilder,
                                                             class [mscorlib]System.Reflection.MethodInfo,
                                                             class [mscorlib]System.Reflection.Emit.FieldBuilder,
                                                             class [mscorlib]System.Reflection.FieldInfo,
                                                             class [mscorlib]System.Type[])
    IL_012b:  nop
    IL_012c:  nop
    IL_012d:  ldloc.s    CS$7$0004
    IL_012f:  ldc.i4.1
    IL_0130:  add
    IL_0131:  stloc.s    CS$7$0004
    IL_0133:  ldloc.s    CS$7$0004
    IL_0135:  ldloc.s    CS$6$0003
    IL_0137:  ldlen
    IL_0138:  conv.i4
    IL_0139:  clt
    IL_013b:  stloc.s    CS$4$0002
    IL_013d:  ldloc.s    CS$4$0002
    IL_013f:  brtrue.s   IL_00d2

    IL_0141:  ldarg.0
    IL_0142:  ldfld      class [mscorlib]System.Type Manager::'type'
    IL_0147:  ldloc.1
    IL_0148:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_014d:  ldc.i4     0x800
    IL_0152:  ldnull
    IL_0153:  ldnull
    IL_0154:  ldc.i4.1
    IL_0155:  newarr     [mscorlib]System.Object
    IL_015a:  stloc.s    CS$0$0005
    IL_015c:  ldloc.s    CS$0$0005
    IL_015e:  ldc.i4.0
    IL_015f:  ldloc.s    myType
    IL_0161:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Reflection.Emit.TypeBuilder::CreateType()
    IL_0166:  ldc.i4.1
    IL_0167:  newarr     [mscorlib]System.Object
    IL_016c:  stloc.s    CS$0$0006
    IL_016e:  ldloc.s    CS$0$0006
    IL_0170:  ldc.i4.0
    IL_0171:  ldloc.1
    IL_0172:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Reflection.FieldInfo::get_FieldType()
    IL_0177:  stelem.ref
    IL_0178:  ldloc.s    CS$0$0006
    IL_017a:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type,
                                                                           object[])
    IL_017f:  stelem.ref
    IL_0180:  ldloc.s    CS$0$0005
    IL_0182:  callvirt   instance object [mscorlib]System.Type::InvokeMember(string,
                                                                             valuetype [mscorlib]System.Reflection.BindingFlags,
                                                                             class [mscorlib]System.Reflection.Binder,
                                                                             object,
                                                                             object[])
    IL_0187:  pop
    IL_0188:  nop
    IL_0189:  nop
    IL_018a:  ldloc.s    CS$7$0001
    IL_018c:  ldc.i4.1
    IL_018d:  add
    IL_018e:  stloc.s    CS$7$0001
    IL_0190:  ldloc.s    CS$7$0001
    IL_0192:  ldloc.s    CS$6$0000
    IL_0194:  ldlen
    IL_0195:  conv.i4
    IL_0196:  clt
    IL_0198:  stloc.s    CS$4$0002
    IL_019a:  ldloc.s    CS$4$0002
    IL_019c:  brtrue     IL_0019

    IL_01a1:  ret
  } // end of method Manager::Initialize

} // end of class Manager



