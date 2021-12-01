// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Globalization;

namespace System.DirectoryServices
{
    /// <devdoc>
    /// Contains the properties on a <see cref='System.DirectoryServices.SearchResult'/>.
    /// </devdoc>
    public class ResultPropertyCollection : DictionaryBase
    {
        internal ResultPropertyCollection()
        {
        }

        /// <devdoc>
        /// Gets the property with the given name.
        /// </devdoc>
        public ResultPropertyValueCollection this[string name]
        {
            get
            {
                object objectName = name.ToLowerInvariant();
                if (Contains((string)objectName))
                {
                    return (ResultPropertyValueCollection)InnerHashtable[objectName]!;
                }
                else
                {
                    return new ResultPropertyValueCollection(Array.Empty<object>());
                }
            }
        }

        public ICollection PropertyNames => Dictionary.Keys;

        public ICollection Values => Dictionary.Values;

        internal void Add(string name, ResultPropertyValueCollection value)
        {
            Dictionary.Add(name.ToLowerInvariant(), value);
        }

        public bool Contains(string propertyName)
        {
            object objectName = propertyName.ToLowerInvariant();
            return Dictionary.Contains(objectName);
        }

        public void CopyTo(ResultPropertyValueCollection[] array, int index)
        {
            Dictionary.Values.CopyTo((Array)array, index);
        }
    }
}
