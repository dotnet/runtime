// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.NET.Test.Runner.Console;
using Microsoft.NET.Test.Runner.JsonRpc;
using Microsoft.NET.Test.Runner.JsonRpc.Messages;
using Microsoft.NET.Test.Runner.JsonRpc.Messages.v1;

namespace Microsoft.NET.Test.Runner.ExtensionsManager
{
    internal class RunTestsOrchestrator : ITestOrchestrator
    {
        private readonly IExtensionsManager _extensionsManager;
        private readonly IConsole _console;
        private readonly IJsonRpcMessageSerializer _jsonRpcMessageSerializer;

        public RunTestsOrchestrator(IExtensionsManager extensionsManager, IConsole console, IJsonRpcMessageSerializer jsonRpcMessageSerializer)
        {
            _extensionsManager = extensionsManager;
            _console = console;
            _jsonRpcMessageSerializer = jsonRpcMessageSerializer;
        }

        public async Task Start()
        {
            RunAllTestsRequest runAllTestsRequest = new(Guid.NewGuid().ToString());
            string runAllTestsRequestText = _jsonRpcMessageSerializer.Serialize(runAllTestsRequest);

            foreach (IExtension inProcessExtension in await _extensionsManager.GetInProcessExtensionsAsync())
            {
                string runAllTestsResponseText = await inProcessExtension.SendMessageAsync(runAllTestsRequestText);
                JsonRpcRawMessageUnion runAllTestsResponseObject = _jsonRpcMessageSerializer.Deserialize<JsonRpcRawMessageUnion>(runAllTestsResponseText);
                if (runAllTestsResponseObject.Success)
                {
                    TestsResultResponse testResult = _jsonRpcMessageSerializer.Deserialize<TestsResultResponse>(runAllTestsResponseObject.result!);
                    _console.WriteLine($"We ran '{testResult.TotalTests}' tests.", ConsoleColor.Green);
                    return;
                }
                else
                {
                    _console.WriteLine($"Failed to run tests, code: {runAllTestsResponseObject.code}\nmessage:{runAllTestsResponseObject.message}", ConsoleColor.Red);
                }
            }

            _console.WriteLine($"We ran '0' tests.", ConsoleColor.Yellow);
        }
    }
}
