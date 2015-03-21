// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

#pragma warning disable 0162
#pragma warning disable 0429
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Diagnostics;
using System.Xml;
using System.Xml.Schema;
using System.Threading;
using System.Security;

using Hashtable = System.Collections.InternalHashtable;

namespace System.Xml.Serialization.LegacyNetCF
{
    /// <summary>
    /// This class contains information about the current MemberValueCollection
    /// that is being deserialized. DeserializationContext objects are not created
    /// at reflect time. They are created and destroyed while objects are being
    /// deserialized. There is a DeserializationContext for each object being
    /// deserialized. It is created at the beinging of object deserialization and
    /// destroyed after the object is finished being deserialize.
    /// </summary>
    /// <remarks>
    /// Design Note: To not expose any underlying collection used to store the info
    ///    in this class through its public interface. This class may change
    ///    depending on performance characteristics. So, adding and looking up
    ///    objects should be done through public methods, hiding the underlying
    ///    collection class.
    ///
    /// The information about the MemberValueCollection is contained in three
    /// lookup tables. They are described below.
    /// </remarks>
    internal class DeserializationContext
    {
        private Hashtable _QNameToMemberMap;
        private Hashtable _QNameToAnyMemberMap;
        private Hashtable _QNameToAnyAccessorMemberMap;

        /// <summary>
        /// Maps a XmlQualifiedName to a Queue&lt;MemberMap&gt;. We
        /// need a queue because one XmlQualifiedName can map to multiple members
        /// The MemberMaps in the queue are ordered. So the MemberMap
        /// maps dequeued in the order that they should appear in the document.
        /// </summary>
        private Hashtable QNameToMemberMap
        {
            get
            {
                if (_QNameToMemberMap == null)
                    _QNameToMemberMap = new Hashtable();
                return _QNameToMemberMap;
            }
        }

        /// <summary>
        /// Similar to m_QNameToMemberMap. The difference
        /// being that this hashtable maps XmlQualifiedNames to a
        /// Queue&lt;LogicalMemberValue&gt;. Where the LogicalMemberValue is an XmlAny
        /// member. We need a queue because one XmlQualifiedName may be mapped to
        /// multiple XmlAny members. Again, the LogicalMemberValue objects in the
        /// queue are ordered. So the LogicalMemberValues are dequeued in the order
        /// that they should appear in the document.
        /// </summary>
        private Hashtable QNameToAnyAccessorMemberMap
        {
            get
            {
                if (_QNameToAnyAccessorMemberMap == null)
                    _QNameToAnyAccessorMemberMap = new Hashtable();
                return _QNameToAnyAccessorMemberMap;
            }
        }

        /// <summary>
        /// Similar to m_QNameToMemberMap. The difference
        ///    being that this hashtable maps XmlQualifiedNames to a
        ///    Queue&lt;LogicalMemberValue&gt;. Where the LogicalMemberValue is an XmlAny
        ///    member. We need a queue because one XmlQualifiedName may be mapped to
        ///    multiple XmlAny members. Again, the LogicalMemberValue objects in the
        ///    queue are ordered. So the LogicalMemberValues are dequeued in the order
        ///    that they should appear in the document.
        /// </summary>
        private Hashtable QNameToAnyMemberMap
        {
            get
            {
                if (_QNameToAnyMemberMap == null)
                    _QNameToAnyMemberMap = new Hashtable();
                return _QNameToAnyMemberMap;
            }
        }

        /// <summary>
        /// Adds a QName<->MemberMapping for a non-XmlAny memebr. Multiple
        /// MemberMappings can map to the same qnames. MemberMappings should be added
        /// in they appear are to appear in the document.
        /// </summary>
        public void AddQNameToMemberMapping(XmlQualifiedName qname, MemberMapping memberMapping)
        {
            if (QNameToMemberMap[qname] == null)
                QNameToMemberMap[qname] = new Queue<MemberMapping>();
            ((Queue<MemberMapping>)QNameToMemberMap[qname]).Enqueue(memberMapping);
        }

        /// <summary>
        /// Adds a QName&lt;-&gt;MemberMapping for a XmlAny memebr. Multiple MemberMappings
        /// can map to the same qnames. MemberMappings should be added
        /// in they appear are to appear in the document.
        /// </summary>
        public void AddQNameToAnyAccessorMemberMapping(XmlQualifiedName qname, MemberMapping memberMapping)
        {
            if (QNameToAnyAccessorMemberMap[qname] == null)
                QNameToAnyAccessorMemberMap[qname] = new Queue<MemberMapping>();
            ((Queue<MemberMapping>)QNameToAnyAccessorMemberMap[qname]).Enqueue(memberMapping);
        }

        /// <summary>
        /// Adds a QName&lt;-&gt;LogicalMemberValue for a XmlAny memebr. Multiple
        /// LogicalMemberValues can map to the same qnames. MemberMappings should be
        /// added in they appear are to appear in the document.
        /// </summary>
        public void AddQNameToAnyMemberMapping(XmlQualifiedName qname, LogicalMemberValue member)
        {
            if (QNameToAnyMemberMap[qname] == null)
                QNameToAnyMemberMap[qname] = new Queue<LogicalMemberValue>();
            ((Queue<LogicalMemberValue>)QNameToAnyMemberMap[qname]).Enqueue(member);
        }

        /// <summary>
        /// Finds the non-XmlAny MemberMapping for the specified QName. If the
        /// MemberMapping is found then it is removed from the context. If the
        /// MemberMapping is not found, null is returned.
        /// </summary>
        public MemberMapping GetMemberMappingForQName(XmlQualifiedName qname)
        {
            if (_QNameToMemberMap == null)
                return null;

            Queue<MemberMapping> orderedMappings = _QNameToMemberMap[qname] as Queue<MemberMapping>;
            if (orderedMappings == null || orderedMappings.Count <= 0)
                return null;

            return orderedMappings.Dequeue();
        }

        /// <summary>
        /// Finds the XmlAny MemberMapping for the specified QName. If the
        /// MemberMapping is found then it is removed from the context. If the
        /// MemberMapping is not found, null is returned.
        /// </summary>
        public MemberMapping GetAnyAccessorMemberMappingForQName(XmlQualifiedName qname)
        {
            if (_QNameToAnyAccessorMemberMap == null)
                return null;

            Queue<MemberMapping> orderedMappings = _QNameToAnyAccessorMemberMap[qname] as Queue<MemberMapping>;
            if (orderedMappings == null || orderedMappings.Count <= 0)
                return null;

            return orderedMappings.Dequeue();
        }

        /// <summary>
        /// Finds the XmlAny LogicalMemberValue for the specified QName. If the
        /// LogicalMemberValue is found then it is removed from the context. If the
        /// LogicalMemberValue is not found, null is returned.
        /// </summary>
        public LogicalMemberValue GetAnyMemberForQName(XmlQualifiedName qname)
        {
            if (_QNameToAnyMemberMap == null)
                return null;

            Queue<LogicalMemberValue> members = _QNameToAnyMemberMap[qname] as Queue<LogicalMemberValue>;
            if (members == null || members.Count <= 0)
                return null;

            LogicalMemberValue member = members.Peek();
            if (!member.IsXmlAnyElementArray)
                members.Dequeue();

            return member;
        }
    }

    internal static class MemberValueComparers
    {
        /// <summary>
        /// This method compares two LogicalMemberValue objects by class definition
        /// level and declaration order (according to reflection). This object is used
        /// only for sorting LogicalMemberValues within a collection.
        /// It should not be used to compare equality of two LogicalMemberValues.
        /// ClassDefinitionLevel determines where in the class
        /// hierarchy that the member is defined. A larger ClassDefinitionLevel value
        /// means that the member was declared higher in the class hierarchy. So,
        /// LogicalMemberValue objects are sorted first by ClassDefinitionLevel
        /// (descending).
        /// </summary>
        public static int ByDefinition(LogicalMemberValue x, LogicalMemberValue y)
        {
            // Reverse the class definition level order so that high numbers come out on top.
            int order = -x.ClassDefinitionLevel.CompareTo(y.ClassDefinitionLevel);
            // Second level sort is based on original declaration order within a class.
            if (order == 0) order = x.SequenceId.CompareTo(y.SequenceId);
            return order;
        }

        /// <summary>
        /// Compares two LogicalMemberValue objects. This object is used only
        /// for sorting LogicalMemberValues within a collection.
        /// It should not be used to compare equality of two LogicalMemberValues.
        /// This object only considers two properties of the LogicalMemberValue:
        /// ClassDefinitionLevel and Order.
        /// ClassDefinitionLevel determines where in the class hierarchy that the
        /// member is defined. A larger ClassDefinitionLevel value means that the
        /// method was declared higher in the class heirarchy.
        /// The Order property determines where a member should be serialized relative
        /// to other members defined within the same class.
        /// So, LogicalMemberValue objects are sorted
        /// first by ClassDefinitionLevel (descending) and then by Order (ascending).
        /// </summary>
        public static int ByExplicitOrder(LogicalMemberValue x, LogicalMemberValue y)
        {
            // Reverse the class definition level order so that high numbers come out on top.
            int order = -x.ClassDefinitionLevel.CompareTo(y.ClassDefinitionLevel);
            // Second level sort is based on original declaration order within a class.
            if (order == 0) order = x.Order.CompareTo(y.Order);
            return order;
        }

        /// <summary>
        /// This method compares two MemberMapping objects.
        /// This object is used only for sorting MemberMappings within a collection.
        /// It should not be used to compare equality of two MemberMappings.
        /// This object uses the ElementMember property to sort the MemberMappings.
        /// See the comment in above overload to understand how
        /// LogicalMemberValues are sorted.
        /// </summary>
        public static int ByExplicitOrder(MemberMapping x, MemberMapping y)
        {
            Debug.Assert(x.ElementMember != null && y.ElementMember != null, "We can only compare member mappings with element members");
            return ByExplicitOrder(x.ElementMember, y.ElementMember);
        }
    }

    /// <summary>
    /// Handles mapping LogicalMemberValues to the XmlAny members contained in
    /// a MemberValueCollection object.
    /// </summary>
    internal class XmlAnyCollection
    {
        public LogicalMemberValue XmlAnyAttribute = null;
        public LogicalMemberValue UnknownHeaders = null;
        public LogicalMemberValue Default = null;

        private MemberValueCollection _owner;
        private QNameContainer _anyRecords = new QNameContainer();
        private Dictionary<LogicalMemberValue, List<XmlQualifiedName>> _anyRecordsRev = new Dictionary<LogicalMemberValue, List<XmlQualifiedName>>();
        private Hashtable _memberAccessor = new Hashtable();
        private QNameContainer _memberMap = null;

        public XmlAnyCollection(MemberValueCollection owner)
        {
            _owner = owner;
        }

        private QNameContainer MemberMap
        {
            get
            {
                if (_memberMap == null)
                    _memberMap = new QNameContainer();
                return _memberMap;
            }
        }

        /// <summary>
        /// Add a mapping between a XmlQualifiedName and a LogicalMemberValue.
        /// </summary>
        public void addAnyMember(string name, string ns, LogicalMemberValue value, string memberName)
        {
            // Insure that the only one qname is mapped to a given any element. If
            // the MemberValueCollection is ordered then duplicate members are allowed.
            XmlQualifiedName qname = new XmlQualifiedName(name, ns);
            if (value.Order == -1 && _anyRecords.ContainsKey(qname))
            {
                Debug.WriteLine("Two any elements for " + ns + ":" + name);
                throw new InvalidOperationException(SR.Format(SR.XmlS_TwoXmlAny_2, ns, name));
            }

            // Add the member to the QName<->XmlAnyMapping map
            if (_anyRecords[qname] == null)
                _anyRecords[qname] = new List<LogicalMemberValue>();
            ((List<LogicalMemberValue>)_anyRecords[qname]).Add(value);

            // Add the member to the LogicalMemberValue<->QName map
            if (!_anyRecordsRev.ContainsKey(value))
                _anyRecordsRev[value] = new List<XmlQualifiedName>();
            _anyRecordsRev[value].Add(qname);

            // Add the member to the owners ordered map, if neccessary
            if (value.Order != -1)
                _owner.addOrderedMemberValue(value, memberName);
        }

        /// <summary>
        /// Look up a LogicalMemberValue by XmlQualifiedName.
        /// </summary>
        public LogicalMemberValue lookupAnyElement(string name, string ns)
        {
            XmlQualifiedName qname = new XmlQualifiedName(name, ns);
            List<LogicalMemberValue> anyMapping = _anyRecords[qname] as List<LogicalMemberValue>;

            // If there is no mapping for the giving qname
            // then return the default
            if (anyMapping == null || anyMapping.Count == 0)
                return Default;

            // If the MemberValueCollection is not ordered
            // or there is only one member in the mapping
            // then return the first element in the mapping
            if (!_owner.IsOrdered || anyMapping.Count == 1)
                return anyMapping[0];

            // If the MemberValueCollection is ordered than
            // look through the deserialization context to
            // find the correct member.
            return FindAnyElementInDeserializationContext(qname);
        }

        /// <summary>
        /// Find a LogicalMemberValue by XmlQualifiedName using the
        /// owner's current DeserializationContext. This should be called only for
        /// ordered XmlAny objects that are not dependent on an XmlChoiceAttribute.
        /// </summary>
        private LogicalMemberValue FindAnyElementInDeserializationContext(XmlQualifiedName qname)
        {
            Debug.Assert(_owner.IsOrdered, "MemberValueCollection is not ordered.");

            Stack<DeserializationContext> contextStack = _owner.GetDeserializationStack(Environment.CurrentManagedThreadId);
            if (contextStack.Count <= 0)
                return null;

            DeserializationContext curDeserializationContext = contextStack.Peek();
            LogicalMemberValue member = curDeserializationContext.GetAnyMemberForQName(qname);

            return member == null ? Default : member;
        }

        /// <summary>
        /// Finds all of the XmlQualifiedNames for a given LogicalMemberValue.
        ///
        /// Design Note: This method should only expose the IEnumerable interface. It
        ///    should never expose the true underlying collection object, because it is
        ///    subject to change.
        /// </summary>
        public IEnumerable lookupAnyElementQName(LogicalMemberValue value)
        {
            if (!_anyRecordsRev.ContainsKey(value))
                return null;
            return _anyRecordsRev[value];
        }

        /// <summary>
        /// Initializes a DeserializationContext with information about the XmlAny's
        /// we know about. The context is only initialized if the owning
        /// MemberValueCollection is ordered.
        /// </summary>
        public void InitDeserializationContext(DeserializationContext context)
        {
            if (!_owner.IsOrdered)
                return;

            foreach (XmlQualifiedName qname in _anyRecords.Keys)
            {
                List<LogicalMemberValue> anyMapping = _anyRecords[qname] as List<LogicalMemberValue>;
                if (anyMapping == null || anyMapping.Count <= 1)
                    continue;

                for (int ndx = 0; ndx < anyMapping.Count; ++ndx)
                {
                    context.AddQNameToAnyMemberMapping(qname, anyMapping[ndx]);
                }
            }

            if (MemberMap != null)
            {
                foreach (XmlQualifiedName qname in MemberMap.Keys)
                {
                    MemberMapping mapping = MemberMap[qname] as MemberMapping;
                    Debug.Assert(mapping != null, "");

                    if (mapping.ElementMembers == null)
                        continue;

                    for (int i = 0; i < mapping.ElementMembers.Count; ++i)
                    {
                        context.AddQNameToAnyAccessorMemberMapping(qname, mapping.ElementMembers[i]);
                    }
                }
            }
        }

        /// <summary>
        /// If an XmlAny member is an collection and dependent on an XmlChoiceAttribute,
        /// then the collection it may contain a objects that are not XmlAny's but
        /// strongly typed objects. This method adds a mapping between the Accessor
        /// and LogicalMemberValue.
        /// </summary>
        public void addElementAccessor(Accessor accessor, LogicalMemberValue member)
        {
            Debug.Assert(accessor != null, "Adding null accessor to XmlAnyCollection's ElementAccessors");
            Debug.Assert(member != null, "Adding accessor mapped to a null member");

            AccessorCollection memberAccessors = (AccessorCollection)_memberAccessor[member];
            if (memberAccessors == null)
            {
                memberAccessors = new AccessorCollection();
                _memberAccessor[member] = memberAccessors;
            }

            Accessor existing = memberAccessors.findAccessorNoThrow(accessor.Type.Type);
            if (existing != null && memberAccessors.Choice == null)
            {
                string memberName = string.Empty;
                if (member.Fixup != null && member.Fixup is MemberFixup)
                    memberName = ((MemberFixup)member.Fixup).m_member.Name;
                throw new InvalidOperationException(SR.Format(SR.XmlChoiceIdentiferMissing, typeof(XmlChoiceIdentifierAttribute).Name, memberName));
            }
            memberAccessors.add(accessor);

            if (_owner.IsOrdered)
            {
                XmlQualifiedName qname = new XmlQualifiedName(accessor.Name, accessor.Namespace);

                if (!MemberMap.ContainsKey(qname))
                {
                    MemberMap[qname] = new MemberMapping(accessor, member, accessor.IsAttribute);
                }
                else
                {
                    MemberMapping existingMapping = MemberMap[qname] as MemberMapping;
                    Debug.Assert(existingMapping != null);

                    if (existingMapping.ElementMember == null)
                    {
                        existingMapping.ElementMember = member;
                        return;
                    }

                    if (existingMapping.ElementMembers == null)
                    {
                        existingMapping.ElementMembers = new List<MemberMapping>();
                        existingMapping.ElementMembers.Add(new MemberMapping(existingMapping.ElementAccessor, existingMapping.ElementMember, false/*isAttribute*/));
                    }

                    existingMapping.ElementMembers.Add(new MemberMapping(accessor, member, false/*isAttribute*/));
                }
            }
        }

        /// <summary>
        /// If an XmlAny member is an collection and dependent on an XmlChoiceAttribute,
        /// then the collection it may contain a objects that are not XmlAny's but
        /// strongly typed objects. When this is true, a mapping exists between an
        /// Accessor and an XmlAny member. This method finds the Accessor and
        /// LogicalMemberValue given an XmlQualifiedName (name:ns).
        /// </summary>
        public bool lookupElementAccessor(string name, string ns, out Accessor accessor, out LogicalMemberValue member)
        {
            if (!_owner.IsOrdered)
            {
                AccessorCollection elementAccessors;
                foreach (LogicalMemberValue key in _memberAccessor.Keys)
                {
                    elementAccessors = (AccessorCollection)_memberAccessor[key];
                    accessor = elementAccessors.findAccessorNoThrow(name, ns);
                    if (accessor != null)
                    {
                        member = key;
                        return true;
                    }
                }
            }
            else
            {
                XmlQualifiedName qname = new XmlQualifiedName(name, ns);
                MemberMapping mapping = MemberMap[qname] as MemberMapping;

                if (mapping != null)
                {
                    if (mapping.ElementMembers == null)
                    {
                        member = mapping.ElementMember;
                        accessor = mapping.ElementAccessor;
                        return true;
                    }

                    MemberMapping orderedMapping = FindOrderedMemberMapping(qname);
                    if (orderedMapping != null)
                    {
                        member = orderedMapping.ElementMember;
                        accessor = orderedMapping.ElementAccessor;
                        return true;
                    }
                }
            }

            member = null;
            accessor = null;
            return false;
        }

        /// <summary>
        /// If an XmlAny member is an collection and dependent on an XmlChoiceAttribute,
        /// then the collection it may contain a objects that are not XmlAny's but
        /// strongly typed objects. When this is true, a MemberMapping exists for the
        /// Accessor and the XmlAny member. This method finds the MemberMaping given
        /// an XmlQualifiedName. This method should only be called when the owning
        /// MemberValueCollection is ordered.
        /// </summary>
        public MemberMapping FindOrderedMemberMapping(XmlQualifiedName qname)
        {
            Debug.Assert(_owner.IsOrdered, "MemberValueCollection is not ordered.");

            Stack<DeserializationContext> contextStack = _owner.GetDeserializationStack(Environment.CurrentManagedThreadId);
            if (contextStack.Count <= 0)
                return null;

            DeserializationContext curDeserializationContext = contextStack.Peek();
            return curDeserializationContext.GetAnyAccessorMemberMappingForQName(qname);
        }

        /// <summary>
        /// If an XmlAny member is an collection and dependent on an XmlChoiceAttribute,
        /// then the collection it may contain a objects that are not XmlAny's but
        /// strongly typed objects. When this is true, a MemberMapping exists for the
        /// Accessor and the XmlAny member. This method finds the Accessor given
        /// the element type and the LogicalMemberValue.
        /// </summary>
        public Accessor lookupElementAccessor(Type eltType, LogicalMemberValue member)
        {
            AccessorCollection memberAccessors = (AccessorCollection)_memberAccessor[member];
            if (memberAccessors == null)
                return null;

            // Look for the accessor in the default accessor list
            return memberAccessors.findAccessorNoThrow(eltType);
        }

        /// <summary>
        /// If an XmlAny member is an collection and dependent on an XmlChoiceAttribute,
        /// then the collection it may contain a objects that are not XmlAny's but
        /// strongly typed objects. When this is true, a mapping exists between the
        /// list Accessor and the XmlAny member (all accessors that exist for a given
        /// member. This method finds the Accessor list for a given LogicalMemberValue.
        /// </summary>
        public AccessorCollection lookupElementAccessors(LogicalMemberValue member)
        {
            return (AccessorCollection)_memberAccessor[member];
        }

        /// <summary>
        /// If an XmlAny member is an collection and dependent on an XmlChoiceAttribute,
        /// then the collection it may contain a objects that are not XmlAny's but
        /// strongly typed objects. This method add support for the XmlChoiceAttribute
        /// for a given LogicalMemberValue.
        /// </summary>
        public void addChoiceSupport(XmlChoiceSupport choice, LogicalMemberValue member)
        {
            Debug.Assert(member != null, "Adding accessor mapped to a nul member");

            AccessorCollection memberAccessors = (AccessorCollection)_memberAccessor[member];
            if (memberAccessors == null)
            {
                memberAccessors = new AccessorCollection();
                _memberAccessor[member] = memberAccessors;
            }

            member.Accessors.Choice = memberAccessors.Choice = choice;
        }

        /// <summary>
        /// If an XmlAny member is an collection and dependent on an XmlChoiceAttribute,
        /// then the collection it may contain a objects that are not XmlAny's but
        /// strongly typed objects. This method finds the XmlChoiceSupport object
        /// for a given LogicalMemberValue.
        /// </summary>
        public XmlChoiceSupport lookupChoiceSupport(LogicalMemberValue member)
        {
            foreach (var memberAccessorMapping in _memberAccessor)
            {
                if (memberAccessorMapping.Key == member)
                {
                    AccessorCollection memberAccessors = ((AccessorCollection)memberAccessorMapping.Value);
                    return memberAccessors != null ? memberAccessors.Choice : null;
                }
            }
            return null;
        }

        /// <summary>
        /// Sets the default LogicalMemberValue that will contain all XmlAny members
        /// that are not mapped to an XmlQualifiedName.
        /// </summary>
        public void setDefault(LogicalMemberValue value, string memberName)
        {
            // There can only be one default member value
            if (Default != null)
                throw new InvalidOperationException(SR.XmlS_TwoDefaultXmlAny);

            // Add the member to the owners ordered map, if neccessary
            if (value.Order != -1)
                _owner.addOrderedMemberValue(value, memberName);

            // Set the default element.
            Default = value;
        }

        /// <summary>
        /// Get all LogicalMemberValue objects that are mapped to a specific
        /// XmlQualifiedName.
        ///
        /// DESIGN NOTE - This method should never return the underlying collection
        ///    used to store the LogicalMemberValues. It should always return an
        ///    IEnumerable, because the underlying collection is subject to change.
        /// </summary>
        public IEnumerable getSpecializedElementAnys()
        {
            List<LogicalMemberValue> elemAnys = new List<LogicalMemberValue>();
            foreach (IEnumerable members in _anyRecords.Values)
            {
                foreach (LogicalMemberValue member in members)
                {
                    elemAnys.Add(member);
                }
            }

            return elemAnys;
        }

        /// <summary>
        /// Prepares this object for serialization. This should only be called once
        /// before de/serialization. Calling it more then once is redundant and will
        /// degrade performance.
        /// </summary>
        public void prepare()
        {
            // Set the QNameContainer for fast access.
            _anyRecords.makeFast();

            // Sort Members in the QName<->List<LogicalMemberValue> List
            foreach (List<LogicalMemberValue> members in _anyRecords.Values)
            {
                members.Sort(MemberValueComparers.ByExplicitOrder);
            }
            // Sort Members in the QName<->MemberMapping List
            foreach (MemberMapping memberMap in MemberMap.Values)
            {
                if (memberMap.ElementMembers != null)
                {
                    memberMap.ElementMembers.Sort(MemberValueComparers.ByExplicitOrder);
                }
            }
        }
    }

    /// <summary>
    /// Maps an Accessor to its corresponding LogicalMemberValue. If there is a
    /// single element member for which this object maps, then ElementMember
    /// references the member. If this object maps multiple element members
    /// then ElementMembers contains a collection of MemberMapping objects.
    /// There is a MemberMapping for each element member. The ElementMembers
    /// list must be sorted in order for the object to be deserialized correctly.
    /// </summary>
    internal class MemberMapping
    {
        // Element Accessor<->MemberMapping(s)
        public Accessor ElementAccessor;
        public List<MemberMapping> ElementMembers;
        public LogicalMemberValue ElementMember;

        // Attribute Accessor<->MemberMapping
        public Accessor AttributeAccessor;
        public LogicalMemberValue AttributeMember;

        public MemberMapping(Accessor accessor, LogicalMemberValue member, bool isAttribute)
        {
            if (isAttribute)
            {
                AttributeAccessor = accessor;
                AttributeMember = member;
            }
            else
            {
                ElementAccessor = accessor;
                ElementMember = member;
            }
        }
    }


    /// <summary>
    /// This class holds all members that will be serialized for a given class.
    /// It builds QName-to-Member lookup maps to be used during
    /// deserialization. Also, during deserialization, this object builds a
    /// stack of DeserializationContext objects. DeserializationContext objects
    /// contain additional information about the object instance that is
    /// currently being deserialized. Information such as, what order the
    /// members should appear in, the next member that should be deserialized,
    /// etc. DeserializationContext objects are created and destroyed
    /// only during deserialization.
    ///
    /// NOTE: For in headers and args, I don't need the maps.  Suppress them?
    /// </summary>
    internal class MemberValueCollection
    {
        /// List of all LogicalMemberValues in the collection
        private List<LogicalMemberValue> _members;

        /// Sorted list LogicalMemberValue objects that determines the order of member de/serialization
        private List<LogicalMemberValue> _orderMap;

        /// Map of Qnames<->MemberMap.
        private QNameContainer _memberMap;

        /// Contains deserialization information for the current object being deserialized.
        /// The key to this Dictionary is a thread id because the Stack of DeserializationContext
        /// objects must be local to the current thread. If it is not, then multiple threads
        /// may pop the stack incorrectly.
        private Dictionary<int, Stack<DeserializationContext>> _deserializationContextTable;

        /// For encoded wrapped messages, the synthetic return value's name is
        /// ignored for deserialization purposes.  The wrapper is assumed to be
        /// the first argument.
        public LogicalMemberValue BoundFirstArgument;

        /// This member stores xml text segments.  Text in this case is a string
        /// between elements.
        public LogicalMemberValue XmlText;

        /// <summary>
        /// An instance of XmlSerializerNamespaces for this class.
        /// </summary>
        public LogicalMemberValue XmlNamespaceDecls;

        /// <summary>
        /// The various anys for this collection
        /// </summary>
        public XmlAnyCollection m_XmlAny;

        /// <summary>
        /// Whether this member value collection represents method arguments.
        /// If true, the members will not be sorted by class definition order. 
        /// They will be left in the order they were added since parameters must be written
        /// in the order they are declared in the method signature.
        /// </summary>
        public bool IsMethodArgumentCollection;

        /// <summary>
        /// Constructs a MemberValueCollection with IsMethodArgumentCollection set to false. 
        /// MemberValueCollections are collections of Class members (properties and fields) by default. 
        /// If the MemberValueCollection represents the arguments of a method then use the overloaded
        /// constructor.
        /// </summary>
        public MemberValueCollection()
            : this(false)
        {
        }

        /// <summary>
        /// Constructs a MemberValueCollection that allows you to specify whether 
        /// it represents a collection of class members or a collection of method arguments.
        /// </summary>
        public MemberValueCollection(bool isMethodArgumentCollection)
        {
            _members = new List<LogicalMemberValue>(4);
            _memberMap = new QNameContainer();
            _orderMap = null;
            _deserializationContextTable = new Dictionary<int, Stack<DeserializationContext>>();
            BoundFirstArgument = null;
            XmlText = null;
            XmlNamespaceDecls = null;
            m_XmlAny = null;
            IsMethodArgumentCollection = isMethodArgumentCollection;
        }

        public XmlAnyCollection XmlAny
        {
            get
            {
                if (m_XmlAny == null)
                    m_XmlAny = new XmlAnyCollection(this);
                return m_XmlAny;
            }
        }

        public bool HasXmlAny
        {
            get { return m_XmlAny != null; }
        }

        public List<LogicalMemberValue> OrderMap
        {
            get
            {
                if (_orderMap == null)
                    _orderMap = new List<LogicalMemberValue>();
                return _orderMap;
            }
        }

        public Dictionary<int, Stack<DeserializationContext>> DeserializationContextTable
        {
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            get
            {
                return _deserializationContextTable;
            }
        }

        public bool IsOrdered
        {
            get { return _orderMap != null && _orderMap.Count > 0; }
        }

        public int count()
        {
            return _members.Count;
        }

        public LogicalMemberValue getMember(int index)
        {
            return _members[index];
        }


        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public LogicalMemberValue GetOrderedMember(int ndx)
        {
            if (!IsOrdered || ndx < 0 || ndx >= _orderMap.Count)
                return null;

            return _orderMap[ndx];
        }

        public MemberMapping FindOrderedMemberMapping(XmlQualifiedName qname)
        {
            Stack<DeserializationContext> contextStack = GetDeserializationStack(Environment.CurrentManagedThreadId);
            if (contextStack.Count <= 0)
                return null;

            DeserializationContext curDeserializationContext = contextStack.Peek();
            return curDeserializationContext.GetMemberMappingForQName(qname);
        }

        public bool doLookup(XmlQualifiedName qname, bool isAttribute, out LogicalMemberValue member, out Accessor accessor)
        {
            // Find the member and accessor mapped to the qname
            MemberMapping mapping = _memberMap[qname] as MemberMapping;
            if (mapping != null)
            {
                member = isAttribute ? mapping.AttributeMember : mapping.ElementMember;
                accessor = isAttribute ? mapping.AttributeAccessor : mapping.ElementAccessor;

                if (!isAttribute)
                {
                    MemberMapping orderedMapping = FindOrderedMemberMapping(qname);
                    if (orderedMapping != null)
                    {
                        member = orderedMapping.ElementMember;
                        accessor = orderedMapping.ElementAccessor;
                    }
                }

                // Return true only if both a member and accessor were found.
                return ((member != null) && (accessor != null));
            }

            if (XmlText != null)
            {
                accessor = XmlText.Accessors.findAccessorNoThrow(qname.Name, qname.Namespace);
                if (accessor != null)
                {
                    member = XmlText;
                    return true;
                }
            }

            if (m_XmlAny != null && m_XmlAny.lookupElementAccessor(qname.Name, qname.Namespace, out accessor, out member))
            {
                return true;
            }

            member = null;
            accessor = null;
            return false;
        }

        public void addOrderedMemberValue(LogicalMemberValue member, string memberName)
        {
            Debug.Assert(member.Order != -1);

            // Walk the ordered member list looking for duplicates
            foreach (LogicalMemberValue curMember in OrderMap)
            {
                if (MemberValueComparers.ByExplicitOrder(curMember, member) == 0)
                    throw new InvalidOperationException(SR.Format(SR.XmlSequenceUnique, member.Order.ToString(NumberFormatInfo.InvariantInfo), "Order", memberName));
            }

            // Add the member to the ordered list of members
            OrderMap.Add(member);
        }

        public void addMemberValue(LogicalMemberValue member, string memberName)
        {
            // Add member to the members list
            _members.Add(member);

            // Add it to the order map, if neccessary
            if (member.Order != -1)
                addOrderedMemberValue(member, memberName);

            // Create member mappings for each accessor
            foreach (Accessor accessor in member.Accessors)
            {
                XmlQualifiedName qname = new XmlQualifiedName(accessor.Name, accessor.Namespace);
                if (!_memberMap.ContainsKey(qname))
                {
                    _memberMap[qname] = new MemberMapping(accessor, member, accessor.IsAttribute);
                }
                else
                {
                    MemberMapping existingMapping = _memberMap[qname] as MemberMapping;
                    Debug.Assert(existingMapping != null);

                    if (!accessor.IsAttribute)
                    {
                        if (existingMapping.ElementMember == null)
                        {
                            existingMapping.ElementAccessor = accessor;
                            existingMapping.ElementMember = member;
                            return;
                        }

                        if (!IsOrdered)
                        {
                            if (existingMapping.ElementMembers != null)
                            {
                                foreach (MemberMapping elementMapping in existingMapping.ElementMembers)
                                {
                                    if (elementMapping.ElementAccessor.Type.Type == accessor.Type.Type)
                                        throw new InvalidOperationException(SR.Format(SR.XmlDuplicateElementName, new string[] { accessor.Name, accessor.Namespace }));
                                }
                            }
                        }

                        if (existingMapping.ElementMembers == null)
                        {
                            existingMapping.ElementMembers = new List<MemberMapping>();
                            existingMapping.ElementMembers.Add(new MemberMapping(existingMapping.ElementAccessor, existingMapping.ElementMember, false/*isAttribute*/));
                        }

                        existingMapping.ElementMembers.Add(new MemberMapping(accessor, member, false/*isAttribute*/));
                    }
                    else
                    {
                        if (existingMapping.AttributeAccessor != null || existingMapping.AttributeMember != null)
                            throw new InvalidOperationException(SR.Format(SR.XmlDuplicateAttributeName, new string[] { accessor.Name, accessor.Namespace }));
                        existingMapping.AttributeAccessor = accessor;
                        existingMapping.AttributeMember = member;
                    }
                }
            }
        }

        private static Object s_InternalSyncObject;
        private static Object InternalSyncObject
        {
            get
            {
                if (s_InternalSyncObject == null)
                {
                    Object o = new Object();
                    Interlocked.CompareExchange(ref s_InternalSyncObject, o, null);
                }
                return s_InternalSyncObject;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public Stack<DeserializationContext> GetDeserializationStack(int tid)
        {
            if (!_deserializationContextTable.ContainsKey(tid))
            {
                lock (InternalSyncObject)
                {
                    Stack<DeserializationContext> newStack = new Stack<DeserializationContext>();
                    _deserializationContextTable[tid] = newStack;
                    return newStack;
                }
            }

            return _deserializationContextTable[tid];
        }

        /// <summary>
        ///
        /// </summary>
        public void PushDeserializationContext()
        {
            DeserializationContext deserializationContext = new DeserializationContext();
            Stack<DeserializationContext> contextStack = GetDeserializationStack(Environment.CurrentManagedThreadId);
            contextStack.Push(deserializationContext);

            if (_memberMap != null)
            {
                foreach (XmlQualifiedName qname in _memberMap.Keys)
                {
                    MemberMapping mapping = _memberMap[qname] as MemberMapping;
                    Debug.Assert(mapping != null, "");

                    if (mapping.ElementMembers == null)
                        continue;

                    for (int i = 0; i < mapping.ElementMembers.Count; ++i)
                    {
                        deserializationContext.AddQNameToMemberMapping(qname, mapping.ElementMembers[i]);
                    }
                }
            }

            if (m_XmlAny != null)
                m_XmlAny.InitDeserializationContext(deserializationContext);
        }

        /// <summary>
        ///
        /// </summary>
        public void PopDeserializationContext()
        {
            Stack<DeserializationContext> contextStack = GetDeserializationStack(Environment.CurrentManagedThreadId);

            if (contextStack.Count > 0)
                contextStack.Pop();
        }

        private bool _prepareWaiting;
        private int _delayedMemberCount;
        public int DelayedMemberCount
        {
            get { return _delayedMemberCount; }
            set
            {
                Debug.Assert(value >= 0);
                _delayedMemberCount = value;
                // If we just added the last delayed member, and if a prepare() was queued, execute it.
                if (value == 0 && _prepareWaiting)
                {
                    _prepareWaiting = false;
                    prepare();
                }
            }
        }
        /// <summary>
        /// Prepares this container for use after reflection.
        /// </summary>
        public void prepare()
        {
            // Don't spend cycles sorting members until all members are added.
            if (DelayedMemberCount > 0)
            {
                _prepareWaiting = true;
                return;
            }

            // Prepare the member map
            _memberMap.makeFast();

            // Prepare the XmlAnyCollection
            if (m_XmlAny != null)
                m_XmlAny.prepare();

            // Order the member value collection by class definition level only if this is
            // not a collection of method parameters. If this is a method of member
            // parameters then they should not be sorted, they should be left in the
            // order they were added into the collection.
            if (!IsMethodArgumentCollection)
            {
                _members.Sort(MemberValueComparers.ByDefinition);
            }

            // Prepare each member's accessor collection
            foreach (LogicalMemberValue lmv in _members)
            {
                if (lmv.Accessors != null)
                    lmv.Accessors.prepare();
            }

            if (IsOrdered)
            {
                // Ensure that each member is ordered correctly
                EnsureOrderedMembers();

                // Sort the ordered map
                OrderMap.Sort(MemberValueComparers.ByExplicitOrder);

                // Sort MemberMappings
                foreach (MemberMapping memberMap in _memberMap.Values)
                {
                    if (memberMap.ElementMembers != null)
                    {
                        memberMap.ElementMembers.Sort(MemberValueComparers.ByExplicitOrder);
                    }
                }
            }
        }

        /// <summary>
        /// Ensures that if the MemberValueCollection is ordered then all XmlElement,
        /// XmlArray, and XmlAnyElements are ordered.
        /// </summary>
        public void EnsureOrderedMembers()
        {
            // Check the Element Members in the member map
            foreach (MemberMapping memberMap in _memberMap.Values)
            {
                if (memberMap.ElementMember != null && memberMap.ElementMember.Order == -1)
                {
                    if (memberMap.ElementMember.ClassDefinitionLevel > 0)
                        throw new InvalidOperationException(SR.XmlSequenceHierarchy);
                    throw new InvalidOperationException(SR.Format(SR.XmlSequenceInconsistent, "Order", memberMap.ElementMember.MemberName));
                }
                if (memberMap.ElementMembers != null)
                {
                    foreach (MemberMapping eleMemberMap in memberMap.ElementMembers)
                    {
                        if (eleMemberMap.ElementMember != null && eleMemberMap.ElementMember.Order == -1)
                        {
                            if (eleMemberMap.ElementMember.ClassDefinitionLevel > 0)
                                throw new InvalidOperationException(SR.XmlSequenceHierarchy);
                            throw new InvalidOperationException(SR.Format(SR.XmlSequenceInconsistent, "Order", memberMap.ElementMember.MemberName));
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// This class represents a class member that will be serialized. It can
    /// also be a Web services element such as a web method argument, the
    /// synthetic soap "argument struct", or a soap header.
    /// </summary>
    internal class LogicalMemberValue
    {
        /// <summary>
        /// The helper that will set this member to its proper value from xml at deserialization time.
        /// </summary>
        public Fixup Fixup;
        /// <summary>
        /// The helper that will get the value from this member at serialization time.
        /// </summary>
        public Fetcher Fetcher;
        /// <summary>
        /// The set of accessors that can be called on to determine the name/namespace of the node
        /// that the represented member will serialize to.
        /// </summary>
        public AccessorCollection Accessors;
        public LogicalMemberValue OutArgument;
        /// <summary>
        /// The peer member to the represented member that shares the same name with this one,
        /// with an added "Specified" suffix at the end if such a member exists.  
        /// For example, if this member were called "Seeds", the SpecifiedMember would be
        /// "SeedsSpecified".  See the xml "specified pattern".
        /// </summary>
        public LogicalMemberValue SpecifiedMember;
        public object DefaultValue;
        /// <summary>
        /// The order an xml element should appear in a containing type, as it is given in
        /// XmlElementAttribute.Order.
        /// </summary>
        public int Order = -1;
        /// <summary>
        /// Stores the levels up the type hierarchy this member originates from
        /// as it appears in a particular reflected type.  The higher the number,
        /// the higher up the hierarchy a member comes from and the earlier
        /// in the list of xml elements the member should appear.`
        /// </summary>
        public int ClassDefinitionLevel = 0;
        /// <summary>
        /// The order this member was reflected within the containing type.
        /// Useful for preserving reflection order when XmlElementAttribute.Order
        /// is not set.
        /// Works in combination with ClassDefinitionLevel.
        /// </summary>
        public int SequenceId = 0;

        // Flags
        public bool Required;
        public bool CanRead;
        public bool CanWrite;
        public bool IsXmlAnyElement;
        public bool IsXmlAnyElementArray;
        public bool IsXmlText;
        /// <summary>
        /// The name of the member in the containing type.
        /// </summary>
        public string MemberName;

        public LogicalMemberValue(AccessorCollection accessors, bool required, bool canRead, bool canWrite)
        {
            this.Required = required;
            this.Accessors = accessors;
            this.CanRead = canRead;
            this.CanWrite = canWrite;
        }

        public LogicalMemberValue copy()
        {
            LogicalMemberValue ret = new LogicalMemberValue(Accessors, Required, CanRead, CanWrite);
            ret.Fetcher = Fetcher;
            ret.Fixup = Fixup;
            ret.Order = Order;
            ret.OutArgument = OutArgument;
            ret.DefaultValue = DefaultValue;
            ret.IsXmlAnyElement = IsXmlAnyElement;
            ret.IsXmlAnyElementArray = IsXmlAnyElementArray;
            ret.ClassDefinitionLevel = ClassDefinitionLevel;
            ret.MemberName = MemberName;

            if (SpecifiedMember != null)
                ret.SpecifiedMember = SpecifiedMember.copy();
            return ret;
        }
    }

    /// <summary>
    /// This class represents a class member that will be serialized. It can
    /// also be a Web services element such as a web method argument, the
    /// synthetic soap "argument struct", or a soap header.
    /// </summary>
    internal class XmlChoiceSupport
    {
        public LogicalMemberValue ChoiceMember;
        public LogicalEnum Enum;
        private Hashtable _valueToAccessor;
        private QNameContainer _accessorToValue;
        private bool _choiceMemberIsArray;
        private LogicalType _enumArrayType;


        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public XmlChoiceSupport(LogicalMemberValue choiceMember, LogicalEnum choiceType)
        {
            ChoiceMember = choiceMember;
            Enum = choiceType;
            _choiceMemberIsArray = false;
            _valueToAccessor = new Hashtable();
            _accessorToValue = new QNameContainer();
        }

        public XmlChoiceSupport(LogicalMemberValue choiceMember, LogicalEnum choiceType, bool choiceMemberIsArray, LogicalType enumArrayType)
        {
            ChoiceMember = choiceMember;
            Enum = choiceType;
            _choiceMemberIsArray = choiceMemberIsArray;
            _enumArrayType = enumArrayType;
            _valueToAccessor = new Hashtable();
            _accessorToValue = new QNameContainer();
        }

        public bool ChoiceMemberIsArray
        {
            get { return _choiceMemberIsArray; }
        }

        public LogicalType EnumArray
        {
            get { return _enumArrayType; }
        }

        /// <summary>
        /// Adds a mapping from an enum value name to an Accessor.
        /// </summary>
        public void addMapping(Accessor accessor)
        {
            XmlQualifiedName enumQName = null;
            object enumValue = null;
            try
            {
                enumQName = new XmlQualifiedName(accessor.Name, accessor.Namespace);
                enumValue = Enum.findValue(enumQName);
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException(SR.Format(SR.XmlChoiceMissingValue, Enum.Type, enumQName, accessor.Name, accessor.Namespace));
            }

            XmlQualifiedName accessorQName = new XmlQualifiedName(accessor.Name, accessor.Namespace);
            if (_valueToAccessor.ContainsKey(enumValue))
            {
                //Debug.WriteLine( "Two mappings for choice name " + enumName );
                return;
            }
            if (_accessorToValue.ContainsKey(accessorQName))
            {
                //Debug.WriteLine( "Two mappings for the same accessor " + accessor.Namespace + ":" + accessor.Name );
                return;
            }
            _valueToAccessor[enumValue] = accessor;
            _accessorToValue[accessorQName] = enumValue;
        }

        public Accessor getAccessor(object enumValue)
        {
            return (Accessor)_valueToAccessor[enumValue];
        }

        public object getValue(Accessor accessor)
        {
            return getValue(accessor.Name, accessor.Namespace);
        }

        public object getValue(string name, string ns)
        {
            XmlQualifiedName accessorQName = new XmlQualifiedName(name, ns);
            return _accessorToValue[accessorQName];
        }
    }

    [Flags]
    internal enum TypeMappingFlags
    {
        None = 0x00,
        Synthetic = 0x01,
        AllowTypeMapping = 0x02,
        AllowNameMapping = 0x04,
        Default = AllowTypeMapping | AllowNameMapping
    }

    internal delegate void AddMembersLateDelegate();

    /// <summary>
    /// Represents the result of reflecting over a CLR type, and stores information
    /// required for serializing/deserializing a type.
    /// </summary>
    /// <remarks>
    /// There may be more than one LogicalType for a given CLR System.Type.
    /// Types that do not explicitly define their own namespaces using
    /// XmlType.Namespace adopt the default namespace of the type whose members
    /// reference this LogicalType.  Since this default namespace will affect
    /// the contents of this LogicalType, a type will have as many LogicalType's
    /// as the different namespaces it will appear in.
    /// </remarks>
    internal class LogicalType
    {
        /// <summary>
        /// The name that this type will be stored as in the 
        /// <see cref="TypeContainer"/>'s lookup table.
        /// It is very important that you never use this member to create
        /// element names.  Always use the <see cref="LogicalType.TypeAccessor"/>.  
        /// This is necessary for Literal Arrays (the type accessor is ArrayOfFoo 
        /// and the type name is Foo[]).
        /// </summary>
        public string Name;
        /// <summary>
        /// The namespace that this type will be stored as in the 
        /// <see cref="TypeContainer"/>'s lookup table.
        /// It is very important that you never use this member to create
        /// element names.  Always use the <see cref="LogicalType.TypeAccessor"/>.  
        /// This is necessary for Literal Arrays (the type accessor is ArrayOfFoo 
        /// and the type name is Foo[]).
        /// </summary>
        public string Namespace;
        /// <summary>
        /// Whether this type has an XmlTypeAttribute.Namespace applied to it, making this particular reflected instance of the type portable to various containing types' default namespaces.
        /// </summary>
        public bool NamespaceIsPortable;
        /// <summary>
        /// The CLR type that was reflected over to create this LogicalType.
        /// </summary>
        public Type Type;
        /// <summary>
        /// A hint as to what code path serialization will take when serializing this type.
        /// </summary>
        public SerializerType Serializer;
        /// <summary>
        /// A hint as to what type among some standard xml primitives this LogicalType matches.
        /// </summary>
        public TypeID TypeID;
        /// <summary>
        /// Indication regarding any special-handling the serializer must take with this type.
        /// </summary>
        public CustomSerializerType CustomSerializer = CustomSerializerType.None;
        /// <summary>
        /// The accessor that describes the default namespace that was used for reflecting over this type.
        /// </summary>
        public Accessor TypeAccessor;
        /// <summary>
        /// The accessor to be used if this type is serialized as the root element in an xml document.
        /// </summary>
        public Accessor RootAccessor;
        /// <summary>
        /// Members declared within a complex type (struct or class with members)
        /// </summary>
        public MemberValueCollection Members;
        /// <summary>
        /// Members that should be added after reflection of this type is completed.
        /// </summary>
        /// <remarks>
        /// This delayed adding of members is useful for bootstrapping type reflection
        /// in cases of some circular references between types.
        /// </remarks>
        public Queue<AddMembersLateDelegate> MembersToAddLate;
        
        /// <summary>
        /// Which lookup tables this LogicalType may be added to in the TypeContainer.
        /// </summary>
        public TypeMappingFlags MappingFlags;

        /// <summary>
        /// Whether the type is nullable (reference type, nullable value type, etc.)
        /// </summary>
        public bool IsNullable;
        /// <summary>
        /// Whether the type is a nullable value type (Nullable&lt;T&gt;)
        /// </summary>
        public bool IsNullableType;
        /// <summary>
        /// The LogicalType representing the T in the Nullable<T> type.
        /// </summary>
        public LogicalType NullableValueType;

        /// <summary>
        /// Whether the XmlTypeAttribute.AnonymousType property was set to true on this type.
        /// </summary>
        public bool IsAnonymousType;

        /// This bad constructor should only be used to make an argument forinitalizeArray.
        public LogicalType() { }

        /// Keep changes here in sync with TypeManager::initializeArrayLike.
        public LogicalType(string name, string ns, bool namespaceIsPortable, Type type, bool isNullable,
            SerializerType serializer, TypeID typeID, TypeMappingFlags mappingFlags)
        {
            Name = name;
            Namespace = ns;
            NamespaceIsPortable = namespaceIsPortable;
            Type = type;
            ValidateSecurity(type.GetTypeInfo());
            IsNullable = isNullable;
            Serializer = serializer;
            TypeID = typeID;

            // The accessor's namespace should never be null.  If the namespace we've
            // been passed is null, then this is probably encoded semantics, and we're
            // in a portable namespace, and the default namespace is the empty string.
            Debug.Assert(ns != null || namespaceIsPortable);
            TypeAccessor = new Accessor(name, ns ?? "", this, isNullable, false);
            RootAccessor = TypeAccessor;

            MappingFlags = mappingFlags;
        }

        public LogicalType(string name, string ns, bool namespaceIsPortable, Type type, bool isNullable,
            string rootName, string rootNS, bool rootIsNullable, SerializerType serializer,
            TypeID typeID, TypeMappingFlags mappingFlags)
            : this(name, ns, namespaceIsPortable, type, isNullable, serializer, typeID, mappingFlags)
        {
            RootAccessor = new Accessor(rootName, rootNS, this, rootIsNullable, false);
        }


        internal static void ValidateSecurity(MemberInfo member)
        {
#if !FEATURE_LEGACYNETCF
            if((member!=null) && (!member.IsVisibleForTransparentAssemblies()))
            {
                throw new SecurityException();
            }
            else
#endif
            return;
        }
    }

    /// <summary>
    /// This class represents a Enum type. This class maps a QNames<->enum
    /// values, enum names to enum values, and enum values to enum names. It
    /// also contains support for Enum.Parse().
    /// </summary>
    /// <remarks>
    /// DESIGN NOTE - Subclassing to extend should usually be avoided, but its
    /// more or less the right thing to do here. Other designs would include
    /// lots of virtual methods, double dispatching, and the strategy pattern.
    /// All of these things are expensive on the .NetCF platform.
    /// </remarks>
    internal class LogicalEnum : LogicalType
    {
        /// <summary>True if the enum has the System.FlagsAttribute attribute.</summary>
        private bool _isFlag;
        /// <summary>Maps enum values to enum names</summary>
        private Hashtable _valueToName = new Hashtable();
        /// <summary>Maps enum names to enum values</summary>
        private Hashtable _nameToValue = new Hashtable();
        /// <summary>Maps enum qnames to enum values</summary>
        private QNameContainer _QNameToValue = new QNameContainer();
        /// <summary>Determines whether the underlying type is either a ulong</summary>
        private bool _underlyingTypeIsULong;

        public LogicalEnum(string name, string ns, Type type, bool isNullable, bool isFlag)
            : base(name, ns, true/*namespaceIsPortable*/, type, isNullable, SerializerType.Enumerated, TypeID.Compound, TypeMappingFlags.Default)
        {
            Debug.Assert(type.GetTypeInfo().IsEnum, "Not an enum");
            _isFlag = isFlag;
            _underlyingTypeIsULong = Enum.GetUnderlyingType(type) == typeof(ulong) && Enum.GetUnderlyingType(type) == typeof(UInt64);
        }

        public LogicalEnum(string name, string ns, Type type, bool isNullable, string rootName, string rootNS, bool rootIsNullable, bool isFlag)
            : base(name, ns, true/*namespaceIsPortable*/, type, isNullable, rootName, rootNS, rootIsNullable, SerializerType.Enumerated, TypeID.Compound, TypeMappingFlags.Default)
        {
            Debug.Assert(type.GetTypeInfo().IsEnum, "Not an enum");
            _isFlag = isFlag;
            _underlyingTypeIsULong = Enum.GetUnderlyingType(type) == typeof(ulong) && Enum.GetUnderlyingType(type) == typeof(UInt64);
        }

        /// <summary>
        /// Finds the enum instance that maps to a given enum name. Answers the
        /// question, "What Enum instance does the string "MyEnumValue" map to?"
        /// </summary>
        public object findValue(string name)
        {
            //
            // If the name is null/empty then simply return an enum with
            // zero value
            //
            if (name == null || name.Length == 0)
            {
                return Enum.ToObject(Type, 0);
            }

            //
            // We run the same code for both flag enums and non-flag enums.
            // We do this because flags and non-flags follow the same format.
            // So, there is no need to handle them seperately. Algo: Split the
            // name into tokens with the name, add the values together, and
            // return it.
            //
            ulong currentULongValue = 0;
            long currentValue = 0;
            string[] tokens = _isFlag ? name.Split() : new string[] { name };

            for (int i = 0; i < tokens.Length; ++i)
            {
                Enum enumValue = (Enum)doFindValue(tokens[i]);
                if (_underlyingTypeIsULong)
                    currentULongValue |= Convert.ToUInt64(enumValue, NumberFormatInfo.InvariantInfo);
                else
                    currentValue |= Convert.ToInt64(enumValue, NumberFormatInfo.InvariantInfo);
            }
            if (_underlyingTypeIsULong)
                return Enum.ToObject(Type, currentULongValue);
            else
                return Enum.ToObject(Type, currentValue);
        }

        /// <summary>
        /// Finds the enum instance that maps to a given qname. Answers the question,
        /// "What enum instance does the string 'MyEnumValue:http://MyEnum' map to?"
        /// </summary>
        public object findValue(XmlQualifiedName enumQName)
        {
            if (enumQName == null || enumQName == XmlQualifiedName.Empty)
            {
                return Enum.ToObject(Type, 0);
            }

            object enumValue = _QNameToValue[enumQName];
            if (enumValue == null)
                throw new InvalidOperationException(SR.Format(SR.XmlS_MissingEnum_1, enumQName));
            return enumValue;
        }

        /// <summary>
        /// Helps finds the enum instance that maps to a given enum name. Answers the
        /// question, "What Enum instance does the string "MyEnumValue" map to?"
        /// </summary>
        public object doFindValue(string name)
        {
            object ret = _nameToValue[name];
            if (ret == null)
            {
                Debug.WriteLine("lookup failed for " + name);
                throw new InvalidOperationException(SR.Format(SR.XmlUnknownConstant, name, Type.FullName));
            }
            return ret;
        }

        /// <summary>
        /// Finds the enum name for the given enum instance. Answers the question,
        /// "What enum name does MyEnum.Blah map to?"
        /// </summary>
        public string findName(object value)
        {
            if (_isFlag)
            {
                // Check if there is an exact match for the value in the value to
                // name hashtable. If so, just return that name. If not we have
                // to walk the values and check for or'ed values.
                string ret = (string)_valueToName[value];
                if (ret != null)
                    return ret;

                // for each value in this enum, bitwise-AND it with inValue.
                // If the result is equal to the enum value, then include the enum
                // name in the string.
                StringBuilder sb = new StringBuilder();
                ulong inValue = ToUInt64(value);
                foreach (var ent in _nameToValue)
                {
                    ulong curEntryValue = ToUInt64(ent.Value);
                    if (curEntryValue != 0 && curEntryValue == (inValue & curEntryValue))
                    {
                        if (sb.Length != 0)
                            sb.Append(" ");
                        sb.Append(ent.Key);
                    }
                }

                return sb.ToString();
            }
            else
            {
                string ret = (string)_valueToName[value];
                if (ret == null)
                {
                    Debug.WriteLine("lookup failed for " + value);
                    throw new InvalidOperationException(SR.Format(SR.XmlS_UnknownEnum_1, value));
                }
                return ret;
            }
        }

        /// <summary>
        /// Converts a CLR value to a UInt64.
        /// </summary>
        private static ulong ToUInt64(Object value)
        {
            // Helper function to silently convert the value to UInt64 from the other base types for enum without throwing an exception.
            // This is need since the Convert functions do overflow checks.
            TypeCode typeCode = value.GetType().GetTypeCode();
            ulong result;

            switch (typeCode)
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    result = (UInt64)Convert.ToInt64(value, NumberFormatInfo.InvariantInfo);
                    break;

                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    result = Convert.ToUInt64(value, NumberFormatInfo.InvariantInfo);
                    break;

                default:
                    // All unsigned types will be directly cast
                    throw new InvalidOperationException(SR.Format(SR.XmlS_UnknownEnum_1, value));
            }
            return result;
        }

        /// <summary>
        /// Adds a mapping between the enum name, including namespace, and the enum
        /// instance.
        /// </summary>
        public void AddEnumMapping(string enumName, string ns, object enumValue)
        {
            string localName, enumNamespace;
            SplitName(enumName, out localName, out enumNamespace);
            if (true /* AppDomain.CompatVersion >= AppDomain.NetCFV37 */)
            {
                if (enumNamespace == null)
                    enumNamespace = ns;
            }
            else
            {
                if (enumNamespace == null || enumNamespace.Length <= 0)
                    enumNamespace = ns;
            }
            XmlQualifiedName enumQName = new XmlQualifiedName(localName, enumNamespace);

            if (_valueToName.ContainsKey(enumValue))
                throw new InvalidOperationException(SR.Format(SR.XmlS_TwoMappings_1, enumValue));
            if (_nameToValue.ContainsKey(enumName))
                throw new InvalidOperationException(SR.Format(SR.XmlS_TwoMappings_1, enumName));
            if (_QNameToValue.ContainsKey(enumQName))
                throw new InvalidOperationException(SR.Format(SR.XmlS_TwoMappings_1, enumQName));

            _valueToName[enumValue] = enumName;
            _nameToValue[enumName] = enumValue;
            _QNameToValue[enumQName] = enumValue;
        }

        /// <summary>
        /// Splits a qname into name and namespace. If the qname does not contain a
        /// namespace then name = qname and ns = null. If the qname is the special
        /// string "##any:", then name = "##any:" and ns = null. Otherwise name and
        /// namespace will be initialized correctly.
        /// </summary>
        private static void SplitName(string qName, out string name, out string ns)
        {
            if (qName == null || qName.Length <= 0 || qName.IndexOf(':') == -1 || qName == "##any:")
            {
                name = qName;
                ns = null;
                return;
            }

            int i = qName.Length - 1;

            StringBuilder nameSb = new StringBuilder();
            while (i >= 0)
            {
                if (qName[i] == ':')
                {
                    --i;
                    break;
                }

                nameSb.Insert(0, new char[] { qName[i] }, 0, 1);
                --i;
            }
            name = nameSb.ToString();

            StringBuilder nsSb = new StringBuilder();
            while (i >= 0)
            {
                nsSb.Insert(0, new char[] { qName[i] }, 0, 1);
                --i;
            }
            ns = nsSb.ToString();
        }
    }

    internal class LogicalCollection : LogicalType
    {
        // I lose here.  The fetcher actually needs to take both an index and a
        // target (unlike all other properties).  I can't use my *wonderful*
        // fetchers and fixups here.
        internal PropertyInfo Indexer;
        internal MethodInfo AddMethod;

        public LogicalCollection(PropertyInfo indexer, MethodInfo addMethod)
        {
            Indexer = indexer;
            AddMethod = addMethod;
        }


        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public LogicalCollection(string name, string ns, bool namespaceIsPortable, Type type, bool isNullable,
                                  PropertyInfo indexer, MethodInfo addMethod)
            : base(name, ns, namespaceIsPortable, type, isNullable, SerializerType.Collection,
            TypeID.ArrayLike, TypeMappingFlags.AllowTypeMapping)
        {
            Indexer = indexer;
            AddMethod = addMethod;
        }
    }

    /// <summary>
    /// Represents a reflected type that implements IEnumerable.
    /// It stores the MethodInfo to the type's Add method required to
    /// fill the enumerable collection with deserialized values.
    /// </summary>
    internal class LogicalEnumerable : LogicalType
    {
        internal MethodInfo AddMethod;

        public LogicalEnumerable(MethodInfo addMethod)
        {
            AddMethod = addMethod;
        }


        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public LogicalEnumerable(string name, string ns, bool namespaceIsPortable, Type type,
            bool isNullable, MethodInfo addMethod)
            : base(name, ns, namespaceIsPortable, type, isNullable, SerializerType.Enumerable,
            TypeID.ArrayLike, TypeMappingFlags.AllowTypeMapping)
        {
            AddMethod = addMethod;
        }
    }

    /// <summary>
    /// Describes how a particular type member will be serialized within the context
    /// of its containing type.
    /// </summary>
    /// <remarks>
    /// Although <see cref="LogicalType"/>s have name and namespace fields tracking
    /// how a type should be named, an accessor's Name and Namespace properties
    /// are what are actually used when a given LogicalType is serialized within
    /// the xml of a containing LogicalType.
    /// This is because a type can be renamed by a containing type using
    /// XmlElementAttribute.Name(space).
    /// Even for root types, the RootAccessor will be the one to determine the 
    /// actual name and namespace of the xml node.
    /// </remarks>
    internal class Accessor
    {
        private string _name;
        /// <summary>
        /// The namespace to be applied to the node.
        /// </summary>
        public string Namespace;
        /// <summary>
        /// The type of the 
        /// </summary>
        public LogicalType Type;
        public bool IsNullable;
        public bool IsAttribute;
        /// <summary>
        /// If Type is an array, and this is true, then this will serialize as an
        /// array.  Otherwise, it will serialize as a repeated element.
        /// </summary>
        public bool AllowRepeats;
        /// <summary>
        /// This is for arrays only.  These contain accessors for one nesting
        /// level down from this one.  So, if this is a rooted array, this will
        /// have all the accessors that are valid for the sub elements.  Those,
        /// in turn will have all the accessors legal for level 2, etc...
        /// If this is a rootless array, than the default gives the encoding for
        /// sub elements.
        /// </summary>
        public AccessorCollection NestedAccessors = null;
        /// <summary>
        /// this is set to true if it is an accessor for a header
        /// </summary>
        public bool IsHeader;
        /// <summary>
        /// this is set to true if it is an accessor for a header that has no corresponding member.
        /// </summary>
        public bool IsDynamicHeader;
        internal Accessor m_nextAccessor;
        /// <summary>
        /// The local name to use for an xml node when using encoded semantics.
        /// </summary>
        public string EncodedName;
        /// <summary>
        /// The namespace to use for an xml node when using encoded semantics for 'ns' in
        /// soapenc:arrayType=ns:somename.
        /// </summary>
        /// <remarks>
        /// At the moment, encoded namespaces seem to only have special application for array types.
        /// But at some future time if they are needed elsewhere, this member could be renamed to
        /// just EncodedNamespace as long as it would be guaranteed to be set everywhere it could be used.
        /// </remarks>
        public string EncodedNamespaceForArrays;
        public bool IsXmlTextArrayElement;
        public bool IsXmlAnyArrayElement;
        public bool IsXmlAnyElement;
        public bool IsXmlArrayItem;

        /// <summary>
        /// Empty constructor to be used in the copy() method.
        /// </summary>
        private Accessor()
        {
        }

        /// <summary>
        /// Creates a new Accessor using the specified properties.
        /// </summary>
        public Accessor(string name, string ns, LogicalType type, bool nullable, bool isAttribute)
        {
            Debug.Assert(type != null);
            _name = name;
            Namespace = ns;
            Type = type;
            IsNullable = nullable;
            IsAttribute = isAttribute;
            AllowRepeats = false;
            EncodedName = XmlConvert.EncodeLocalName(name);
            IsHeader = false;
            IsDynamicHeader = false;
            IsXmlTextArrayElement = false;
            IsXmlArrayItem = false;
        }

        /// <summary>
        /// Returns a new Accessor that contains copies my properties to the new
        /// Accessor. The accessors in the NestedAccessors property are also copied.
        /// </summary>
        public Accessor copy()
        {
            Accessor ret = new Accessor();
            ret._name = _name;
            ret.Namespace = Namespace;
            ret.Type = Type;
            ret.IsNullable = IsNullable;
            ret.IsAttribute = IsAttribute;
            ret.AllowRepeats = AllowRepeats;
            ret.EncodedName = EncodedName;
            ret.IsHeader = IsHeader;
            ret.IsDynamicHeader = IsDynamicHeader;
            ret.IsXmlTextArrayElement = IsXmlTextArrayElement;
            ret.IsXmlArrayItem = IsXmlArrayItem;

            if (NestedAccessors != null)
            {
                ret.NestedAccessors = new AccessorCollection();
                if (NestedAccessors.Default != null)
                    ret.NestedAccessors.Default = NestedAccessors.Default;

                foreach (Accessor accessor in NestedAccessors)
                {
                    ret.NestedAccessors.add(accessor.copy());
                }
            }

            return ret;
        }

        /// <summary>
        /// Changes the the name of the accessor. This also sets the name of the
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                Debug.Assert(value != null && value.Length > 0, "Setting the accessor name to an empty string");
                _name = value;
                this.EncodedName = XmlConvert.EncodeLocalName(_name);
            }
        }

        /// <summary>
        /// Special handling of the namespace to use for reading/writing arrays, based on the difference
        /// in namespaces between original SOAP and SOAP 1.2.
        /// This function works properly in literal (non-encoded) scenarios as well.
        /// </summary>
        /// <param name="soap12">True for SOAP 1.2 scenarios, false for earlier SOAP or literal styles.</param>
        public string GetEncodedNamespace(bool soap12)
        {
            if (soap12 && EncodedName == Soap.ArrayType && Namespace == Soap.Encoding)
            {
                return Soap12.Encoding;
            }
            else
            {
                return soap12 ? string.Empty : this.Namespace;
            }
        }
    }

    internal class AccessorCollection : IEnumerable
    {
        private Accessor _head;
        private Accessor _default;
        private QNameContainer _byQname = new QNameContainer();
        public XmlChoiceSupport Choice;


        public void prepare()
        {
            _byQname.makeFast();
            foreach (Accessor accessor in this)
            {
                if (accessor.NestedAccessors != null)
                    accessor.NestedAccessors.prepare();
            }
        }

        private class AccessorEnumerator : IEnumerator
        {
            private Accessor _head;
            private Accessor _current;

            public AccessorEnumerator(Accessor head)
            {
                _head = head;
                _current = null;
            }

            public void Reset()
            {
                _current = null;
            }

            public bool MoveNext()
            {
                //empty list
                if (_head == null)
                {
                    return false;
                }
                if (_current == null)
                {
                    _current = _head;
                    return true;
                }
                else if (_current.m_nextAccessor == null)
                {
                    return false;
                }
                else
                {
                    _current = _current.m_nextAccessor;
                    return true;
                }
            }

            public object Current
            {
                get
                {
                    return _current;
                }
            }
        }

        public IEnumerator GetEnumerator()
        {
            return new AccessorEnumerator(_head);
        }

        /// <summary>
        /// Adds an accessor to the collection.  The accessors must be sorted
        /// from most derived to least derived.  This makes selection quick.
        /// </summary>
        public void add(Accessor a)
        {
            Debug.Assert(a != null, "Null accessor");
            if (_head == null)
                _head = a;
            else if (lessThan(a, _head))
            {
                a.m_nextAccessor = _head;
                _head = a;
            }
            else
            {
                Accessor current = _head;
                while (current.m_nextAccessor != null)
                {
                    if (lessThan(a, current.m_nextAccessor))
                        break;
                    current = current.m_nextAccessor;
                }
                a.m_nextAccessor = current.m_nextAccessor;
                current.m_nextAccessor = a;
            }
            XmlQualifiedName accessorQName = new XmlQualifiedName(a.Name, a.Namespace);
            _byQname[accessorQName] = a;

            if (Choice != null)
            {
                Choice.addMapping(a);
            }
        }

        /// <summary>
        /// Adds the default accessor.
        /// </summary>
        public Accessor Default
        {
            get
            {
                return _default;
            }
            set
            {
                Debug.Assert(value != null, "Null accessor");
                if (!_byQname.ContainsKey(new XmlQualifiedName(value.Name, value.Namespace)))
                    add(value);

                _default = value;
            }
        }

        /// <summary>
        /// Finds an Accessor given an element name and namespace.  This is used
        /// for deserialization.
        /// </summary>
        public Accessor findAccessorNoThrow(string name, string ns)
        {
            return (Accessor)_byQname[new XmlQualifiedName(name, ns)];
        }

        /// <summary>
        /// Finds an accessor for the given type.  The accessor is the most
        /// derived type that is still assignable from the specified type.
        /// </summary>
        public Accessor findAccessor(Type t)
        {
            Accessor current = _head;
            while (current != null)
            {
                if (current.Type.Type.IsAssignableFrom(t))
                    return current;
                current = current.m_nextAccessor;
            }
            Debug.WriteLine("Couldn't find accessor for type " + t);
            throw new InvalidOperationException(SR.Format(SR.XmlUnexpectedType, t));
        }

        public Accessor findAccessorNoThrow(Type t)
        {
            Accessor current = _head;
            while (current != null)
            {
                if (current.Type.Type.IsAssignableFrom(t))
                    return current;
                current = current.m_nextAccessor;
            }
            return null;
        }

        /// <summary>
        /// This is used for choice support.  Given a name, it finds the
        /// accessor that matches.  This is not for deserialization.
        /// </summary>

        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public Accessor findAccessor(string name)
        {
            Accessor current = _head;
            while (current != null)
            {
                if (current.Name.Equals(name))
                    return current;
                current = current.m_nextAccessor;
            }
            Debug.WriteLine("Couldn't find accessor for type " + name);
            throw new InvalidOperationException(SR.Format(SR.XmlS_UnknownAccessor_1, name));
        }

        /// <summary>
        /// Accessor A is less than Accessor B if B is derived from A.  In other words,
        /// if A IsAssignableFrom B.
        /// </summary>
        private bool lessThan(Accessor lhs, Accessor rhs)
        {
            return rhs.Type.Type.IsAssignableFrom(lhs.Type.Type);
        }
    }

    /// <summary>
    /// This is a "sorted list" key based container.  Additions to the data
    /// structure are made by appending data to the end of the list.  On the
    /// first access, the container is sorted and pruned to the the exact size
    /// necessary.
    ///
    /// Even though this list is "sorted", you can't use it to iterate through
    /// the items "in order".  They're sorted based on hashcode.
    /// </summary>
    internal class QNameContainer
    {
        private static ListElementComparer s_comparer = new ListElementComparer();      // Static IComparer for the container
        private ListElement[] _list;                                                   // The elements in the container
        private bool _sorted;                                                          // Flag for specifing whether the container is sorted
        private int _count;                                                            // The number of items in the container

        private struct ListElement
        {
            public int HashCode;
            public XmlQualifiedName Key;
            public object Value;

            public ListElement(XmlQualifiedName key)
            {
                HashCode = key.GetHashCode();
                Key = key;
                Value = null;
            }
        }

        private class ListElementComparer : IComparer
        {
            int IComparer.Compare(object a, object b)
            {
                int c;
                ListElement lhs = (ListElement)a;
                ListElement rhs = (ListElement)b;

                if (lhs.HashCode == rhs.HashCode)
                {
                    c = string.Compare(lhs.Key.Name, rhs.Key.Name, StringComparison.Ordinal);
                    if (c == 0)
                    {
                        c = string.Compare(lhs.Key.Namespace, rhs.Key.Namespace, StringComparison.Ordinal);
                    }
                }
                else if (lhs.HashCode < rhs.HashCode)
                {
                    c = -1;
                }
                else
                {
                    c = 1;
                }

                return c;
            }
        }

        public QNameContainer()
        {
            _list = new ListElement[4];
            _count = 0;
            _sorted = false;
        }

        /// <summary>
        /// This function switches the container from slow linear search mode
        /// into fast binary search mode.  This also prunes the container.  Once
        /// it's in fast mode, it is read only.
        /// </summary>
        internal void makeFast()
        {
            if (_count > 3)
            {
                Array.Sort(_list, 0, _count, s_comparer);
                _sorted = true;
            }

            if (_count > 0)
            {
                ListElement[] newList = new ListElement[_count];
                Array.Copy(_list, 0, newList, 0, _count);
                _list = newList;
            }
        }

        /// <summary>
        /// Finds the index in the array of the key.  Returns -1 if it isn't
        /// found. If the list has not been sorted then a linear search is performed.
        /// Therefore, it is more effiecent to sort the list before doing a lookup,
        /// at which time a binary search is performed.
        /// </summary>
        private int doLookup(XmlQualifiedName key)
        {
            if (_sorted)
            {
                ListElement keyElt = new ListElement(key);
                int ret = Array.BinarySearch(_list, 0, _count, keyElt, s_comparer);
                if (ret < 0) return -1;
                return ret;
            }
            else
            {
                for (int i = 0; i < _count; ++i)
                {
                    if (_list[i].Key.Equals(key))
                        return i;
                }
                return -1;
            }
        }


        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        private int doLookup(string localName)
        {
            for (int i = 0; i < _count; ++i)
            {
                if (_list[i].Key.Name.Equals(localName))
                    return i;
            }
            return -1;
        }

        public object this[XmlQualifiedName key]
        {
            get
            {
                int idx = doLookup(key);

                if (idx == -1)
                    return null;
                else
                    return _list[idx].Value;
            }
            set
            {
                Debug.Assert(_sorted == false, "adding to a sorted list");
                int ndx = doLookup(key);
                if (ndx < 0)
                {
                    if (_list.Length == _count)
                    {
                        ListElement[] newList = new ListElement[_count * 2];
                        Array.Copy(_list, 0, newList, 0, _count);
                        _list = newList;
                    }

                    _list[_count].HashCode = key.GetHashCode();
                    _list[_count].Key = key;
                    _list[_count].Value = value;
                    ++_count;
                }
                else
                {
                    _list[ndx].HashCode = key.GetHashCode();
                    _list[ndx].Key = key;
                    _list[ndx].Value = value;
                }
            }
        }

        public object this[string localName]
        {
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            get
            {
                int idx;
                return ((idx = doLookup(localName)) == -1) ? null : _list[idx].Value;
            }
        }

        public bool ContainsKey(XmlQualifiedName key)
        {
            return doLookup(key) != -1;
        }

        private class Enumerator : IEnumerator, IEnumerable
        {
            private ListElement[] _elements;   // Internal list of values
            private int _count;                // Total number of values
            private int _idx;                  // Current index
            private bool _enumValues;          // If true, enumerates values. Enumerates keys otherwise.

            internal Enumerator(ListElement[] elts, int count, bool enumValues)
            {
                Debug.Assert(elts.Length >= count, "The number of elements in the ListElement array is smaller then the specified count parameter");
                Debug.Assert(count >= 0, "The count parameter must be a positive integer");

                _elements = elts;
                _count = count;
                _idx = -1;
                _enumValues = enumValues;
            }

            bool IEnumerator.MoveNext()
            {
                if (_count <= 0)
                    return false;
                return ++_idx < _count;
            }

            void IEnumerator.Reset()
            {
                _idx = -1;
            }

            object IEnumerator.Current
            {
                get
                {
                    if (_idx < 0 || _idx >= _count)
                        throw new InvalidOperationException();
                    return _enumValues ? _elements[_idx].Value : _elements[_idx].Key;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }
        }

        public IEnumerable Values
        {
            get
            {
                return new Enumerator(_list, _count, true);
            }
        }

        public IEnumerable Keys
        {
            get
            {
                return new Enumerator(_list, _count, false);
            }
        }

        public int Count
        {
            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            get
            {
                return _count;
            }
        }
    }
}
