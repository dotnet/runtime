




.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .ver 2:0:0:0
}
.assembly sql_stress4
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module sql_stress4.exe
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       
.corflags 0x00000001    



.class private auto ansi beforefieldinit sov02_CrossAppdomain_30
       extends [mscorlib]System.Object
{
  .field private static literal int32 size = int32(0x00000005)
  .method public hidebysig static void  p() cil managed
  {
    .maxstack  8
    tail.
    IL_0000:  call       void sov02_CrossAppdomain_30::p()
    IL_0005:  ret
  } 

  .method public hidebysig static void  gc() cil managed
  {
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0023

    IL_0004:  call       void sov02_CrossAppdomain_30::gc1()
    IL_0009:  ldloc.0
    IL_000a:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_000f:  call       void [mscorlib]System.GC::Collect()
    IL_0014:  ldloc.0
    IL_0015:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_001a:  call       void sov02_CrossAppdomain_30::gc1()
    IL_001f:  ldloc.0
    IL_0020:  ldc.i4.1
    IL_0021:  add
    IL_0022:  stloc.0
    IL_0023:  ldloc.0
    IL_0024:  ldc.i4.s   10
    IL_0026:  blt.s      IL_0004

    IL_0028:  ret
  } 

  .method public hidebysig static void  gc1() cil managed noinlining
  {
    .maxstack  3
    .locals init (object[] V_0,
             int32 V_1)
    IL_0000:  ldc.i4.5
    IL_0001:  newarr     [mscorlib]System.Object
    IL_0006:  stloc.0
    IL_0007:  ldc.i4.0
    IL_0008:  stloc.1
    IL_0009:  br.s       IL_0017

    IL_000b:  ldloc.0
    IL_000c:  ldloc.1
    IL_000d:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_0012:  stelem.ref
    IL_0013:  ldloc.1
    IL_0014:  ldc.i4.1
    IL_0015:  add
    IL_0016:  stloc.1
    IL_0017:  ldloc.1
    IL_0018:  ldloc.0
    IL_0019:  ldlen
    IL_001a:  conv.i4
    IL_001b:  blt.s      IL_000b

    IL_001d:  ret
  } 

  .method private hidebysig static int32 
          Main() cil managed
  {
    .entrypoint
    .maxstack  3
    .locals init (class [mscorlib]System.Threading.Thread V_0,
             class [mscorlib]System.Threading.Thread V_1)
    IL_0000:  ldnull
    IL_0001:  ldftn      void sov02_CrossAppdomain_30::p()
    IL_0007:  newobj     instance void [mscorlib]System.Threading.ThreadStart::.ctor(object,
                                                                                     native int)
    IL_000c:  newobj     instance void [mscorlib]System.Threading.Thread::.ctor(class [mscorlib]System.Threading.ThreadStart)
    IL_0011:  stloc.0
    IL_0012:  ldloc.0
    IL_0013:  callvirt   instance void [mscorlib]System.Threading.Thread::Start()
    IL_0018:  br.s       IL_0021

    IL_001a:  ldc.i4.s   100
    IL_001c:  call       void [mscorlib]System.Threading.Thread::Sleep(int32)
    IL_0021:  ldloc.0
    IL_0022:  callvirt   instance bool [mscorlib]System.Threading.Thread::get_IsAlive()
    IL_0027:  brfalse.s  IL_001a

    IL_0029:  ldnull
    IL_002a:  ldftn      void sov02_CrossAppdomain_30::gc()
    IL_0030:  newobj     instance void [mscorlib]System.Threading.ThreadStart::.ctor(object,
                                                                                     native int)
    IL_0035:  newobj     instance void [mscorlib]System.Threading.Thread::.ctor(class [mscorlib]System.Threading.ThreadStart)
    IL_003a:  stloc.1
    IL_003b:  ldloc.1
    IL_003c:  callvirt   instance void [mscorlib]System.Threading.Thread::Start()
    IL_0041:  ldloc.1
    IL_0042:  ldc.i4     0x7530
    IL_0047:  callvirt   instance bool [mscorlib]System.Threading.Thread::Join(int32)
    IL_004c:  brtrue.s   IL_0066

    IL_004e:  ldstr      "Fail - t1 could not be interrupted for a GC!"
    IL_0053:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0058:  ldloc.1
    IL_0059:  callvirt   instance void [mscorlib]System.Threading.Thread::Abort()
    IL_005e:  ldloc.0
    IL_005f:  callvirt   instance void [mscorlib]System.Threading.Thread::Abort()
    IL_0064:  ldc.i4.0
    IL_0065:  ret

    IL_0066:  ldstr      "Pass"
    IL_006b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0070:  ldloc.0
    IL_0071:  callvirt   instance void [mscorlib]System.Threading.Thread::Abort()
    IL_0076:  ldc.i4.s   100
    IL_0078:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 



