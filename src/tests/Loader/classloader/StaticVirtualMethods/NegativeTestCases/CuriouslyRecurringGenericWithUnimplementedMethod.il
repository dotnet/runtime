// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console {}
.assembly extern mscorlib {}
.assembly extern System.Runtime {}
.assembly extern TypeHierarchyCommonCs {}
.assembly TypeHierarchyTest {}
.class interface public abstract auto ansi InterfaceScenario1`1<T>
{
  .method public newslot virtual abstract static int32 Method() cil managed noinlining
  {
  } // end of method Method
} // end of class InterfaceScenario1

.class public auto ansi BaseScenario1`1<V>
       extends [System.Runtime]System.Object
       implements class InterfaceScenario1`1<class BaseScenario1`1<!0>>
{
} // end of class BaseScenario1

.class public auto ansi TestEntrypoint
       extends [System.Runtime]System.Object
{
  .method public static class [System.Runtime]System.Type Test_Scenario1() cil managed noinlining
  {
    ldtoken class BaseScenario1`1<object>
    call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ret
  } // end of method Test_Scenario1
  .method public static int32 Main() cil managed noinlining
  {
    .entrypoint
    .locals init (class [System.Runtime]System.Exception V_0)
    .try {
        call class [System.Runtime]System.Type TestEntrypoint::Test_Scenario1()
        pop
        ldstr "CuriouslyRecurringGenericWithUnimplementedMethod"
        ldstr "TypeLoadException"
        ldstr "Did not throw exception"
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s CuriouslyRecurringGenericWithUnimplementedMethodDone
    } catch [System.Runtime]System.Exception {
        stloc.0
        ldstr "CuriouslyRecurringGenericWithUnimplementedMethod"
        ldstr "TypeLoadException"
        ldloc.0
        callvirt   instance class [System.Runtime]System.Type [System.Runtime]System.Exception::GetType()
        callvirt   instance string [System.Runtime]System.Reflection.MemberInfo::get_Name()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s CuriouslyRecurringGenericWithUnimplementedMethodDone
    }
CuriouslyRecurringGenericWithUnimplementedMethodDone: nop
    
    call int32 [TypeHierarchyCommonCs]Statics::ReportResults()
    ret  } // end of method Main
} // end of class TestEntrypoint
