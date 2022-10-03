// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace System.Runtime.Serialization
{
    public class ExportOptions
    {
        private Collection<Type>? _knownTypes;
#if SUPPORT_SURROGATE
        private IDataContractSurrogate? _dataContractSurrogate;

        public IDataContractSurrogate? DataContractSurrogate
        {
            get { return _dataContractSurrogate; }
            set { _dataContractSurrogate = value; }
        }

        internal IDataContractSurrogate? GetSurrogate()
        {
            return _dataContractSurrogate;
        }
#endif

        public Collection<Type> KnownTypes => _knownTypes ??= new Collection<Type>();
    }
}
