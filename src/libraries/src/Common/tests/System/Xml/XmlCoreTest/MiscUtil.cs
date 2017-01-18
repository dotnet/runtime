// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

[assembly: System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAssembly]

namespace XmlCoreTest.Common
{
    public static class MiscUtil
    {
        public static bool IsCurrentCultureHasLimitedDateRange
        {
            get
            {
                // test if the current culture support full range of date time 1/1/1 to 12/31/9999
                // in Arabic, it's limited to 4/30/1900 11/16/2077
                try
                {
                    DateTimeOffset dto1 = new DateTimeOffset(1, 1, 1, 1, 1, 1, new TimeSpan(0, 0, 0));
                    string s1 = dto1.ToString();

                    DateTimeOffset dto2 = new DateTimeOffset(9999, 12, 31, 1, 1, 1, new TimeSpan(0, 0, 0));
                    string s2 = dto2.ToString();
                }
                catch (ArgumentOutOfRangeException)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
