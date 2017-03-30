// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Configuration
{
    [DebuggerDisplay("LocationSectionRecord {ConfigKey}")]
    internal class LocationSectionRecord
    {
        private List<ConfigurationException> _errors; // errors

        internal LocationSectionRecord(SectionXmlInfo sectionXmlInfo, List<ConfigurationException> errors)
        {
            SectionXmlInfo = sectionXmlInfo;
            _errors = errors;
        }

        internal string ConfigKey => SectionXmlInfo.ConfigKey;

        internal SectionXmlInfo SectionXmlInfo { get; }

        // Errors associated with the parse of a location section.
        internal ICollection<ConfigurationException> Errors => _errors;

        internal List<ConfigurationException> ErrorsList => _errors;

        internal bool HasErrors => ErrorsHelper.GetHasErrors(_errors);

        internal void AddError(ConfigurationException e)
        {
            ErrorsHelper.AddError(ref _errors, e);
        }
    }
}