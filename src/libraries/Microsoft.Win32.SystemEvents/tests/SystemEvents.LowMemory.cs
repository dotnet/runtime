// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using static Interop;

namespace Microsoft.Win32.SystemEventsTests
{
    public class LowMemoryTests : GenericEventTests
    {
        protected override int MessageId => User32.WM_COMPACTING;

        protected override event EventHandler Event
        {
#pragma warning disable CS0618 // Type or member is obsolete
            add
            {
                SystemEvents.LowMemory += value;
            }
            remove
            {
                SystemEvents.LowMemory -= value;
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
