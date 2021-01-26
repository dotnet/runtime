// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    internal sealed class IgnoreReferenceHandler : ReferenceHandler
    {
        public IgnoreReferenceHandler() => HandlingStrategy = ReferenceHandlingStrategy.IgnoreCycle;

        public override ReferenceResolver CreateResolver() => new IgnoreReferenceResolver();
    }
}
