// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.







.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib
{
}
.assembly 'arrres'// as "arrres"
{
}
.assembly extern xunit.core {}
.namespace GCTest_arrres_il
{
  .class public auto ansi Test
         extends [mscorlib]System.Object
  {
    .field private int32 m_indx
    .field private int32 m_resCount
    .field private static class GCTest_arrres_il.Test[-100...100] arr
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(int32 indx) il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldarg.1
      IL_0008:  stfld      int32 GCTest_arrres_il.Test::m_indx
      IL_000d:  ldarg.0
      IL_000e:  ldc.i4.0
      IL_000f:  stfld      int32 GCTest_arrres_il.Test::m_resCount
      IL_0014:  ret
    } // end of method 'Test::.ctor'

    .method public hidebysig newslot virtual 
            instance void CheckValid() il managed
    {
      .maxstack  10
      IL_0000:  ldsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
                dup
      			brinst.s   IL_0005
      			
      			pop
      			ldstr		"Warning: Array is NULL in CheckValid"
      			call		void [System.Console]System.Console::WriteLine(string)
      			ret

      IL_0005:  ldarg.0
      IL_0006:  ldfld      int32 GCTest_arrres_il.Test::m_indx
      

      IL_000b:  callvirt   instance class GCTest_arrres_il.Test class GCTest_arrres_il.Test[-100...100]::Get(int32)
      IL_000c:  ldarg.0
      IL_000d:  beq.s      IL_0015

      IL_000f:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_0014:  throw

      IL_0015:  ret
    } // end of method 'Test::CheckValid'

    .method family hidebysig virtual instance void
            Finalize() il managed
    {
      .maxstack  5
      .locals (string[] V_0,
               int32 V_1)
      			ldarg.0
      			ldfld      int32 GCTest_arrres_il.Test::m_indx
      			ldc.i4 -10000
      			beq   IL_RET
      			
      IL_0000:  ldsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
                dup
      			brinst.s   IL_0005
      			
      			pop
      			ldstr		"Warning: Array is NULL in Finalize"
      			call		void [System.Console]System.Console::WriteLine(string)
      			ret
      
      IL_0005:  ldarg.0
      IL_0006:  ldfld      int32 GCTest_arrres_il.Test::m_indx
      IL_000b:  callvirt   instance class GCTest_arrres_il.Test class GCTest_arrres_il.Test[-100...100]::Get(int32)
      IL_000c:  brfalse.s  IL_0014

      IL_000e:  newobj     instance void [mscorlib]System.Exception::.ctor()
      			throw

      IL_0014:  ldsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
      IL_0019:  ldarg.0
      IL_001a:  ldfld      int32 GCTest_arrres_il.Test::m_indx
      IL_001f:  ldarg.0
      IL_0020:  callvirt   instance void class GCTest_arrres_il.Test[-100...100]::Set(int32, class GCTest_arrres_il.Test)
      IL_0021:  ldarg.0
      IL_0022:  call       void [mscorlib]System.GC::ReRegisterForFinalize(class [mscorlib]System.Object)
      IL_0027:  ldc.i4.5
      IL_0028:  newarr     [mscorlib]System.String
      IL_002d:  stloc.0
      IL_002e:  ldloc.0
      IL_002f:  ldc.i4.0
      			ldstr      "Object at index ["
      			stelem.ref
      IL_0036:  ldloc.0
      IL_0037:  ldc.i4.1
      IL_0038:  ldarg.0
      IL_0039:  ldflda     int32 GCTest_arrres_il.Test::m_indx
      IL_003e:  call       instance string [mscorlib]System.Int32::ToString()
      IL_0043:  stelem.ref
      IL_0044:  ldloc.0
      IL_0045:  ldc.i4.2
      IL_0046:  ldstr      "] has been resurrected for the "
      IL_004b:  stelem.ref
      IL_004c:  ldloc.0
      IL_004d:  ldc.i4.3
      IL_004e:  ldarg.0
      IL_004f:  dup
      IL_0050:  ldfld      int32 GCTest_arrres_il.Test::m_resCount
      IL_0055:  ldc.i4.1
      IL_0056:  add
      IL_0057:  dup
      IL_0058:  stloc.1
      IL_0059:  stfld      int32 GCTest_arrres_il.Test::m_resCount
      IL_005e:  ldloc.1
      IL_005f:  stloc.1
      IL_0060:  ldloca.s   V_1
      IL_0062:  call       instance string [mscorlib]System.Int32::ToString()
      IL_0067:  stelem.ref
      IL_0068:  ldloc.0
      IL_0069:  ldc.i4.4
      IL_006a:  ldstr      " time."
      IL_006f:  stelem.ref
      IL_0070:  ldloc.0
      IL_0071:  call       string [mscorlib]System.String::Concat(string[])
               pop
     IL_RET:
      IL_007b:  ret
    } // end of method 'Test::Finalize'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  4
      .locals (int32 V_0,
               int32 V_1,
               int32 V_2,
               int32 V_3,
               int32 V_4,
               int32 V_5,
               int32 V_6)
      IL_0000:  ldc.i4.s   -100
      IL_0002:  stloc.0
      IL_0003:  br.s       IL_0017

      IL_0005:  ldsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
      IL_000a:  ldloc.0
      IL_000b:  ldloc.0
      IL_000c:  newobj     instance void GCTest_arrres_il.Test::.ctor(int32)
      IL_0011:  callvirt   instance void class GCTest_arrres_il.Test[-100...100]::Set(int32, class GCTest_arrres_il.Test)
      IL_0012:  ldloc.0
      IL_0013:  ldc.i4.s   50
      IL_0015:  add
      IL_0016:  stloc.0
      IL_0017:  ldloc.0
      IL_0018:  ldc.i4.s   100
      IL_001a:  ble.s      IL_0005

      IL_001c:  call       void [mscorlib]System.GC::Collect()
      IL_0021:  call       void [mscorlib]System.GC::WaitForPendingFinalizers()
      IL_0026:  call       void [mscorlib]System.GC::Collect()
      
      IL_002b:  ldc.i4.s   -100
      IL_002d:  stloc.1
      IL_002e:  br.s       IL_0058

      IL_0030:  ldsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
      IL_0035:  ldloc.1
      IL_0036:  callvirt   instance class GCTest_arrres_il.Test class GCTest_arrres_il.Test[-100...100]::Get(int32)
      IL_0037:  brtrue.s   IL_003f

      IL_0039:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_003e:  throw

      IL_003f:  ldsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
      IL_0044:  ldloc.1
      IL_0045:  callvirt   instance class GCTest_arrres_il.Test class GCTest_arrres_il.Test[-100...100]::Get(int32)
      IL_0046:  callvirt   instance void GCTest_arrres_il.Test::CheckValid()
      IL_004b:  ldsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
      IL_0050:  ldloc.1
      IL_0051:  ldnull
      
      IL_0052:  callvirt   instance void class GCTest_arrres_il.Test[-100...100]::Set(int32, class GCTest_arrres_il.Test)
      IL_0053:  ldloc.1
      IL_0054:  ldc.i4.s   50
      IL_0056:  add
      IL_0057:  stloc.1
      IL_0058:  ldloc.1
      IL_0059:  ldc.i4.s   100
      IL_005b:  ble.s      IL_0030

      IL_005d:  call       void [mscorlib]System.GC::Collect()
      IL_0062:  call       void [mscorlib]System.GC::WaitForPendingFinalizers()
      IL_0067:  call       void [mscorlib]System.GC::Collect()
      
      IL_006c:  ldc.i4.s   -100
      IL_006e:  stloc.2
      IL_006f:  br.s       IL_0099

      IL_0071:  ldsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
      IL_0076:  ldloc.2
      IL_0077:  callvirt   instance class GCTest_arrres_il.Test class GCTest_arrres_il.Test[-100...100]::Get(int32)
      IL_0078:  brtrue.s   IL_0080

      IL_007a:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_007f:  throw

      IL_0080:  ldsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
      IL_0085:  ldloc.2
      IL_0086:  callvirt   instance class GCTest_arrres_il.Test class GCTest_arrres_il.Test[-100...100]::Get(int32)
      IL_0087:  callvirt   instance void GCTest_arrres_il.Test::CheckValid()
      IL_008c:  ldsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
      IL_0091:  ldloc.2
      IL_0092:  ldnull
      IL_0093:  callvirt   instance void class GCTest_arrres_il.Test[-100...100]::Set(int32, class GCTest_arrres_il.Test)
      IL_0094:  ldloc.2
      IL_0095:  ldc.i4.s   50
      IL_0097:  add
      IL_0098:  stloc.2
      IL_0099:  ldloc.2
      IL_009a:  ldc.i4.s   100
      IL_009c:  ble.s      IL_0071

      IL_009e:  call       void [mscorlib]System.GC::Collect()
      IL_00a3:  call       void [mscorlib]System.GC::WaitForPendingFinalizers()
      IL_00a8:  call       void [mscorlib]System.GC::Collect()
      IL_00ad:  ldc.i4.s   -100
      IL_00af:  stloc.3
      IL_00b0:  br.s       IL_00da

      IL_00b2:  ldsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
      IL_00b7:  ldloc.3
      IL_00b8:  callvirt   instance class GCTest_arrres_il.Test class GCTest_arrres_il.Test[-100...100]::Get(int32)
      IL_00b9:  brtrue.s   IL_00c1

      IL_00bb:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_00c0:  throw

      IL_00c1:  ldsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
      IL_00c6:  ldloc.3
      IL_00c7:  callvirt   instance class GCTest_arrres_il.Test class GCTest_arrres_il.Test[-100...100]::Get(int32)
      IL_00c8:  callvirt   instance void GCTest_arrres_il.Test::CheckValid()
      IL_00cd:  ldsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
      IL_00d2:  ldloc.3
      IL_00d3:  ldnull
      IL_00d4:  callvirt   instance void class GCTest_arrres_il.Test[-100...100]::Set(int32, class GCTest_arrres_il.Test)
      IL_00d5:  ldloc.3
      IL_00d6:  ldc.i4.s   50
      IL_00d8:  add
      IL_00d9:  stloc.3
      IL_00da:  ldloc.3
      IL_00db:  ldc.i4.s   100
      IL_00dd:  ble.s      IL_00b2

      IL_00df:  call       void [mscorlib]System.GC::Collect()
      IL_00e4:  call       void [mscorlib]System.GC::WaitForPendingFinalizers()
      IL_00e9:  call       void [mscorlib]System.GC::Collect()
      IL_00ee:  ldc.i4.s   -100
      IL_00f0:  stloc.s    V_4
      IL_00f2:  br.s       IL_0121

      IL_00f4:  ldsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
      IL_00f9:  ldloc.s    V_4
      IL_00fb:  callvirt   instance class GCTest_arrres_il.Test class GCTest_arrres_il.Test[-100...100]::Get(int32)
      IL_00fc:  brtrue.s   IL_0104

      IL_00fe:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_0103:  throw

      IL_0104:  ldsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
      IL_0109:  ldloc.s    V_4
      IL_010b:  callvirt   instance class GCTest_arrres_il.Test class GCTest_arrres_il.Test[-100...100]::Get(int32)
      IL_010c:  callvirt   instance void GCTest_arrres_il.Test::CheckValid()
      IL_0111:  ldsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
      IL_0116:  ldloc.s    V_4
      IL_0118:  ldnull
      IL_0119:  callvirt   instance void class GCTest_arrres_il.Test[-100...100]::Set(int32, class GCTest_arrres_il.Test)
      IL_011a:  ldloc.s    V_4
      IL_011c:  ldc.i4.s   50
      IL_011e:  add
      IL_011f:  stloc.s    V_4
      IL_0121:  ldloc.s    V_4
      IL_0123:  ldc.i4.s   100
      IL_0125:  ble.s      IL_00f4

      IL_0127:  call       void [mscorlib]System.GC::Collect()
      IL_012c:  call       void [mscorlib]System.GC::WaitForPendingFinalizers()
      IL_0131:  call       void [mscorlib]System.GC::Collect()
      IL_0136:  ldc.i4.s   -100
      IL_0138:  stloc.s    V_5
      IL_013a:  br.s       IL_0169

      IL_013c:  ldsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
      IL_0141:  ldloc.s    V_5
      IL_0143:  callvirt   instance class GCTest_arrres_il.Test class GCTest_arrres_il.Test[-100...100]::Get(int32)
      IL_0144:  brtrue.s   IL_014c

                ldstr      "Warning: object at index "
				call       void [System.Console]System.Console::Write(string)
				ldloc.s    V_5
				call       void [System.Console]System.Console::Write(int32)
                ldstr      " is NULL"
				call       void [System.Console]System.Console::WriteLine(string)
				br.s       IL_0162
		
      IL_014c:  ldsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
      IL_0151:  ldloc.s    V_5
      IL_0153:  callvirt   instance class GCTest_arrres_il.Test class GCTest_arrres_il.Test[-100...100]::Get(int32)
      			dup
      IL_0154:  callvirt   instance void GCTest_arrres_il.Test::CheckValid()
      			ldc.i4 		-10000
      			stfld      int32 GCTest_arrres_il.Test::m_indx
      
      IL_0159:  ldsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
      IL_015e:  ldloc.s    V_5
      IL_0160:  ldnull
      IL_0161:  callvirt   instance void class GCTest_arrres_il.Test[-100...100]::Set(int32, class GCTest_arrres_il.Test)
      
      IL_0162:  ldloc.s    V_5
      IL_0164:  ldc.i4.s   50
      IL_0166:  add
      IL_0167:  stloc.s    V_5
      IL_0169:  ldloc.s    V_5
      IL_016b:  ldc.i4.s   100
      IL_016d:  ble.s      IL_013c

				ldnull
				stsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
				
      IL_016f:  ldstr      "Test passed."
      IL_0174:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0179:  ldc.i4    0x64
      IL_017a:  stloc.s    V_6
      IL_017c:  br.s       IL_017e

      IL_017e:  ldloc.s    V_6
      IL_0180:  ret
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      .maxstack  8
      			ldc.i4 -100
      			ldc.i4 201
      			newobj      instance void class GCTest_arrres_il.Test[-100...100]::.ctor(int32, int32)
      IL_0007:  stsfld     class GCTest_arrres_il.Test[-100...100] GCTest_arrres_il.Test::arr
      IL_000c:  ret
    } // end of method 'Test::.cctor'

  } // end of class 'Test'

} // end of namespace 'GCTest_arrres_il'

