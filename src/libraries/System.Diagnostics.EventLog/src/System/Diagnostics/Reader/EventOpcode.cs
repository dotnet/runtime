// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// The metadata for a specific Opcode defined by a Provider.
    /// An instance of this class is obtained from a ProviderMetadata object.
    /// </summary>
    public sealed class EventOpcode
    {
        private readonly int _value;
        private string? _name;
        private string? _displayName;
        [MemberNotNullWhen(false, nameof(_pmReference))]
        private bool DataReady { get; set; }
        private readonly ProviderMetadata? _pmReference;
        private readonly object _syncObject;

        internal EventOpcode(int value, ProviderMetadata pmReference)
        {
            _value = value;
            _pmReference = pmReference;
            _syncObject = new object();
        }

        internal EventOpcode(string? name, int value, string? displayName)
        {
            _value = value;
            _name = name;
            _displayName = displayName;
            DataReady = true;
            _syncObject = new object();
        }

        internal void PrepareData()
        {
            lock (_syncObject)
            {
                if (DataReady)
                    return;

                // Get the data
                IEnumerable<EventOpcode> result = _pmReference.Opcodes;
                // Set the names and display names to null
                _name = null;
                _displayName = null;
                DataReady = true;
                foreach (EventOpcode op in result)
                {
                    if (op.Value == _value)
                    {
                        _name = op.Name;
                        _displayName = op.DisplayName;
                        DataReady = true;
                        break;
                    }
                }
            }
        } // End Prepare Data

        public string? Name
        {
            get
            {
                PrepareData();
                return _name;
            }
        }

        public int Value
        {
            get
            {
                return _value;
            }
        }

        public string? DisplayName
        {
            get
            {
                PrepareData();
                return _displayName;
            }
        }
    }
}
