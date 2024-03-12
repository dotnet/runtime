// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this is regression test for VSW 491577
// we have nested types (up to depth 73).
// Loading the 73rd type resulted in AV
// this tests passes as long as we don't AV.


using System;
using Xunit;

public class Test_vsw491577
{
   [Fact]
   public static void TestEntryPoint()
   {
      MyClass0.MyClass1.MyClass2.MyClass3.MyClass4.MyClass5.MyClass6.MyClass7.MyClass8.MyClass9.MyClass10.MyClass11.MyClass12.MyClass13.MyClass14.MyClass15.MyClass16.MyClass17.MyClass18.MyClass19.MyClass20.MyClass21.MyClass22.MyClass23.MyClass24.MyClass25.MyClass26.MyClass27.MyClass28.MyClass29.MyClass30.MyClass31.MyClass32.MyClass33.MyClass34.MyClass35.MyClass36.MyClass37.MyClass38.MyClass39.MyClass40.MyClass41.MyClass42.MyClass43.MyClass44.MyClass45.MyClass46.MyClass47.MyClass48.MyClass49.MyClass50.MyClass51.MyClass52.MyClass53.MyClass54.MyClass55.MyClass56.MyClass57.MyClass58.MyClass59.MyClass60.MyClass61.MyClass62.MyClass63.MyClass64.MyClass65.MyClass66.MyClass67.MyClass68.MyClass69.MyClass70.MyClass71.MyClass72 obj = new MyClass0.MyClass1.MyClass2.MyClass3.MyClass4.MyClass5.MyClass6.MyClass7.MyClass8.MyClass9.MyClass10.MyClass11.MyClass12.MyClass13.MyClass14.MyClass15.MyClass16.MyClass17.MyClass18.MyClass19.MyClass20.MyClass21.MyClass22.MyClass23.MyClass24.MyClass25.MyClass26.MyClass27.MyClass28.MyClass29.MyClass30.MyClass31.MyClass32.MyClass33.MyClass34.MyClass35.MyClass36.MyClass37.MyClass38.MyClass39.MyClass40.MyClass41.MyClass42.MyClass43.MyClass44.MyClass45.MyClass46.MyClass47.MyClass48.MyClass49.MyClass50.MyClass51.MyClass52.MyClass53.MyClass54.MyClass55.MyClass56.MyClass57.MyClass58.MyClass59.MyClass60.MyClass61.MyClass62.MyClass63.MyClass64.MyClass65.MyClass66.MyClass67.MyClass68.MyClass69.MyClass70.MyClass71.MyClass72();
   }
}

public class MyClass0 {
public class MyClass1 {
public class MyClass2 {
public class MyClass3 {
public class MyClass4 {
public class MyClass5 {
public class MyClass6 {
public class MyClass7 {
public class MyClass8 {
public class MyClass9 {
public class MyClass10 {
public class MyClass11 {
public class MyClass12 {
public class MyClass13 {
public class MyClass14 {
public class MyClass15 {
public class MyClass16 {
public class MyClass17 {
public class MyClass18 {
public class MyClass19 {
public class MyClass20 {
public class MyClass21 {
public class MyClass22 {
public class MyClass23 {
public class MyClass24 {
public class MyClass25 {
public class MyClass26 {
public class MyClass27 {
public class MyClass28 {
public class MyClass29 {
public class MyClass30 {
public class MyClass31 {
public class MyClass32 {
public class MyClass33 {
public class MyClass34 {
public class MyClass35 {
public class MyClass36 {
public class MyClass37 {
public class MyClass38 {
public class MyClass39 {
public class MyClass40 {
public class MyClass41 {
public class MyClass42 {
public class MyClass43 {
public class MyClass44 {
public class MyClass45 {
public class MyClass46 {
public class MyClass47 {
public class MyClass48 {
public class MyClass49 {
public class MyClass50 {
public class MyClass51 {
public class MyClass52 {
public class MyClass53 {
public class MyClass54 {
public class MyClass55 {
public class MyClass56 {
public class MyClass57 {
public class MyClass58 {
public class MyClass59 {
public class MyClass60 {
public class MyClass61 {
public class MyClass62 {
public class MyClass63 {
public class MyClass64 {
public class MyClass65 {
public class MyClass66 {
public class MyClass67 {
public class MyClass68 {
public class MyClass69 {
public class MyClass70 {
public class MyClass71 {
public class MyClass72 {
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
}
