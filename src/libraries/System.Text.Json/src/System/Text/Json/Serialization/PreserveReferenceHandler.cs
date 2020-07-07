// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    internal sealed class PreserveReferenceHandler : ReferenceHandler
    {
        public override ReferenceResolver CreateResolver() => throw new InvalidOperationException();

        internal override ReferenceResolver CreateResolver(bool writing) => new PreserveReferenceResolver(writing);
    }
}
