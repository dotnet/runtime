// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization.Json;

namespace System.Runtime.Serialization
{
    internal sealed class GenericParameterDataContract : DataContract
    {
        private readonly GenericParameterDataContractCriticalHelper _helper;

        [RequiresUnreferencedCode(DataContractJsonSerializer.SerializerTrimmerWarning)]
        internal GenericParameterDataContract(Type type)
            : base(new GenericParameterDataContractCriticalHelper(type))
        {
            _helper = (base.Helper as GenericParameterDataContractCriticalHelper)!;
        }

        internal int ParameterPosition
        {
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

        private sealed class GenericParameterDataContractCriticalHelper : DataContract.DataContractCriticalHelper
        {
            private readonly int _parameterPosition;

            [RequiresUnreferencedCode(DataContractJsonSerializer.SerializerTrimmerWarning)]
            internal GenericParameterDataContractCriticalHelper(
                [DynamicallyAccessedMembers(
                    DynamicallyAccessedMemberTypes.PublicConstructors |
                    DynamicallyAccessedMemberTypes.NonPublicConstructors |
                    DynamicallyAccessedMemberTypes.PublicMethods |
                    DynamicallyAccessedMemberTypes.NonPublicMethods)]
                Type type)
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
