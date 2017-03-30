// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Xml;

    public abstract class DirectoryOperation
    {
        internal string directoryRequestID = null;

        protected DirectoryOperation() { }
    }
}
