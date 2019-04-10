// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System
{
#if CORERT
    public // Needs to be public so that Reflection.Core can see it.
#else
    internal
#endif
    sealed class Empty
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
