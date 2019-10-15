// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
class SwitchTest
{
 const int Pass = 100;
 const int Fail = -1;

 public static int Main()
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
