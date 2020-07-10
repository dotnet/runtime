// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.DirectoryServices.Protocols
{
    public abstract class DirectoryOperation
    {
        internal string _directoryRequestID;

        protected DirectoryOperation() { }
    }
}
