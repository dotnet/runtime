// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Ports
{
    public class SerialErrorReceivedEventArgs : EventArgs
    {
        internal SerialErrorReceivedEventArgs(SerialError eventCode)
        {
            EventType = eventCode;
        }

        public SerialError EventType { get; private set; }
    }
}
