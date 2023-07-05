// Metadata version: v4.0.30319
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
}
.assembly 'library'
{
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
.module library.dll

// =============== CLASS MEMBERS DECLARATION ===================

.class public abstract auto ansi sealed beforefieldinit Library.TypeInfoCalls
       extends [System.Runtime]System.Object
{
  .method public hidebysig static void  TestGetConstructors(class [System.Runtime]System.Reflection.TypeInfo 'type') cil managed
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  callvirt   instance class [System.Runtime]System.Reflection.ConstructorInfo[] [System.Runtime]System.Reflection.TypeInfo::GetConstructors()
    IL_0007:  pop
    IL_0008:  ret
  }

  .method public hidebysig static void  TestGetMethods(class [System.Runtime]System.Reflection.TypeInfo 'type') cil managed
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  callvirt   instance class [System.Runtime]System.Reflection.MethodInfo[] [System.Runtime]System.Reflection.TypeInfo::GetMethods()
    IL_0007:  pop
    IL_0008:  ret
  }

  .method public hidebysig static void  TestGetFields(class [System.Runtime]System.Reflection.TypeInfo 'type') cil managed
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  callvirt   instance class [System.Runtime]System.Reflection.FieldInfo[] [System.Runtime]System.Reflection.TypeInfo::GetFields()
    IL_0007:  pop
    IL_0008:  ret
  }

  .method public hidebysig static void  TestGetProperties(class [System.Runtime]System.Reflection.TypeInfo 'type') cil managed
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  callvirt   instance class [System.Runtime]System.Reflection.PropertyInfo[] [System.Runtime]System.Reflection.TypeInfo::GetProperties()
    IL_0007:  pop
    IL_0008:  ret
  }

  .method public hidebysig static void  TestGetEvents(class [System.Runtime]System.Reflection.TypeInfo 'type') cil managed
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  callvirt   instance class [System.Runtime]System.Reflection.EventInfo[] [System.Runtime]System.Reflection.TypeInfo::GetEvents()
    IL_0007:  pop
    IL_0008:  ret
  }

  .method public hidebysig static void  TestGetNestedTypes(class [System.Runtime]System.Reflection.TypeInfo 'type') cil managed
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  callvirt   instance class [System.Runtime]System.Type[] [System.Runtime]System.Reflection.TypeInfo::GetNestedTypes()
    IL_0007:  pop
    IL_0008:  ret
  }

  .method public hidebysig static void  TestGetField(class [System.Runtime]System.Reflection.TypeInfo 'type') cil managed
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldstr      "unknown"
    IL_0007:  callvirt   instance class [System.Runtime]System.Reflection.FieldInfo [System.Runtime]System.Reflection.TypeInfo::GetField(string)
    IL_000c:  pop
    IL_000d:  ret
  }

  .method public hidebysig static void  TestGetProperty(class [System.Runtime]System.Reflection.TypeInfo 'type') cil managed
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldstr      "unknown"
    IL_0007:  callvirt   instance class [System.Runtime]System.Reflection.PropertyInfo [System.Runtime]System.Reflection.TypeInfo::GetProperty(string)
    IL_000c:  pop
    IL_000d:  ret
  }

  .method public hidebysig static void  TestGetEvent(class [System.Runtime]System.Reflection.TypeInfo 'type') cil managed
  {
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldstr      "unknown"
    IL_0007:  callvirt   instance class [System.Runtime]System.Reflection.EventInfo [System.Runtime]System.Reflection.TypeInfo::GetEvent(string)
    IL_000c:  pop
    IL_000d:  ret
  }

} // end of class Library.TypeInfoCalls