// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

using System;
using System.Xml;
using System.Reflection;
using System.Collections;
using System.Diagnostics;

using Hashtable = System.Collections.InternalHashtable;

namespace System.Xml.Serialization.LegacyNetCF
{
    /// <summary>
    /// Base class for all Fixups. A Fixup object is responsible for taking a de-serialized 
    /// object and ensuring that it is handed back to the user in the correct object.
    /// </summary>
    internal abstract class Fixup
    {
        public Fixup() { }
        public abstract void fixup(object target, object value);
    }


    /// <summary>
    /// Simple class that represents a fixup that does nothing. 
    /// </summary>
    internal class NullFixup : Fixup
    {
        public NullFixup() { }
        public override void fixup(object ignored1, object ignored2) { }
    }

    /// <summary>
    /// This class represents a forward reference to an object in the document. The reference
    /// contains the id of the reference and the value it refers to. There is also a bool flag
    /// that tells whether the reference record has actually been reference somewhere in the 
    /// document. This flag is used to determine when a unreferenced object is left over in the
    /// document. 
    /// </summary>
    internal class ReferenceRecord
    {
        public ReferenceRecord(string id, object value)
        {
            this.id = id;
            this.value = value;
        }

        public string id;
        public object value;
        public bool referenced;
    }

    /// <summary>
    /// This Fixup handles assigning values that are pointed to by a forward reference. The  
    /// fixup contains a nested Fixup (maybe a MemberFixup or an argumentFixup) and a nested
    /// FixupTarget so that the de-serialized value can be assigned properly. The reference to 
    /// the reference ID and XmlSerializationReader are also kept. These are used to look up 
    /// the ReferenceRecord that contains the de-serailized value. 
    /// </summary>
    internal class ReferenceFixup : Fixup
    {
        private string _ID;
        private Fixup _nestedFixup;
        private object _nestedTarget;
        private XmlSerializationReader _reader;

        public ReferenceFixup(string id, Fixup nestedFixup, object nestedTarget, XmlSerializationReader reader)
        {
            _ID = id;
            _nestedFixup = nestedFixup;
            _nestedTarget = nestedTarget;
            _reader = reader;
        }

        public override void fixup(object ignored, object ignored2)
        {
            ReferenceRecord record = _reader.getRecord(_ID);
            if (record == null)
            {
                Debug.WriteLine("Referenced identifier #" + _ID + " not found in document");
                throw new XmlException(SR.Format(SR.XmlS_UnknownID_1, _ID));
            }

            record.referenced = true;
            _nestedFixup.fixup(_nestedTarget, record.value);
        }
    }

    // -----------------------------------------------------------------------------
    // This section handles fixups for arrays and collections..  There are
    // two steps to array-like fixups.
    //
    // Step 1: Add the elements to a temporary collection
    // Step 2: After serialization is complete, tranform the collection
    //          into an array (depending on element type specified in the
    //          message).
    // Step 3: When assigning, check the type of the LogicalMemberValue,
    //          and if that type is a collection or enumerable, then
    //          transform the array into the right collection type.
    //
    // Since Step 2 is handled after serialization is complete, there must
    // be an ordered list of fixups that must be run LIFO order.
    //
    // Step 1 is handled by the ArrayLikeFixup, getFixupForCollection, and
    // getIndexForNextElement.
    //
    // Step 3 is handled in the AssigningFixup.
    // -----------------------------------------------------------------------------


    /// <summary>
    /// This fixup assigns an actual array-like object to a member of an object instance. 
    /// </summary>
    /// <remarks>
    /// This is done by calling the doAssignment method of the ArrayLikeFixup. This method assigns 
    /// assigns the contents of the temporary ArrayList stored in the ArrayLikeFixup directly
    /// to the member of the object instance in the case of the member being a collection or 
    /// an enumerable. If the member is a Array then a new instace of the array is created 
    /// using the target type and the elements of the temp ArrayList are assigned to the new
    /// array.
    /// </remarks>
    internal class ArrayLikeAssignmentFixup : Fixup
    {
        private ArrayLikeFixup _fixup;
        public ArrayLikeAssignmentFixup(ArrayLikeFixup fixup)
        {
            _fixup = fixup;
        }

        public override void fixup(object ignored1, object ignored2)
        {
            _fixup.doAssignment();
        }
    }

    /// <summary>
    /// This class handles assigning de-serialized elements from an array-like objects to a 
    /// member of a temporary ArrayList. This is done by creating a temporary ArrayList to hold 
    /// each deserialized element. After all of the elements are de-serialized, we make a proper
    /// Array from the ArrayList. The items in the proper Array is used to populate a specific 
    /// instance of the array-like.
    /// </summary>
    internal class ArrayLikeFixup : Fixup
    {
        /// <summary>The type of the array-like</summary>
        private LogicalType _collectionType;
        /// <summary>The type of the elements contained in the arry-like</summary>
        private Type _elementType;
        /// <summary>The array-like's member fixup </summary>
        private Fixup _memberFixup;
        /// <summary>The fixup target for the array-like member</summary>
        private object _memberFixupTarget;
        /// <summary>Cache of forward reference ids.</summary>
        private Hashtable _IDCache;
        /// <summary>Forward reference identifier used for encoded arrys</summary>
        private string _identifier;
        /// <summary>The next fixup in this list.</summary>
        internal ArrayLikeFixup m_nextFixup;
        /// <summary>The temporary collection to hold the array elements.</summary>
        internal ArrayList m_elements;
        // PERF: use a collection that doesn't copy to grow here.        


        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public ArrayLikeFixup(LogicalType collectionType, Fixup memberFixup, object memberFixupTarget) :
            this(collectionType, null, memberFixup, memberFixupTarget, null)
        {
        }


        public ArrayLikeFixup(LogicalType collectionType, Hashtable idCache,
                               Fixup memberFixup, object memberFixupTarget,
                               string identifier)
        {
            _memberFixup = memberFixup;
            _memberFixupTarget = memberFixupTarget;
            _identifier = identifier;
            _IDCache = idCache;
            m_elements = new ArrayList();
            _collectionType = collectionType;
            _elementType = collectionType.TypeAccessor.NestedAccessors.Default.Type.Type;
        }

        /// <summary>
        /// This overload of the fixup() method assigns a specific element in the 
        /// the array-like. The first object parameter should be the index into 
        /// the array-like at which the element is inserted. 
        /// </summary>
        public override void fixup(object index, object value)
        {
            m_elements[(int)index] = value;
        }

        /// <summary>
        /// This method assigns elements in the temporary ArrayList to the array-like member. 
        /// The fixup used is a member fixup, so the elements are assigned directly to the 
        /// fixupTarget's array-like member. If there is identifer for this member 
        /// (encoded messages only), then a ReferenceRecord is created with the temporary 
        /// ArrayList.
        /// </summary>
        /// <remarks>
        /// PERF: The ReferenceRecord contains the temporary ArrayList and not the transformed 
        /// array-like value. So, we have to transform the ArrayList to the correct array-like 
        /// everytime a reference to this array-like is dereference. If the collection is large 
        /// and there are multiple references to it, then this could be a big performance bottle
        /// neck.
        /// </remarks>
        internal void doAssignment()
        {
            object elements = Elements;

            if (_identifier != null)
            {
                if (_IDCache.ContainsKey(_identifier))
                {
                    Debug.WriteLine("The identifier " + _identifier + " appears more than once.");
                    throw new XmlException(SR.Format(SR.XmlS_RepeatedIdentifier_1, _identifier));
                }

                ReferenceRecord record = new ReferenceRecord(_identifier, elements);
                _IDCache[_identifier] = record;
            }

            _memberFixup.fixup(_memberFixupTarget, elements);
        }

        private object Elements
        {
            get
            {
                if (m_elements == null) return null;

                if (_memberFixup is AssigningFixup)
                    return m_elements.ToArray(_elementType);

                object transformedValue = m_elements;
                if (_collectionType is LogicalCollection)
                {
                    transformedValue = SerializationHelper.CreateInstance(((LogicalCollection)_collectionType).Type);
                    if (null == transformedValue) return null;

                    TransformHelper.CopyArrayLikeToTargetArrayLike(transformedValue, m_elements, ((LogicalCollection)_collectionType).AddMethod);
                }
                else if (_collectionType is LogicalEnumerable)
                {
                    transformedValue = SerializationHelper.CreateInstance(((LogicalEnumerable)_collectionType).Type);
                    if (null == transformedValue) return null;

                    TransformHelper.CopyArrayLikeToTargetArrayLike(transformedValue, m_elements, ((LogicalEnumerable)_collectionType).AddMethod);
                }
                else if (SerializationHelper.isLogicalArray(_collectionType))
                {
                    try
                    {
                        transformedValue = m_elements.ToArray(_elementType);
                    }
                    catch (InvalidCastException)
                    {
                        throw new InvalidCastException(SR.Format(SR.XmlS_XmlInvalidCast_2, _elementType.FullName, _collectionType.Type.FullName));
                    }
                }

                return transformedValue;
            }
        }

        /// <summary>
        /// Use the identifier for the match also.  In the case of literal,
        /// or other embedded arrays, I want the target and the fixup.
        /// However, for encoded messages, many arrays have no fixup or
        /// target (i.e. they're just forward refs).  In this case, match by
        /// identifier.        
        /// </summary>
        public bool MatchFixup(Fixup memberFixup, object memberFixupTarget, string identifier)
        {
            return (_memberFixup == memberFixup)
                && (_memberFixupTarget == memberFixupTarget)
                && (identifier == _identifier);
        }

        /// <summary>
        /// Gets the index of the next free slot in the temporary ArrayList.  
        /// A null element is add to the ArrayList. This is neccessary for 
        /// fixup method to behave correctly. 
        /// </summary>
        public int GetIndex()
        {
            int ret = m_elements.Count;
            //in order for these fixups to work, I need to bind the slot
            //now.  So, create an empty slot, and then return the index
            m_elements.Add(null);//fill in a dummy slot
            return ret;
        }
    }

    /// <summary>
    /// This Fixup is used by the XmlSerializer. It is a simple fixup that only assigns the 
    /// the de-serialized value to a member value. The XmlSerializer will return this value
    /// from the Deserialize method. If the object is a collection or an enumerable, then the 
    /// target type is a collection or an enumeration then the de-serialized value (the temp 
    /// ArrayList) is transformed into the correct exception before assigning. If the target
    /// type is an Array then the de-serialized value (the temp ArrayList) is also transformed
    /// before assigning.
    /// </summary>
    internal class ObjectFixup : AssigningFixup
    {
        private object _obj = null;

        public ObjectFixup(LogicalType logicalType)
        {
            this.TargetType = logicalType;
        }

        public object Object
        {
            get { return _obj; }
        }

        protected override void AssignValue(object ignored, object value)
        {
            _obj = value;
        }
    }

    /// <summary>
    /// This Fixup assigns a de-serailized value to the Member of a give object instance. This
    /// class special cases transformation of collections and enumerations. The elements of 
    /// the collection/enumeration are assigned directly from the temporary ArrayList into the
    /// member of the object instance. So, collections and enumerations are deserialize using
    /// only the getter of a property accessor (MemberInfo.GetValue).
    /// </summary>
    internal class MemberFixup : AssigningFixup
    {
        public MemberInfo m_member;
        public bool m_CanWrite;

        public MemberFixup(MemberInfo member)
        {
            if (member is PropertyInfo)
            {
                PropertyInfo property = member as PropertyInfo;
                if (property != null)
                {
                    MethodInfo getMethod = property.GetMethod;
                    MethodInfo setMethod = property.SetMethod;
                    LogicalType.ValidateSecurity(getMethod);
                    LogicalType.ValidateSecurity(setMethod);
                }
            }
            else
                LogicalType.ValidateSecurity(member);
            m_member = member;
            m_CanWrite = m_member is PropertyInfo ? ((PropertyInfo)m_member).CanWrite : true;
        }

        /// <summary>
        /// Override this method to return the target's collection object member.        
        /// </summary>
        protected override object GetTargetCollection(object target)
        {
            object collection = SerializationHelper.GetValue(target, m_member);
            if (collection == null && m_CanWrite)
            {
                SerializationHelper.SetValue(target, m_member, SerializationHelper.CreateInstance(((LogicalCollection)m_TargetType).Type));
                collection = SerializationHelper.GetValue(target, m_member);
            }

            return collection;
        }


        /// <summary>
        /// Override this method to return the target's enumerable object member.        
        /// </summary>
        protected override object GetTargetEnumerable(object target)
        {
            object enumerable = SerializationHelper.GetValue(target, m_member);
            if (enumerable == null && m_CanWrite)
            {
                SerializationHelper.SetValue(target, m_member, SerializationHelper.CreateInstance(((LogicalEnumerable)m_TargetType).Type));
                enumerable = SerializationHelper.GetValue(target, m_member);
            }

            return enumerable;
        }

        /// <summary>
        /// Override this method to assign the de-serialized value using MemberInfo.SetValue(). 
        /// For collections/enumerables, we do not assign the value using SetValue(), because the 
        /// items were assigned to the member value directly durning transformation in inherited
        /// method TransformValue.
        /// </summary>
        protected override void AssignValue(object target, object value)
        {
            if ((m_TargetType is LogicalCollection || m_TargetType is LogicalEnumerable) &&
                !m_TargetType.Type.GetTypeInfo().IsValueType)
            {
                return;
            }
            SerializationHelper.SetValue(target, m_member, value);
        }
    }

    /// <summary>
    // This is a fixup that assigns a value to some specified location. The specific location 
    // is determined by subclass. For example, The MemberFixup class assigns a value to a 
    // property or field, while the ArgumentFixup class assigns a value to the argument list 
    // of a Web Service method call. To define how the deserialized value is assign the 
    // subclass would override one method.
    //
    // - AssignValue() - This method actually assigns the de-serialized value. Fo example,
    //                   The MemberFixup class uses MemberInfo.SetValue() to assign, while 
    //                   the ArgumentFixup simply assigns the value to the correct position 
    //                   in the method's argument array.  

    // If the specified location is a collection or enumerable, then a subclass can specify 
    // how the value is assigned. This is done by overriding three methods.
    //
    // - GetTargetCollection() - This method returns the collection that will recieve the 
    //                           deserialized collection elements. By default this method
    //                           creates a new instance of the collection.
    // - GetTargetEnumerable() - This method returns the enumerable that will recieve the 
    //                           deserialized enumarable elements. By default this method
    //                           creates a new instance of the enumerable.    
    /// </summary>
    internal abstract class AssigningFixup : Fixup
    {
        protected LogicalType m_TargetType;

        public AssigningFixup()
        {
        }

        public LogicalType TargetType
        {
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            get
            {
                //check to make sure I don't return a null type
                Debug.Assert(m_TargetType != null, "Returning null Type");
                return m_TargetType;
            }
            set
            {
                if (value != null)
                {
                    m_TargetType = value;
                }
            }
        }

        public virtual bool CanGetValue
        {
            get { return false; }
        }

        public virtual object GetValue(object ignored)
        {
            Debug.Assert(false, "Can not get value");

            return null;
        }

        /// <summary>
        /// This method is called to assign a de-serialized value to a given object that will then
        /// be returned to the user. When this method is called the de-serialized value has already
        /// be transformed. This function should be overriden by subclasses to customize how the 
        /// assignment is performed. For example, the MemberFixup class calles MemberInfo.SetValue
        /// to assign the value to a property or field.
        /// </summary>
        protected abstract void AssignValue(object target, object value);

        /// <summary>
        /// Assigns a given value to the target. The value that is being assigned is 
        /// transformed before being assigned. How the assignment takes place is left 
        /// up to subclasses by implementing the AssignValue method.
        /// </summary>
        public override void fixup(object target, object value)
        {
            object transformedValue = TransformValue(target, value);
            AssignValue(target, transformedValue);
        }

        public virtual void FixupWithDefaultValueForType(object target)
        {
            if (m_TargetType == null || (!m_TargetType.Type.GetTypeInfo().IsPrimitive && !m_TargetType.Type.GetTypeInfo().IsValueType))
                return;

            fixup(target, SerializationHelper.GetDefaultValue(m_TargetType.Type));
        }

        /// <summary>
        /// Returns the target collection that will recieve the items stored in a de-serialized 
        /// collection. By default, the target parameter is ignored and a new collection based 
        /// on the m_TargetType member. Subclasses can override this method to specifiy a 
        /// different collection. For example the MemberFixup class overrides this method to 
        /// return the actual collection member of the fixup target. This allows collection 
        /// items to be assigned directly into the member itself.
        /// </summary>
        protected virtual object GetTargetCollection(object target)
        {
            return SerializationHelper.CreateInstance(((LogicalCollection)m_TargetType).Type);
        }

        /// <summary>
        /// Returns the target enumerable that will recieve the items stored in a de-serialized 
        /// enumerable. By default, the target parameter is ignored and a new enumerable based 
        /// on the m_TargetType member. Subclasses can override this method to specifiy a 
        /// different enumerable. For example the MemberFixup class overrides this method to 
        /// return the actual enumerable member of the fixup target. This allows enumerable 
        /// items to be assigned directly into the member itself.
        /// </summary>
        protected virtual object GetTargetEnumerable(object target)
        {
            return SerializationHelper.CreateInstance(((LogicalEnumerable)m_TargetType).Type);
        }

        /// <summary>
        /// Transforms the de-serialized value to a value that is ready to be assigned to target
        /// object. If the target object is a collection then the de-serialized value is 
        /// transformed to a collection that has the same type as the target object. If the target
        /// object is an enumerable then the de-serialized value is transformed to an enumerable 
        /// that has the same type as the target object. If the target object is an Array then the
        /// de-serialized value is transofrmed to an Array that has the same type as the target. 
        /// The target collection/enumerable is found using the virtual GetTargetCollection and 
        /// GetTargetEnumerable methods. By default these method create new instances of the 
        /// the collection/enumerable. This behavior can be overridden. For example, the 
        /// MemberFixup class overrides these methods to return the actual value of the member so 
        /// that the items of the collection can be assigned dorectly to the member itself.
        /// </summary>
        protected object TransformValue(object target, object value)
        {
            if (value == null) return null;

            object transformedValue = value;

            if (m_TargetType is LogicalCollection)
            {
                transformedValue = GetTargetCollection(target);
                if (null == transformedValue) return null;

                TransformHelper.CopyArrayLikeToTargetArrayLike(transformedValue, value, ((LogicalCollection)m_TargetType).AddMethod);
            }
            else if (m_TargetType is LogicalEnumerable)
            {
                transformedValue = GetTargetEnumerable(target);
                if (null == transformedValue) return null;

                TransformHelper.CopyArrayLikeToTargetArrayLike(transformedValue, value, ((LogicalEnumerable)m_TargetType).AddMethod);
            }
            else if (SerializationHelper.isLogicalArray(m_TargetType))
            {
                if (!m_TargetType.Type.IsAssignableFrom(value.GetType()))
                {
                    Array srcArray = value as Array;
                    if (srcArray == null)
                    {
                        srcArray = TransformHelper.CreateArrayFromCollection(value);
                        if (srcArray == null) return null;
                    }

                    try
                    {
                        transformedValue = Array.CreateInstance(m_TargetType.Type.GetElementType(), srcArray.Length);
                        Array.Copy(srcArray, (Array)transformedValue, srcArray.Length);
                    }
                    catch (InvalidCastException)
                    {
                        throw new InvalidCastException(SR.Format(SR.XmlS_XmlInvalidCast_2, srcArray.GetType().FullName, m_TargetType.Type.FullName));
                    }
                }
            }

            return transformedValue;
        }
    }




    internal static class TransformHelper
    {
        /// <summary>
        /// This is a helper method converts an ICollection or IEnumerable to an object array. 
        /// This is neccessary because 
        /// </summary>
        public static Array CreateArrayFromCollection(object collection)
        {
            Debug.Assert(collection != null, "Attempting to create an object array from a null collection");

            Array srcArray = null;

            if (typeof(ICollection).IsAssignableFrom(collection.GetType()))
            {
                srcArray = new object[((ICollection)collection).Count];
                ((ICollection)collection).CopyTo(srcArray, 0);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(collection.GetType()))
            {
                ArrayList tempArrayList = new ArrayList();
                foreach (object item in (IEnumerable)collection)
                {
                    tempArrayList.Add(item);
                }
                srcArray = tempArrayList.ToArray();
            }

            return srcArray;
        }

        /// <summary>
        /// This is a helper method that copies the items in a array like to a object that has an
        /// Add() method. The destination array like can be an ICollection, ICollection<T>,
        /// IEnumerable, or IEnumerable<T>. The destination can not be an array. Arrays do not have 
        /// and Add method and should be handle differently. The source, however, can be an ICollection,
        /// ICollection<T>, IEnumerble, IEnumerble<T>, or an Array. 
        /// </summary>
        public static void CopyArrayLikeToTargetArrayLike(object destination, object source, MethodInfo addMethod)
        {
            Debug.Assert(destination != null, "Attempting to copy objects to a null array like collection");
            Debug.Assert(source != null, "Attempting to copy objects to an array like collection from a null array");
            Debug.Assert(addMethod != null, "Attempting to copy objects to an array like collection ");

            object[] args = new object[1];
            Type srcType = source.GetType();

            if (typeof(ICollection).IsAssignableFrom(srcType))
            {
                Array collectionArray = new object[((ICollection)source).Count];
                ((ICollection)source).CopyTo(collectionArray, 0);

                for (int idx = 0; idx < collectionArray.Length; ++idx)
                {
                    args[0] = collectionArray.GetValue(idx);
                    addMethod.Invoke(destination, args);
                }
            }
            else if (typeof(IEnumerable).IsAssignableFrom(srcType))
            {
                foreach (object item in (IEnumerable)source)
                {
                    args[0] = item;
                    addMethod.Invoke(destination, args);
                }
            }
            else if (typeof(Array).IsAssignableFrom(srcType))
            {
                foreach (object item in (Array)source)
                {
                    args[0] = item;
                    addMethod.Invoke(destination, args);
                }
            }
        }
    }
}
