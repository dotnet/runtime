// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console {}
.assembly extern mscorlib {}
.assembly extern System.Runtime {}
.assembly extern TypeHierarchyCommonCs {}
.assembly TypeHierarchyTest {}

.class interface public abstract auto ansi InterfaceScenario1`1<- T>
{
  .method public newslot virtual abstract static int32 Method() cil managed noinlining
  {
  } // end of method Method
} // end of class InterfaceScenario1`1

.class interface public abstract auto ansi InterfaceScenario2`1<- T>
{
  .method public newslot virtual abstract static int32 Method() cil managed noinlining
  {
  }
  .method public newslot virtual abstract static int32 Method2() cil managed noinlining
  {
  } // end of method Method
} // end of class InterfaceScenario1

.class public auto ansi BaseScenario1
       extends [System.Runtime]System.Object
       implements class InterfaceScenario1`1<object>
{
  .method public static int32 Method() cil managed noinlining
  {
    .override method int32 class InterfaceScenario1`1<object>::Method()
    ldc.i4 1
    ret
  } // end of method Method
} // end of class BaseScenario1

.class public auto ansi DerivedScenario1
       extends BaseScenario1
{
} // end of class DerivedScenario1


.class public auto ansi BaseScenario2
       extends [System.Runtime]System.Object
       implements class InterfaceScenario2`1<string>, class InterfaceScenario2`1<object>
{
  // NOTE: The order of the methods in this .il file is important as it controls the MethodImpl record creation order
  .method public static int32 Method() cil managed noinlining
  {
    .override method int32 class InterfaceScenario2`1<string>::Method()
    ldc.i4 2
    ret
  } // end of method Method
  .method public static int32 Method_Obj() cil managed noinlining
  {
    .override method int32 class InterfaceScenario2`1<object>::Method()
    ldc.i4 3
    ret
  } // end of method Method
  .method public static int32 Method2_Obj() cil managed noinlining
  {
    .override method int32 class InterfaceScenario2`1<object>::Method2()
    ldc.i4 4
    ret
  } // end of method Method
  .method public static int32 Method2() cil managed noinlining
  {
    .override method int32 class InterfaceScenario2`1<string>::Method2()
    ldc.i4 5
    ret
  } // end of method Method
} // end of class BaseScenario2

.class public auto ansi DerivedScenario2
       extends BaseScenario2
{
    .method public static int32 Method() cil managed noinlining
  {
    .override method int32 class InterfaceScenario2`1<object>::Method()
    ldc.i4 6
    ret
  } // end of method Method

  .method public static int32 Method2() cil managed noinlining
  {
    .override method int32 class InterfaceScenario2`1<object>::Method2()
    ldc.i4 7
    ret
  } // end of method Method
} // end of class DerivedScenario2


.class public auto ansi TestEntrypoint
       extends [System.Runtime]System.Object
{
  .method public static string Test_Scenario<T, (class InterfaceScenario1`1<!!0>) ImplType>() cil managed noinlining
  {
    // Variant dispatch to method on base type
    .locals init (int32 V_1)
    constrained. !!1
    call int32 class InterfaceScenario1`1<!!0>::Method()
    stloc.0
    ldloca.s   V_1
    call       instance string [System.Runtime]System.Int32::ToString()
    ret
  } // end of method Test_Scenario1

  .method public static string Test_Scenario2_1<T, (class InterfaceScenario2`1<!!0>) ImplType>() cil managed noinlining
  {
    // Variant dispatch to method on base type
    .locals init (int32 V_1)
    constrained. !!1
    call int32 class InterfaceScenario2`1<!!0>::Method()
    stloc.0
    ldloca.s   V_1
    call       instance string [System.Runtime]System.Int32::ToString()
    ret
  } // end of method Test_Scenario1

  .method public static string Test_Scenario2_2<T, (class InterfaceScenario2`1<!!0>) ImplType>() cil managed noinlining
  {
    // Variant dispatch to method on base type
    .locals init (int32 V_1)
    constrained. !!1
    call int32 class InterfaceScenario2`1<!!0>::Method2()
    stloc.0
    ldloca.s   V_1
    call       instance string [System.Runtime]System.Int32::ToString()
    ret
  } // end of method Test_Scenario1


  .method public static int32 Main() cil managed noinlining
  {
    .entrypoint
    .locals init (class [System.Runtime]System.Exception V_0)

    .try {
        ldstr "VariantDispatchToBaseTypeMethodVariantly"
        ldstr "1"
        call string TestEntrypoint::Test_Scenario<string, class BaseScenario1>()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)

        ldstr "VariantDispatchToBaseTypeMethodFromDerivedTypeVariantly"
        ldstr "1"
        call string TestEntrypoint::Test_Scenario<string, class DerivedScenario1>()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)

        leave.s VariantDispatchToBaseTypeMethodDone
    } catch [System.Runtime]System.Exception {
        stloc.0
        ldstr "VariantDispatchToBaseTypeMethod"
        ldstr "No Exception"
        ldloc.0
        callvirt   instance class [System.Runtime]System.Type [System.Runtime]System.Exception::GetType()
        callvirt   instance string [System.Runtime]System.Reflection.MemberInfo::get_Name()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s VariantDispatchToBaseTypeMethodDone
    }
VariantDispatchToBaseTypeMethodDone: nop

    .try {
        // Variant dispatch when applied at a single layer in the type hierarchy does not take priority
        // over non-variant dispatch. This test verifies that the ordering of MethodImpl's has no effect
        // and that the correct function is called
        ldstr "NonVariantDispatchToMethodTakesPriorityOverVariantMatch"
        ldstr "2"
        call string TestEntrypoint::Test_Scenario2_1<string, class BaseScenario2>()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)

        ldstr "NonVariantDispatchToMethodTakesPriorityOverVariantMatch"
        ldstr "3"
        call string TestEntrypoint::Test_Scenario2_1<object, class BaseScenario2>()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)

        ldstr "NonVariantDispatchToMethodTakesPriorityOverVariantMatch"
        ldstr "4"
        call string TestEntrypoint::Test_Scenario2_2<object, class BaseScenario2>()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)

        ldstr "NonVariantDispatchToMethodTakesPriorityOverVariantMatch"
        ldstr "5"
        call string TestEntrypoint::Test_Scenario2_2<string, class BaseScenario2>()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)

        leave.s NonVariantDispatchToMethodTakesPriorityOverVariantMatchDone
    } catch [System.Runtime]System.Exception {
        stloc.0
        ldstr "NonVariantDispatchToMethodTakesPriorityOverVariantMatch"
        ldstr "No Exception"
        ldloc.0
        callvirt   instance class [System.Runtime]System.Type [System.Runtime]System.Exception::GetType()
        callvirt   instance string [System.Runtime]System.Reflection.MemberInfo::get_Name()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s NonVariantDispatchToMethodTakesPriorityOverVariantMatchDone
    }
NonVariantDispatchToMethodTakesPriorityOverVariantMatchDone: nop

    .try {
        // Variant dispatch when applied at a two layer in the type hierarchy results in calls
        // where the variant match on the derived type will override the exact match on the base type
        ldstr "VariantDispatchToMethodOnDerivedTypeOverridesExactMatchOnBaseType"
        ldstr "6"
        call string TestEntrypoint::Test_Scenario2_1<string, class DerivedScenario2>()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)

        ldstr "VariantDispatchToMethodOnDerivedTypeOverridesExactMatchOnBaseType"
        ldstr "6"
        call string TestEntrypoint::Test_Scenario2_1<object, class DerivedScenario2>()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)

        ldstr "VariantDispatchToMethodOnDerivedTypeOverridesExactMatchOnBaseType"
        ldstr "7"
        call string TestEntrypoint::Test_Scenario2_2<string, class DerivedScenario2>()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)

        ldstr "VariantDispatchToMethodOnDerivedTypeOverridesExactMatchOnBaseType"
        ldstr "7"
        call string TestEntrypoint::Test_Scenario2_2<object, class DerivedScenario2>()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)

        leave.s VariantDispatchToMethodOnDerivedTypeOverridesExactMatchOnBaseTypeDone
    } catch [System.Runtime]System.Exception {
        stloc.0
        ldstr "VariantDispatchToBaseTypeMethodFromDerivedTypeVariantly"
        ldstr "No Exception"
        ldloc.0
        callvirt   instance class [System.Runtime]System.Type [System.Runtime]System.Exception::GetType()
        callvirt   instance string [System.Runtime]System.Reflection.MemberInfo::get_Name()
        call void [TypeHierarchyCommonCs]Statics::CheckForFailure(string,string,string)
        leave.s VariantDispatchToMethodOnDerivedTypeOverridesExactMatchOnBaseTypeDone
    }
VariantDispatchToMethodOnDerivedTypeOverridesExactMatchOnBaseTypeDone: nop

    call int32 [TypeHierarchyCommonCs]Statics::ReportResults()
    ret  } // end of method Main
} // end of class TestEntrypoint
