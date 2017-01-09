// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Diagnostics;

    public class ReplicationOperationInformation
    {
        internal DateTime startTime;
        internal ReplicationOperation currentOp = null;
        internal ReplicationOperationCollection collection = null;

        public ReplicationOperationInformation()
        {
        }

        public DateTime OperationStartTime
        {
            get
            {
                return startTime;
            }
        }

        public ReplicationOperation CurrentOperation
        {
            get
            {
                return currentOp;
            }
        }

        public ReplicationOperationCollection PendingOperations
        {
            get
            {
                return collection;
            }
        }
    }
}
