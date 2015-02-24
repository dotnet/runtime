// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;

namespace System.Diagnostics
{
    // the one currently supported with the csee.dat 
    // (mcee.dat, autoexp.dat) file. 
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class DebuggerBrowsableAttribute : Attribute
    {
        private readonly DebuggerBrowsableState _state;
        public DebuggerBrowsableAttribute(DebuggerBrowsableState state)
        {
            if (state < DebuggerBrowsableState.Never || state > DebuggerBrowsableState.RootHidden)
                throw new ArgumentOutOfRangeException("state");
            Contract.EndContractBlock();

            this._state = state;
        }
        public DebuggerBrowsableState State
        {
            get { return _state; }
        }
    }
}
