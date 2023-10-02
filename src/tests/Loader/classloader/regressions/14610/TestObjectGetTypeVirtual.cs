// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// created 10/24/2002, mwilk
using System;
using System.Text;
using System.Reflection;
using Xunit;

public class MyObject{
  public const int MY_OBJECT_FOO = 42;

  public int foo(){
    return MY_OBJECT_FOO;
  }
}

public interface IFoo{
  int foo();
}

public class SubObject : MyObject, IFoo{
  // Note that this class claims to implement IFoo but does
  // not appear to provide an impl for the foo() method.  
  // In this case, the compiler notices that the class extends
  // MyObject, which does have a method foo() by the same signature,
  // though it is not necessarily intended to implement the IFoo interface.
  // the compiler will mark MyObject.foo() as 'virtual final' (use ildasm to prove it
  // to yourself.), and all this class to inherit the implementation.
}

public class MySecondObject{
  public const int MY_SECOND_OBJECT_FOO = 43;

  public int foo(){
    return MY_SECOND_OBJECT_FOO;
  }
}

public class SecondSubObject : MySecondObject, IFoo{
  public const int MY_SECOND_SUB_OBJECT_FOO = 44;

  // Note that since this class *does* provide an implementation
  // of foo(), ....
  public new int foo(){
    return MY_SECOND_SUB_OBJECT_FOO;
  }
}

public class Test_TestObjectGetTypeVirtual{
  public const int PASS = 100;
  public const int FAIL = 42;

  [Fact]
  public static int TestEntryPoint(){

    // Note: These first two tests are just to demonstrate the principle
    // behind this bug/regression.  If they fail, it is not really critical,
    // They could be commented out if they ever started failing for some 
    // unknown reason.

    if(typeof(MyObject).GetMethod("foo").IsVirtual){
      Console.WriteLine("Good. MyObject.foo() is virtual, as expected.");
    }
    else{
      StringBuilder sb = new StringBuilder();
      sb.Append("Error.  MyObject.foo() should have been made virtual ");
      sb.Append("by the compiler.  This is not necessarily an error, but it ");
      sb.Append("violates the assumptions of this test.  As long as ");
      sb.Append("Object.GetType() is not virtual, you may safely ignore this error, ");
      sb.Append("or you may be curious to look into why this has changed and update ");
      sb.Append("this test. (B6CAF320-B776-AB69-FE96-7E3166D9F1B9)");
      Console.WriteLine(sb.ToString());
      return FAIL;
    }

    if(typeof(MySecondObject).GetMethod("foo").IsVirtual){
      StringBuilder sb = new StringBuilder();
      sb.Append("Error.  MySecondObject.foo() should NOT have been made virtual ");
      sb.Append("by the compiler.  This is not necessarily an error, but it ");
      sb.Append("violates the assumptions of this test.  As long as ");
      sb.Append("Object.GetType() is not virtual, you may safely ignore this error, ");
      sb.Append("or you may be curious to look into why this has changed and update ");
      sb.Append("this test. (522823E1-C992-0EE2-EAE5-E5D3D137116C)");
      Console.WriteLine(sb.ToString());
      return FAIL;
    }
    else{
      Console.WriteLine("Good. MySecondObject.foo() is not virtual, as expected.");
    }


    // This is the REAL test.  Make sure that Object.GetType() is NOT virtual.
    if(typeof(Object).GetMethod("GetType").IsVirtual){
      StringBuilder sb = new StringBuilder();
      sb.Append("Error.  Object.GetType() should NOT have been made virtual ");
      sb.Append("by the compiler.  According to NDPWhidbey bug 14610, this is ");
      sb.Append("a bug.  Perhaps this has resulted from a change in the compiler. ");
      sb.Append("To investigate, use ildasm to inspect the signature of the Object.GetType ");
      sb.Append("method in mscorlib.dll for the build that this error has occurred on.  ");
      sb.Append("You should find that the method is erroneously flagged as virtual.");
      sb.Append("(6E27D9E2-C491-0F89-705C-1C0A66673D9E)");
      Console.WriteLine(sb.ToString());
      return FAIL;
    }
    else{
      Console.WriteLine("Good. Object.GetType() is not virtual, as expected.");
    }

    Console.WriteLine("PASS!");
    return PASS;
  }
}
