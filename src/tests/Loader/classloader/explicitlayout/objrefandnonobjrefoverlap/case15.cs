// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// struct
//   int
//   struct
//     objref (delegate)

using System;
using System.Runtime.InteropServices;
using Xunit;

public class Foo{
    public int i=42;
    public int getI(){return i;}
}
public class Bar{
    private int i=1;
    public int getI(){return i;}
}

public delegate void FooDelegate(Object o);
public delegate void BarDelegate(Object o);

public struct WrapFoo { public FooDelegate o; }
public struct WrapBar { public BarDelegate o; }
	
[ StructLayout( LayoutKind.Explicit )] public struct MyUnion1 {
    [ FieldOffset( 0 )] public int i;
    [ FieldOffset( 0 )] public WrapBar o;
}

[ StructLayout( LayoutKind.Explicit )] public struct MyUnion2 {
    [ FieldOffset( 0 )] public int i;
    [ FieldOffset( 0 )] public WrapFoo o;
}

public class Test{

    public static void MyCallback(Object o){
        return;
    }

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
    MyUnion2 u2;
    MyUnion1 u1;

    u1.i = 0;
    u1.o.o = new BarDelegate(MyCallback);

    u2.i = 0;
    u2.o.o = new FooDelegate(MyCallback);

    // write the Foo's objref value now in u2.o into the int field of u1, 
    // thereby overwriting the Bar objref that had been in u1.o.
    u1.i = u2.i; 

    // Not doing further checks on delegate specific function calls.  Unless the bug regresses,
    // the test case should never reach this point.  Even it does, the lack of TypeLoadException
    // and the mere execution and returning of this method will indicate failure for the test.
  }
}
