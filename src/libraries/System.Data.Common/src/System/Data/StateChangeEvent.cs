// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Data
{
    public sealed class StateChangeEventArgs : EventArgs
    {
        private readonly ConnectionState _originalState;
        private readonly ConnectionState _currentState;

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
