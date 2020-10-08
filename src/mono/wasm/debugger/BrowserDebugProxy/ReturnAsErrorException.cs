// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.WebAssembly.Diagnostics
{
    internal class ReturnAsErrorException : Exception
    {
        public Result Error { get; }

        public ReturnAsErrorException(JObject error)
            => Error = Result.Err(error);

        public ReturnAsErrorException(string message)
            => Error = Result.Err(message);

        // FIXME: remove classname=null stuff?
        public static ReturnAsErrorException ErrorObject(string message, string className)
            => new ReturnAsErrorException(JObject.FromObject(new
            {
                type = "object",
                subtype = "error",
                description = message,
                className
            }));
    }
}
