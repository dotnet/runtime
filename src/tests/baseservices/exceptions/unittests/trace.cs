// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;

//
// infrastructure
//
public class Trace
{
  public Trace(string tag, string expected)
  {
    Console.WriteLine("-----------------------------");
    Console.WriteLine(tag);
    Console.WriteLine("-----------------------------");
    _expected = expected;
  }
      
  public void Write(string str)
  {
    _actual += str;
    // Console.Write(str);
  }

  public void WriteLine(string str)
  {
    _actual += str;
    _actual += Environment.NewLine;

    // Console.WriteLine(str);
  }

    public int Match()
  {
    // Console.WriteLine("");
    Console.Write(_expected);
    if (_actual.Equals(_expected))
    {
      Console.WriteLine(": PASS");
      return 100;
    }
    else
    {
      Console.WriteLine(": FAIL: _actual='" + _actual + "'");
      Console.WriteLine("_expected='" + _expected + "'");
      return 999;
    }
  }

  string _actual;
  string _expected;
}

