// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// The metadata for a specific Opcode defined by a Provider.
    /// An instance of this class is obtained from a ProviderMetadata object.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Opcode", Justification = "matell: Shipped public in 3.5, breaking change to fix now.")]
    public sealed class EventOpcode
    {
        private int _value;
        private string _name;
        private string _displayName;
        private bool _dataReady;
        private ProviderMetadata _pmReference;
        private object _syncObject;

        internal EventOpcode(int value, ProviderMetadata pmReference)
        {
            _value = value;
            _pmReference = pmReference;
            _syncObject = new object();
        }

        internal EventOpcode(string name, int value, string displayName)
        {
            _value = value;
            _name = name;
            _displayName = displayName;
            _dataReady = true;
            _syncObject = new object();
        }

        internal void PrepareData()
        {
            lock (_syncObject)
            {
                if (_dataReady == true)
                    return;

                // Get the data
                IEnumerable<EventOpcode> result = _pmReference.Opcodes;
                // Set the names and display names to null
                _name = null;
                _displayName = null;
                _dataReady = true;
                foreach (EventOpcode op in result)
                {
                    if (op.Value == _value)
                    {
                        _name = op.Name;
                        _displayName = op.DisplayName;
                        _dataReady = true;
                        break;
                    }
                }
            }
        } // End Prepare Data

        public string Name
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
