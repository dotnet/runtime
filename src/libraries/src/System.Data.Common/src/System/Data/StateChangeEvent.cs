// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



//------------------------------------------------------------------------------

using System;

namespace System.Data
{
    public sealed class StateChangeEventArgs : System.EventArgs
    {
        private ConnectionState _originalState;
        private ConnectionState _currentState;

        public StateChangeEventArgs(ConnectionState originalState, ConnectionState currentState)
        {
            _originalState = originalState;
            _currentState = currentState;
        }

        public ConnectionState CurrentState
        {
            get
            {
                return _currentState;
            }
        }

        public ConnectionState OriginalState
        {
            get
            {
                return _originalState;
            }
        }
    }
}
