// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class TestClass1
{
    public TestClass1() { Member = 0; }
    public TestClass1(int i) { Member = i; }
    int Member;
}

class TestClass2
{
    TestClass2() { Member = 0; }
    TestClass2(int i) { Member = i; }
    int Member;
}