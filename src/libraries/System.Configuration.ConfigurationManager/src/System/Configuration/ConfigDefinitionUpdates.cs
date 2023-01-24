// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.Configuration
{
    // Contains all the updates to section definitions across all location sections.
    internal sealed class ConfigDefinitionUpdates
    {
        internal ConfigDefinitionUpdates()
        {
            LocationUpdatesList = new ArrayList();
        }

        internal ArrayList LocationUpdatesList { get; }

        internal bool RequireLocation { get; set; }

        // Find the location update with a certain set of location attributes.
        internal LocationUpdates FindLocationUpdates(OverrideModeSetting overrideMode, bool inheritInChildApps)
        {
            foreach (LocationUpdates locationUpdates in LocationUpdatesList)
                if (OverrideModeSetting.CanUseSameLocationTag(locationUpdates.OverrideMode, overrideMode) &&
                    (locationUpdates.InheritInChildApps == inheritInChildApps))
                    return locationUpdates;

            return null;
        }

        // Add a section definition update to the correct location update.
        internal DefinitionUpdate AddUpdate(OverrideModeSetting overrideMode, bool inheritInChildApps, bool moved,
            string updatedXml, SectionRecord sectionRecord)
        {
            LocationUpdates locationUpdates = FindLocationUpdates(overrideMode, inheritInChildApps);
            if (locationUpdates == null)
            {
                locationUpdates = new LocationUpdates(overrideMode, inheritInChildApps);
                LocationUpdatesList.Add(locationUpdates);
            }

            DefinitionUpdate definitionUpdate = new DefinitionUpdate(sectionRecord.ConfigKey, moved, updatedXml,
                sectionRecord);
            locationUpdates.SectionUpdates.AddSection(definitionUpdate);
            return definitionUpdate;
        }

        // Determine which section definition updates are new.
        internal void CompleteUpdates()
        {
            foreach (LocationUpdates locationUpdates in LocationUpdatesList) locationUpdates.CompleteUpdates();
        }

        internal void FlagLocationWritten()
        {
            RequireLocation = false;
        }
    }
}
