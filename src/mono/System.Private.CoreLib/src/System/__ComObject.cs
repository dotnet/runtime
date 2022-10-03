// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1852 // __ComObject should not be sealed

namespace System
{
    internal class __ComObject
    {
        private __ComObject()
        {
            throw new NotSupportedException();
        }
    }
}
