// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ReplicationFailure
    {
        private readonly string? _sourceDsaDN;
        internal int lastResult;

        private readonly DirectoryServer _server;
        private string? _sourceServer;
        private readonly Hashtable _nameTable;

        internal ReplicationFailure(IntPtr addr, DirectoryServer server, Hashtable table)
        {
            DS_REPL_KCC_DSA_FAILURE failure = new DS_REPL_KCC_DSA_FAILURE();
            Marshal.PtrToStructure(addr, failure);

            _sourceDsaDN = Marshal.PtrToStringUni(failure.pszDsaDN);
            SourceServerGuid = failure.uuidDsaObjGuid;
            FirstFailureTime = DateTime.FromFileTime(failure.ftimeFirstFailure);
            ConsecutiveFailureCount = failure.cNumFailures;
            lastResult = failure.dwLastResult;

            _server = server;
            _nameTable = table;
        }

        public string? SourceServer
        {
            get
            {
                if (_sourceServer == null)
                {
                    // check whether we have got it before
                    if (_nameTable.Contains(SourceServerGuid))
                    {
                        _sourceServer = (string)_nameTable[SourceServerGuid]!;
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

        private Guid SourceServerGuid { get; }

        public DateTime FirstFailureTime { get; }

        public int ConsecutiveFailureCount { get; }

        public int LastErrorCode => lastResult;

        public string LastErrorMessage => ExceptionHelper.GetErrorMessage(LastErrorCode, false);
    }
}
