// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata;

namespace System.Reflection.PortableExecutable
{
    /// <summary>
    /// Base class for PE resource section builder. Implement to provide serialization logic for native resources.
    /// </summary>
    public abstract class ResourceSectionBuilder
    {
        protected ResourceSectionBuilder()
        {
        }

        protected internal abstract void Serialize(BlobBuilder builder, SectionLocation location);
    }
}
