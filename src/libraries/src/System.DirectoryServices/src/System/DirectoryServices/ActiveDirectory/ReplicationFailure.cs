//------------------------------------------------------------------------------
// <copyright file="ReplicationFailure.cs" company="Microsoft">
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

    public class ReplicationFailure {
        string sourceDsaDN;  
        Guid uuidDsaObjGuid;  
        DateTime timeFirstFailure;  
        int numFailures;  
        internal int lastResult;

        private DirectoryServer server = null;
        private string sourceServer = null;
        private Hashtable nameTable = null;
        
        internal ReplicationFailure(IntPtr addr, DirectoryServer server, Hashtable table)
        {
            DS_REPL_KCC_DSA_FAILURE failure = new DS_REPL_KCC_DSA_FAILURE();
            Marshal.PtrToStructure(addr, failure);

            sourceDsaDN = Marshal.PtrToStringUni(failure.pszDsaDN);
            uuidDsaObjGuid = failure.uuidDsaObjGuid;
            timeFirstFailure = DateTime.FromFileTime(failure.ftimeFirstFailure);
            numFailures = failure.cNumFailures;
            lastResult = failure.dwLastResult;

            this.server = server;
            this.nameTable = table;
            
        }

        public string SourceServer {
            get {
                if(sourceServer == null)
                {
                    // check whether we have got it before
                    if(nameTable.Contains(SourceServerGuid))
                    {  
                        sourceServer = (string) nameTable[SourceServerGuid];
                    }
                    else if(sourceDsaDN != null)
                    {                        
                        sourceServer = Utils.GetServerNameFromInvocationID(sourceDsaDN, SourceServerGuid, server);
                        // add it to the hashtable
                        nameTable.Add(SourceServerGuid, sourceServer);
                    }
                }

                return sourceServer;
            }
        }

        private Guid SourceServerGuid {
            get {
                return uuidDsaObjGuid;
            }
        }

        public DateTime FirstFailureTime {
            get {
                return timeFirstFailure;
            }
        }

        public int ConsecutiveFailureCount {
            get {
                return numFailures;
            }
        }

        public int LastErrorCode {
            get {
                return lastResult;
            }
        }

        public string LastErrorMessage {
            get {
                return ExceptionHelper.GetErrorMessage(lastResult, false);
            }
        }
    }
}
