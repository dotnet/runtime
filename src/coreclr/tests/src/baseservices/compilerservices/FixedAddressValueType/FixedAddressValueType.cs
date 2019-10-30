// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
using System;
using System.Runtime.CompilerServices;

public struct Age {
   public int years;
   public int months;
}

public class FreeClass
{
   public static Age FreeAge;

   public static unsafe IntPtr AddressOfFreeAge()
   { 
      fixed (Age* pointer = &FreeAge) 
      { return (IntPtr) pointer; } 
   }
}

public class FixedClass
{
   [FixedAddressValueType]
   public static Age FixedAge;

   public static unsafe IntPtr AddressOfFixedAge()
   { 
      fixed (Age* pointer = &FixedAge) 
      { return (IntPtr) pointer; } 
   }   
}

public class Example
{
   public static int Main()
   {
      // Get addresses of static Age fields.
      IntPtr freePtr1 = FreeClass.AddressOfFreeAge();

      IntPtr fixedPtr1 = FixedClass.AddressOfFixedAge();

      // Garbage collection.
      GC.Collect(3, GCCollectionMode.Forced, true, true);
      GC.WaitForPendingFinalizers();
      
      // Get addresses of static Age fields after garbage collection.
      IntPtr freePtr2 = FreeClass.AddressOfFreeAge();
      IntPtr fixedPtr2 = FixedClass.AddressOfFixedAge();

      if(freePtr1 != freePtr2 && fixedPtr1 == fixedPtr2)
      {
          return 100;
      }

      return -1;
   }
}
