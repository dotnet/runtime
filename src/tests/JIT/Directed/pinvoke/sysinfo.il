// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'sysinfo_il' { }
.module sysinfo.exe
.namespace JitTest
{
  .class public sequential ansi sealed beforefieldinit SYSTEM_INFO
         extends [mscorlib]System.ValueType
  {
    .field private unsigned int32 dwOemId
    .field private unsigned int32 dwPageSize
    .field private  native uint lpMinimumApplicationAddress
    .field private  native uint lpMaximumApplicationAddress
    .field private  native uint dwActiveProcessorMask
    .field private unsigned int32 dwNumberOfProcessors
    .field private unsigned int32 dwProcessorType
    .field private unsigned int32 dwAllocationGranularity
    .field private unsigned int16 wProcessorLevel
    .field private unsigned int16 wProcessorRevision
    .method public hidebysig static pinvokeimpl("kernel32" ansi winapi) 
            void  GetSystemInfo(valuetype JitTest.SYSTEM_INFO& si) cil managed preservesig
    {
    }
    .method public hidebysig static int32 Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       83 (0x53)
      .maxstack  3
      .locals (valuetype JitTest.SYSTEM_INFO V_0,
               int32 V_1,
               object V_2)
      IL_0000:  ldloca.s   V_0
      IL_0002:  initobj    JitTest.SYSTEM_INFO
      			ldloc.0
      			box JitTest.SYSTEM_INFO
      			stloc.2
      .try
      {
      			ldloc.2
      			unbox JitTest.SYSTEM_INFO
                call       void JitTest.SYSTEM_INFO::GetSystemInfo(valuetype JitTest.SYSTEM_INFO&)
      			ldloc.2
      			unbox JitTest.SYSTEM_INFO
      			ldobj JitTest.SYSTEM_INFO
      			stloc.0
                leave.s    IL_004d
      }  // end .try
      finally
      {
        IL_0011:  ldloca.s   V_0
        IL_0013:  ldflda     unsigned int32 JitTest.SYSTEM_INFO::dwNumberOfProcessors
        IL_0018:  call       instance string [mscorlib]System.UInt32::ToString()
        IL_001d:  ldstr      " processor(s) found"
        IL_0022:  call       string [mscorlib]System.String::Concat(string,
                                                                    string)
        IL_0027:  call       void [System.Console]System.Console::WriteLine(string)
        IL_002c:  ldstr      "Allocation granularity is "
        IL_0031:  ldloca.s   V_0
        IL_0033:  ldflda     unsigned int32 JitTest.SYSTEM_INFO::dwAllocationGranularity
        IL_0038:  call       instance string [mscorlib]System.UInt32::ToString()
        IL_003d:  ldstr      " for this system."
        IL_0042:  call       string [mscorlib]System.String::Concat(string,
                                                                    string,
                                                                    string)
        IL_0047:  call       void [System.Console]System.Console::WriteLine(string)
        IL_004c:  endfinally
      }  // end handler
      IL_004d:  ldc.i4 100
      IL_004e:  stloc.1
      IL_004f:  br.s       IL_0051

      IL_0051:  ldloc.1
      IL_0052:  ret
    } // end of method SYSTEM_INFO::Main

  } // end of class SYSTEM_INFO

} // end of namespace JitTest

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file sysinfo.res
