// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.Common.Tests
{
    public sealed class ThreadCultureChange : IDisposable
    {
        private CultureInfo _originalDefaultCultureInfo;
        private CultureInfo _originalDefaultUICultureInfo;

        public ThreadCultureChange()
        {
            _originalDefaultCultureInfo = CultureInfo.DefaultThreadCurrentCulture;
            _originalDefaultUICultureInfo = CultureInfo.DefaultThreadCurrentUICulture;
        }

        public void ChangeCultureInfo(string culture)
        {
            CultureInfo newCulture = new CultureInfo(culture);
            CultureInfo.DefaultThreadCurrentCulture = newCulture;
            CultureInfo.DefaultThreadCurrentUICulture = newCulture;
        }

        public void Dispose()
        {
            CultureInfo.DefaultThreadCurrentCulture = _originalDefaultCultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = _originalDefaultUICultureInfo;
        }
    }
}
