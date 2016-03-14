// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

/*
 This class has the HostProtectionAttribute. The purpose of this attribute is to enforce host-specific programming model guidelines, not security behavior. 
 Suppress FxCop message - BUT REVISIT IF ADDING NEW SECURITY ATTRIBUTES.
*/
[assembly: SuppressMessage("Microsoft.Security", "CA2112:SecuredTypesShouldNotExposeFields", Scope = "type", Target = "System.ComponentModel.AttributeCollection")]
[assembly: SuppressMessage("Microsoft.Security", "CA2112:SecuredTypesShouldNotExposeFields", Scope = "type", Target = "System.ComponentModel.AttributeCollection")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.AttributeCollection.CopyTo(System.Array,System.Int32):System.Void")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.AttributeCollection.System.Collections.IEnumerable.GetEnumerator():System.Collections.IEnumerator")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.AttributeCollection.System.Collections.ICollection.get_IsSynchronized():System.Boolean")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.AttributeCollection.System.Collections.ICollection.get_Count():System.Int32")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.AttributeCollection.System.Collections.ICollection.get_SyncRoot():System.Object")]
[assembly: SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member", Target = "System.ComponentModel.AttributeCollection.Contains(System.Attribute):System.Boolean")]
[assembly: SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member", Target = "System.ComponentModel.AttributeCollection.CopyTo(System.Array,System.Int32):System.Void")]
[assembly: SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member", Target = "System.ComponentModel.AttributeCollection..ctor(System.Attribute[])")]
[assembly: SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member", Target = "System.ComponentModel.AttributeCollection.GetEnumerator():System.Collections.IEnumerator")]
[assembly: SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member", Target = "System.ComponentModel.AttributeCollection.get_Item(System.Type):System.Attribute")]
[assembly: SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member", Target = "System.ComponentModel.AttributeCollection.get_Item(System.Int32):System.Attribute")]
[assembly: SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member", Target = "System.ComponentModel.AttributeCollection.get_Count():System.Int32")]

namespace System.ComponentModel
{
    using System.Reflection;
    using System.Diagnostics;
    using System.Collections;


    /// <devdoc>
    ///     Represents a collection of attributes.
    /// </devdoc>
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    [System.Security.Permissions.HostProtection(Synchronization = true)]
    public class AttributeCollection : ICollection
    {
        /// <devdoc>
        ///     An empty AttributeCollection that can used instead of creating a new one.
        /// </devdoc>
        public static readonly AttributeCollection Empty = new AttributeCollection((Attribute[])null);
        private static Hashtable s_defaultAttributes;

        private Attribute[] _attributes;

        private static object s_internalSyncObject = new object();

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
            if (attributes == null)
            {
                attributes = new Attribute[0];
            }
            _attributes = attributes;

            for (int idx = 0; idx < attributes.Length; idx++)
            {
                if (attributes[idx] == null)
                {
                    throw new ArgumentNullException("attributes");
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
                throw new ArgumentNullException("existing");
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
                    throw new ArgumentNullException("newAttributes");
                }

                // We must see if this attribute is already in the existing
                // array.  If it is, we replace it.
                bool match = false;
                for (int existingIdx = 0; existingIdx < existing.Count; existingIdx++)
                {
                    if (newArray[existingIdx].TypeId.Equals(newAttributes[idx].TypeId))
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
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "Matches constructor input type")]
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
                        if (attributeType.IsAssignableFrom(aType))
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
        ///     Returns the default value for an attribute.  This uses the following hurestic:
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

                // If we have already encountered this, use what's in the
                // table.
                if (s_defaultAttributes.ContainsKey(attributeType))
                {
                    return (Attribute)s_defaultAttributes[attributeType];
                }

                Attribute attr = null;

                // Nope, not in the table, so do the legwork to discover the default value.
                Type reflect = TypeDescriptor.GetReflectionType(attributeType);
                System.Reflection.FieldInfo field = reflect.GetField("Default", BindingFlags.Public | BindingFlags.Static | BindingFlags.GetField);
                if (field != null && field.IsStatic)
                {
                    attr = (Attribute)field.GetValue(null);
                }
                else
                {
                    ConstructorInfo ci = reflect.UnderlyingSystemType.GetConstructor(new Type[0]);
                    if (ci != null)
                    {
                        attr = (Attribute)ci.Invoke(new object[0]);

                        // If we successfully created, verify that it is the
                        // default.  Attributes don't have to abide by this rule.
                        if (!attr.IsDefaultAttribute())
                        {
                            attr = null;
                        }
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
                if (Attributes[i].Match(attribute))
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

