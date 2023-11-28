// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
public class SwitchTest
{
 const int Pass = 100;
 const int Fail = -1;

 [Fact]
 public static int TestEntryPoint()
 {
  int sum =0;
  for(int i=2; i < 5; i++) {
   switch(i) {
   case 2:
        sum += i; 
        break;
   case 3:
        sum += i;
        break;
   default:
        sum -= 5;
        break;
   }
  }

  return sum == 0 ? Pass : Fail;
 }
}
