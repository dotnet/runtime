// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Describes the metadata for a specific Level defined by a Provider.
    /// An instance of this class is obtained from a ProviderMetadata object.
    /// </summary>
    public sealed class EventLevel
    {
        private string _name;
        private string _displayName;
        private bool _dataReady;
        private readonly ProviderMetadata _pmReference;
        private readonly object _syncObject;

        internal EventLevel(int value, ProviderMetadata pmReference)
        {
            Value = value;
            _pmReference = pmReference;
            _syncObject = new object();
        }

        internal EventLevel(string name, int value, string displayName)
        {
            Value = value;
            _name = name;
            _displayName = displayName;
            _dataReady = true;
            _syncObject = new object();
        }

        internal void PrepareData()
        {
            if (_dataReady)
                return;

            lock (_syncObject)
            {
                if (_dataReady)
                    return;

                IEnumerable<EventLevel> result = _pmReference.Levels;
                _name = null;
                _displayName = null;
                _dataReady = true;
                foreach (EventLevel lev in result)
                {
                    if (lev.Value == Value)
                    {
                        _name = lev.Name;
                        _displayName = lev.DisplayName;
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
    }
}
