// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.Test.Runner.JsonRpc.Messages
{
    internal abstract class MethodMessageBase : MessageIdBase
    {
        public MethodMessageBase(string id) : base(id)
        {

        }

        public abstract string method { get; }
    }
}
