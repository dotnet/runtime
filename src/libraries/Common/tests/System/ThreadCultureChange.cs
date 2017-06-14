// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.Common.Tests
{
    public sealed class ThreadCultureChange : IDisposable
    {
        private readonly CultureInfo _originalCultureInfo = CultureInfo.CurrentCulture;
        private readonly CultureInfo _originalUICultureInfo = CultureInfo.CurrentUICulture;

        public ThreadCultureChange() { }

        public ThreadCultureChange(string culture) : this()
        {
            ChangeCultureInfo(culture);
        }

        public ThreadCultureChange(CultureInfo newCulture) : this()
        {
            ChangeCultureInfo(newCulture);
        }

        public void ChangeCultureInfo(string culture) => ChangeCultureInfo(new CultureInfo(culture));

        public void ChangeCultureInfo(CultureInfo newCulture)
        {
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
