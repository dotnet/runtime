// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Private.CoreLib.Generators.Models
{
    internal sealed class EventMethod
    {
        public string Name { get; set; }

        public string MethodHeader { get; set; }

        public int EventId { get; set; }

        public List<EventMethodArgument> Arguments { get; set; }
    }
}
