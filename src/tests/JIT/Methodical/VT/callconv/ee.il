// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.




.file alignment 512

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME // as "call"
{
}
.assembly extern xunit.core {}
// MVID: {CF1D567F-C985-45B8-8028-219BDC43D791}
.namespace JitTest_ee_il
{
  .class interface private abstract auto ansi ISomething
  {
    .method public hidebysig newslot virtual abstract 
            instance void  DoSomething(value class JitTest_ee_il.VT vt1,
                                       value class JitTest_ee_il.VT& vt2) il managed
    {
    } // end of method ISomething::DoSomething

    .method public hidebysig newslot virtual abstract 
            instance value class JitTest_ee_il.VT 
            RetSomething(value class JitTest_ee_il.VT vt) il managed
    {
    } // end of method ISomething::RetSomething

  } // end of class ISomething

  .class value public auto ansi sealed VT
         extends [mscorlib]System.ValueType
         implements JitTest_ee_il.ISomething
  {
    .field private int32 m_vn
    .field private class [mscorlib]System.String m_vs
    .method private hidebysig specialname rtspecialname 
            instance void  .ctor(int32 vn,
                                 class [mscorlib]System.String vs) il managed
    {
      // Code size       15 (0xf)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  stfld      int32 JitTest_ee_il.VT::m_vn
      IL_0007:  ldarg.0
      IL_0008:  ldarg.2
      IL_0009:  stfld      class [mscorlib]System.String JitTest_ee_il.VT::m_vs
      IL_000e:  ret
    } // end of method VT::.ctor

    .method public hidebysig newslot final virtual 
            instance void  DoSomething(value class JitTest_ee_il.VT vt1,
                                       value class JitTest_ee_il.VT& vt2) il managed
    {
      // Code size       80 (0x50)
      .maxstack  3
      .locals (class [mscorlib]System.String[] V_0)
      IL_0000:  ldc.i4.4
      IL_0001:  newarr     [mscorlib]System.String
      IL_0006:  stloc.0
      IL_0007:  ldloc.0
      IL_0008:  ldc.i4.0
      IL_0009:  ldstr      "ISomething::DoSomething - "
      IL_000e:  stelem.ref
      IL_000f:  ldloc.0
      IL_0010:  ldc.i4.1
      IL_0011:  ldarga.s   vt1
      IL_0013:  call       instance class [mscorlib]System.String JitTest_ee_il.VT::ToString()
      IL_0018:  stelem.ref
      IL_0019:  ldloc.0
      IL_001a:  ldc.i4.2
      IL_001b:  ldstr      " "
      IL_0020:  stelem.ref
      IL_0021:  ldloc.0
      IL_0022:  ldc.i4.3
      IL_0023:  ldarg.2
      IL_0024:  call       instance class [mscorlib]System.String JitTest_ee_il.VT::ToString()
      IL_0029:  stelem.ref
      IL_002a:  ldloc.0
      IL_002b:  call       class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String[])
      IL_0030:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_0035:  ldstr      "this is {"
      IL_003a:  ldarg.0
      IL_003b:  call       instance class [mscorlib]System.String JitTest_ee_il.VT::ToString()
      IL_0040:  ldstr      "}"
      IL_0045:  call       class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String,
                                                                               class [mscorlib]System.String,
                                                                               class [mscorlib]System.String)
      IL_004a:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_004f:  ret
    } // end of method VT::DoSomething

    .method public hidebysig newslot final virtual 
            instance value class JitTest_ee_il.VT 
            RetSomething(value class JitTest_ee_il.VT vt) il managed
    {
      // Code size       68 (0x44)
      .maxstack  3
      IL_0000:  ldstr      "ISomething::RetSomething - "
      IL_0005:  ldarga.s   vt
      IL_0007:  call       instance class [mscorlib]System.String JitTest_ee_il.VT::ToString()
      IL_000c:  call       class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String,
                                                                               class [mscorlib]System.String)
      IL_0011:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_0016:  ldstr      "this is {"
      IL_001b:  ldarg.0
      IL_001c:  call       instance class [mscorlib]System.String JitTest_ee_il.VT::ToString()
      IL_0021:  ldstr      "}"
      IL_0026:  call       class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String,
                                                                               class [mscorlib]System.String,
                                                                               class [mscorlib]System.String)
      IL_002b:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_0030:  ldarga.s   vt
      IL_0032:  ldfld      int32 JitTest_ee_il.VT::m_vn
      IL_0037:  ldarga.s   vt
      IL_0039:  ldfld      class [mscorlib]System.String JitTest_ee_il.VT::m_vs
      IL_003e:  newobj     instance void JitTest_ee_il.VT::.ctor(int32,
                                                           class [mscorlib]System.String)
      IL_0043:  ret
    } // end of method VT::RetSomething

    .method public hidebysig instance void 
            _DoSomething(value class JitTest_ee_il.VT vt1,
                         value class JitTest_ee_il.VT& vt2) il managed
    {
      // Code size       80 (0x50)
      .maxstack  3
      .locals (class [mscorlib]System.String[] V_0)
      IL_0000:  ldc.i4.4
      IL_0001:  newarr     [mscorlib]System.String
      IL_0006:  stloc.0
      IL_0007:  ldloc.0
      IL_0008:  ldc.i4.0
      IL_0009:  ldstr      "ISomething::DoSomething - "
      IL_000e:  stelem.ref
      IL_000f:  ldloc.0
      IL_0010:  ldc.i4.1
      IL_0011:  ldarga.s   vt1
      IL_0013:  call       instance class [mscorlib]System.String JitTest_ee_il.VT::ToString()
      IL_0018:  stelem.ref
      IL_0019:  ldloc.0
      IL_001a:  ldc.i4.2
      IL_001b:  ldstr      " "
      IL_0020:  stelem.ref
      IL_0021:  ldloc.0
      IL_0022:  ldc.i4.3
      IL_0023:  ldarg.2
      IL_0024:  call       instance class [mscorlib]System.String JitTest_ee_il.VT::ToString()
      IL_0029:  stelem.ref
      IL_002a:  ldloc.0
      IL_002b:  call       class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String[])
      IL_0030:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_0035:  ldstr      "this is {"
      IL_003a:  ldarg.0
      IL_003b:  call       instance class [mscorlib]System.String JitTest_ee_il.VT::ToString()
      IL_0040:  ldstr      "}"
      IL_0045:  call       class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String,
                                                                               class [mscorlib]System.String,
                                                                               class [mscorlib]System.String)
      IL_004a:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_004f:  ret
    } // end of method VT::_DoSomething

    .method public hidebysig instance value class JitTest_ee_il.VT 
            _RetSomething(value class JitTest_ee_il.VT vt) il managed
    {
      // Code size       68 (0x44)
      .maxstack  3
      IL_0000:  ldstr      "ISomething::RetSomething - "
      IL_0005:  ldarga.s   vt
      IL_0007:  call       instance class [mscorlib]System.String JitTest_ee_il.VT::ToString()
      IL_000c:  call       class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String,
                                                                               class [mscorlib]System.String)
      IL_0011:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_0016:  ldstr      "this is {"
      IL_001b:  ldarg.0
      IL_001c:  call       instance class [mscorlib]System.String JitTest_ee_il.VT::ToString()
      IL_0021:  ldstr      "}"
      IL_0026:  call       class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String,
                                                                               class [mscorlib]System.String,
                                                                               class [mscorlib]System.String)
      IL_002b:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_0030:  ldarga.s   vt
      IL_0032:  ldfld      int32 JitTest_ee_il.VT::m_vn
      IL_0037:  ldarga.s   vt
      IL_0039:  ldfld      class [mscorlib]System.String JitTest_ee_il.VT::m_vs
      IL_003e:  newobj     instance void JitTest_ee_il.VT::.ctor(int32,
                                                           class [mscorlib]System.String)
      IL_0043:  ret
    } // end of method VT::_RetSomething

    .method public hidebysig virtual instance class [mscorlib]System.String
            ToString() il managed
    {
      // Code size       28 (0x1c)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldflda     int32 JitTest_ee_il.VT::m_vn
      IL_0006:  call       instance class [mscorlib]System.String [mscorlib]System.Int32::ToString()
      IL_000b:  ldstr      "/"
      IL_0010:  ldarg.0
      IL_0011:  ldfld      class [mscorlib]System.String JitTest_ee_il.VT::m_vs
      IL_0016:  call       class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String,
                                                                               class [mscorlib]System.String,
                                                                               class [mscorlib]System.String)
      IL_001b:  ret
    } // end of method VT::ToString

    .method private hidebysig static void 
            TryInterface(class JitTest_ee_il.ISomething smth) il managed
    {
      // Code size       64 (0x40)
      .maxstack  4
      .locals (value class JitTest_ee_il.VT V_0)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldc.i4.s   11
      IL_0004:  ldstr      "11"
      IL_0009:  call       instance void JitTest_ee_il.VT::.ctor(int32,
                                                           class [mscorlib]System.String)
      IL_000e:  ldarg.0
      IL_000f:  ldc.i4.0
      IL_0010:  ldstr      "-"
      IL_0015:  newobj     instance void JitTest_ee_il.VT::.ctor(int32,
                                                           class [mscorlib]System.String)
      IL_001a:  ldloca.s   V_0
      IL_001c:  callvirt   instance void JitTest_ee_il.ISomething::DoSomething(value class JitTest_ee_il.VT,
                                                                         value class JitTest_ee_il.VT&)
      IL_0021:  ldarg.0
      IL_0022:  ldloc.0
      IL_0023:  callvirt   instance value class JitTest_ee_il.VT JitTest_ee_il.ISomething::RetSomething(value class JitTest_ee_il.VT)
      IL_0028:  stloc.0
      IL_0029:  ldstr      "Got : "
      IL_002e:  ldloca.s   V_0
      IL_0030:  call       instance class [mscorlib]System.String JitTest_ee_il.VT::ToString()
      IL_0035:  call       class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String,
                                                                               class [mscorlib]System.String)
      IL_003a:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_003f:  ret
    } // end of method VT::TryInterface

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       227 (0xe3)
      .maxstack  4
      .locals (value class JitTest_ee_il.VT V_0)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldc.i4.s   10
      IL_0004:  ldstr      "10"
      IL_0009:  call       instance void JitTest_ee_il.VT::.ctor(int32,
                                                           class [mscorlib]System.String)
      IL_000e:  ldloca.s   V_0
      IL_0010:  ldc.i4.0
      IL_0011:  ldstr      "-"
      IL_0016:  newobj     instance void JitTest_ee_il.VT::.ctor(int32,
                                                           class [mscorlib]System.String)
      IL_001b:  ldloca.s   V_0
      IL_001d:  call       instance void JitTest_ee_il.VT::_DoSomething(value class JitTest_ee_il.VT,
                                                                  value class JitTest_ee_il.VT&)
      IL_0022:  ldloca.s   V_0
      IL_0024:  ldloc.0
      IL_0025:  call       instance value class JitTest_ee_il.VT JitTest_ee_il.VT::_RetSomething(value class JitTest_ee_il.VT)
      IL_002a:  stloc.0
      IL_002b:  ldstr      "Got : "
      IL_0030:  ldloc.s   V_0
      			box   JitTest_ee_il.VT
      IL_0032:  callvirt   instance class [mscorlib]System.String [mscorlib]System.Object::ToString()
      IL_0037:  call       class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String,
                                                                               class [mscorlib]System.String)
      IL_003c:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_0041:  ldloca.s   V_0
      IL_0043:  ldfld      int32 JitTest_ee_il.VT::m_vn
      IL_0048:  ldc.i4.s   10
      IL_004a:  bne.un.s   IL_005f

      IL_004c:  ldloca.s   V_0
      IL_004e:  ldfld      class [mscorlib]System.String JitTest_ee_il.VT::m_vs
      IL_0053:  ldstr      "10"
      IL_0058:  call       bool [mscorlib]System.String::Equals(class [mscorlib]System.String,
                                                                class [mscorlib]System.String)
      IL_005d:  brtrue.s   IL_006c

      IL_005f:  ldstr      "Failed"
      IL_0064:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_0069:  ldc.i4.s   101
      IL_006b:  ret

      IL_006c:  ldloc.s   V_0
      			box   JitTest_ee_il.VT
      			castclass JitTest_ee_il.ISomething
      
      IL_006e:  ldc.i4.0
      IL_006f:  ldstr      "-"
      IL_0074:  newobj     instance void JitTest_ee_il.VT::.ctor(int32,
                                                           class [mscorlib]System.String)
      IL_0079:  ldloca.s   V_0
      IL_007b:  callvirt       instance void JitTest_ee_il.ISomething::DoSomething(value class JitTest_ee_il.VT,
                                                                 value class JitTest_ee_il.VT&)
      IL_0080:  ldloc.s   V_0
      			box   JitTest_ee_il.VT
      			castclass JitTest_ee_il.ISomething
      			
      IL_0082:  ldloc.0
      IL_0083:  callvirt       instance value class JitTest_ee_il.VT JitTest_ee_il.ISomething::RetSomething(value class JitTest_ee_il.VT)
      IL_0088:  stloc.0
      IL_0089:  ldstr      "Got : "
      IL_008e:  ldloc.s   V_0
      			box   JitTest_ee_il.VT
      IL_0090:  callvirt       instance class [mscorlib]System.String [mscorlib]System.Object::ToString()
      IL_0095:  call       class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String,
                                                                               class [mscorlib]System.String)
      IL_009a:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_009f:  ldloca.s   V_0
      IL_00a1:  ldfld      int32 JitTest_ee_il.VT::m_vn
      IL_00a6:  ldc.i4.s   10
      IL_00a8:  bne.un.s   IL_00bd

      IL_00aa:  ldloca.s   V_0
      IL_00ac:  ldfld      class [mscorlib]System.String JitTest_ee_il.VT::m_vs
      IL_00b1:  ldstr      "10"
      IL_00b6:  call       bool [mscorlib]System.String::Equals(class [mscorlib]System.String,
                                                                class [mscorlib]System.String)
      IL_00bb:  brtrue.s   IL_00ca

      IL_00bd:  ldstr      "Failed"
      IL_00c2:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_00c7:  ldc.i4.s   101
      IL_00c9:  ret

      IL_00ca:  ldloc.s   V_0
      IL_00cc:  box        JitTest_ee_il.VT
      IL_00d1:  call       void JitTest_ee_il.VT::TryInterface(class JitTest_ee_il.ISomething)
      IL_00d6:  ldstr      "Passed"
      IL_00db:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_00e0:  ldc.i4.s   100
      IL_00e2:  ret
    } // end of method VT::Main

  } // end of class VT

} // end of namespace JitTest_ee_il

//*********** DISASSEMBLY COMPLETE ***********************
