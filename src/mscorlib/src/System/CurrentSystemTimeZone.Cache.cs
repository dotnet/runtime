// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Globalization;

namespace System
{
    internal partial class CurrentSystemTimeZone
    {
        private DaylightTime GetCachedDaylightChanges(int year)
        {
            Object objYear = (Object)year;

            if (!m_CachedDaylightChanges.Contains(objYear))
            {
                DaylightTime currentDaylightChanges = CreateDaylightChanges(year);
                lock (m_CachedDaylightChanges)
                {
                    if (!m_CachedDaylightChanges.Contains(objYear))
                    {
                        m_CachedDaylightChanges.Add(objYear, currentDaylightChanges);
                    }
                }
            }

            return (DaylightTime)m_CachedDaylightChanges[objYear];
        }

        // The per-year information is cached in in this instance value. As a result it can
        // be cleaned up by CultureInfo.ClearCachedData, which will clear the instance of this object
        private readonly Hashtable m_CachedDaylightChanges = new Hashtable();
    }
}
