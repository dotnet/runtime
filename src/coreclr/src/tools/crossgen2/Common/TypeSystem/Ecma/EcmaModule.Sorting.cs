// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem.Ecma
{
    partial class EcmaModule
    {
        internal int CompareTo(EcmaModule other)
        {
            if (this == other)
                return 0;

            Guid thisMvid = _metadataReader.GetGuid(_metadataReader.GetModuleDefinition().Mvid);
            Guid otherMvid = other._metadataReader.GetGuid(other.MetadataReader.GetModuleDefinition().Mvid);

            Debug.Assert(thisMvid.CompareTo(otherMvid) != 0, "Different instance of EcmaModule but same MVID?");
            return thisMvid.CompareTo(otherMvid);
        }
    }
}
