// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Diagnostics;

    public class ReplicationFailure
    {
        private string _sourceDsaDN;
        private Guid _uuidDsaObjGuid;
        private DateTime _timeFirstFailure;
        private int _numFailures;
        internal int lastResult;

        private DirectoryServer _server = null;
        private string _sourceServer = null;
        private Hashtable _nameTable = null;

        internal ReplicationFailure(IntPtr addr, DirectoryServer server, Hashtable table)
        {
            DS_REPL_KCC_DSA_FAILURE failure = new DS_REPL_KCC_DSA_FAILURE();
            Marshal.PtrToStructure(addr, failure);

            _sourceDsaDN = Marshal.PtrToStringUni(failure.pszDsaDN);
            _uuidDsaObjGuid = failure.uuidDsaObjGuid;
            _timeFirstFailure = DateTime.FromFileTime(failure.ftimeFirstFailure);
            _numFailures = failure.cNumFailures;
            lastResult = failure.dwLastResult;

            _server = server;
            _nameTable = table;
        }

        public string SourceServer
        {
            get
            {
                if (_sourceServer == null)
                {
                    // check whether we have got it before
                    if (_nameTable.Contains(SourceServerGuid))
                    {
                        _sourceServer = (string)_nameTable[SourceServerGuid];
                    }
                    else if (_sourceDsaDN != null)
                    {
                        _sourceServer = Utils.GetServerNameFromInvocationID(_sourceDsaDN, SourceServerGuid, _server);
                        // add it to the hashtable
                        _nameTable.Add(SourceServerGuid, _sourceServer);
                    }
                }

                return _sourceServer;
            }
        }

        private Guid SourceServerGuid
        {
            get
            {
                return _uuidDsaObjGuid;
            }
        }

        public DateTime FirstFailureTime
        {
            get
            {
                return _timeFirstFailure;
            }
        }

        public int ConsecutiveFailureCount
        {
            get
            {
                return _numFailures;
            }
        }

        public int LastErrorCode
        {
            get
            {
                return lastResult;
            }
        }

        public string LastErrorMessage
        {
            get
            {
                return ExceptionHelper.GetErrorMessage(lastResult, false);
            }
        }
    }
}
