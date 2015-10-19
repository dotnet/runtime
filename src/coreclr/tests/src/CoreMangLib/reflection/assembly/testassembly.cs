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