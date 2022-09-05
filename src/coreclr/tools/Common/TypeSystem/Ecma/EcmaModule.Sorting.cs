// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem.Ecma
{
    public partial class EcmaModule
    {
        public int CompareTo(EcmaModule other)
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
