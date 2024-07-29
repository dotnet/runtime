// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this is regression for VSW 491671
// we load a generic type that takes itself as generic parameter.
// at depth 39 we were AVing when ngening the assembly.
// This test is for JIT.
// NGEN test will be located in the NGEN tree.


using System;
using Xunit;

public class Test_GenTypeItself {
   [Fact]
   public static void TestEntryPoint()
   {
      MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<int>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> obj = new MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<MyClass<int>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>();
   }
}

public class MyClass<T> {}
