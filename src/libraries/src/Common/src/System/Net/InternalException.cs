// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    internal sealed class InternalException : Exception
    {
        private readonly object _unexpectedValue;

        internal InternalException()
        {
            NetEventSource.Fail(this, "InternalException thrown.");
        }

        internal InternalException(object unexpectedValue)
        {
            _unexpectedValue = unexpectedValue;
            if (NetEventSource.IsEnabled)
            {
                NetEventSource.Fail(this, $"InternalException thrown for unexpected value: {unexpectedValue}");
            }
        }

        public override string Message => _unexpectedValue != null ?
            base.Message + " " + _unexpectedValue :
            base.Message;
    }
}
