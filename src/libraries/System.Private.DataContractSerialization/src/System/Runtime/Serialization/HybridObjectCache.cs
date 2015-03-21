// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------
//------------------------------------------------------------

using System;
using System.Xml;
using System.Collections.Generic;


namespace System.Runtime.Serialization
{
    internal class HybridObjectCache
    {
        private Dictionary<string, object> _objectDictionary;
        internal HybridObjectCache()
        {
        }

        internal void Add(string id, object obj)
        {
            if (_objectDictionary == null)
                _objectDictionary = new Dictionary<string, object>();

            object existingObject;
            if (_objectDictionary.TryGetValue(id, out existingObject))
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.MultipleIdDefinition, id)));
            _objectDictionary.Add(id, obj);
        }

        internal object GetObject(string id)
        {
            if (_objectDictionary != null)
            {
                object obj;
                _objectDictionary.TryGetValue(id, out obj);
                return obj;
            }

            return null;
        }
    }
}
