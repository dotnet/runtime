// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

public class Test

{
   public static int Main()
   {
      Console.WriteLine();


      C28<int> obj405 = new C28<int>();


      Console.WriteLine(obj405.M28());
      Console.WriteLine(obj405.M3());
      Console.WriteLine();


      C28<int> obj433 = new C29();


      Console.WriteLine(obj433.M28());

      Console.WriteLine(obj433.M3());


      C29 obj434 = new C29();


      Console.WriteLine(obj434.M28());
      Console.WriteLine(obj434.M3());
      Console.WriteLine();
      Console.WriteLine("PASS");

      return 100;
   }
}
