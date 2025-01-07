// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

public class E<T> : Exception { 
  T fld; 
  public E(T x) { fld = x; }
  public T Get() { return fld; }
  public void Show() { Console.WriteLine("E<" + typeof(T) + ">(" + fld + ")"); }
}


public class D { 
  // Fifth test: polymorphic catch in shared code in a generic method
  public static int Test5<T>(bool str,int x) {
    if (x < 100) 
        if (str) throw new E<string>(x.ToString());
        else throw new E<object>(x.ToString());
    else 
    try {
      Test5<T>(str,x-7);
    }
    catch (E<T> ei) { ei.Show(); }
    catch (Exception e) { 
	Console.WriteLine("Not caught: "+e.GetType().ToString());
	return -1; 
    }
    return 100;
  }

}

public class M {
  [Fact]
  public static int TestEntryPoint() {
    M test = new M();
    return test.Run();
  }
  public int Run(){
    int val = D.Test5<string>(true,129);
    if (val == 100)
    	val = D.Test5<object>(false,128);
    else
	D.Test5<object>(false,128);
    return val;
  }
}
