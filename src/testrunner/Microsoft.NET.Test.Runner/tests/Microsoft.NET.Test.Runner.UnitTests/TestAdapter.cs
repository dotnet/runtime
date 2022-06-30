#pragma warning disable CA2255

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Jsonite;

namespace Microsoft.NET.Test.Runner.UnitTests.TestAdapter
{
    internal class TestAdapter
    {
        public static class InProcessJsonRpcServer
        {
            [ModuleInitializer]
            public static void Init()
            {
                TestExtensions.RegisterRunnerToExtensionCallback(new JsonRpcServer().Call);
            }
        }
    }

    internal class JsonRpcServer
    {
        public async Task<string> Call(string jsonMessage)
        {
            JsonObject request = (JsonObject)Json.Deserialize(jsonMessage);

            JsonObject response = new();
            response["id"] = (string)request["id"];
            response["jsonrpc"] = "2.0";

            if ((string)request["method"] == "runAllTests")
            {
                await Task.WhenAll(Task.Run(() => new UnitTest1().Test1()));
                JsonObject result = new()
                {
                    { "totalTests", 1 }
                };
                response.Add("result", result);
                return Json.Serialize(response);
            }

            throw new NotImplementedException();
        }
    }
}
