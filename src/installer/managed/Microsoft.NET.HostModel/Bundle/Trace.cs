// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.NET.HostModel.Bundle
{
    /// <summary>
    ///  Tracing utilities for diagnostic output
    /// </summary>
    public class Trace
    {
        private readonly bool Verbose;

        public Trace(bool verbose)
        {
            Verbose = verbose;
        }

        public void Log(string fmt, params object[] args)
        {
            if (Verbose)
            {
                Console.WriteLine("LOG: " + fmt, args);
            }
        }

        public void Error(string type, string message)
        {
            Console.Error.WriteLine($"ERROR: {message}");
        }
    }
}
