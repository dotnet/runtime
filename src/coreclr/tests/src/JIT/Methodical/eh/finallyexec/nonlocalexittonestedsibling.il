// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

 

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
 .assembly extern mscorlib {}
 .assembly extern common{}
 .assembly test {}
 
 
 .namespace hello
 {
   .class private auto ansi beforefieldinit Class1
          extends [mscorlib]System.Object
   {
    .field private static class [common]TestUtil.TestLog testLog
    .method private hidebysig specialname rtspecialname static void  .cctor() cil managed
    {
      .maxstack  4
      .locals init (class [mscorlib]System.IO.StringWriter expectedOut)
      newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
      stloc.s    expectedOut
      
      ldloc.s    expectedOut
      ldstr      "In try"
      callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      ldloc.s    expectedOut
      ldstr      "caught an exception: "
      newobj     instance void [mscorlib]System.ArithmeticException::.ctor()
      callvirt   instance string [mscorlib]System.Exception::get_Message()
      call       string [mscorlib]System.String::Concat(string, string)
      callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      ldloc.s    expectedOut
      ldstr      "HelpLink changed into: test"
      callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      ldloc.s    expectedOut
      ldstr      "In Finally"
      callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      ldloc.s    expectedOut
      ldstr      "In innermost try"
      callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      ldloc.s    expectedOut
      ldstr      "In finally 2"
      callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)

      ldloc.s    expectedOut
      newobj     instance void [common]TestUtil.TestLog::.ctor(object)
      stsfld     class [common]TestUtil.TestLog hello.Class1::testLog
      ret
    } 
    
     .method public hidebysig static int32 
             Main(string[] args) cil managed
     {
       .entrypoint
       .maxstack  3
       .locals init (int32 V_0,
                class [mscorlib]System.ArithmeticException V_1,
                class [mscorlib]System.MissingMemberException V_2,
                class [mscorlib]System.ArithmeticException V_3,
                string V_4,
                int32 V_5)
		ldsfld     class [common]TestUtil.TestLog hello.Class1::testLog
		callvirt   instance void [common]TestUtil.TestLog::StartRecording()
		
        ldc.i4     0x4d2
         stloc.0
         ldnull
         stloc.1
       .try
       {
         .try
         {
             ldstr      "In try"
             call       void [System.Console]System.Console::WriteLine(string)
             newobj     instance void [mscorlib]System.ArithmeticException::.ctor()
             throw
 
         }  
         catch [mscorlib]System.MissingMemberException 
         {
             stloc.2
             ldstr      "Unreached "
             ldloc.2
             callvirt   instance string [mscorlib]System.Exception::get_Message()
             call       string [mscorlib]System.String::Concat(string, string)
             call       void [System.Console]System.Console::WriteLine(string)
             leave.s    branch_0068
 
         }  
         catch [mscorlib]System.ArithmeticException 
         {
             stloc.3
             ldstr      "caught an exception: "
             ldloc.3
             callvirt   instance string [mscorlib]System.Exception::get_Message()
             call       string [mscorlib]System.String::Concat(string, string)
             call       void [System.Console]System.Console::WriteLine(string)
             ldloc.3
             ldstr      "test"
             callvirt   instance void [mscorlib]System.Exception::set_HelpLink(string)
             ldstr      "HelpLink changed into: "
             ldloc.3
             callvirt   instance string [mscorlib]System.Exception::get_HelpLink()
             call       string [mscorlib]System.String::Concat(string,
                                                                       string)
             call       void [System.Console]System.Console::WriteLine(string)
             leave.s    branch_007f
 
         }  
         branch_0068:  leave.s    branch_0075
 
       }  
       finally
       {
           ldstr      "In Finally"
           call       void [System.Console]System.Console::WriteLine(string)
           endfinally
       }  
       branch_0075:  ldstr      "In between try -- unreached"
         call       void [System.Console]System.Console::WriteLine(string)
       .try
       {
         .try
         {
           .try
           {
             .try
             {
               branch_007f:  ldstr      "In innermost try"
                 call       void [System.Console]System.Console::WriteLine(string)
                 leave.s    branch_0098
 
             }  
             catch [mscorlib]System.Exception 
             {
                 pop
                 ldstr      "Unreached"
                 call       void [System.Console]System.Console::WriteLine(string)
                 leave.s    branch_0098
 
             }  
             branch_0098:  leave.s    branch_00ad
 
           }  
           fault
           {
               ldstr      "In fault 1"
               call       void [System.Console]System.Console::WriteLine(string)
               ldloc.1
               callvirt   instance string [mscorlib]System.Exception::get_Message()
               stloc.s    V_4
               endfinally
           }  
           branch_00ad:  leave.s    branch_00bc
 
         }  
         catch [mscorlib]System.NullReferenceException 
         {
             pop
             ldstr      "caught an exception"
             call       void [System.Console]System.Console::WriteLine(string)
             newobj     instance void [mscorlib]System.ArithmeticException::.ctor()
             stloc.1

             leave.s    branch_007f
 
         }  

         branch_00bc:  leave.s    branch_00c9
 
       }  
       finally
       {
           ldstr      "In finally 2"
           call       void [System.Console]System.Console::WriteLine(string)
           endfinally
       }  
       branch_done:
       branch_00c9:  ldloc.0
         stloc.s    V_5
         br.s       branch_00ce
 
       branch_00ce:
       
		ldsfld     class [common]TestUtil.TestLog hello.Class1::testLog
      	callvirt   instance void [common]TestUtil.TestLog::StopRecording()
      
      	ldsfld     class [common]TestUtil.TestLog hello.Class1::testLog
      	callvirt   instance int32 [common]TestUtil.TestLog::VerifyOutput()
        
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
