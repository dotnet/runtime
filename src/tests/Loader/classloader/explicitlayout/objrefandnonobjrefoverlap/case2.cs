// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Same as case1, but exercises a different error path by going ahead and trying to use
// the invalid type.  That code path should never be reached, however, because the bug fix
// is "fail-fast".

// Before this bug was fixed, this test would result in a Fatal Execution Engine error.
// Now, it should produce a TypeLoadException long before it gets to the point where
// the Fatal Execution Engine error would have occurred.
// mwilk 8/15/2003.

using System;
using System.Runtime.InteropServices;
using Xunit;

public struct Wrapper { public Object o; }
	
[ StructLayout( LayoutKind.Explicit )] public struct MyUnion {
  [ FieldOffset( 0 )] public int i;
  [ FieldOffset( 0 )] public Wrapper o;
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
    MyUnion u;
    u.i = 1;
    u.o.o = null;

    Console.WriteLine("u.i = {0}", u.i);  // prints 0, showing the null assigned to the object ref overwrote the 1 assigned to the int.

    u.o.o = new object();
    Console.WriteLine("u.i = {0}", u.i);  // prints some large number, now that the object instance has overwritten the int again

    Console.WriteLine("u.o.o = {0}", u.o.o);  // prints System.Object
    u.i = 1000;
    Console.WriteLine("u.i = {0}", u.i);  // prints 1000 now that the int have overwritten 1000

    Console.WriteLine("u.o.o = {0}", u.o.o);   // bang!  since the object is now invalid, having overwritten the start with 1000.
  }
}
