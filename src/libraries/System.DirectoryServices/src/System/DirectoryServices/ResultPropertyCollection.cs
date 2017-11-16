// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                object objectName = name.ToLower(CultureInfo.InvariantCulture);
                if (Contains((string)objectName))
                {
                    return (ResultPropertyValueCollection)InnerHashtable[objectName];
                }
                else
                {
                    return new ResultPropertyValueCollection(new object[0]);
                }
            }
        }

        public ICollection PropertyNames => Dictionary.Keys;

        public ICollection Values => Dictionary.Values;

        internal void Add(string name, ResultPropertyValueCollection value)
        {
            Dictionary.Add(name.ToLower(CultureInfo.InvariantCulture), value);
        }

        public bool Contains(string propertyName)
        {
            object objectName = propertyName.ToLower(CultureInfo.InvariantCulture);
            return Dictionary.Contains(objectName);
        }

        public void CopyTo(ResultPropertyValueCollection[] array, int index)
        {
            Dictionary.Values.CopyTo((Array)array, index);
        }
    }
}
