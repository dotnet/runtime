// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
using System;
using System.Runtime.CompilerServices;

namespace GCD
{
    /// <summary>
    /// Summary description for Class1.
    /// </summary>
    class GCD
    {
        private int _val = -2;
        private int _exitcode = -1;
        public GCD() {}
        public int GetExitCode(){ return _exitcode;}
        public void g ()
        {
            throw new System.Exception("TryCode test");
        }
        public void TryCode0 (object obj)
        {
            _val = (int)obj;
            g();
        }
        public void CleanupCode0 (object obj, bool excpThrown)
        {
            if(excpThrown && ((int)obj == _val))
            {
                _exitcode = 100;
            }
        }
    }


    class GCDTest 
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
            static int Main(string[] args)
            {
                GCD gcd = new GCD();
                RuntimeHelpers.TryCode t = new RuntimeHelpers.TryCode(gcd.TryCode0);
                RuntimeHelpers.CleanupCode c = new RuntimeHelpers.CleanupCode(gcd.CleanupCode0);
                int val = 21;
                try
                {
                    RuntimeHelpers.ExecuteCodeWithGuaranteedCleanup(t, c, val);
                }
                catch (Exception Ex)
                {

                }

                return gcd.GetExitCode();
            }
    }
}
