// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Eventing.Reader
{
    public sealed class EventProperty
    {
        internal EventProperty(object value)
        {
            Value = value;
        }

        public object Value { get; }
    }
}
