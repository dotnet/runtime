// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.Test.Runner.JsonRpc.Messages
{
    internal abstract class MessageBase
    {
        public string jsonrpc { get; } = "2.0";
    }
}
