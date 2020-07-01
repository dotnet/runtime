// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Diagnostics.Tools.RuntimeClient;
using Tracing.Tests.Common;
using System.Text;
using System.Threading;
using System.IO;
using Microsoft.Diagnostics.Tracing;

namespace Tracing.Tests.ReverseValidation
{
    public class FAKETEST
    {
        // THIS TEST PURPOSEFULLY TIMES OUT!!!!
        public static async Task<bool> TEST_PurposefullyTimeout()
        {
            bool fSuccess = true;
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            Task<bool> subprocessTask = Utils.RunSubprocess(
                currentAssembly: Assembly.GetExecutingAssembly(),
                environment: new Dictionary<string,string> 
                {
                    { Utils.DiagnosticsMonitorAddressEnvKey, serverName }
                    // with this commented out, the subprocess should pause on start
                    // causing the subprocess to never exit
                    // and the wait below to be indefinite
                    // { Utils.DiagnosticsMonitorPauseOnStartEnvKey, "0" }
                },
                duringExecution: async (_) =>
                {
                    var ad1 = await ReverseServer.CreateServerAndReceiveAdvertisement(serverName);
                    Logger.logger.Log(ad1.ToString());
                    var ad2 = await ReverseServer.CreateServerAndReceiveAdvertisement(serverName);
                    Logger.logger.Log(ad2.ToString());
                    var ad3 = await ReverseServer.CreateServerAndReceiveAdvertisement(serverName);
                    Logger.logger.Log(ad3.ToString());
                    var ad4 = await ReverseServer.CreateServerAndReceiveAdvertisement(serverName);
                    Logger.logger.Log(ad4.ToString());
                }
            );

            // THIS SHOULD TIMEOUT THE TEST
            fSuccess &= await subprocessTask;

            return fSuccess;
        }

        public static async Task<int> Main(string[] args)
        {
            if (args.Length >= 1)
            {
                Console.Out.WriteLine("Subprocess started!  Waiting for input...");
                var input = Console.In.ReadLine(); // will block until data is sent across stdin
                Console.Out.WriteLine($"Received '{input}'.  Exiting...");
                return 0;
            }

            bool fSuccess = true;
            IEnumerable<MethodInfo> tests = typeof(FAKETEST).GetMethods().Where(mi => mi.Name.StartsWith("TEST_"));
            foreach (var test in tests)
            {
                Logger.logger.Log($"::== Running test: {test.Name}");
                bool result = true;
                try
                {
                    result = await (Task<bool>)test.Invoke(null, new object[] {});
                }
                catch (Exception e)
                {
                    result = false;
                    Logger.logger.Log(e.ToString());
                }
                fSuccess &= result;
                Logger.logger.Log($"Test passed: {result}");
                Logger.logger.Log($"");

            }
            return fSuccess ? 100 : -1;
        }
    }
}