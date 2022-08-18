// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization.DataContracts
{
    internal sealed class GenericParameterDataContract : DataContract
    {
        private readonly GenericParameterDataContractCriticalHelper _helper;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal GenericParameterDataContract(Type type)
            : base(new GenericParameterDataContractCriticalHelper(type))
        {
            _helper = (base.Helper as GenericParameterDataContractCriticalHelper)!;
        }

        internal int ParameterPosition => _helper.ParameterPosition;

        public override bool IsBuiltInDataContract => true;

        private sealed class GenericParameterDataContractCriticalHelper : DataContract.DataContractCriticalHelper
        {
            private readonly int _parameterPosition;

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            internal GenericParameterDataContractCriticalHelper(
                [DynamicallyAccessedMembers(ClassDataContract.DataContractPreserveMemberTypes)]
                Type type)
                : base(type)
            {
                SetDataContractName(DataContract.GetXmlName(type));
                _parameterPosition = type.GenericParameterPosition;
            }

            internal int ParameterPosition => _parameterPosition;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal override DataContract BindGenericParameters(DataContract[] paramContracts, Dictionary<DataContract, DataContract>? boundContracts = null)
        {
            return paramContracts[ParameterPosition];
        }
    }
}
