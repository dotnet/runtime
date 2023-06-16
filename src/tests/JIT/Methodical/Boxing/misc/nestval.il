// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib { }
.assembly ASSEMBLY_NAME { }
.assembly extern xunit.core {}
.namespace BoxTest_nestval_il
{
  .class value auto ansi sealed MyBool extends [mscorlib]System.ValueType
  {
    .field public bool val
  } // end of class 'MyBool'

  .class value auto ansi sealed MyInt extends [mscorlib]System.ValueType
  {
    .field public int32 val
  } // end of class 'MyInt'

  .class value auto ansi sealed ArgInfo extends [mscorlib]System.ValueType
  {
    .field public value class BoxTest_nestval_il.MyBool m_flag
    .field public value class BoxTest_nestval_il.MyInt m_num
  } // end of class 'ArgInfo'

  .class public auto ansi Test extends [mscorlib]System.Object
  {
    .method family hidebysig instance class System.Object 
            Fibonacci(class System.Object args) il managed
    {
      .maxstack  8
      IL_0000:  ldarg.1
      IL_0001:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0006:  ldtoken    BoxTest_nestval_il.ArgInfo
      IL_000b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0010:  beq.s      IL_0018

      IL_0012:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_0017:  throw

      IL_0018:  ldarg.0
      IL_0019:  ldarg.1
      IL_001a:  call       instance class System.Object BoxTest_nestval_il.Test::FibonacciImpl(class System.Object)
      IL_001f:  ret
    } // end of method 'Test::Fibonacci'

    .method family hidebysig instance class System.Object 
            FibonacciImpl(class System.Object args) il managed
    {
      .maxstack  3
      .locals ([0] class System.Object N,
               [1] value class BoxTest_nestval_il.ArgInfo newargs1,
               [2] value class BoxTest_nestval_il.ArgInfo newargs2,
               [3] value class BoxTest_nestval_il.ArgInfo V_3,
               [4] value class BoxTest_nestval_il.MyInt V_4,
               [5] int32 V_5,
               [6] value class BoxTest_nestval_il.MyBool V_6)
      IL_0000:  ldarg.1
      IL_0001:  unbox      BoxTest_nestval_il.ArgInfo
      IL_0006:  ldobj      BoxTest_nestval_il.ArgInfo
      IL_000b:  stloc.3
      IL_000c:  ldloca.s   V_3
      IL_000e:  ldfld      value class BoxTest_nestval_il.MyInt BoxTest_nestval_il.ArgInfo::m_num
      IL_0013:  stloc.s    V_4
      IL_0015:  ldloca.s   V_4
      IL_0017:  ldfld      int32 BoxTest_nestval_il.MyInt::val
      IL_001c:  ldc.i4.1
      IL_001d:  bgt.s      IL_004a

      IL_001f:  ldarg.1
      IL_0020:  unbox      BoxTest_nestval_il.ArgInfo
      IL_0025:  ldobj      BoxTest_nestval_il.ArgInfo
      IL_002a:  stloc.3
      IL_002b:  ldloca.s   V_3
      IL_002d:  ldfld      value class BoxTest_nestval_il.MyInt BoxTest_nestval_il.ArgInfo::m_num
      IL_0032:  stloc.s    V_4
      IL_0034:  ldloca.s   V_4
      IL_0036:  ldfld      int32 BoxTest_nestval_il.MyInt::val
      IL_003b:  stloc.s    V_5
      IL_003d:  ldloc.s   V_5
      IL_003f:  box [mscorlib]System.Int32
      IL_0044:  stloc.0
      IL_0045:  br         IL_0114

      IL_004a:  ldloca.s   newargs1
      IL_004c:  initobj    BoxTest_nestval_il.ArgInfo
      IL_0052:  ldloca.s   newargs1
      IL_0054:  ldflda     value class BoxTest_nestval_il.MyInt BoxTest_nestval_il.ArgInfo::m_num
      IL_0059:  ldarg.1
      IL_005a:  unbox      BoxTest_nestval_il.ArgInfo
      IL_005f:  ldobj      BoxTest_nestval_il.ArgInfo
      IL_0064:  stloc.3
      IL_0065:  ldloca.s   V_3
      IL_0067:  ldfld      value class BoxTest_nestval_il.MyInt BoxTest_nestval_il.ArgInfo::m_num
      IL_006c:  stloc.s    V_4
      IL_006e:  ldloca.s   V_4
      IL_0070:  ldfld      int32 BoxTest_nestval_il.MyInt::val
      IL_0075:  ldc.i4.2
      IL_0076:  sub
      IL_0077:  stfld      int32 BoxTest_nestval_il.MyInt::val
      IL_007c:  ldloca.s   newargs1
      IL_007e:  ldflda     value class BoxTest_nestval_il.MyBool BoxTest_nestval_il.ArgInfo::m_flag
      IL_0083:  ldc.i4.0
      IL_0084:  stfld      bool BoxTest_nestval_il.MyBool::val
      IL_0089:  ldloca.s   newargs2
      IL_008b:  initobj    BoxTest_nestval_il.ArgInfo
      IL_0091:  ldloca.s   newargs2
      IL_0093:  ldflda     value class BoxTest_nestval_il.MyInt BoxTest_nestval_il.ArgInfo::m_num
      IL_0098:  ldarg.1
      IL_0099:  unbox      BoxTest_nestval_il.ArgInfo
      IL_009e:  ldobj      BoxTest_nestval_il.ArgInfo
      IL_00a3:  stloc.3
      IL_00a4:  ldloca.s   V_3
      IL_00a6:  ldfld      value class BoxTest_nestval_il.MyInt BoxTest_nestval_il.ArgInfo::m_num
      IL_00ab:  stloc.s    V_4
      IL_00ad:  ldloca.s   V_4
      IL_00af:  ldfld      int32 BoxTest_nestval_il.MyInt::val
      IL_00b4:  ldc.i4.1
      IL_00b5:  sub
      IL_00b6:  stfld      int32 BoxTest_nestval_il.MyInt::val
      IL_00bb:  ldloca.s   newargs2
      IL_00bd:  ldflda     value class BoxTest_nestval_il.MyBool BoxTest_nestval_il.ArgInfo::m_flag
      IL_00c2:  ldarg.1
      IL_00c3:  unbox      BoxTest_nestval_il.ArgInfo
      IL_00c8:  ldobj      BoxTest_nestval_il.ArgInfo
      IL_00cd:  stloc.3
      IL_00ce:  ldloca.s   V_3
      IL_00d0:  ldfld      value class BoxTest_nestval_il.MyBool BoxTest_nestval_il.ArgInfo::m_flag
      IL_00d5:  stloc.s    V_6
      IL_00d7:  ldloca.s   V_6
      IL_00d9:  ldfld      bool BoxTest_nestval_il.MyBool::val
      IL_00de:  stfld      bool BoxTest_nestval_il.MyBool::val
      IL_00e3:  ldarg.0
      IL_00e4:  ldloc.s   newargs1
      IL_00e6:  box BoxTest_nestval_il.ArgInfo
      IL_00eb:  call       instance class System.Object BoxTest_nestval_il.Test::Fibonacci(class System.Object)
      IL_00f0:  unbox      [mscorlib]System.Int32
      IL_00f5:  ldind.i4
      IL_00f6:  ldarg.0
      IL_00f7:  ldloc.s   newargs2
      IL_00f9:  box BoxTest_nestval_il.ArgInfo
      IL_00fe:  call       instance class System.Object BoxTest_nestval_il.Test::Fibonacci(class System.Object)
      IL_0103:  unbox      [mscorlib]System.Int32
      IL_0108:  ldind.i4
      IL_0109:  add
      IL_010a:  stloc.s    V_5
      IL_010c:  ldloc.s   V_5
      IL_010e:  box [mscorlib]System.Int32
      IL_0113:  stloc.0
      IL_0114:  ldarg.1
      IL_0115:  unbox      BoxTest_nestval_il.ArgInfo
      IL_011a:  ldobj      BoxTest_nestval_il.ArgInfo
      IL_011f:  stloc.3
      IL_0120:  ldloca.s   V_3
      IL_0122:  ldfld      value class BoxTest_nestval_il.MyBool BoxTest_nestval_il.ArgInfo::m_flag
      IL_0127:  stloc.s    V_6
      IL_0129:  ldloca.s   V_6
      IL_012b:  ldfld      bool BoxTest_nestval_il.MyBool::val
      IL_0130:  brfalse.s  IL_017f

      IL_0132:  ldarg.1
      IL_0133:  unbox      BoxTest_nestval_il.ArgInfo
      IL_0138:  ldobj      BoxTest_nestval_il.ArgInfo
      IL_013d:  stloc.3
      IL_013e:  ldloca.s   V_3
      IL_0140:  ldfld      value class BoxTest_nestval_il.MyInt BoxTest_nestval_il.ArgInfo::m_num
      IL_0145:  stloc.s    V_4
      IL_0147:  ldloca.s   V_4
      IL_0149:  ldfld      int32 BoxTest_nestval_il.MyInt::val
      IL_014e:  ldc.i4.2
      IL_014f:  rem
      IL_0150:  ldc.i4.0
      IL_0151:  bne.un.s   IL_016a

      IL_0153:  ldloc.0
      IL_0154:  callvirt   instance class System.String [mscorlib]System.Object::ToString()
      IL_0159:  ldstr      " "
      IL_015e:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0163:  call       void [System.Console]System.Console::Write(class System.String)
      IL_0168:  br.s       IL_017f

      IL_016a:  ldloc.0
      IL_016b:  callvirt   instance class System.String [mscorlib]System.Object::ToString()
      IL_0170:  ldstr      " "
      IL_0175:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_017a:  call       void [System.Console]System.Console::Write(class System.String)
      IL_017f:  ldloc.0
      IL_0180:  ret
    } // end of method 'Test::FibonacciImpl'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  2
      .locals ([0] value class BoxTest_nestval_il.ArgInfo args)
      IL_0000:  ldloca.s   args
      IL_0002:  initobj    BoxTest_nestval_il.ArgInfo
      IL_0008:  ldloca.s   args
      IL_000a:  ldflda     value class BoxTest_nestval_il.MyBool BoxTest_nestval_il.ArgInfo::m_flag
      IL_000f:  ldc.i4.1
      IL_0010:  stfld      bool BoxTest_nestval_il.MyBool::val
      IL_0015:  ldloca.s   args
      IL_0017:  ldflda     value class BoxTest_nestval_il.MyInt BoxTest_nestval_il.ArgInfo::m_num
      IL_001c:  ldc.i4.s   20
      IL_001e:  stfld      int32 BoxTest_nestval_il.MyInt::val
      IL_0023:  newobj     instance void BoxTest_nestval_il.Test::.ctor()
      IL_0028:  ldloc.s   args
      IL_002a:  box BoxTest_nestval_il.ArgInfo
      IL_002f:  call       instance class System.Object BoxTest_nestval_il.Test::Fibonacci(class System.Object)
      IL_0034:  pop
      IL_0035:  call       void [System.Console]System.Console::WriteLine()
      IL_003a:  ldstr      "*** PASSED ***"
      IL_003f:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0044:  ldc.i4   0x64
      IL_0045:  ret
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'BoxTest_nestval_il'

