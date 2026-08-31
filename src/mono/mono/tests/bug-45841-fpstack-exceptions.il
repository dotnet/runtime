
// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly extern System.Data
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly x
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 02 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module x.exe
// MVID: {0EDDDD7B-2C90-4D6D-9C28-383B7DA8A1EB}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit SqlSingleTest
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .entrypoint
    // Code size       164 (0xa4)
    .maxstack  2
    .locals init (valuetype [System.Data]System.Data.SqlTypes.SqlDecimal V_0)
    .try
    {
	ldstr "Exception 1"
    	newobj     instance void [mscorlib]System.OverflowException::.ctor(string)
    	throw
			  pop
      IL_001c:  leave.s    IL_0021

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_001e:  pop
      IL_001f:  leave.s    IL_0021

    }  // end handler
    IL_0021:  nop
    .try
    {
	ldstr "Exception 2"
    	newobj     instance void [mscorlib]System.OverflowException::.ctor(string)
    	throw
			  pop
      IL_002e:  leave.s    IL_0033

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0030:  pop
      IL_0031:  leave.s    IL_0033

    }  // end handler
    IL_0033:  nop
    .try
    {
	ldstr "Exception 3"
    	newobj     instance void [mscorlib]System.OverflowException::.ctor(string)
    	throw
      IL_0044:  leave.s    IL_0049

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0046:  pop
      IL_0047:  leave.s    IL_0049

    }  // end handler
    IL_0049:  nop
    .try
    {
	ldstr "Exception 4"
    	newobj     instance void [mscorlib]System.OverflowException::.ctor(string)
    	throw
		pop
      IL_005a:  leave.s    IL_005f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_005c:  pop
      IL_005d:  leave.s    IL_005f

    }  // end handler
    IL_005f:  nop
    .try
    {
	ldstr "Exception 5"
    	newobj     instance void [mscorlib]System.OverflowException::.ctor(string)
    	throw
		pop
      IL_006c:  leave.s    IL_0071

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_006e:  pop
      IL_006f:  leave.s    IL_0071

    }  // end handler
    IL_0071:  nop
    .try
    {
	ldstr "Exception 6"
    	newobj     instance void [mscorlib]System.OverflowException::.ctor(string)
    	throw
		pop
      IL_0082:  leave.s    IL_0087

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0084:  pop
      IL_0085:  leave.s    IL_0087

    }  // end handler
	IL_0087:	
    ldloca.s   V_0
    IL_0089:  ldc.r8     5
    IL_0092:  call       instance void [System.Data]System.Data.SqlTypes.SqlDecimal::.ctor(float64)
    IL_0097:  ldloca.s   V_0
    IL_0099:  call       instance valuetype [mscorlib]System.Decimal [System.Data]System.Data.SqlTypes.SqlDecimal::get_Value()
    call       float64 [mscorlib]System.Decimal::ToDouble([mscorlib]System.Decimal)
	ldc.r8 5
	ceq
	ldc.i4 0
	ceq
    ret
  } // end of method SqlSingleTest::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method SqlSingleTest::.ctor

} // end of class SqlSingleTest


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
