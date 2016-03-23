// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;
using CoreFXTestLibrary;

class MarshalClassTests
{
  [StructLayout(LayoutKind.Auto)]
  public struct SomeTestStruct_Auto
  {
    public int i;
  }

  [STAThread]
  static int Main()
  {
    SomeTestStruct_Auto someTs_Auto = new SomeTestStruct_Auto();
    try
    {
      Marshal.StructureToPtr(someTs_Auto, new IntPtr(123), true);
    }
    catch (ArgumentException ex)
    {
      if (ex.ParamName != "structure")
      {
        Console.WriteLine("Thrown ArgumentException is incorrect.");
        return 103;
      }
      if (!ex.Message.Contains("The specified structure must be blittable or have layout information."))
      {
        Console.WriteLine("Thrown ArgumentException is incorrect.");
        return 104;
      }
      return 100;
    }
    catch (Exception e)
    {
      Console.WriteLine("Marshal.StructureToPtr threw unexpected exception {0}.", e);
      return 102;
    }
    Console.WriteLine("Marshal.StructureToPtr did not throw an exception.");
    return 101;
  }
}
