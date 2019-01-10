// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Describes the metadata for a specific Log Reference defined
    /// by a Provider. An instance of this class is obtained from
    /// a ProviderMetadata object.
    /// </summary>
    public sealed class EventLogLink
    {
        private string _channelName;
        private bool _isImported;
        private string _displayName;
        private bool _dataReady;
        private ProviderMetadata _pmReference;
        private object _syncObject;

        internal EventLogLink(uint channelId, ProviderMetadata pmReference)
        {
            ChannelId = channelId;
            _pmReference = pmReference;
            _syncObject = new object();
        }

        internal EventLogLink(string channelName, bool isImported, string displayName, uint channelId)
        {
            _channelName = channelName;
            _isImported = isImported;
            _displayName = displayName;
            ChannelId = channelId;

            _dataReady = true;
            _syncObject = new object();
        }

        private void PrepareData()
        {
            if (_dataReady == true)
                return;

            lock (_syncObject)
            {
                if (_dataReady == true)
                    return;

                IEnumerable<EventLogLink> result = _pmReference.LogLinks;

                _channelName = null;
                _isImported = false;
                _displayName = null;
                _dataReady = true;

                foreach (EventLogLink ch in result)
                {
                    if (ch.ChannelId == ChannelId)
                    {
                        _channelName = ch.LogName;
                        _isImported = ch.IsImported;
                        _displayName = ch.DisplayName;

                        _dataReady = true;

                        break;
                    }
                }
            }
        }

        public string LogName
        {
            get
            {
                this.PrepareData();
                return _channelName;
            }
        }

        public bool IsImported
        {
            get
            {
                this.PrepareData();
                return _isImported;
            }
        }

        public string DisplayName
        {
            get
            {
                this.PrepareData();
                return _displayName;
            }
        }

        internal uint ChannelId { get; }
    }
}
