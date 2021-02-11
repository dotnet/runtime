// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





// Metadata version: v1.1.1919
.assembly extern mscorlib
{
}
.assembly 'switch'
{

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 00 01 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module switch.exe
// MVID: {663074E2-0B73-434A-85B1-7077AA61100B}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001
// Image base: 0x000007FFFE250000

// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit foo
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       67 (0x43)
    .maxstack  1
    .locals init (int32 V_0,
             int32 V_1,
             int32 V_2)
    IL_0000:  ldc.i4.3
    IL_0001:  stloc.0
    IL_0002:  ldloc.0
    IL_0003:  stloc.2
    IL_0004:  ldloc.2
    IL_0005:  switch     ( 
                          IL_0020,
                          IL_0025,
                          IL_002a,
//                          IL_002f,
                          IL_0034)
    IL_001e:  br.s       IL_0039

    IL_0020:  ldc.i4.s   101
    IL_0022:  stloc.1
    IL_0023:  br.s       IL_0041

    IL_0025:  ldc.i4.s   102
    IL_0027:  stloc.1
    IL_0028:  br.s       IL_0041

    IL_002a:  ldc.i4.s   103
    IL_002c:  stloc.1
    IL_002d:  br.s       IL_0041

//    IL_002f:  ldc.i4.s   100
//    IL_0031:  stloc.1
//    IL_0032:  br.s       IL_0041

    IL_0034:  ldc.i4.s   100
    IL_0036:  stloc.1
    IL_0037:  br.s       IL_0041

    IL_0039:  ldc.i4     0x4d2
    IL_003e:  stloc.1
    IL_003f:  br.s       IL_0041

    IL_0041:  ldloc.1
    IL_0042:  ret
  } // end of method foo::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method foo::.ctor

} // end of class foo


// =============================================================


//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file switch.res
