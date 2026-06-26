// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console {}
.assembly extern xunit.core {}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary { .ver 0:0:0:0 }
.assembly extern mscorlib {}
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern TypeHierarchyCommonCs {}
.assembly 'VarianceSafety' {}

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
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string('https://github.com/dotnet/runtimelab/issues/155: Compatible TypeLoadException for invalid inputs') type([TestLibrary]TestLibrary.Utilities) string[1] ('IsNativeAot') }
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
