// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly array2// as "array2"
{
}
.assembly extern xunit.core {}
// MVID: {1781A471-82F3-4159-8D0F-B3B5A9FF1BB6}
.namespace JitTest_array2_refany_il
{
  .class public auto ansi Test
         extends [mscorlib]System.Object
  {
    .method private hidebysig static unsigned int64 MyGet(unsigned int64[1000...,1000...],int32,int32) il managed
     {
       .maxstack 8
       ldarg.0
       ldarg.1
       ldc.i4 1000
       add
       ldarg.2
       ldc.i4 1000
       add
       tail. call       instance unsigned int64 unsigned int64[1000...,1000...]::Get(int32,int32)
       ret
     }
    .method private hidebysig static void MySet(unsigned int64[1000...,1000...],int32,int32,unsigned int64) il managed
     {
       .maxstack 8
       ldarg.0
       ldarg.1
       ldc.i4 1000
       add
       ldarg.2
       ldc.i4 1000
       add
       ldarg.3
       tail. call       instance void unsigned int64[1000...,1000...]::Set(int32,
                                                                        int32,
                                                                        unsigned int64)
       ret
     }
    .method private hidebysig static void 
            TestRef(typedref _ref) il managed
    {
      // Code size       78 (0x4e)
      .maxstack  6
      .locals (int32 V_0,
               int32 V_1,
               unsigned int64[0...,0...] V_2,
               int32 V_3,
               int32 V_4)
      IL_0000:  ldarg.0
      IL_0001:  refanytype
      IL_0003:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0008:  ldtoken    unsigned int64[1000...,1000...]
      IL_000d:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0012:  bne.un.s   IL_004d

      IL_0014:  ldc.i4.0
      IL_0015:  stloc.0
      IL_0016:  br.s       IL_0049

      IL_0018:  ldc.i4.0
      IL_0019:  stloc.1
      IL_001a:  br.s       IL_0041

      IL_001c:  ldarg.0
      IL_001d:  refanyval  unsigned int64[1000...,1000...]
      IL_0022:  ldind.ref
      IL_0023:  dup
      IL_0024:  stloc.2
      IL_0025:  ldloc.0
      IL_0026:  dup
      IL_0027:  stloc.3
      IL_0028:  ldloc.1
      IL_0029:  dup
      IL_002a:  stloc.s    V_4
      IL_002c:  ldloc.2
      IL_002d:  ldloc.3
      IL_002e:  ldloc.s    V_4
      
      
      IL_0030:  call  unsigned int64 JitTest_array2_refany_il.Test::MyGet(	unsigned int64[1000...,1000...],
      														int32,
                                                            int32)
      IL_0035:  ldc.i4.1
      IL_0036:  conv.i8
      IL_0037:  sub

      			call  void JitTest_array2_refany_il.Test::MySet(unsigned int64[1000...,1000...],int32,int32,unsigned int64)

      IL_003d:  ldloc.1
      IL_003e:  ldc.i4.1
      IL_003f:  add
      IL_0040:  stloc.1
      IL_0041:  ldloc.1
      IL_0042:  ldc.i4.3
      IL_0043:  blt.s      IL_001c

      IL_0045:  ldloc.0
      IL_0046:  ldc.i4.1
      IL_0047:  add
      IL_0048:  stloc.0
      IL_0049:  ldloc.0
      IL_004a:  ldc.i4.2
      IL_004b:  blt.s      IL_0018

      IL_004d:  ret
    } // end of method Test::TestRef

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       150 (0x96)
      .maxstack  4
      .locals (unsigned int64[1000...,1000...] V_0,
               int32 V_1,
               int32 V_2,
               int32 V_3,
               unsigned int64[1000...,1000...] V_4)
               	ldc.i4 1000
      			ldc.i4.2
      			ldc.i4 1000
      			ldc.i4.3
      			newobj     instance void unsigned int64[1000...,1000...]::.ctor(int32,
                                                                          int32,
                                                                          int32,
                                                                          int32)
      IL_0007:  stloc.s    V_4
      IL_0009:  ldloc.s    V_4
      IL_000b:  ldc.i4 1000
      IL_000c:  ldc.i4 1000
      IL_000d:  ldc.i4.1
      IL_000e:  conv.i8
      IL_000f:  call       instance void unsigned int64[1000...,1000...]::Set(int32,
                                                                        int32,
                                                                        unsigned int64)
      IL_0014:  ldloc.s    V_4
      IL_0016:  ldc.i4 1000
      IL_0017:  ldc.i4 1001
      IL_0018:  ldc.i4.2
      IL_0019:  conv.i8
      IL_001a:  call       instance void unsigned int64[1000...,1000...]::Set(int32,
                                                                        int32,
                                                                        unsigned int64)
      IL_001f:  ldloc.s    V_4
      IL_0021:  ldc.i4 1000
      IL_0022:  ldc.i4 1002
      IL_0023:  ldc.i4.3
      IL_0024:  conv.i8
      IL_0025:  call       instance void unsigned int64[1000...,1000...]::Set(int32,
                                                                        int32,
                                                                        unsigned int64)
      IL_002a:  ldloc.s    V_4
      IL_002c:  ldc.i4 1001
      IL_002d:  ldc.i4 1000
      IL_002e:  ldc.i4.4
      IL_002f:  conv.i8
      IL_0030:  call       instance void unsigned int64[1000...,1000...]::Set(int32,
                                                                        int32,
                                                                        unsigned int64)
      IL_0035:  ldloc.s    V_4
      IL_0037:  ldc.i4 1001
      IL_0038:  ldc.i4 1001
      IL_0039:  ldc.i4.5
      IL_003a:  conv.i8
      IL_003b:  call       instance void unsigned int64[1000...,1000...]::Set(int32,
                                                                        int32,
                                                                        unsigned int64)
      IL_0040:  ldloc.s    V_4
      IL_0042:  ldc.i4 1001
      IL_0043:  ldc.i4 1002
      IL_0044:  ldc.i4.6
      IL_0045:  conv.i8
      IL_0046:  call       instance void unsigned int64[1000...,1000...]::Set(int32,
                                                                        int32,
                                                                        unsigned int64)
      IL_004b:  ldloc.s    V_4
      IL_004d:  stloc.0
      IL_004e:  ldloca.s   V_0
      IL_0050:  mkrefany   unsigned int64[1000...,1000...]
      IL_0055:  call       void JitTest_array2_refany_il.Test::TestRef(typedref)
      IL_005a:  ldc.i4.0
      IL_005b:  stloc.1
      IL_005c:  br.s       IL_0082

      IL_005e:  ldc.i4.0
      IL_005f:  stloc.2
      IL_0060:  br.s       IL_007a

      IL_0062:  ldloc.0
      IL_0063:  ldloc.1
      			ldc.i4 1000
      			add
      IL_0064:  ldloc.2
      			ldc.i4 1000
      			add
      IL_0065:  call       instance unsigned int64 unsigned int64[1000...,1000...]::Get(int32,
                                                                                  int32)
      IL_006a:  ldloc.1
      IL_006b:  ldc.i4.3
      IL_006c:  mul
      IL_006d:  ldloc.2
      IL_006e:  add
      IL_006f:  conv.i8
      IL_0070:  beq.s      IL_0076

      IL_0072:  ldc.i4.3
      IL_0073:  stloc.3
      IL_0074:  br.s       IL_0094

      IL_0076:  ldloc.2
      IL_0077:  ldc.i4.1
      IL_0078:  add
      IL_0079:  stloc.2
      IL_007a:  ldloc.2
      IL_007b:  ldc.i4.3
      IL_007c:  blt.s      IL_0062

      IL_007e:  ldloc.1
      IL_007f:  ldc.i4.1
      IL_0080:  add
      IL_0081:  stloc.1
      IL_0082:  ldloc.1
      IL_0083:  ldc.i4.2
      IL_0084:  blt.s      IL_005e

      IL_0086:  ldstr      "Test passed."
      IL_008b:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_0090:  ldc.i4 100
      IL_0091:  stloc.3
      IL_0092:  br.s       IL_0094

      IL_0094:  ldloc.3
      IL_0095:  ret
    } // end of method Test::Main

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test::.ctor

  } // end of class Test

} // end of namespace JitTest_array2_refany_il

//*********** DISASSEMBLY COMPLETE ***********************
