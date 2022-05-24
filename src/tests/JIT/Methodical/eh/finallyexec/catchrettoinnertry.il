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
 .assembly extern eh_common{}
 .assembly catchrettoinnertry
 {
   .hash algorithm 0x00008004
   .ver 0:0:0:0
 }
.assembly extern xunit.core {}

.namespace strswitch_catchrettoinnertry_il
{
   .class public auto ansi beforefieldinit Class1
          extends [mscorlib]System.Object
   {
    .field private static class [eh_common]TestUtil.TestLog testLog
    .method private hidebysig specialname rtspecialname static void  .cctor() cil managed
        {
            .maxstack  2
            .locals init (class [mscorlib]System.IO.StringWriter expectedOut)
            newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
            stloc.s    expectedOut
            ldloc.s    expectedOut
            ldstr      "Caught an exception"
            callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
            ldloc.s    expectedOut
            ldstr      "bye"
            callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
            ldloc.s    expectedOut
            ldstr      "In outer finally"
            callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
            ldloc.s    expectedOut
            newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
            stsfld     class [eh_common]TestUtil.TestLog strswitch_catchrettoinnertry_il.Class1::testLog
            ret
    }
     .method public hidebysig static int32
             Main() cil managed
     {
       .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
           01 00 00 00
       )
       .entrypoint
       .maxstack  2

        ldsfld     class [eh_common]TestUtil.TestLog strswitch_catchrettoinnertry_il.Class1::testLog
        callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()

       .locals init (int32 V_0,
                class [mscorlib]System.Exception V_1,
                int32 V_2)
         ldc.i4.3
         stloc.0
       .try
       {
         .try
         {
           branch_0002:  ldloc.0
             ldc.i4.3
             bne.un.s   branch_000c

             newobj     instance void [mscorlib]System.IndexOutOfRangeException::.ctor()
             throw

           branch_000c:  ldloc.0
             ldc.i4.4
             bne.un.s   branch_001a

             ldstr      "bye"
             call       void [System.Console]System.Console::WriteLine(string)
           branch_001a:  leave.s    branch_002d

         }
         catch [mscorlib]System.Exception
         {
             stloc.1
             ldstr      "Caught an exception"
             call       void [System.Console]System.Console::WriteLine(string)
             ldloc.0
             ldc.i4.1
             add
             stloc.0
             leave.s    branch_0002

         }
         branch_002d:  leave.s    branch_003a

       }
       finally
       {
           ldstr      "In outer finally"
           call       void [System.Console]System.Console::WriteLine(string)
           endfinally
       }
       branch_003a:
        ldsfld     class [eh_common]TestUtil.TestLog strswitch_catchrettoinnertry_il.Class1::testLog
        callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()

        ldsfld     class [eh_common]TestUtil.TestLog strswitch_catchrettoinnertry_il.Class1::testLog
        callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()

         ret
     }

     .method public hidebysig specialname rtspecialname
             instance void  .ctor() cil managed
     {
       .maxstack  1
         ldarg.0
         call       instance void [mscorlib]System.Object::.ctor()
          ret
     }
   }
}
