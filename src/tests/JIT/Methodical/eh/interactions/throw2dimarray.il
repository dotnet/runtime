// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// throw a 2 dimensional array, and then print the array



.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
  .ver 0:0:0:0
}
.assembly extern eh_common{}
.assembly extern System.Windows.Forms
{
  .ver 0:0:0:0
}
.assembly ASSEMBLY_NAME
{
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001








.namespace Test_throw2dimarray
{
.class public auto ansi beforefieldinit App
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  4
    .locals init (int32[,] V_0,
             int32[,] V_1,
             int32 V_2,
             int32 V_3,
             int32[,] V_4,
             object V_5,
             class [mscorlib]System.IO.StringWriter expectedOut,
			 class [eh_common]TestUtil.TestLog testLog
		)

    newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    stloc.s    expectedOut
    ldloc.s    expectedOut
    ldstr      "2"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "6"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In finally1"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "1"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "2"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "3"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "4"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "5"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "6"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In catch"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "1"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "2"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "3"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "4"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "5"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "6"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In finally2"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
     ldloc.s    expectedOut
    ldstr      "1"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "2"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "3"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "4"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "5"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "6"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
   
    ldloc.s    expectedOut
    newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()


    IL_0000:  ldnull
    IL_0001:  stloc.0
    IL_0002:  ldnull
    IL_0003:  stloc.1
    IL_0004:  ldnull
    IL_0005:  stloc.s    V_5
    IL_0007:  ldc.i4.2
    IL_0008:  ldc.i4.3
    IL_0009:  newobj     instance void int32[,]::.ctor(int32,
                                                       int32)
    IL_000e:  stloc.s    V_4
    IL_0010:  ldloc.s    V_4
    IL_0012:  call       instance void [mscorlib]System.Array::Initialize()
    IL_0017:  ldloc.s    V_4
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.0
    IL_001c:  ldc.i4.0
    IL_001d:  ldc.i4.1
    IL_001e:  call       instance void int32[,]::Set(int32,
                                                     int32,
                                                     int32)
    IL_0023:  ldloc.0
    IL_0024:  ldc.i4.0
    IL_0025:  ldc.i4.1
    IL_0026:  ldc.i4.2
    IL_0027:  call       instance void int32[,]::Set(int32,
                                                     int32,
                                                     int32)
    IL_002c:  ldloc.0
    IL_002d:  ldc.i4.0
    IL_002e:  ldc.i4.2
    IL_002f:  ldc.i4.3
    IL_0030:  call       instance void int32[,]::Set(int32,
                                                     int32,
                                                     int32)
    IL_0035:  ldloc.0
    IL_0036:  ldc.i4.1
    IL_0037:  ldc.i4.0
    IL_0038:  ldc.i4.4
    IL_0039:  call       instance void int32[,]::Set(int32,
                                                     int32,
                                                     int32)
    IL_003e:  ldloc.0
    IL_003f:  ldc.i4.1
    IL_0040:  ldc.i4.1
    IL_0041:  ldc.i4.5
    IL_0042:  call       instance void int32[,]::Set(int32,
                                                     int32,
                                                     int32)
    IL_0047:  ldloc.0
    IL_0048:  ldc.i4.1
    IL_0049:  ldc.i4.2
    IL_004a:  ldc.i4.6
    IL_004b:  call       instance void int32[,]::Set(int32,
                                                     int32,
                                                     int32)
    IL_0050:  ldloc.0
    IL_0051:  call       instance int32 [mscorlib]System.Array::get_Rank()
    IL_0056:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_005b:  ldloc.0
    IL_005c:  callvirt   instance int32 [mscorlib]System.Array::get_Length()
    IL_005d:  call       void [System.Console]System.Console::WriteLine(int32)

    .try {
      .try {
        .try
        {
          ldloc.0
          throw

          leave    IL_00a3
        } finally {
          ldstr      "In finally1"	
          call       void [System.Console]System.Console::WriteLine(string)
          ldc.i4.0
          stloc.3
          br.s       test_i_fin

        inc_i_fin:  
          ldloc.3
          ldc.i4.1
          add
          stloc.3

        test_i_fin:  
          ldloc.3
          ldloc.0
          call       instance int32 [mscorlib]System.Array::get_Rank()
          bge.s      done_fin

          ldc.i4.0
          stloc.2
          br.s       test_j_fin

        inc_j_fin:
          ldloc.2
          ldc.i4.1
          add
          stloc.2

        test_j_fin:  
          ldloc.2
          ldloc.0
          callvirt   instance int32 [mscorlib]System.Array::get_Length()
          ldloc.0
          call instance int32 [mscorlib]System.Array::get_Rank()
          div
          bge.s      doneinner_fin

          ldloc.0
          ldloc.3
          ldloc.2
          call       instance int32 int32[,]::Get(int32,
                                                  int32)
          call       void [System.Console]System.Console::WriteLine(int32)
          br.s       inc_j_fin

        doneinner_fin:  br.s       inc_i_fin

        done_fin:  
          endfinally
        } 

      }  
      catch [mscorlib]System.Object 
      {
        stloc.s    V_5
        ldloc.s    V_5
        castclass int32[0...,0...]
        stloc.1
        ldstr      "In catch"	
        call       void [System.Console]System.Console::WriteLine(string)

        ldc.i4.0
        stloc.3
        br.s       test_i

      inc_i:  
        ldloc.3
        ldc.i4.1
        add
        stloc.3

      test_i:  
        ldloc.3
        ldloc.1
        call       instance int32 [mscorlib]System.Array::get_Rank()
        bge.s      done

        ldc.i4.0
        stloc.2
        br.s       test_j

      inc_j:
        ldloc.2
        ldc.i4.1
        add
        stloc.2

      test_j:  
        ldloc.2
        ldloc.1
        callvirt   instance int32 [mscorlib]System.Array::get_Length()
        ldloc.1
        call instance int32 [mscorlib]System.Array::get_Rank()
        div
        bge.s      doneinner

        ldloc.1
        ldloc.3
        ldloc.2
        call       instance int32 int32[,]::Get(int32,
                                                int32)
        call       void [System.Console]System.Console::WriteLine(int32)
        br.s       inc_j

      doneinner:  
        br.s       inc_i

      done:  
        leave.s    IL_00a3


      }  
    } finally {
      ldstr      "In finally2"	
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.0
      stloc.3
      br.s       test_i_fin2

    inc_i_fin2:  
      ldloc.3
      ldc.i4.1
      add
      stloc.3

    test_i_fin2:  
      ldloc.3
      ldloc.0
      call       instance int32 [mscorlib]System.Array::get_Rank()
      bge.s      done_fin2

      ldc.i4.0
      stloc.2
      br.s       test_j_fin2

    inc_j_fin2:
      ldloc.2
      ldc.i4.1
      add
      stloc.2

    test_j_fin2:  
      ldloc.2
      ldloc.0
      callvirt   instance int32 [mscorlib]System.Array::get_Length()
      ldloc.0
      call instance int32 [mscorlib]System.Array::get_Rank()
      div
      bge.s      doneinner_fin2

      ldloc.0
      ldloc.3
      ldloc.2
      call       instance int32 int32[,]::Get(int32,
                                              int32)
      call       void [System.Console]System.Console::WriteLine(int32)
      br.s       inc_j_fin2

    doneinner_fin2:  br.s       inc_i_fin2

    done_fin2:  
      endfinally
    }
    IL_00a3:  
       ldloc.s    testLog
		callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
  
		ldloc.s    testLog
		callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()
    IL_00a4:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 

.class private auto ansi beforefieldinit MyException
       extends [mscorlib]System.Exception
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string msg) cil managed
  {
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void [mscorlib]System.Exception::.ctor(string)
    IL_0007:  ldarg.0
    IL_0008:  ldstr      "http://NotARealURL.Microsoft.com/help.html"
    IL_000d:  callvirt   instance void [mscorlib]System.Exception::set_HelpLink(string)
    IL_0012:  ret
  } 

} 
}
