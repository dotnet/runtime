// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console {}
.assembly extern mscorlib {}
.assembly extern System.Runtime {}
.assembly extern TypeHierarchyCommonCs {}
.assembly TypeHierarchyTest {}

.class interface public abstract auto ansi InterfaceScenario1`1<- T>
{
  // Default implementation not allowed
  .method public newslot virtual abstract static !T Method(!T) cil managed noinlining
  {
  } // end of method Method
} // end of class InterfaceScenario1

.class interface public abstract auto ansi InterfaceScenario2`1<+ T>
{
  // Default implementation not allowed
  .method public newslot virtual abstract static !T Method(!T) cil managed noinlining
  {
  } // end of method Method
} // end of class InterfaceScenario1

.class public auto ansi TestEntrypoint
       extends [System.Runtime]System.Object
{
  .method public static void Test_Scenario1() cil managed noinlining
  {
    ldtoken class InterfaceScenario1`1<object>
    pop
    ret
  } // end of method Test_Scenario1

  .method public static void Test_Scenario2() cil managed noinlining
  {
    ldtoken class InterfaceScenario2`1<object>
    pop
    ret
  } // end of method Test_Scenario1

  .method public static int32 Main() cil managed noinlining
  {
    .entrypoint
    .locals init (class [System.Runtime]System.Exception V_0)
    .try {
        call void TestEntrypoint::Test_Scenario1()
        ldstr "Contravariant safety test"
        ldstr "TypeLoadException"
        ldstr "Did not throw exception"
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s ContravariantDone
    } catch [System.Runtime]System.Exception {
        stloc.0
        ldstr "Contravariant safety test"
        ldstr "TypeLoadException"
        ldloc.0
        callvirt   instance class [System.Runtime]System.Type [System.Runtime]System.Exception::GetType()
        callvirt   instance string [System.Runtime]System.Reflection.MemberInfo::get_Name()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s ContravariantDone
    }
ContravariantDone: nop

    .try {
        call void TestEntrypoint::Test_Scenario2()
        ldstr "Covariant safety test"
        ldstr "TypeLoadException"
        ldstr "Did not throw exception"
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s CovariantDone
    } catch [System.Runtime]System.Exception {
        stloc.0
        ldstr "Covariant safety test"
        ldstr "TypeLoadException"
        ldloc.0
        callvirt   instance class [System.Runtime]System.Type [System.Runtime]System.Exception::GetType()
        callvirt   instance string [System.Runtime]System.Reflection.MemberInfo::get_Name()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s CovariantDone
    }
CovariantDone: nop
    
    call int32 [TypeHierarchyCommonCs]Statics::ReportResults()
    ret  } // end of method Main
} // end of class TestEntrypoint
