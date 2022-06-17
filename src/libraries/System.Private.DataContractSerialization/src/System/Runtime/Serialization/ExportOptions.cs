// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace System.Runtime.Serialization
{
    public class ExportOptions
    {
        private Collection<Type>? _knownTypes;
#if smolloy_add_ext_surrogate
        // NOTE TODO smolloy WARNING!!!! - This is modifying (adding to) a public API. Would have been great if we could have sealed this class.
        private ISerializationExtendedSurrogateProvider? _serializationExtendedSurrogateProvider;

        public ISerializationExtendedSurrogateProvider? SerializationExtendedSurrogateProvider
        {
            get { return _serializationExtendedSurrogateProvider; }
            set { _serializationExtendedSurrogateProvider = value; }
        }

        internal ISerializationExtendedSurrogateProvider? GetSurrogate()
        {
            return _serializationExtendedSurrogateProvider;
        }
#endif

        public Collection<Type> KnownTypes
        {
            get
            {
                if (_knownTypes == null)
                {
                    _knownTypes = new Collection<Type>();
                }
                return _knownTypes;
            }
        }
    }
}
