// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.Test.Runner.JsonRpc.Messages
{
    internal abstract class MessageIdBase : MessageBase
    {
        public MessageIdBase(string? id = null) => this.id = id;
        public string? id { get; private set; }
    }
}
