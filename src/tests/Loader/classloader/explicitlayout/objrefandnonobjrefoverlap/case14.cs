// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// struct
//   enum
//   struct
//     objref

using System;
using System.Runtime.InteropServices;
using Xunit;

public enum MyEnum {nil, alpha, beta};

public class Foo{
    public MyEnum i=MyEnum.alpha;
    public MyEnum getI(){return i;}
}
public class Bar{
    private MyEnum i=MyEnum.beta;
    public MyEnum getI(){return i;}
}

public struct WrapFoo { public Foo o; }
public struct WrapBar { public Bar o; }
	
[ StructLayout( LayoutKind.Explicit )] public struct MyUnion1 {
    [ FieldOffset( 0 )] public MyEnum i;
    [ FieldOffset( 0 )] public WrapBar o;
}

[ StructLayout( LayoutKind.Explicit )] public struct MyUnion2 {
    [ FieldOffset( 0 )] public MyEnum i;
    [ FieldOffset( 0 )] public WrapFoo o;
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
    MyUnion2 u2;
    MyUnion1 u1;

    u1.i = MyEnum.nil;
    u1.o.o = new Bar();

    Console.WriteLine("BEFORE: u1.o.o.getI(): {0}.  (EXPECT 1)",u1.o.o.getI());

    u2.i = MyEnum.nil;
    u2.o.o = new Foo();

    // write the Foo's objref value now in u2.o into the int field of u1, 
    // thereby overwriting the Bar objref that had been in u1.o.
    u1.i = u2.i; 

    // If u1.o.o.getI() returns 42, that means that we were able to write to a private 
    // member variable of Bar, a huge security problem!
    MyEnum curI = u1.o.o.getI();
    Console.WriteLine("AFTER: u1.o.o.getI(): {0}.  (BUG if 42)",curI);
  }
}
