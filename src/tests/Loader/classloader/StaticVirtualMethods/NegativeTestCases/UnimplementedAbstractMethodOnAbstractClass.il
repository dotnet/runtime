// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console {}
.assembly extern mscorlib {}
.assembly extern System.Runtime {}
.assembly extern TypeHierarchyCommonCs {}
.assembly TypeHierarchyTest {}
.class interface public abstract auto ansi InterfaceScenario1
{
  .method public newslot virtual abstract static int32 Method() cil managed noinlining
  {
  } // end of method Method
} // end of class InterfaceScenario1

.class interface public abstract auto ansi InterfaceScenarioUnimplemented
{
  .method public newslot virtual abstract static int32 Method() cil managed noinlining
  {
  } // end of method Method
} // end of class InterfaceScenario1

.class public auto abstract ansi BaseScenario1
       extends [System.Runtime]System.Object
       implements InterfaceScenario1, InterfaceScenarioUnimplemented
{
  .method public static int32 Method() cil managed noinlining
  {
    .override method int32 InterfaceScenario1::Method()
    .locals init (int32 V_O)
    ldloca.s 0
    initobj int32
    ldloc.0
    ret
  } // end of method Method
} // end of class BaseScenario1

.class public auto ansi TestEntrypoint
       extends [System.Runtime]System.Object
{
  .method public static void Test_Scenario1() cil managed noinlining
  {
    // Constrained call to unimplemented code. This doesn't need a good error, but some exception should be thrown instead of crashing the runtime. The exception I'd prefer to see is TypeLoadException, but any exception will do I believe.
    constrained. BaseScenario1
    call int32 InterfaceScenarioUnimplemented::Method()
    pop
    ret
  } // end of method Test_Scenario1

  .method public static void TestScenario1_2<T>() cil managed noinlining
  {
    // Constrained call to unimplemented code. This doesn't need a good error, but some exception should be thrown instead of crashing the runtime. The exception I'd prefer to see is TypeLoadException, but any exception will do I believe.
    constrained. !!0
    call int32 InterfaceScenarioUnimplemented::Method()
    pop
    ret
  } // end of method Test_Scenario1

  .method private hidebysig static void  TestScenario1_2Wrapper() cil managed noinlining
  {
    call void TestEntrypoint::TestScenario1_2<object>()
    ret
  } // end of method Test_Scenario1

  .method private hidebysig static void  TestScenario2<(InterfaceScenarioUnimplemented) T>() cil managed noinlining
  {
    // Constraint checker should now allow this method to be called/created/etc.
    ret
  } // end of method Test_Scenario1

  .method private hidebysig static void  TestScenario2Wrapper() cil managed noinlining
  {
    call void TestEntrypoint::TestScenario2<class BaseScenario1>()
    ret
  } // end of method Test_Scenario1

  .method private hidebysig static void  TestScenario3<(InterfaceScenario1) T>() cil managed noinlining
  {
    // Constraint checker should allow this method to be called/created/etc as it isn't constrained over an interface partially implemented
    constrained. !!0
    call int32 InterfaceScenario1::Method()
    pop
    ret
  } // end of method Test_Scenario1

  .method private hidebysig static void  TestScenario3Wrapper() cil managed noinlining
  {
    call void TestEntrypoint::TestScenario3<class BaseScenario1>()
    ret
  } // end of method Test_Scenario1

  .method public static int32 Main() cil managed noinlining
  {
    .entrypoint
    .locals init (class [System.Runtime]System.Exception V_0)

    .try {
        call void TestEntrypoint::Test_Scenario1()
        ldstr "ConstrainedCallOnAbstractClassToAbstractMethod"
        ldstr "TypeLoadException"
        ldstr "Did not throw exception"
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s ConstrainedCallOnAbstractClassToAbstractMethodDone
    } catch [System.Runtime]System.Exception {
        stloc.0
        ldstr "ConstrainedCallOnAbstractClassToAbstractMethod"
        ldstr "Threw Exception"
        ldstr "Threw Exception"
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s ConstrainedCallOnAbstractClassToAbstractMethodDone
    }
ConstrainedCallOnAbstractClassToAbstractMethodDone: nop

    .try {
        call void TestEntrypoint::TestScenario1_2Wrapper()
        ldstr "ConstrainedCallOnAbstractClassToAbstractMethodOnSharedGeneric"
        ldstr "TypeLoadException"
        ldstr "Did not throw exception"
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s ConstrainedCallOnAbstractClassToAbstractMethodOnSharedGenericDone
    } catch [System.Runtime]System.Exception {
        stloc.0
        ldstr "ConstrainedCallOnAbstractClassToAbstractMethodOnSharedGeneric"
        ldstr "Threw Exception"
        ldstr "Threw Exception"
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s ConstrainedCallOnAbstractClassToAbstractMethodOnSharedGenericDone
    }
ConstrainedCallOnAbstractClassToAbstractMethodOnSharedGenericDone: nop

    .try {
        call void TestEntrypoint::TestScenario2Wrapper()
        ldstr "ConstraintCheckShouldFail"
        ldstr "VerificationException"
        ldstr "Did not throw exception"
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s ConstraintCheckShouldFailDone
    } catch [System.Runtime]System.Exception {
        stloc.0
        ldstr "ConstraintCheckShouldFail"
        ldstr "VerificationException"
        ldloc.0
        callvirt   instance class [System.Runtime]System.Type [System.Runtime]System.Exception::GetType()
        callvirt   instance string [System.Runtime]System.Reflection.MemberInfo::get_Name()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s ConstraintCheckShouldFailDone
    }
ConstraintCheckShouldFailDone: nop

    .try {
        call void TestEntrypoint::TestScenario3Wrapper()
        ldstr "ConstraintCheckShouldSucceed"
        ldstr "Did not throw exception"
        ldstr "Did not throw exception"
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s ConstraintCheckShouldSucceedDone
    } catch [System.Runtime]System.Exception {
        stloc.0
        ldstr "ConstraintCheckShouldSucceed"
        ldstr "Should not throw exception"
        ldloc.0
        callvirt   instance class [System.Runtime]System.Type [System.Runtime]System.Exception::GetType()
        callvirt   instance string [System.Runtime]System.Reflection.MemberInfo::get_Name()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s ConstraintCheckShouldSucceedDone
    }
ConstraintCheckShouldSucceedDone: nop
    
    call int32 [TypeHierarchyCommonCs]Statics::ReportResults()
    ret  } // end of method Main
} // end of class TestEntrypoint
