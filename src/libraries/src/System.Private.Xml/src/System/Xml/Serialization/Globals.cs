// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Reflection;
using System;


namespace System.Xml.Serialization
{
    internal static class Globals
    {
        internal static Exception NotSupported(string msg)
        {
            System.Diagnostics.Debug.Assert(false, msg);
            return new NotSupportedException(msg);
        }
    }
}
