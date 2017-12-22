// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace System.ServiceProcess.Tests
{
    public class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 1 || args.Length == 2)
            {
                TestService testService = new TestService(args[0]);
                ServiceBase.Run(testService);
                return 0;
            }
            else if (args.Length == 3)
            {
                TestServiceInstaller testServiceInstaller = new TestServiceInstaller();

                testServiceInstaller.ServiceName = args[0];
                testServiceInstaller.DisplayName = args[1];

                if (args[2] == "create")
                {
                    testServiceInstaller.Install();
                    return 0;
                }
                else if (args[2] == "delete")
                {
                    testServiceInstaller.RemoveService();
                    return 0;
                }
                else
                {
                    Console.WriteLine("EROOR: Invalid Service verb. Only suppot create or delete.");
                    return 2;
                }
            }

            Console.WriteLine($"usage: <ServiceName> <DisplayName> [create|delete]");
            return 1;
        }
    }
}
