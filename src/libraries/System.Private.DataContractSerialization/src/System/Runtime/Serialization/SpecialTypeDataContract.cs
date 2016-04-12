// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Security;

namespace System.Runtime.Serialization
{
    internal sealed class SpecialTypeDataContract : DataContract
    {
        [SecurityCritical]
        /// <SecurityNote>
        /// Critical - holds instance of CriticalHelper which keeps state that is cached statically for serialization. 
        ///            Static fields are marked SecurityCritical or readonly to prevent
        ///            data from being modified or leaked to other components in appdomain.
        /// </SecurityNote>
        private SpecialTypeDataContractCriticalHelper _helper;
        /// <SecurityNote>
        /// Critical - initializes SecurityCritical field 'helper'
        /// Safe - doesn't leak anything
        /// </SecurityNote>
        [SecuritySafeCritical]
        public SpecialTypeDataContract(Type type, XmlDictionaryString name, XmlDictionaryString ns) : base(new SpecialTypeDataContractCriticalHelper(type, name, ns))
        {
            _helper = base.Helper as SpecialTypeDataContractCriticalHelper;
        }

        public override bool IsBuiltInDataContract
        {
            get
            {
                return true;
            }
        }
        [SecurityCritical]

        /// <SecurityNote>
        /// Critical - holds all state used for (de)serializing known types like System.Enum, System.ValueType, etc.
        ///            since the data is cached statically, we lock down access to it.
        /// </SecurityNote>
        private class SpecialTypeDataContractCriticalHelper : DataContract.DataContractCriticalHelper
        {
            internal SpecialTypeDataContractCriticalHelper(Type type, XmlDictionaryString name, XmlDictionaryString ns) : base(type)
            {
                SetDataContractName(name, ns);
            }
        }
    }
}

