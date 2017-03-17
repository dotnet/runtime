// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.Reflection;

namespace System.Runtime.Serialization
{
#if USE_REFEMIT || uapaot
    public sealed class KnownTypeDataContractResolver : DataContractResolver
#else
    internal sealed class KnownTypeDataContractResolver : DataContractResolver
#endif
    {
        private XmlObjectSerializerContext _context;

        internal KnownTypeDataContractResolver(XmlObjectSerializerContext context)
        {
            Fx.Assert(context != null, "KnownTypeDataContractResolver should not be instantiated with a null context");
            _context = context;
        }

        public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            if (type == null)
            {
                typeName = null;
                typeNamespace = null;
                return false;
            }
            if (declaredType != null && declaredType.IsInterface && CollectionDataContract.IsCollectionInterface(declaredType))
            {
                typeName = null;
                typeNamespace = null;
                return true;
            }

            DataContract contract = DataContract.GetDataContract(type);
            if (_context.IsKnownType(contract, contract.KnownDataContracts, declaredType))
            {
                typeName = contract.Name;
                typeNamespace = contract.Namespace;
                return true;
            }
            else
            {
                typeName = null;
                typeNamespace = null;
                return false;
            }
        }

        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
        {
            if (typeName == null || typeNamespace == null)
                return null;
            return _context.ResolveNameFromKnownTypes(new XmlQualifiedName(typeName, typeNamespace));
        }
    }
}
