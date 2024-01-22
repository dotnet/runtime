// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Test_runtest

{
   [Fact]
   public static int TestEntryPoint()
   {
      Console.WriteLine();
      bool failed = false;


      C28<int> obj405 = new C28<int>();


      Console.WriteLine(obj405.M28());
      if (obj405.M28() != 54)
      {
          failed = true;
          Console.WriteLine("FAIL");
      }

      Console.WriteLine(obj405.M3());
      if (obj405.M3() != 54)
      {
          failed = true;
          Console.WriteLine("FAIL");
      }

      Console.WriteLine();


      C28<int> obj433 = new C29();


      Console.WriteLine(obj433.M28());
      if (obj433.M28() != 56)
      {
          failed = true;
          Console.WriteLine("FAIL");
      }

      Console.WriteLine(obj433.M3());
      if (obj433.M3() != 56)
      {
          failed = true;
          Console.WriteLine("FAIL");
      }


      C29 obj434 = new C29();


      Console.WriteLine(obj434.M28());
      if (obj434.M28() != 56)
      {
          failed = true;
          Console.WriteLine("FAIL");
      }
      Console.WriteLine(obj434.M3());
      if (obj434.M3() != 56)
      {
          failed = true;
          Console.WriteLine("FAIL");
      }
      Console.WriteLine();
      Console.WriteLine("PASS");

      if (failed)
      {
          return 1;
      }
      else
      {
          Console.WriteLine("PASS");
          return 100;
      }
   }
}
