




.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .ver 1:0:2411:0
}
.assembly extern Microsoft.VisualBasic
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         
  .ver 7:0:0:0
}
.assembly extern System
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .ver 1:0:2411:0
}
.assembly extern System.Data
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .ver 1:0:2411:0
}
.assembly extern System.Xml
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .ver 1:0:2411:0
}
.assembly CByte7a
{


  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 37 37 32 33 34 36 31 34 2D 34 44 37 37   
                                                                                                  2D 34 38 45 42 2D 39 45 46 34 2D 36 45 34 42 36   
                                                                                                  45 30 33 42 42 45 44 00 00 )                      
  .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = ( 01 00 01 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.AssemblyTrademarkAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.AssemblyCopyrightAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.AssemblyProductAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.AssemblyCompanyAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.AssemblyDescriptionAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.AssemblyTitleAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 1:0:585:20133
}
.module CByte7a.exe
.imagebase 0x11000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       
.corflags 0x00000001    


.namespace CByte7a
{
  .class private auto ansi sealed Module1
         extends [mscorlib]System.Object
  {
    .custom instance void [Microsoft.VisualBasic]Microsoft.VisualBasic.Globals/StandardModuleAttribute::.ctor() = ( 01 00 00 00 ) 
    .method public static int32 Main() cil managed
    {
      .entrypoint
      .custom instance void [mscorlib]System.STAThreadAttribute::.ctor() = ( 01 00 00 00 ) 
      .maxstack  2
      .locals init (bool V_0,
               uint8 V_1)
      IL_0000:  nop
      IL_0001:  ldc.i4.1
      IL_0002:  stloc.0
      IL_0003:  ldloc.0
      IL_0004:  ldc.i4.s   31
      IL_0006:  shl
      IL_0007:  ldc.i4.s   31
      IL_0009:  shr
      IL_000a:  conv.u1
      IL_000b:  stloc.1
      IL_000c:  ldloc.1
      IL_000d:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_0012:  nop
      IL_0013:  nop
      IL_0014:  ldc.i4 100
		ret
    } 

  } 

} 



