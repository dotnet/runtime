//------------------------------------------------------------------------------
// <copyright file="ReplicationOperationInformation.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

 namespace System.DirectoryServices.ActiveDirectory {
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Diagnostics;

    public class ReplicationOperationInformation{
        internal DateTime startTime;
        internal ReplicationOperation currentOp = null;
        internal ReplicationOperationCollection collection = null;

        public ReplicationOperationInformation()
        {            
        }

        public DateTime OperationStartTime {
            get {
                return startTime;
            }
        }

        public ReplicationOperation CurrentOperation {
            get {
                return currentOp;
            }
        }

        public ReplicationOperationCollection PendingOperations {
            get {
                return collection;
            }
        }
        
    }
}
