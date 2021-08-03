// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console {}
.assembly extern mscorlib {}
.assembly extern System.Runtime {}
.assembly extern TypeHierarchyCommonCs {}
.assembly TypeHierarchyTest {}

.class public auto ansi BaseScenario1
       extends [System.Runtime]System.Object
{
  // Test virtual flag
  .method public static virtual int32 Method() cil managed noinlining
  {
    .locals init (int32 V_O)
    ldloca.s 0
    initobj int32
    ldloc.0
    ret
  } // end of method Method
} // end of class BaseScenario1

.class public auto ansi abstract BaseScenario2
       extends [System.Runtime]System.Object
{
  // Test abstract flag
  .method public static virtual abstract int32 Method() cil managed noinlining
  {
  } // end of method Method
} // end of class BaseScenario2

.class interface public abstract auto ansi InterfaceScenario3
{
  // Default implementation not allowed
  .method public newslot virtual static int32 Method() cil managed noinlining
  {
    ldc.i4.0
    ret
  } // end of method Method
} // end of class InterfaceScenario1


.class public auto ansi TestEntrypoint
       extends [System.Runtime]System.Object
{
  .method public static void Test_Scenario1() cil managed noinlining
  {
    ldtoken BaseScenario1
    pop
    ret
  } // end of method Test_Scenario1

  .method public static void Test_Scenario2() cil managed noinlining
  {
    ldtoken BaseScenario2
    pop
    ret
  } // end of method Test_Scenario1

  .method public static void Test_Scenario3() cil managed noinlining
  {
    ldtoken InterfaceScenario3
    pop
    ret
  } // end of method Test_Scenario1

  .method public static int32 Main() cil managed noinlining
  {
    .entrypoint
    .locals init (class [System.Runtime]System.Exception V_0)
    .try {
        call void TestEntrypoint::Test_Scenario1()
        ldstr "Virtual flag set on static class method"
        ldstr "TypeLoadException"
        ldstr "Did not throw exception"
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s VirtualFlagOnClassStaticDone
    } catch [System.Runtime]System.Exception {
        stloc.0
        ldstr "Virtual flag set on static class method"
        ldstr "TypeLoadException"
        ldloc.0
        callvirt   instance class [System.Runtime]System.Type [System.Runtime]System.Exception::GetType()
        callvirt   instance string [System.Runtime]System.Reflection.MemberInfo::get_Name()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s VirtualFlagOnClassStaticDone
    }
VirtualFlagOnClassStaticDone: nop

    .try {
        call void TestEntrypoint::Test_Scenario2()
        ldstr "Abstract flag set on static class method"
        ldstr "TypeLoadException"
        ldstr "Did not throw exception"
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s AbstractFlagOnClassStaticDone
    } catch [System.Runtime]System.Exception {
        stloc.0
        ldstr "Abstract flag set on static class method"
        ldstr "TypeLoadException"
        ldloc.0
        callvirt   instance class [System.Runtime]System.Type [System.Runtime]System.Exception::GetType()
        callvirt   instance string [System.Runtime]System.Reflection.MemberInfo::get_Name()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s AbstractFlagOnClassStaticDone
    }
AbstractFlagOnClassStaticDone: nop
    
    .try {
        call void TestEntrypoint::Test_Scenario3()
        ldstr "Default implementation of static virtual interface method"
        ldstr "TypeLoadException"
        ldstr "Did not throw exception"
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s DefaultImplementationProhibitedDone
    } catch [System.Runtime]System.Exception {
        stloc.0
        ldstr "Default implementation of static virtual interface method"
        ldstr "TypeLoadException"
        ldloc.0
        callvirt   instance class [System.Runtime]System.Type [System.Runtime]System.Exception::GetType()
        callvirt   instance string [System.Runtime]System.Reflection.MemberInfo::get_Name()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s DefaultImplementationProhibitedDone
    }
DefaultImplementationProhibitedDone: nop
    
    call int32 [TypeHierarchyCommonCs]Statics::ReportResults()
    ret  } // end of method Main
} // end of class TestEntrypoint
