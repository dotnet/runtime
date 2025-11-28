// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace SharedTypes.ComInterfaces.MarshallingFails
{
    internal class MarshallingFailureException : Exception
    {
        public MarshallingFailureException() : base() { }
        public MarshallingFailureException(string message) : base(message) { }
    }
}
