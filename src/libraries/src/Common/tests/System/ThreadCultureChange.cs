// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.Common.Tests
{
    public sealed class ThreadCultureChange : IDisposable
    {
        private readonly CultureInfo _originalCultureInfo;
        private readonly CultureInfo _originalUICultureInfo;

        public ThreadCultureChange()
        {
            _originalCultureInfo = CultureInfo.CurrentCulture;
            _originalUICultureInfo = CultureInfo.CurrentUICulture;
        }

        public void ChangeCultureInfo(string culture)
        {
            var newCulture = new CultureInfo(culture);
            CultureInfo.CurrentCulture = newCulture;
            CultureInfo.CurrentUICulture = newCulture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCultureInfo;
            CultureInfo.CurrentUICulture = _originalUICultureInfo;
        }
    }
}
