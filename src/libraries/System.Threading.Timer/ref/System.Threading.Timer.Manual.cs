// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


using System.Runtime.InteropServices;

namespace System.Threading
{
    // The ComVisible attributes are here to ensure there is no delta when rebuilding
    // the contract vs. the manual source code that was originally used for this contract.
    // The attributes are not actually meaningful since we don't emit ComVisible(false) at
    // the assembly level in contracts.
    [ComVisible(true)]
    public partial class Timer { }

    // This is providedInSource as it's the only way to add an attribute to a delegate.
    [ComVisible(true)]
    public delegate void TimerCallback(object state);
}
