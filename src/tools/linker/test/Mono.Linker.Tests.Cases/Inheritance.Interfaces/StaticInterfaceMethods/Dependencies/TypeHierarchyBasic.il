// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
}

.assembly 'library' { }

.class interface public abstract auto ansi InterfaceScenario1
{
  .method public newslot virtual abstract static int32 Method() cil managed noinlining
  {
  } // end of method Method
} // end of class InterfaceScenario1
.class public auto ansi BaseScenario1
       extends [System.Runtime]System.Object
       implements InterfaceScenario1
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
.class public auto ansi DerivedScenario1
       extends BaseScenario1
{
} // end of class DerivedScenario1
.class interface public abstract auto ansi InterfaceScenario2
{
  .method public newslot virtual abstract static int32 Method() cil managed noinlining
  {
  } // end of method Method
} // end of class InterfaceScenario2
.class public auto ansi BaseScenario2
       extends [System.Runtime]System.Object
       implements InterfaceScenario2
{
  .method public static int32 Method() cil managed noinlining
  {
    .override method int32 InterfaceScenario2::Method()
    .locals init (int32 V_O)
    ldloca.s 0
    initobj int32
    ldloc.0
    ret
  } // end of method Method
} // end of class BaseScenario2
.class public auto ansi DerivedScenario2
       extends BaseScenario2
       implements InterfaceScenario2
{
} // end of class DerivedScenario2
.class interface public abstract auto ansi InterfaceScenario3
{
  .method public newslot virtual abstract static int32 Method() cil managed noinlining
  {
  } // end of method Method
} // end of class InterfaceScenario3
.class public abstract auto ansi BaseScenario3
       extends [System.Runtime]System.Object
       implements InterfaceScenario3
{
} // end of class BaseScenario3
.class public auto ansi DerivedScenario3
       extends BaseScenario3
       implements InterfaceScenario3
{
  .method public static int32 MethodImplOnDerived() cil managed noinlining
  {
    .override method int32 InterfaceScenario3::Method()
    .locals init (int32 V_O)
    ldloca.s 0
    initobj int32
    ldloc.0
    ret
  } // end of method MethodImplOnDerived
} // end of class DerivedScenario3

.class public auto ansi TestEntrypoint
       extends [System.Runtime]System.Object
{
  .method public static void Test_Scenario1() cil managed noinlining
  {
    constrained. DerivedScenario1
    call int32 InterfaceScenario1::Method()
    pop
    ret
  } // end of method Test_Scenario1
  .method public static void Test_Scenario2() cil managed noinlining
  {
    constrained. DerivedScenario2
    call int32 InterfaceScenario2::Method()
    pop
    ret
  } // end of method Test_Scenario2
  .method public static void Test_Scenario3() cil managed noinlining
  {
    constrained. DerivedScenario3
    call int32 InterfaceScenario3::Method()
    pop
    ret
  } // end of method Test_Scenario3
  .method public static int32 Test() cil managed noinlining
  {
    .locals init (class [System.Runtime]System.Exception V_0)
    .try {
        call void TestEntrypoint::Test_Scenario1()
        call void TestEntrypoint::Test_Scenario2()
        call void TestEntrypoint::Test_Scenario3()
        leave.s Scenario1Done
    } catch [System.Runtime]System.Exception {
        stloc.0
        leave.s Scenario1Done
    }
Scenario1Done: nop
    ret
  } // end of method Test
} // end of class TestEntrypoint


