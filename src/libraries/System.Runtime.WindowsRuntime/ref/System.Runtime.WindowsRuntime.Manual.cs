// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


using System;
using System.Security;

// Including the explicit implementations of IFormattable because if we use what
// the tool generates we will end up with a member name "global::System.IFormattable.ToString"
// which is different then what is in the implementation. We can avoid it in the manual
// file because of the usings at the top of this file instead of fully qualify with global::.

namespace Windows.Foundation
{
    public partial struct Point : IFormattable
    {
        [SecuritySafeCriticalAttribute]
        string IFormattable.ToString(string format, IFormatProvider provider) { return default(string); }
    }
    public partial struct Rect : IFormattable
    {
        [SecuritySafeCriticalAttribute]
        string IFormattable.ToString(string format, IFormatProvider provider) { return default(string); }
    }
}
namespace Windows.UI
{
    public partial struct Color : IFormattable
    {
        [SecuritySafeCriticalAttribute]
        string IFormattable.ToString(string format, IFormatProvider provider) { return default(string); }
    }
}
