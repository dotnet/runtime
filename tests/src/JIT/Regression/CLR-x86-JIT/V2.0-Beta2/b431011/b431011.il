

.module extern user32.dll

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .ver 1:0:5000:0
}
.assembly LdfldEnum
{
  .custom instance void [mscorlib]System.Security.SecurityRulesAttribute::.ctor(valuetype [mscorlib]System.Security.SecurityRuleSet) = ( 01 00 02 00 00 )  .custom instance void [mscorlib]System.Security.SecurityTransparentAttribute::.ctor() = ( 01 00 00 00 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module LdfldEnum.exe
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001
.class public auto ansi sealed MyEnum
       extends [mscorlib]System.Enum
{
} 

.class public auto ansi beforefieldinit cs1
       extends [mscorlib]System.Object
{
} 









.class public auto ansi sealed MyEnum
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
} 

.class public auto ansi beforefieldinit cs1
       extends [mscorlib]System.Object
{
  .field private static literal int32 DEFAULT_PE_FILE_BASE_ADDRESS = int32(0x00400000)
  .method public hidebysig static pinvokeimpl("user32.dll" winapi) 
          int32  MessageBox(native int hWnd,
                            string lpText,
                            string lpCaption,
                            int32 uType) cil managed preservesig
  {
  }
  .method public hidebysig static int32  Main(string[] args) cil managed noinlining
  {
    .entrypoint
    .maxstack  1
    .locals init (valuetype MyEnum V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  ldc.i4     0x400000
    IL_0007:  stloc.0
    IL_0008:  ldloc.0
		ldfld int32 valuetype MyEnum::value__
    IL_0009:  call       void [System.Console]System.Console::WriteLine(int32)

	leave FAILURE
    CATCHVERIFICATION:
	leave SUCCESS

    FAILURE: 
	ldstr "Test FAILURE"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4 101
	ret 

    SUCCESS:
	ldstr "Test SUCCESS"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4 100
	ret

	.try IL_0000 to CATCHVERIFICATION 
	catch [mscorlib]System.Security.VerificationException 
	handler CATCHVERIFICATION to FAILURE
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



