// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.




.file alignment 512

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib
{
}
.assembly 'jumps5' // as "jumps"
{
                        
}
.assembly extern xunit.core {}
.namespace MS_jumps5_il
{
  .class value public auto ansi sealed VT
         extends [mscorlib]System.ValueType
  {
    .field private class System.String path
    .field private int32 target
    
    .method public hidebysig virtual instance class System.String
            ToString() il managed
    {
		.maxstack  8
				ldarg.0
				dup
				ldfld      class System.String MS_jumps5_il.VT::path
				ldstr      "->ToString"
				call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
				stfld      class System.String MS_jumps5_il.VT::path
      
      			ldarg.0
      			ldfld      int32 MS_jumps5_il.VT::target
      			switch     (case0, case1, case2)
      			newobj     instance void [mscorlib]System.Exception::.ctor()
      			throw

      case0:	ldstr      "VT"
      			ret

      case1:	ldarg.0
      			ldc.i4.0
      			stfld      int32 MS_jumps5_il.VT::target
      			jmp 		instance class System.String MS_jumps5_il.VT::ToStringHelper()

      case2:	ldarg.0
      			ldc.i4.0
      			stfld      int32 MS_jumps5_il.VT::target
      			jmp		instance class System.String MS_jumps5_il.VT::ToString()
    } // end of method VT::ToString

    .method public hidebysig instance class System.String
            ToStringHelper() il managed
    {
      .maxstack  3
      .locals (class System.String V_0,
               int32 V_1)
				ldarg.0
				dup
				ldfld      class System.String MS_jumps5_il.VT::path
				ldstr      "->ToStringHelper"
				call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
				stfld      class System.String MS_jumps5_il.VT::path
      
      			ldarg.0
      			ldfld      int32 MS_jumps5_il.VT::target
      			switch     (case0, case1, case2)
      			newobj     instance void [mscorlib]System.Exception::.ctor()
      			throw

      case0:	ldstr      "VT"
      			ret

      case1:	ldarg.0
      			ldc.i4.0
      			stfld      int32 MS_jumps5_il.VT::target
      			jmp		instance class System.String MS_jumps5_il.VT::ToStringHelper()

      case2:	ldarg.0
      			ldc.i4.0
      			stfld      int32 MS_jumps5_il.VT::target
            	jmp		instance class System.String MS_jumps5_il.VT::ToString()
            	
    } // end of method VT::ToStringHelper

    .method private hidebysig instance bool
            RunTests() il managed
    {
      .maxstack  3
      .locals (bool V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.0
      IL_0002:  stfld      int32 MS_jumps5_il.VT::target
      IL_0007:  ldarg.0
      IL_0008:  ldnull
      IL_0009:  stfld      class System.String MS_jumps5_il.VT::path
      IL_000e:  ldarg.0
      IL_000f:  call       instance class System.String MS_jumps5_il.VT::ToStringHelper()
      IL_0014:  ldstr      " : "
      IL_0019:  ldarg.0
      IL_001a:  ldfld      class System.String MS_jumps5_il.VT::path
      IL_001f:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String,
                                                                               class System.String)
      IL_0024:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0029:  ldarg.0
      IL_002a:  ldfld      class System.String MS_jumps5_il.VT::path
      IL_002f:  ldstr      "->ToStringHelper"
      IL_0034:  call       bool [mscorlib]System.String::Equals(class System.String,
                                                                class System.String)
      IL_0039:  brtrue.s   IL_0042

      IL_003b:  ldc.i4.0
      IL_003c:  stloc.0
      IL_003d:  br         IL_018a

      IL_0042:  ldarg.0
      IL_0043:  ldc.i4.1
      IL_0044:  stfld      int32 MS_jumps5_il.VT::target
      IL_0049:  ldarg.0
      IL_004a:  ldnull
      IL_004b:  stfld      class System.String MS_jumps5_il.VT::path
      IL_0050:  ldarg.0
      IL_0051:  call       instance class System.String MS_jumps5_il.VT::ToStringHelper()
      IL_0056:  ldstr      " : "
      IL_005b:  ldarg.0
      IL_005c:  ldfld      class System.String MS_jumps5_il.VT::path
      IL_0061:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String,
                                                                               class System.String)
      IL_0066:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_006b:  ldarg.0
      IL_006c:  ldfld      class System.String MS_jumps5_il.VT::path
      IL_0071:  ldstr      "->ToStringHelper->ToStringHelper"
      IL_0076:  call       bool [mscorlib]System.String::Equals(class System.String,
                                                                class System.String)
      IL_007b:  brtrue.s   IL_0084

      IL_007d:  ldc.i4.0
      IL_007e:  stloc.0
      IL_007f:  br         IL_018a

      IL_0084:  ldarg.0
      IL_0085:  ldc.i4.2
      IL_0086:  stfld      int32 MS_jumps5_il.VT::target
      IL_008b:  ldarg.0
      IL_008c:  ldnull
      IL_008d:  stfld      class System.String MS_jumps5_il.VT::path
      IL_0092:  ldarg.0
      IL_0093:  call       instance class System.String MS_jumps5_il.VT::ToStringHelper()
      IL_0098:  ldstr      " : "
      IL_009d:  ldarg.0
      IL_009e:  ldfld      class System.String MS_jumps5_il.VT::path
      IL_00a3:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String,
                                                                               class System.String)
      IL_00a8:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_00ad:  ldarg.0
      IL_00ae:  ldfld      class System.String MS_jumps5_il.VT::path
      IL_00b3:  ldstr      "->ToStringHelper->ToString"
      IL_00b8:  call       bool [mscorlib]System.String::Equals(class System.String,
                                                                class System.String)
      IL_00bd:  brtrue.s   IL_00c6

      IL_00bf:  ldc.i4.0
      IL_00c0:  stloc.0
      IL_00c1:  br         IL_018a

      IL_00c6:  ldarg.0
      IL_00c7:  ldc.i4.0
      IL_00c8:  stfld      int32 MS_jumps5_il.VT::target
      IL_00cd:  ldarg.0
      IL_00ce:  ldnull
      IL_00cf:  stfld      class System.String MS_jumps5_il.VT::path
      IL_00d4:  ldarg.0
      IL_00d5:  call       instance class System.String MS_jumps5_il.VT::ToString()
      IL_00da:  ldstr      " : "
      IL_00df:  ldarg.0
      IL_00e0:  ldfld      class System.String MS_jumps5_il.VT::path
      IL_00e5:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String,
                                                                               class System.String)
      IL_00ea:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_00ef:  ldarg.0
      IL_00f0:  ldfld      class System.String MS_jumps5_il.VT::path
      IL_00f5:  ldstr      "->ToString"
      IL_00fa:  call       bool [mscorlib]System.String::Equals(class System.String,
                                                                class System.String)
      IL_00ff:  brtrue.s   IL_0108

      IL_0101:  ldc.i4.0
      IL_0102:  stloc.0
      IL_0103:  br         IL_018a

      IL_0108:  ldarg.0
      IL_0109:  ldc.i4.1
      IL_010a:  stfld      int32 MS_jumps5_il.VT::target
      IL_010f:  ldarg.0
      IL_0110:  ldnull
      IL_0111:  stfld      class System.String MS_jumps5_il.VT::path
      IL_0116:  ldarg.0
      IL_0117:  call       instance class System.String MS_jumps5_il.VT::ToString()
      IL_011c:  ldstr      " : "
      IL_0121:  ldarg.0
      IL_0122:  ldfld      class System.String MS_jumps5_il.VT::path
      IL_0127:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String,
                                                                               class System.String)
      IL_012c:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0131:  ldarg.0
      IL_0132:  ldfld      class System.String MS_jumps5_il.VT::path
      IL_0137:  ldstr      "->ToString->ToStringHelper"
      IL_013c:  call       bool [mscorlib]System.String::Equals(class System.String,
                                                                class System.String)
      IL_0141:  brtrue.s   IL_0147

      IL_0143:  ldc.i4.0
      IL_0144:  stloc.0
      IL_0145:  br.s       IL_018a

      IL_0147:  ldarg.0
      IL_0148:  ldc.i4.2
      IL_0149:  stfld      int32 MS_jumps5_il.VT::target
      IL_014e:  ldarg.0
      IL_014f:  ldnull
      IL_0150:  stfld      class System.String MS_jumps5_il.VT::path
      IL_0155:  ldarg.0
      IL_0156:  call       instance class System.String MS_jumps5_il.VT::ToString()
      IL_015b:  ldstr      " : "
      IL_0160:  ldarg.0
      IL_0161:  ldfld      class System.String MS_jumps5_il.VT::path
      IL_0166:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String,
                                                                               class System.String)
      IL_016b:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0170:  ldarg.0
      IL_0171:  ldfld      class System.String MS_jumps5_il.VT::path
      IL_0176:  ldstr      "->ToString->ToString"
      IL_017b:  call       bool [mscorlib]System.String::Equals(class System.String,
                                                                class System.String)
      IL_0180:  brtrue.s   IL_0186

      IL_0182:  ldc.i4.0
      IL_0183:  stloc.0
      IL_0184:  br.s       IL_018a

      IL_0186:  ldc.i4.1
      IL_0187:  stloc.0
      IL_0188:  br.s       IL_018a

      IL_018a:  ldloc.0
      IL_018b:  ret
    } // end of method VT::RunTests

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  1
      .locals (int32 V_0,
               value class MS_jumps5_il.VT V_1)
      IL_0000:  ldloca.s   V_1
      IL_0002:  initobj    MS_jumps5_il.VT
      IL_0008:  ldloc.1
      IL_0009:  stloc.1
      IL_000a:  ldloca.s   V_1
      IL_000c:  call       instance bool MS_jumps5_il.VT::RunTests()
      IL_0011:  brfalse.s  IL_0022

      IL_0013:  ldstr      "PASSED."
      IL_0018:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_001d:  ldc.i4.s   100
      IL_001f:  stloc.0
      IL_0020:  br.s       IL_0031

      IL_0022:  ldstr      "FAILED."
      IL_0027:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_002c:  ldc.i4.s   101
      IL_002e:  stloc.0
      IL_002f:  br.s       IL_0031

      IL_0031:  ldloc.0
      IL_0032:  ret
    } // end of method VT::Main

  } // end of class VT

} // end of namespace MS_jumps5_il

