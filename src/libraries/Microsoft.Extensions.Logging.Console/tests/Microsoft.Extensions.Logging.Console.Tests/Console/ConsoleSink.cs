// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Logging.Test.Console
{
    public class ConsoleSink
    {
        public List<ConsoleContext> Writes { get; set; } = new List<ConsoleContext>();

        public void Write(ConsoleContext context)
        {
            Writes.Add(context);
        }
    }
}
