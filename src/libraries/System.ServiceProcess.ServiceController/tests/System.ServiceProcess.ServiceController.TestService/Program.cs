// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.ServiceProcess.Tests
{
    public class Program
    {
        static int Main(string[] args)
        {
            Thread.CurrentThread.Name = "Test Service";
            if (args.Length == 1 || args.Length == 2)
            {
                TestService testService;
                if (args[0].StartsWith("PropagateExceptionFromOnStart"))
                {
                    var expectedException = new InvalidOperationException("Fail on startup.");
                    testService = new TestService(args[0], expectedException);
                    try
                    {
                        ServiceBase.Run(testService);
                    }
                    catch (Exception actualException)
                    {
                        if (object.ReferenceEquals(expectedException, actualException))
                        {
                            testService.WriteStreamAsync(PipeMessageByteCode.ExceptionThrown).Wait();
                        }
                        else
                        {
                            throw actualException;
                        }
                    }
                }
                else if (args[0].StartsWith("LogWritten"))
                {
                    testService = new TestService(args[0], throwException: null);
                    testService.AutoLog = false;
                    ServiceBase.Run(testService);
                }
                else
                {
                    testService = new TestService(args[0]);
                    ServiceBase.Run(testService);
                }
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
                    Console.WriteLine("EROOR: Invalid Service verb. Only support create or delete.");
                    return 2;
                }
            }

            Console.WriteLine($"usage: <ServiceName> <DisplayName> [create|delete]");
            return 1;
        }
    }
}
