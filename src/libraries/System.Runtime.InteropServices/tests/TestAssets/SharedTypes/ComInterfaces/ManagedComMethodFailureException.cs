// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Serialization;

namespace SharedTypes.ComInterfaces
{
    internal class ManagedComMethodFailureException : Exception
    {
        public ManagedComMethodFailureException()
        {
        }

        public ManagedComMethodFailureException(string? message) : base(message)
        {
        }
    }
}
