using System;
using System.Collections.Generic;

class A {
  static int Main ()
  {
    A [] aa = new A [1];
    IList<object> io = aa;
    try {
      io [0] = new object ();
      A a = aa [0];
      Console.WriteLine ("{0}", a.GetType ());
    } catch (ArrayTypeMismatchException) {
      return 0;
    }
    return 1;
    
  }
}
