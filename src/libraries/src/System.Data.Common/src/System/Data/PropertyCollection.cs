// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Runtime.Serialization;

namespace System.Data
{
    /// <summary>
    /// Represents a collection of properties that can be added to <see cref='System.Data.DataColumn'/>, 
    /// <see cref='System.Data.DataSet'/>, or <see cref='System.Data.DataTable'/>.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class PropertyCollection : Hashtable, ICloneable
    {
        public PropertyCollection() : base()
        {
        }

        protected PropertyCollection(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override object Clone()
        {
            // override Clone so that returned object is an
            // instance of PropertyCollection instead of Hashtable
            PropertyCollection clone = new PropertyCollection();
            foreach (DictionaryEntry pair in this)
            {
                clone.Add(pair.Key, pair.Value);
            }
            return clone;
        }
    }
}
