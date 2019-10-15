// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization
{

    // InvalidDataContract is used to create a DataContract that will throw
    // an exception if used to serialize or deserialize.
    public sealed class InvalidDataContract : DataContract
    {
        public InvalidDataContract()
            : base(new InvalidDataContractCriticalHelper())
        {
        }

        public InvalidDataContract(Type type, string errorMessage)
            : base(new InvalidDataContractCriticalHelper(type))
        {
            ErrorMessage = errorMessage;
        }

        public string ErrorMessage { get; set; }

        public override void PrepareToRead(XmlReaderDelegator xmlReader)
        {
            throw CreateInvalidDataContractException();
        }

        public override object ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context)
        {
            throw CreateInvalidDataContractException();
        }

        public override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
        {
            throw CreateInvalidDataContractException();
        }

        private InvalidDataContractException CreateInvalidDataContractException()
        {
            return new InvalidDataContractException(ErrorMessage);
        }

        private class InvalidDataContractCriticalHelper : DataContract.DataContractCriticalHelper
        {
            private XmlDictionary _xmlDictionary;

            public InvalidDataContractCriticalHelper()
            {
            }

            public InvalidDataContractCriticalHelper(Type type)
                : base(type)
            {
                StableName = DataContract.GetStableName(type);
                _xmlDictionary = new XmlDictionary(2);
                this.Name = this.TopLevelElementName = _xmlDictionary.Add(StableName.Name);
                this.Namespace = this.TopLevelElementNamespace = _xmlDictionary.Add(StableName.Namespace);
            }
        }
    }
}
