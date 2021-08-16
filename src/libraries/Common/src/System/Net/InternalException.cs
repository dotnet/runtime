// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net
{
    internal sealed class InternalException : Exception
    {
        private readonly object? _unexpectedValue;

        internal InternalException() : this(null) { }

        internal InternalException(object? unexpectedValue)
        {
            Debug.Fail($"InternalException thrown for unexpected value: {unexpectedValue}");
            _unexpectedValue = unexpectedValue;
        }

        public override string Message => _unexpectedValue != null ?
            base.Message + " " + _unexpectedValue :
            base.Message;
    }
}
