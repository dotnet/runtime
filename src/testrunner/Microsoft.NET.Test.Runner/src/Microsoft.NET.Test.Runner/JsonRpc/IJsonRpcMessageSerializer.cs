// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.NET.Test.Runner.JsonRpc.Messages;

namespace Microsoft.NET.Test.Runner.JsonRpc
{
    internal interface IJsonRpcMessageSerializer
    {
        string Serialize(MessageBase message);
        T Deserialize<T>(string message);
    }
}
