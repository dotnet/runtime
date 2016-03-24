// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

// This test is basically a clone of a regression test
// for bug 106647.  The difference is, this uses generic
// interfaces at the root of the inheritance hierarchy.
// 
// mwilk.  5/22/2003

public class Foo<A> : Int<A,Foo<A>> {
    public void FV(ref MethodsFired pMF) {
      pMF |= MethodsFired.Public;
    }
}

public class Bar : Foo<Bar>, Int<Bar,Foo<Bar>> {
    void Int<Bar,Foo<Bar>>.FV(ref MethodsFired pMF) {
      pMF |= MethodsFired.ExplicitInt;
      base.FV(ref pMF);
    }
}

public class M {
    public static int PASS=100;
    public static int FAIL=101;

    public static int Indirect(){
      bool success=true;

      // The generic case.
      MethodsFired mfGen = MethodsFired.None;
      Bar bar = new Bar();
      bar.FV(ref mfGen);

      if((mfGen ^ (MethodsFired.Public))!=0){
        Console.WriteLine("FAIL!");
        Console.WriteLine("\tExpected: {0}",MethodsFired.Public);
        Console.WriteLine("\tGot: {0}",mfGen);
        success=false;
      }
      
      mfGen = MethodsFired.None;
      Int<Bar,Foo<Bar>> ibar = bar;
      ibar.FV(ref mfGen);
      if((mfGen ^ (MethodsFired.ExplicitInt|MethodsFired.Public))!=0){
        Console.WriteLine("FAIL!");
        Console.WriteLine("\tExpected: {0}",MethodsFired.ExplicitInt | MethodsFired.Public);
        Console.WriteLine("\tGot: {0}",mfGen);
        success=false;
      }
      
      if(success){
        Console.WriteLine("PASS");
        return PASS;
      }
      else return FAIL;
    }
    public static int Main() {
      int rc=FAIL;
      try{
        rc=Indirect();
      }
      catch(Exception e){
        Console.WriteLine("FAIL!");
        Console.WriteLine("90D50F72-CA6A-8101-FBEE-0066B7E72176");
        Console.WriteLine(e.ToString());
        rc=FAIL;
      }
      return rc;
    }
}

