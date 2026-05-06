// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Ports
{
    public class SerialPinChangedEventArgs : EventArgs
    {
        internal SerialPinChangedEventArgs(SerialPinChange eventCode)
        {
            EventType = eventCode;
        }

        public SerialPinChange EventType { get; }
    }
}
