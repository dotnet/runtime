// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Data.Odbc
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class OdbcError
    {
        //Data
        internal string _message;
        internal string _state;
        internal int _nativeerror;
        internal string? _source;

        internal OdbcError(string? source, string message, string state, int nativeerror)
        {
            _source = source;
            _message = message;
            _state = state;
            _nativeerror = nativeerror;
        }

        public string Message
        {
            get
            {
                return _message ?? string.Empty;
            }
        }

        public string SQLState
        {
            get
            {
                return _state;
            }
        }

        public int NativeError
        {
            get
            {
                return _nativeerror;
            }
        }

        public string Source
        {
            get
            {
                return _source ?? string.Empty;
            }
        }

        internal void SetSource(string Source)
        {
            _source = Source;
        }

        public override string ToString()
        {
            return Message;
        }
    }
}
