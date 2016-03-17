// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Collections;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2112:SecuredTypesShouldNotExposeFields", Scope = "type", Target = "System.ComponentModel.AttributeCollection")]

namespace System.ComponentModel
{
    /// <devdoc>
    ///     Represents a collection of attributes.
    /// </devdoc>
    public class AttributeCollection : ICollection
    {
        /// <devdoc>
        ///     An empty AttributeCollection that can used instead of creating a new one.
        /// </devdoc>
        public static readonly AttributeCollection Empty = new AttributeCollection(null);
        private static Hashtable s_defaultAttributes;

        private Attribute[] _attributes;

        private static readonly object s_internalSyncObject = new object();

        private struct AttributeEntry
        {
            public Type type;
            public int index;
        }

        private const int FOUND_TYPES_LIMIT = 5;

        private AttributeEntry[] _foundAttributeTypes;

        private int _index = 0;

        /// <devdoc>
        ///     Creates a new AttributeCollection.
        /// </devdoc>
        public AttributeCollection(params Attribute[] attributes)
        {
            _attributes = attributes ?? new Attribute[0];

            for (int idx = 0; idx < _attributes.Length; idx++)
            {
                if (_attributes[idx] == null)
                {
                    throw new ArgumentNullException(nameof(attributes));
                }
            }
        }

        protected AttributeCollection()
        {
        }

        /// <devdoc>
        ///     Creates a new AttributeCollection from an existing AttributeCollection
        /// </devdoc>
        public static AttributeCollection FromExisting(AttributeCollection existing, params Attribute[] newAttributes)
        {
            // VSWhidbey #75418
            // This method should be a constructor, but making it one introduces a breaking change.
            // 
            if (existing == null)
            {
                throw new ArgumentNullException(nameof(existing));
            }

            if (newAttributes == null)
            {
                newAttributes = new Attribute[0];
            }

            Attribute[] newArray = new Attribute[existing.Count + newAttributes.Length];
            int actualCount = existing.Count;
            existing.CopyTo(newArray, 0);

            for (int idx = 0; idx < newAttributes.Length; idx++)
            {
                if (newAttributes[idx] == null)
                {
                    throw new ArgumentNullException(nameof(newAttributes));
                }

                // We must see if this attribute is already in the existing
                // array.  If it is, we replace it.
                bool match = false;
                for (int existingIdx = 0; existingIdx < existing.Count; existingIdx++)
                {
#if FEATURE_ATTRIBUTE_TYPEID
                    if (newArray[existingIdx].TypeId.Equals(newAttributes[idx].TypeId))
#else
                    if (newArray[existingIdx].GetType().Equals(newAttributes[idx].GetType()))
#endif
                    {
                        match = true;
                        newArray[existingIdx] = newAttributes[idx];
                        break;
                    }
                }

                if (!match)
                {
                    newArray[actualCount++] = newAttributes[idx];
                }
            }

            // Now, if we collapsed some attributes, create a new array.
            //

            Attribute[] attributes = null;
            if (actualCount < newArray.Length)
            {
                attributes = new Attribute[actualCount];
                Array.Copy(newArray, 0, attributes, 0, actualCount);
            }
            else
            {
                attributes = newArray;
            }

            return new AttributeCollection(attributes);
        }

        /// <devdoc>
        ///     Gets the attributes collection.
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Matches constructor input type")]
        protected virtual Attribute[] Attributes
        {
            get
            {
                return _attributes;
            }
        }

        /// <devdoc>
        ///     Gets the number of attributes.
        /// </devdoc>
        public int Count
        {
            get
            {
                return Attributes.Length;
            }
        }

        /// <devdoc>
        ///     Gets the attribute with the specified index number.
        /// </devdoc>
        public virtual Attribute this[int index]
        {
            get
            {
                return Attributes[index];
            }
        }

        /// <devdoc>
        ///    Gets the attribute with the specified type.
        /// </devdoc>
        public virtual Attribute this[Type attributeType]
        {
            get
            {
                lock (s_internalSyncObject)
                {
                    // 2 passes here for perf.  Really!  first pass, we just 
                    // check equality, and if we don't find it, then we
                    // do the IsAssignableFrom dance.   Turns out that's
                    // a relatively expensive call and we try to avoid it
                    // since we rarely encounter derived attribute types
                    // and this list is usually short. 
                    //
                    if (_foundAttributeTypes == null)
                    {
                        _foundAttributeTypes = new AttributeEntry[FOUND_TYPES_LIMIT];
                    }

                    int ind = 0;

                    for (; ind < FOUND_TYPES_LIMIT; ind++)
                    {
                        if (_foundAttributeTypes[ind].type == attributeType)
                        {
                            int index = _foundAttributeTypes[ind].index;
                            if (index != -1)
                            {
                                return Attributes[index];
                            }
                            else
                            {
                                return GetDefaultAttribute(attributeType);
                            }
                        }
                        if (_foundAttributeTypes[ind].type == null)
                            break;
                    }

                    ind = _index++;

                    if (_index >= FOUND_TYPES_LIMIT)
                    {
                        _index = 0;
                    }

                    _foundAttributeTypes[ind].type = attributeType;

                    int count = Attributes.Length;


                    for (int i = 0; i < count; i++)
                    {
                        Attribute attribute = Attributes[i];
                        Type aType = attribute.GetType();
                        if (aType == attributeType)
                        {
                            _foundAttributeTypes[ind].index = i;
                            return attribute;
                        }
                    }

                    // now check the hierarchies.
                    for (int i = 0; i < count; i++)
                    {
                        Attribute attribute = Attributes[i];
                        Type aType = attribute.GetType();
                        if (attributeType.GetTypeInfo().IsAssignableFrom(aType.GetTypeInfo()))
                        {
                            _foundAttributeTypes[ind].index = i;
                            return attribute;
                        }
                    }

                    _foundAttributeTypes[ind].index = -1;
                    return GetDefaultAttribute(attributeType);
                }
            }
        }

        /// <devdoc>
        ///     Determines if this collection of attributes has the specified attribute.
        /// </devdoc>
        public bool Contains(Attribute attribute)
        {
            Attribute attr = this[attribute.GetType()];
            if (attr != null && attr.Equals(attribute))
            {
                return true;
            }
            return false;
        }

        /// <devdoc>
        ///     Determines if this attribute collection contains the all
        ///     the specified attributes in the attribute array.
        /// </devdoc>
        public bool Contains(Attribute[] attributes)
        {
            if (attributes == null)
            {
                return true;
            }

            for (int i = 0; i < attributes.Length; i++)
            {
                if (!Contains(attributes[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <devdoc>
        ///     Returns the default value for an attribute.  This uses the following heuristic:
        ///     1.  It looks for a public static field named "Default".
        /// </devdoc>
        protected Attribute GetDefaultAttribute(Type attributeType)
        {
            lock (s_internalSyncObject)
            {
                if (s_defaultAttributes == null)
                {
                    s_defaultAttributes = new Hashtable();
                }

                // If we have already encountered this, use what's in the table.
                if (s_defaultAttributes.ContainsKey(attributeType))
                {
                    return (Attribute)s_defaultAttributes[attributeType];
                }

                Attribute attr = null;

                // Not in the table, so do the legwork to discover the default value.
                Type reflect = TypeDescriptor.GetReflectionType(attributeType);
                FieldInfo field = reflect.GetTypeInfo().GetField("Default", BindingFlags.Public | BindingFlags.Static | BindingFlags.GetField);

                if (field != null && field.IsStatic)
                {
                    attr = (Attribute)field.GetValue(null);
                }
                else
                {
                    ConstructorInfo ci = reflect.GetTypeInfo().UnderlyingSystemType.GetTypeInfo().GetConstructor(new Type[0]);
                    if (ci != null)
                    {
                        attr = (Attribute)ci.Invoke(new object[0]);

#if FEATURE_ATTRIBUTE_ISDEFAULTATTRIBUTE
                        // If we successfully created, verify that it is the
                        // default.  Attributes don't have to abide by this rule.
                        if (!attr.IsDefaultAttribute())
                        {
                            attr = null;
                        }
#endif
                    }
                }

                s_defaultAttributes[attributeType] = attr;
                return attr;
            }
        }

        /// <devdoc>
        ///     Gets an enumerator for this collection.
        /// </devdoc>
        public IEnumerator GetEnumerator()
        {
            return Attributes.GetEnumerator();
        }

        /// <devdoc>
        ///     Determines if a specified attribute is the same as an attribute
        ///     in the collection.
        /// </devdoc>
        public bool Matches(Attribute attribute)
        {
            for (int i = 0; i < Attributes.Length; i++)
            {
#if FEATURE_ATTRIBUTE_MATCH
                if (Attributes[i].Match(attribute))
#else
                if (Attributes[i].Equals(attribute))
#endif
                {
                    return true;
                }
            }
            return false;
        }

        /// <devdoc>
        ///     Determines if the attributes in the specified array are
        ///     the same as the attributes in the collection.
        /// </devdoc>
        public bool Matches(Attribute[] attributes)
        {
            for (int i = 0; i < attributes.Length; i++)
            {
                if (!Matches(attributes[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <internalonly/>
        int ICollection.Count
        {
            get
            {
                return Count;
            }
        }

        /// <internalonly/>
        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        /// <internalonly/>
        object ICollection.SyncRoot
        {
            get
            {
                return null;
            }
        }

        /// <devdoc>
        ///     Copies this collection to an array.
        /// </devdoc>
        public void CopyTo(Array array, int index)
        {
            Array.Copy(Attributes, 0, array, index, Attributes.Length);
        }

        /// <internalonly/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

