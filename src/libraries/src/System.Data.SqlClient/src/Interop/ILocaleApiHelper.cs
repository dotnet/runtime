// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Data
{
    internal interface ILocaleApiHelper
    {
        string LcidToLocaleNameInternal(int lcid);
        int LocaleNameToAnsiCodePage(string localeName);
        int GetLcidForLocaleName(string localeName);
    }
}