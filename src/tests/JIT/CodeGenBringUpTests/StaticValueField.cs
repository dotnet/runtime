// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
struct TestValue
{
  public int a;
  public short b;
  public long c;
}

// This test stores a primitive (no-GC fields) value type to a static field
// and checks if the contents are correct.
class StaticValueField
{
  const int Pass = 100;
  const int Fail = -1;
  static TestValue sField;
  public static void Init()
  {
    TestValue v = new TestValue();
    v.a = 100;
    v.b = 200;
    v.c = 300;
    sField = v;
  }

  public static int Main()
  {
    Init();
    if (sField.a == 100
      && sField.b == 200
      && sField.c == 300)
    {
      return Pass;
    }
    return Fail;
  }
}
