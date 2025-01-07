// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// super simple case. forget wrapper structs, just overlap an int and an objref!
using System;
using System.Runtime.InteropServices;
using Xunit;

[ StructLayout( LayoutKind.Explicit )] public struct MyUnion1 {
    [ FieldOffset( 0 )] public int i;
    [ FieldOffset( 0 )] public Object o;
}

public class Test{

  [Fact]
  public static int TestEntryPoint(){
      bool caught=false;
      try{
          Go();
      }
      catch(TypeLoadException e){
          caught=true;
          Console.WriteLine(e);
      }
      if(caught){
          Console.WriteLine("PASS: caught expected exception");
          return 100;
      }
      else{
          Console.WriteLine("FAIL: was allowed to overlap an objref with a scalar.");
          return 101;
      }
  }
  public static void Go(){
    MyUnion1 u1;

    u1.i = 0;
    u1.o = new Object();
    // that's enough. if we didn't throw a TypeLoadException, the test case will fail.
  }
}
