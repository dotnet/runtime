// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security;

namespace System.Runtime.Serialization
{
    internal sealed class GenericParameterDataContract : DataContract
    {
        [SecurityCritical]
        private GenericParameterDataContractCriticalHelper _helper;

        [SecuritySafeCritical]
        internal GenericParameterDataContract(Type type)
            : base(new GenericParameterDataContractCriticalHelper(type))
        {
            _helper = base.Helper as GenericParameterDataContractCriticalHelper;
        }

        internal int ParameterPosition
        {
            [SecuritySafeCritical]
            get
            { return _helper.ParameterPosition; }
        }

        public override bool IsBuiltInDataContract
        {
            get
            {
                return true;
            }
        }

        private class GenericParameterDataContractCriticalHelper : DataContract.DataContractCriticalHelper
        {
            private int _parameterPosition;

            internal GenericParameterDataContractCriticalHelper(Type type)
                : base(type)
            {
                SetDataContractName(DataContract.GetStableName(type));
                _parameterPosition = type.GenericParameterPosition;
            }

            internal int ParameterPosition
            {
                get { return _parameterPosition; }
            }
        }

        internal DataContract BindGenericParameters(DataContract[] paramContracts, Dictionary<DataContract, DataContract> boundContracts)
        {
            return paramContracts[ParameterPosition];
        }
    }
}
