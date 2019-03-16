// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
