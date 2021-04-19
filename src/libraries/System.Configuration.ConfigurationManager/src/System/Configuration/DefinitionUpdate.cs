// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    internal sealed class DefinitionUpdate : Update
    {
        internal DefinitionUpdate(string configKey, bool moved, string updatedXml, SectionRecord sectionRecord) :
            base(configKey, moved, updatedXml)
        {
            SectionRecord = sectionRecord;
        }

        internal SectionRecord SectionRecord { get; }
    }
}
