// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ReplicationCursor
    {
        private readonly string? _serverDN;
        private readonly DateTime _syncTime;
        private readonly bool _advanced;
        private string? _sourceServer;

        private readonly DirectoryServer _server = null!;

        private ReplicationCursor() { }

        internal ReplicationCursor(DirectoryServer server, string partition, Guid guid, long filter, long time, IntPtr dn)
        {
            PartitionName = partition;
            SourceInvocationId = guid;
            UpToDatenessUsn = filter;

            // convert filetime to DateTime
            _syncTime = DateTime.FromFileTime(time);

            // get the dn
            _serverDN = Marshal.PtrToStringUni(dn);

            _advanced = true;

            _server = server;
        }

        internal ReplicationCursor(DirectoryServer server, string partition, Guid guid, long filter)
        {
            PartitionName = partition;
            SourceInvocationId = guid;
            UpToDatenessUsn = filter;

            _server = server;
        }

        public string PartitionName { get; } = null!;

        public Guid SourceInvocationId { get; }

        public long UpToDatenessUsn { get; }

        public string? SourceServer
        {
            get
            {
                // get the source server name if we are on win2k, or above win2k and serverDN is not NULL (means KCC translation is successful)
                if (!_advanced || (_advanced && _serverDN != null))
                {
                    _sourceServer = Utils.GetServerNameFromInvocationID(_serverDN, SourceInvocationId, _server);
                }

                return _sourceServer;
            }
        }

        public DateTime LastSuccessfulSyncTime
        {
            get
            {
                if (_advanced)
                    return _syncTime;
                else
                {
                    // win2k client machine does not support this
                    if ((Environment.OSVersion.Version.Major == 5) && (Environment.OSVersion.Version.Minor == 0))
                        throw new PlatformNotSupportedException(SR.DSNotSupportOnClient);
                    else
                        throw new PlatformNotSupportedException(SR.DSNotSupportOnDC);
                }
            }
        }
    }
}
