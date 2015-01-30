// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.Contracts;
namespace System.Reflection 
{  
    using System;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public struct ParameterModifier 
    {
        #region Private Data Members
        private bool[] _byRef;
        #endregion

        #region Constructor
        public ParameterModifier(int parameterCount) 
        {
            if (parameterCount <= 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_ParmArraySize"));
            Contract.EndContractBlock();

            _byRef = new bool[parameterCount];
        }
        #endregion

        #region Internal Members
        internal bool[] IsByRefArray { get { return _byRef; } }
        #endregion

        #region Public Members
        public bool this[int index] 
        {
            get 
            {
                return _byRef[index]; 
            }
            set 
            {
                _byRef[index] = value;
            }
        }
        #endregion
    }
}
