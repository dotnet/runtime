// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.Test.Runner.JsonRpc.Messages
{
    /// <summary>
    /// Temporary union object to speedup the prototyping, we should use something better like
    /// JsonRcpMessage message = ...;
    /// message
    /// .Error(error => ...)
    /// .SupportedMessages(supportedMessages => ..);
    /// Or some enum for the type...
    /// </summary>
    internal class JsonRpcRawMessageUnion
    {
        public string? jsonrpc { get; set; }
        public string? method { get; set; }
        public string? id { get; set; }
        public string? @params { get; set; }
        public string? code { get; set; }
        public string? message { get; set; }
        public string? data { get; set; }
        public string? result { get; set; }
        public bool Success => code is null;
    }
}
