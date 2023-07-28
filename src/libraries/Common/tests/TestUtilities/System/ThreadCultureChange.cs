// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.Tests
{
    public sealed class ThreadCultureChange : IDisposable
    {
        private readonly CultureInfo _origCulture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _origUICulture = CultureInfo.CurrentUICulture;

        public ThreadCultureChange(string? cultureName) :
            this(cultureName != null ? new CultureInfo(cultureName) : null)
        {
        }

        public ThreadCultureChange(CultureInfo? newCulture) :
            this(newCulture, null)
        {
        }

        public ThreadCultureChange(CultureInfo? newCulture, CultureInfo? newUICulture)
        {
            if (newCulture != null)
            {
                _origCulture = CultureInfo.CurrentCulture;
                CultureInfo.CurrentCulture = newCulture;
            }

            if (newUICulture != null)
            {
                _origUICulture = CultureInfo.CurrentUICulture;
                CultureInfo.CurrentUICulture = newUICulture;
            }
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _origCulture;
            CultureInfo.CurrentUICulture = _origUICulture;
        }
    }
}
