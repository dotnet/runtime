// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    internal sealed class PreserveReferenceHandler : ReferenceHandler
    {
        public override ReferenceResolver CreateResolver() => throw new InvalidOperationException();

        internal override ReferenceResolver CreateResolver(bool writing) => new PreserveReferenceResolver(writing);
    }
}
