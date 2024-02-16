.assembly extern System.Runtime {  }
.assembly extern xunit.core {}

.assembly genericldtoken { }

.class public sequential ansi sealed beforefieldinit GenericStruct`1<T>
       extends [System.Runtime]System.ValueType
{
  .pack 0
  .size 1
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig newslot virtual final 
          instance string  B() cil managed
  {
    // Code size       6 (0x6)
    .maxstack  8
    IL_0000:  ldstr      "B"
    IL_0005:  ret
  }
}

.class public auto ansi beforefieldinit Program
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       124 (0x7c)
    .maxstack  5
    .locals init (valuetype [System.Runtime]System.RuntimeMethodHandle V_0,
             valuetype [System.Runtime]System.RuntimeMethodHandle V_1)
    IL_0016:  ldtoken    method instance string valuetype GenericStruct`1<string>::B()
              stloc.0

    IL_0053:  ldtoken    valuetype GenericStruct`1<string>
    IL_0058:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_005d:  ldstr      "B"
    IL_0062:  call       instance class [System.Runtime]System.Reflection.MethodInfo [System.Runtime]System.Type::GetMethod(string)
    IL_0067:  callvirt   instance valuetype [System.Runtime]System.RuntimeMethodHandle [System.Runtime]System.Reflection.MethodBase::get_MethodHandle()
    IL_006c:  stloc.1

    IL_006d:  ldloca.s   V_0
    IL_006f:  ldloc.1
    IL_0070:  call       instance bool [System.Runtime]System.RuntimeMethodHandle::Equals(valuetype [System.Runtime]System.RuntimeMethodHandle)
    IL_0075:  brtrue.s   IL_0079

    IL_0077:  ldc.i4.m1
    IL_0078:  ret

    IL_0079:  ldc.i4.s   100
    IL_007b:  ret
  }
}
