// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    // LocationUpdates contains all the updates that share the same location characteristics.
    internal sealed class LocationUpdates
    {
        internal LocationUpdates(OverrideModeSetting overrideMode, bool inheritInChildApps)
        {
            OverrideMode = overrideMode;
            InheritInChildApps = inheritInChildApps;
            SectionUpdates = new SectionUpdates(string.Empty);
        }

        internal OverrideModeSetting OverrideMode { get; }

        internal bool InheritInChildApps { get; }

        internal SectionUpdates SectionUpdates { get; }

        internal bool IsDefault => OverrideMode.IsDefaultForLocationTag && InheritInChildApps;

        internal void CompleteUpdates()
        {
            SectionUpdates.CompleteUpdates();
        }
    }
}
