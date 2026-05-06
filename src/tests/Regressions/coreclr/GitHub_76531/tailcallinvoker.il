// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern dependencytodelete {}
.assembly 'tailcallinvoker' {}

.class public sequential ansi sealed beforefieldinit TailCallInvoker
       extends [mscorlib]System.Object
{
  .method public static void Test() cil managed noinlining
  {
    .maxstack  1
    tail. call void [dependencytodelete]Dependency.DependencyClass::Test()
    ret
  } // end of method TailCallInvoker.Test
} // end of class TailCallInvoker