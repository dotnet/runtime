// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using System.Security;

namespace OtherAssemblyTest
{
    public class TestOtherAssemblyClass
    {

        static internal void MethodF(string s)
        {
            Console.WriteLine("Static internal method MethodE on TestOtherAssemblyClass:  s = {0}", s);
        }
        private void MethodH(string s)
        {
            Console.WriteLine("instance private method MethodE on TestOtherAssemblyClass:  s = {0}", s);
        }
    }
}
