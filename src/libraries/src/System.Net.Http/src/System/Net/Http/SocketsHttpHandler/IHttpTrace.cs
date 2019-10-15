// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Net.Http
{
    internal interface IHttpTrace
    {
        void Trace(string message, [CallerMemberName] string memberName = null);
    }
}
