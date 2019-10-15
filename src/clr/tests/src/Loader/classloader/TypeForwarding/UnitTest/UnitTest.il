// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { } 

.assembly extern MyDep
{
  .ver 1:0:0:0
}

.assembly UnitTest { }



.class public auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main(string[] args) cil managed noinlining
  {
    .entrypoint
    // Code size       410 (0x19a)
    .maxstack  3
    .locals init ([0] int32 V_0)
    IL_0000:  nop
    IL_0001:  nop
    IL_0002:  newobj     instance void [MyDep]MyDep.Enclosing1::.ctor()
    IL_0007:  pop
    IL_0008:  ldstr      "MyDep.Enclosing2, MyDep, Version=1.0.0.0, Culture="
    + "neutral, PublicKeyToken=null"
    IL_000d:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string)
    IL_0012:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_0017:  pop
    IL_0018:  newobj     instance void [MyDep]MyDep.Enclosing1/Nested1/SubNested1::.ctor()
    IL_001d:  pop
    IL_001e:  ldstr      "MyDep.Enclosing1+Nested1+SubNested2, MyDep, Versio"
    + "n=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    IL_0023:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string)
    IL_0028:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_002d:  pop
    IL_002e:  newobj     instance void [MyDep]MyDep.Enclosing1/Nested2/SubNested1::.ctor()
    IL_0033:  pop
    IL_0034:  ldstr      "MyDep.Enclosing1+Nested2+SubNested2, MyDep, Versio"
    + "n=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    IL_0039:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string)
    IL_003e:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_0043:  pop
    IL_0044:  ldstr      "MyDep.Enclosing2+Nested1+SubNested1, MyDep, Versio"
    + "n=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    IL_0049:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string)
    IL_004e:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_0053:  pop
    IL_0054:  ldstr      "MyDEP.EnclosING2+NesTED1+SubNesTED2, MyDep, Versio"
    + "n=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    IL_0059:  ldc.i4.1
    IL_005a:  ldc.i4.1
    IL_005b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string,
                                                                                                bool,
                                                                                                bool)
    IL_0060:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_0065:  pop
    IL_0066:  ldstr      "MyDep.Enclosing2+Nested2+SubNested1, MyDep, Versio"
    + "n=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    IL_006b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string)
    IL_0070:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_0075:  pop
    IL_0076:  ldstr      "MyDep.Enclosing2+Nested2+SubNested2, MyDep, Versio"
    + "n=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    IL_007b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string)
    IL_0080:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_0085:  pop
    IL_0086:  nop
    IL_0087:  nop
    IL_0088:  newobj     instance void [MyDep]MyDep2.Enclosing1::.ctor()
    IL_008d:  pop
    IL_008e:  ldstr      "MyDep2.Enclosing2, MyDep, Version=1.0.0.0, Culture"
    + "=neutral, PublicKeyToken=null"
    IL_0093:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string)
    IL_0098:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_009d:  pop
    IL_009e:  newobj     instance void [MyDep]MyDep2.Enclosing1/Nested1/SubNested1::.ctor()
    IL_00a3:  pop
    IL_00a4:  ldstr      "MyDep2.Enclosing1+Nested1+SubNested2, MyDep, Versi"
    + "on=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    IL_00a9:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string)
    IL_00ae:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_00b3:  pop
    IL_00b4:  newobj     instance void [MyDep]MyDep2.Enclosing1/Nested2/SubNested1::.ctor()
    IL_00b9:  pop
    IL_00ba:  ldstr      "MyDep2.Enclosing1+Nested2+SubNested2, MyDep, Versi"
    + "on=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    IL_00bf:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string)
    IL_00c4:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_00c9:  pop
    IL_00ca:  ldstr      "MyDep2.Enclosing2+Nested1+SubNested1, MyDep, Versi"
    + "on=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    IL_00cf:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string)
    IL_00d4:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_00d9:  pop
    IL_00da:  ldstr      "MyDEP2.EnclosING2+NesTED1+SubNesTED2, MyDep, Versi"
    + "on=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    IL_00df:  ldc.i4.1
    IL_00e0:  ldc.i4.1
    IL_00e1:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string,
                                                                                                bool,
                                                                                                bool)
    IL_00e6:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_00eb:  pop
    IL_00ec:  ldstr      "MyDep2.Enclosing2+Nested2+SubNested1, MyDep, Versi"
    + "on=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    IL_00f1:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string)
    IL_00f6:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_00fb:  pop
    IL_00fc:  ldstr      "MyDep2.Enclosing2+Nested2+SubNested2, MyDep, Versi"
    + "on=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    IL_0101:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string)
    IL_0106:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_010b:  pop
    IL_010c:  nop
    IL_010d:  nop
    IL_010e:  newobj     instance void [MyDep]MyDep3.Enclosing1::.ctor()
    IL_0113:  pop
    IL_0114:  ldstr      "MyDep3.Enclosing2, MyDep, Version=1.0.0.0, Culture"
    + "=neutral, PublicKeyToken=null"
    IL_0119:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string)
    IL_011e:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_0123:  pop
    IL_0124:  newobj     instance void [MyDep]MyDep3.Enclosing1/Nested1/SubNested1::.ctor()
    IL_0129:  pop
    IL_012a:  ldstr      "MyDep3.Enclosing1+Nested1+SubNested2, MyDep, Versi"
    + "on=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    IL_012f:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string)
    IL_0134:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_0139:  pop
    IL_013a:  newobj     instance void [MyDep]MyDep3.Enclosing1/Nested2/SubNested1::.ctor()
    IL_013f:  pop
    IL_0140:  ldstr      "MyDep3.Enclosing1+Nested2+SubNested2, MyDep, Versi"
    + "on=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    IL_0145:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string)
    IL_014a:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_014f:  pop
    IL_0150:  ldstr      "MyDep3.Enclosing2+Nested1+SubNested1, MyDep, Versi"
    + "on=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    IL_0155:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string)
    IL_015a:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_015f:  pop
    IL_0160:  ldstr      "MyDEP3.EnclosING2+NesTED1+SubNesTED2, MyDep, Versi"
    + "on=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    IL_0165:  ldc.i4.1
    IL_0166:  ldc.i4.1
    IL_0167:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string,
                                                                                                bool,
                                                                                                bool)
    IL_016c:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_0171:  pop
    IL_0172:  ldstr      "MyDep3.Enclosing2+Nested2+SubNested1, MyDep, Versi"
    + "on=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    IL_0177:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string)
    IL_017c:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_0181:  pop
    IL_0182:  ldstr      "MyDep3.Enclosing2+Nested2+SubNested2, MyDep, Versi"
    + "on=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    IL_0187:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string)
    IL_018c:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_0191:  pop
    IL_0192:  nop
    IL_0193:  ldc.i4.s   100
    IL_0195:  stloc.0
    IL_0196:  br.s       IL_0198

    IL_0198:  ldloc.0
    IL_0199:  ret
  } // end of method Program::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Program::.ctor

}