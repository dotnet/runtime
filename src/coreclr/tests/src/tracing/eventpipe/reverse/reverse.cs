// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Diagnostics.Tools.RuntimeClient;
using Tracing.Tests.Common;

namespace Tracing.Tests.ReverseValidation
{
    public class ReverseValidation
    {
        public static async Task RunSubprocess(string serverName, Func<Task> beforeExecution = null, Func<Task> duringExecution = null, Func<Task> afterExecution = null)
        {
            using (var process = new Process())
            {
                if (beforeExecution != null)
                    await beforeExecution();

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.Environment.Add("DOTNET_DiagnosticsMonitorAddress", serverName);
                process.StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                process.StartInfo.Arguments = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath + " 0";
                Logger.logger.Log($"running sub-process: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
                bool fSuccess = process.Start();
                Logger.logger.Log($"subprocess started: {fSuccess}");

                if (duringExecution != null)
                    await duringExecution();

                process.Kill();

                if (afterExecution != null)
                    await afterExecution();
            }
        }
        public static async Task<bool> TEST_RuntimeIsResilientToServerClosing()
        {
            string serverName = ReverseServer.MakeServerAddress();
            Logger.logger.Log($"Server name is '{serverName}'");
            await RunSubprocess(
                serverName: serverName,
                duringExecution: async () =>
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

            return true;
        }

        public static async Task<bool> TEST_RuntimeConnectsToExistingServer()
        {
            string serverName = ReverseServer.MakeServerAddress();
            Task<IpcAdvertise> advertiseTask = ReverseServer.CreateServerAndReceiveAdvertisement(serverName);
            Logger.logger.Log($"Server name is `{serverName}`");
            await RunSubprocess(
                serverName: serverName,
                duringExecution: async () => 
                {
                    IpcAdvertise advertise = await advertiseTask;
                    Logger.logger.Log(advertise.ToString());
                }
            );

            return true;
        }


        public static async Task<int> Main(string[] args)
        {
            if (args.Length >= 1)
            {
                await Task.Delay(-1); // will be killed in test
                return 1;
            }

            bool fSuccess = true;
            IEnumerable<MethodInfo> tests = typeof(ReverseValidation).GetMethods().Where(mi => mi.Name.StartsWith("TEST_"));
            foreach (var test in tests)
            {
                Logger.logger.Log($"::== Running test: {test.Name}");
                bool result = await (Task<bool>)test.Invoke(null, new object[] {});
                fSuccess &= result;
                Logger.logger.Log($"Test passed: {result}");

            }
            return fSuccess ? 100 : -1;
        }
    }
}