// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
