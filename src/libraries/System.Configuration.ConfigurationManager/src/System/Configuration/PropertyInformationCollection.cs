// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;
using System.Collections;
using System.Runtime.Serialization;

namespace System.Configuration
{
    public sealed class PropertyInformationCollection : NameObjectCollectionBase
    {
        internal PropertyInformationCollection(ConfigurationElement thisElement) : base(StringComparer.Ordinal)
        {
            ConfigurationElement thisElement1 = thisElement;
            foreach (ConfigurationProperty prop in thisElement1.Properties)
                if (prop.Name != thisElement1.ElementTagName)
                    BaseAdd(prop.Name, new PropertyInformation(thisElement, prop.Name));
            IsReadOnly = true;
        }

        public PropertyInformation this[string propertyName]
        {
            get
            {
                PropertyInformation result = (PropertyInformation)BaseGet(propertyName);

                // check for default collection name
                if (result == null)
                {
                    PropertyInformation defaultColl =
                        (PropertyInformation)BaseGet(ConfigurationProperty.s_defaultCollectionPropertyName);

                    if ((defaultColl != null) && (defaultColl.ProvidedName == propertyName)) result = defaultColl;
                }
                return result;
            }
        }

        internal PropertyInformation this[int index] => (PropertyInformation)BaseGet(BaseGetKey(index));


        public void CopyTo(PropertyInformation[] array, int index)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));

            if (array.Length < Count + index) throw new ArgumentOutOfRangeException(nameof(index));

            foreach (PropertyInformation pi in this) array[index++] = pi;
        }


        public override IEnumerator GetEnumerator()
        {
            int c = Count;
            for (int i = 0; i < c; i++) yield return this[i];
        }
    }
}
