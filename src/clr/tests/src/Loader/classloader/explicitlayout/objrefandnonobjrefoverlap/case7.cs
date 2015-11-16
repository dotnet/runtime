// like case1 except that we have multiple instances of structs
// overlapping with the int.
// struct
//   int
//   struct
//     objref
//   struct
//     objref

using System;
using System.Runtime.InteropServices;

public class Foo{
    public int i=42;
    public int getI(){return i;}
}
public class Bar{
    private int i=1;
    public int getI(){return i;}
}

public struct WrapFoo { public Foo o; }
public struct WrapBar { public Bar o; }
	
[ StructLayout( LayoutKind.Explicit )] public struct MyUnion1 {
    [ FieldOffset( 0 )] public int i;
    [ FieldOffset( 0 )] public WrapBar o;
    [ FieldOffset( 0 )] public WrapBar o2;
}

[ StructLayout( LayoutKind.Explicit )] public struct MyUnion2 {
    [ FieldOffset( 0 )] public int i;
    [ FieldOffset( 0 )] public WrapFoo o;
    [ FieldOffset( 0 )] public WrapFoo o2;
}

public class Test{

  public static int Main(string[] args){
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
    u1.o.o = new Bar();

    Console.WriteLine("BEFORE: u1.o.o.getI(): {0}.  (EXPECT 1)",u1.o.o.getI());

    u2.i = 0;
    u2.o.o = new Foo();

    // write the Foo's objref value now in u2.o into the int field of u1, 
    // thereby overwriting the Bar objref that had been in u1.o.
    u1.i = u2.i; 

    // If u1.o.o.getI() returns 42, that means that we were able to write to a private 
    // member variable of Bar, a huge security problem!
    int curI = u1.o.o.getI();
    Console.WriteLine("AFTER: u1.o.o.getI(): {0}.  (BUG if 42)",curI);
  }
}
