

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
  .permissionset reqrefuse = (3C 00 50 00 65 00 72 00 6D 00 69 00 73 00 73 00   
                              69 00 6F 00 6E 00 53 00 65 00 74 00 20 00 63 00   
                              6C 00 61 00 73 00 73 00 3D 00 22 00 53 00 79 00   
                              73 00 74 00 65 00 6D 00 2E 00 53 00 65 00 63 00   
                              75 00 72 00 69 00 74 00 79 00 2E 00 50 00 65 00   
                              72 00 6D 00 69 00 73 00 73 00 69 00 6F 00 6E 00   
                              53 00 65 00 74 00 22 00 0D 00 0A 00 20 00 20 00   
                              20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00   
                              20 00 20 00 20 00 20 00 20 00 76 00 65 00 72 00   
                              73 00 69 00 6F 00 6E 00 3D 00 22 00 31 00 22 00   
                              3E 00 0D 00 0A 00 20 00 20 00 20 00 3C 00 49 00   
                              50 00 65 00 72 00 6D 00 69 00 73 00 73 00 69 00   
                              6F 00 6E 00 20 00 63 00 6C 00 61 00 73 00 73 00   
                              3D 00 22 00 53 00 79 00 73 00 74 00 65 00 6D 00   
                              2E 00 53 00 65 00 63 00 75 00 72 00 69 00 74 00   
                              79 00 2E 00 50 00 65 00 72 00 6D 00 69 00 73 00   
                              73 00 69 00 6F 00 6E 00 73 00 2E 00 53 00 65 00   
                              63 00 75 00 72 00 69 00 74 00 79 00 50 00 65 00   
                              72 00 6D 00 69 00 73 00 73 00 69 00 6F 00 6E 00   
                              2C 00 20 00 6D 00 73 00 63 00 6F 00 72 00 6C 00   
                              69 00 62 00 2C 00 20 00 56 00 65 00 72 00 73 00   
                              69 00 6F 00 6E 00 3D 00 31 00 2E 00 30 00 2E 00   
                              35 00 30 00 30 00 30 00 2E 00 30 00 2C 00 20 00   
                              43 00 75 00 6C 00 74 00 75 00 72 00 65 00 3D 00   
                              6E 00 65 00 75 00 74 00 72 00 61 00 6C 00 2C 00   
                              20 00 50 00 75 00 62 00 6C 00 69 00 63 00 4B 00   
                              65 00 79 00 54 00 6F 00 6B 00 65 00 6E 00 3D 00   
                              62 00 37 00 37 00 61 00 35 00 63 00 35 00 36 00   
                              31 00 39 00 33 00 34 00 65 00 30 00 38 00 39 00   
                              22 00 0D 00 0A 00 20 00 20 00 20 00 20 00 20 00   
                              20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00   
                              20 00 20 00 20 00 76 00 65 00 72 00 73 00 69 00   
                              6F 00 6E 00 3D 00 22 00 31 00 22 00 0D 00 0A 00   
                              20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00   
                              20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00   
                              46 00 6C 00 61 00 67 00 73 00 3D 00 22 00 53 00   
                              6B 00 69 00 70 00 56 00 65 00 72 00 69 00 66 00   
                              69 00 63 00 61 00 74 00 69 00 6F 00 6E 00 22 00   
                              2F 00 3E 00 0D 00 0A 00 3C 00 2F 00 50 00 65 00   
                              72 00 6D 00 69 00 73 00 73 00 69 00 6F 00 6E 00   
                              53 00 65 00 74 00 3E 00 0D 00 0A 00 )             
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



