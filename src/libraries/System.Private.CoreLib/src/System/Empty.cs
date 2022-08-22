// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    internal sealed class Empty
    {
        private Empty()
        {
        }

        public static readonly Empty Value = new Empty();

        public override string ToString()
        {
            return string.Empty;
        }
    }
}
