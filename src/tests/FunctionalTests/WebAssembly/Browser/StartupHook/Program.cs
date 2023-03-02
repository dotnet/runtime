// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;

namespace Sample
{
    public partial class Test
    {
        public static void Main()
        {
        }

        [JSExport]
        public static int TestMeaning()
        {
            string appContextKey = "Test.StartupHookForFunctionalTest.DidRun";
            var data = (string) AppContext.GetData (appContextKey);

            if (data != "Yes") {
                string msg = $"Expected startup hook to set {appContextKey} to 'Yes', got '{data}'";
                Console.Error.WriteLine(msg);
                return 104;
            }
            return 42;
        }
    }
}
