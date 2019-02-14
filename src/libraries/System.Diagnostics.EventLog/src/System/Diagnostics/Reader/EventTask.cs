// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Describes the metadata for a specific Task defined by a Provider.
    /// An instance of this class is obtained from a ProviderMetadata object.
    /// </summary>
    public sealed class EventTask
    {
        private string _name;
        private string _displayName;
        private Guid _guid;
        private bool _dataReady;
        private ProviderMetadata _pmReference;
        private object _syncObject;

        internal EventTask(int value, ProviderMetadata pmReference)
        {
            Value = value;
            _pmReference = pmReference;
            _syncObject = new object();
        }

        internal EventTask(string name, int value, string displayName, Guid guid)
        {
            Value = value;
            _name = name;
            _displayName = displayName;
            _guid = guid;
            _dataReady = true;
            _syncObject = new object();
        }

        internal void PrepareData()
        {
            lock (_syncObject)
            {
                if (_dataReady == true)
                    return;

                IEnumerable<EventTask> result = _pmReference.Tasks;

                _name = null;
                _displayName = null;
                _guid = Guid.Empty;
                _dataReady = true;

                foreach (EventTask task in result)
                {
                    if (task.Value == Value)
                    {
                        _name = task.Name;
                        _displayName = task.DisplayName;
                        _guid = task.EventGuid;
                        _dataReady = true;
                        break;
                    }
                }
            }
        }

        public string Name
        {
            get
            {
                PrepareData();
                return _name;
            }
        }

        public int Value { get; }

        public string DisplayName
        {
            get
            {
                PrepareData();
                return _displayName;
            }
        }

        public Guid EventGuid
        {
            get
            {
                PrepareData();
                return _guid;
            }
        }
    }
}
