// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;

namespace System.ComponentModel
{
    internal sealed partial class ReflectTypeDescriptionProvider : TypeDescriptionProvider
    {
        /// <summary>
        ///     This class contains all the reflection information for a
        ///     given type.
        /// </summary>
        private class ReflectedTypeData
        {
            private readonly Type _type;
            private AttributeCollection _attributes;
            private EventDescriptorCollection _events;
            private PropertyDescriptorCollection _properties;
            private TypeConverter _converter;
            private object[] _editors;
            private Type[] _editorTypes;
            private int _editorCount;

            internal ReflectedTypeData(Type type)
            {
                _type = type;
            }

            /// <summary>
            ///     This method returns true if the data cache in this reflection 
            ///     type descriptor has data in it.
            /// </summary>
            internal bool IsPopulated
            {
                get
                {
                    return (_attributes != null) | (_events != null) | (_properties != null);
                }
            }

            /// <summary>
            ///     Retrieves custom attributes.
            /// </summary>
            internal AttributeCollection GetAttributes()
            {
                // Worst case collision scenario:  we don't want the perf hit
                // of taking a lock, so if we collide we will query for
                // attributes twice.  Not a big deal.
                //
                if (_attributes == null)
                {
                    // Obtaining attributes follows a very critical order: we must take care that
                    // we merge attributes the right way.  Consider this:
                    //
                    // [A4]
                    // interface IBase;
                    //
                    // [A3]
                    // interface IDerived;
                    //
                    // [A2]
                    // class Base : IBase;
                    //
                    // [A1]
                    // class Derived : Base, IDerived
                    //
                    // Calling GetAttributes on type Derived must merge attributes in the following
                    // order:  A1 - A4.  Interfaces always lose to types, and interfaces and types
                    // must be merged in the same order.  At the same time, we must be careful
                    // that we don't always go through reflection here, because someone could have
                    // created a custom provider for a type.  Because there is only one instance
                    // of ReflectTypeDescriptionProvider created for typeof(object), if our code
                    // is invoked here we can be sure that there is no custom provider for
                    // _type all the way up the base class chain.
                    // We cannot be sure that there is no custom provider for
                    // interfaces that _type implements, however, because they are not derived
                    // from _type.  So, for interfaces, we must go through TypeDescriptor
                    // again to get the interfaces attributes.  

                    // Get the type's attributes. This does not recurse up the base class chain.
                    // We append base class attributes to this array so when walking we will
                    // walk from Length - 1 to zero.
                    //
                    Attribute[] attrArray = ReflectTypeDescriptionProvider.ReflectGetAttributes(_type);
                    Type baseType = _type.GetTypeInfo().BaseType;

                    while (baseType != null && baseType != typeof(object))
                    {
                        Attribute[] baseArray = ReflectTypeDescriptionProvider.ReflectGetAttributes(baseType);
                        Attribute[] temp = new Attribute[attrArray.Length + baseArray.Length];
                        Array.Copy(attrArray, 0, temp, 0, attrArray.Length);
                        Array.Copy(baseArray, 0, temp, attrArray.Length, baseArray.Length);
                        attrArray = temp;
                        baseType = baseType.GetTypeInfo().BaseType;
                    }

                    // Next, walk the type's interfaces.  We append these to
                    // the attribute array as well.
                    //
                    int ifaceStartIdx = attrArray.Length;
                    Type[] interfaces = _type.GetTypeInfo().GetInterfaces();
                    for (int idx = 0; idx < interfaces.Length; idx++)
                    {
                        Type iface = interfaces[idx];

                        // only do this for public interfaces.
                        //
                        if ((iface.GetTypeInfo().Attributes & (TypeAttributes.Public | TypeAttributes.NestedPublic)) != 0)
                        {
                            // No need to pass an instance into GetTypeDescriptor here because, if someone provided a custom
                            // provider based on object, it already would have hit.
                            AttributeCollection ifaceAttrs = TypeDescriptor.GetAttributes(iface);
                            if (ifaceAttrs.Count > 0)
                            {
                                Attribute[] temp = new Attribute[attrArray.Length + ifaceAttrs.Count];
                                Array.Copy(attrArray, 0, temp, 0, attrArray.Length);
                                ifaceAttrs.CopyTo(temp, attrArray.Length);
                                attrArray = temp;
                            }
                        }
                    }

                    // Finally, put all these attributes in a dictionary and filter out the duplicates.
                    //
                    OrderedDictionary attrDictionary = new OrderedDictionary(attrArray.Length);

                    for (int idx = 0; idx < attrArray.Length; idx++)
                    {
                        bool addAttr = true;
                        if (idx >= ifaceStartIdx)
                        {
                            for (int ifaceSkipIdx = 0; ifaceSkipIdx < s_skipInterfaceAttributeList.Length; ifaceSkipIdx++)
                            {
                                if (s_skipInterfaceAttributeList[ifaceSkipIdx].GetTypeInfo().IsInstanceOfType(attrArray[idx]))
                                {
                                    addAttr = false;
                                    break;
                                }
                            }
                        }

                        if (addAttr && !attrDictionary.Contains(attrArray[idx].TypeId))
                        {
                            attrDictionary[attrArray[idx].TypeId] = attrArray[idx];
                        }
                    }

                    attrArray = new Attribute[attrDictionary.Count];
                    attrDictionary.Values.CopyTo(attrArray, 0);
                    _attributes = new AttributeCollection(attrArray);
                }

                return _attributes;
            }

            /// <summary>
            ///     Retrieves the class name for our type.
            /// </summary>
            internal string GetClassName(object instance)
            {
                return _type.FullName;
            }

            /// <summary>
            ///     Retrieves the component name from the site.
            /// </summary>
            internal string GetComponentName(object instance)
            {
                IComponent comp = instance as IComponent;
                if (comp != null)
                {
                    ISite site = comp.Site;
                    if (site != null)
                    {
                        INestedSite nestedSite = site as INestedSite;
                        if (nestedSite != null)
                        {
                            return nestedSite.FullName;
                        }
                        else
                        {
                            return site.Name;
                        }
                    }
                }

                return null;
            }

            /// <summary>
            ///     Retrieves the type converter.  If instance is non-null,
            ///     it will be used to retrieve attributes.  Otherwise, _type
            ///     will be used.
            /// </summary>
            internal TypeConverter GetConverter(object instance)
            {
                TypeConverterAttribute typeAttr = null;

                // For instances, the design time object for them may want to redefine the
                // attributes.  So, we search the attribute here based on the instance.  If found,
                // we then search on the same attribute based on type.  If the two don't match, then
                // we cannot cache the value and must re-create every time.  It is rare for a designer
                // to override these attributes, so we want to be smart here.
                //
                if (instance != null)
                {
                    typeAttr = (TypeConverterAttribute)TypeDescriptor.GetAttributes(_type)[typeof(TypeConverterAttribute)];
                    TypeConverterAttribute instanceAttr = (TypeConverterAttribute)TypeDescriptor.GetAttributes(instance)[typeof(TypeConverterAttribute)];
                    if (typeAttr != instanceAttr)
                    {
                        Type converterType = GetTypeFromName(instanceAttr.ConverterTypeName);
                        if (converterType != null && typeof(TypeConverter).GetTypeInfo().IsAssignableFrom(converterType))
                        {
                            return (TypeConverter)ReflectTypeDescriptionProvider.CreateInstance(converterType, _type);
                        }
                    }
                }

                // If we got here, we return our type-based converter.
                //
                if (_converter == null)
                {
                    if (typeAttr == null)
                    {
                        typeAttr = (TypeConverterAttribute)TypeDescriptor.GetAttributes(_type)[typeof(TypeConverterAttribute)];
                    }

                    if (typeAttr != null)
                    {
                        Type converterType = GetTypeFromName(typeAttr.ConverterTypeName);
                        if (converterType != null && typeof(TypeConverter).GetTypeInfo().IsAssignableFrom(converterType))
                        {
                            _converter = (TypeConverter)ReflectTypeDescriptionProvider.CreateInstance(converterType, _type);
                        }
                    }

                    if (_converter == null)
                    {
                        // We did not get a converter.  Traverse up the base class chain until
                        // we find one in the stock hashtable.
                        //
                        _converter = (TypeConverter)ReflectTypeDescriptionProvider.SearchIntrinsicTable(IntrinsicTypeConverters, _type);
                        Debug.Assert(_converter != null, "There is no intrinsic setup in the hashtable for the Object type");
                    }
                }

                return _converter;
            }

            /// <summary>
            ///     Return the default event. The default event is determined by the
            ///     presence of a DefaultEventAttribute on the class.
            /// </summary>
            internal EventDescriptor GetDefaultEvent(object instance)
            {
                AttributeCollection attributes;

                if (instance != null)
                {
                    attributes = TypeDescriptor.GetAttributes(instance);
                }
                else
                {
                    attributes = TypeDescriptor.GetAttributes(_type);
                }

                DefaultEventAttribute attr = (DefaultEventAttribute)attributes[typeof(DefaultEventAttribute)];
                if (attr != null && attr.Name != null)
                {
                    if (instance != null)
                    {
                        return TypeDescriptor.GetEvents(instance)[attr.Name];
                    }
                    else
                    {
                        return TypeDescriptor.GetEvents(_type)[attr.Name];
                    }
                }

                return null;
            }

            /// <summary>
            ///     Return the default property.
            /// </summary>
            internal PropertyDescriptor GetDefaultProperty(object instance)
            {
                AttributeCollection attributes;

                if (instance != null)
                {
                    attributes = TypeDescriptor.GetAttributes(instance);
                }
                else
                {
                    attributes = TypeDescriptor.GetAttributes(_type);
                }

                DefaultPropertyAttribute attr = (DefaultPropertyAttribute)attributes[typeof(DefaultPropertyAttribute)];
                if (attr != null && attr.Name != null)
                {
                    if (instance != null)
                    {
                        return TypeDescriptor.GetProperties(instance)[attr.Name];
                    }
                    else
                    {
                        return TypeDescriptor.GetProperties(_type)[attr.Name];
                    }
                }

                return null;
            }

            /// <summary>
            ///     Retrieves the editor for the given base type.
            /// </summary>
            internal object GetEditor(object instance, Type editorBaseType)
            {
                EditorAttribute typeAttr;

                // For instances, the design time object for them may want to redefine the
                // attributes.  So, we search the attribute here based on the instance.  If found,
                // we then search on the same attribute based on type.  If the two don't match, then
                // we cannot cache the value and must re-create every time.  It is rare for a designer
                // to override these attributes, so we want to be smart here.
                //
                if (instance != null)
                {
                    typeAttr = GetEditorAttribute(TypeDescriptor.GetAttributes(_type), editorBaseType);
                    EditorAttribute instanceAttr = GetEditorAttribute(TypeDescriptor.GetAttributes(instance), editorBaseType);
                    if (typeAttr != instanceAttr)
                    {
                        Type editorType = GetTypeFromName(instanceAttr.EditorTypeName);
                        if (editorType != null && editorBaseType.GetTypeInfo().IsAssignableFrom(editorType))
                        {
                            return ReflectTypeDescriptionProvider.CreateInstance(editorType, _type);
                        }
                    }
                }

                // If we got here, we return our type-based editor.
                //
                lock (this)
                {
                    for (int idx = 0; idx < _editorCount; idx++)
                    {
                        if (_editorTypes[idx] == editorBaseType)
                        {
                            return _editors[idx];
                        }
                    }
                }

                // Editor is not cached yet.  Look in the attributes.
                //
                object editor = null;

                typeAttr = GetEditorAttribute(TypeDescriptor.GetAttributes(_type), editorBaseType);
                if (typeAttr != null)
                {
                    Type editorType = GetTypeFromName(typeAttr.EditorTypeName);
                    if (editorType != null && editorBaseType.GetTypeInfo().IsAssignableFrom(editorType))
                    {
                        editor = ReflectTypeDescriptionProvider.CreateInstance(editorType, _type);
                    }
                }

                // Editor is not in the attributes.  Search intrinsic tables.
                //
                if (editor == null)
                {
                    Hashtable intrinsicEditors = ReflectTypeDescriptionProvider.GetEditorTable(editorBaseType);
                    if (intrinsicEditors != null)
                    {
                        editor = ReflectTypeDescriptionProvider.SearchIntrinsicTable(intrinsicEditors, _type);
                    }

                    // As a quick sanity check, check to see that the editor we got back is of 
                    // the correct type.
                    //
                    if (editor != null && !editorBaseType.GetTypeInfo().IsInstanceOfType(editor))
                    {
                        Debug.Fail("Editor " + editor.GetType().FullName + " is not an instance of " + editorBaseType.FullName + " but it is in that base types table.");
                        editor = null;
                    }
                }

                if (editor != null)
                {
                    lock (this)
                    {
                        if (_editorTypes == null || _editorTypes.Length == _editorCount)
                        {
                            int newLength = (_editorTypes == null ? 4 : _editorTypes.Length * 2);

                            Type[] newTypes = new Type[newLength];
                            object[] newEditors = new object[newLength];

                            if (_editorTypes != null)
                            {
                                _editorTypes.CopyTo(newTypes, 0);
                                _editors.CopyTo(newEditors, 0);
                            }

                            _editorTypes = newTypes;
                            _editors = newEditors;

                            _editorTypes[_editorCount] = editorBaseType;
                            _editors[_editorCount++] = editor;
                        }
                    }
                }

                return editor;
            }

            /// <summary>
            ///     Helper method to return an editor attribute of the correct base type.
            /// </summary>
            private static EditorAttribute GetEditorAttribute(AttributeCollection attributes, Type editorBaseType)
            {
                foreach (Attribute attr in attributes)
                {
                    EditorAttribute edAttr = attr as EditorAttribute;
                    if (edAttr != null)
                    {
                        Type attrEditorBaseType = Type.GetType(edAttr.EditorBaseTypeName);

                        if (attrEditorBaseType != null && attrEditorBaseType == editorBaseType)
                        {
                            return edAttr;
                        }
                    }
                }

                return null;
            }

            /// <summary>
            ///     Retrieves the events for this type.
            /// </summary>
            internal EventDescriptorCollection GetEvents()
            {
                // Worst case collision scenario:  we don't want the perf hit
                // of taking a lock, so if we collide we will query for
                // events twice.  Not a big deal.
                //
                if (_events == null)
                {
                    EventDescriptor[] eventArray;
                    Dictionary<string, EventDescriptor> eventList = new Dictionary<string, EventDescriptor>(16);
                    Type baseType = _type;
                    Type objType = typeof(object);

                    do
                    {
                        eventArray = ReflectGetEvents(baseType);
                        foreach (EventDescriptor ed in eventArray)
                        {
                            if (!eventList.ContainsKey(ed.Name))
                            {
                                eventList.Add(ed.Name, ed);
                            }
                        }
                        baseType = baseType.GetTypeInfo().BaseType;
                    }
                    while (baseType != null && baseType != objType);

                    eventArray = new EventDescriptor[eventList.Count];
                    eventList.Values.CopyTo(eventArray, 0);
                    _events = new EventDescriptorCollection(eventArray, true);
                }

                return _events;
            }

            /// <summary>
            ///     Retrieves the properties for this type.
            /// </summary>
            internal PropertyDescriptorCollection GetProperties()
            {
                // Worst case collision scenario:  we don't want the perf hit
                // of taking a lock, so if we collide we will query for
                // properties twice.  Not a big deal.
                //
                if (_properties == null)
                {
                    PropertyDescriptor[] propertyArray;
                    Dictionary<string, PropertyDescriptor> propertyList = new Dictionary<string, PropertyDescriptor>(10);
                    Type baseType = _type;
                    Type objType = typeof(object);

                    do
                    {
                        propertyArray = ReflectGetProperties(baseType);
                        foreach (PropertyDescriptor p in propertyArray)
                        {
                            if (!propertyList.ContainsKey(p.Name))
                            {
                                propertyList.Add(p.Name, p);
                            }
                        }
                        baseType = baseType.GetTypeInfo().BaseType;
                    }
                    while (baseType != null && baseType != objType);

                    propertyArray = new PropertyDescriptor[propertyList.Count];
                    propertyList.Values.CopyTo(propertyArray, 0);
                    _properties = new PropertyDescriptorCollection(propertyArray, true);
                }

                return _properties;
            }

            /// <summary>
            ///     Retrieves a type from a name.  The Assembly of the type
            ///     that this PropertyDescriptor came from is first checked,
            ///     then a global Type.GetType is performed.
            /// </summary>
            private Type GetTypeFromName(string typeName)
            {
                if (typeName == null || typeName.Length == 0)
                {
                    return null;
                }

                int commaIndex = typeName.IndexOf(',');
                Type t = null;

                if (commaIndex == -1)
                {
                    t = _type.GetTypeInfo().Assembly.GetType(typeName);
                }

                if (t == null)
                {
                    t = Type.GetType(typeName);
                }

                if (t == null && commaIndex != -1)
                {
                    // At design time, it's possible for us to reuse
                    // an assembly but add new types.  The app domain
                    // will cache the assembly based on identity, however,
                    // so it could be looking in the previous version
                    // of the assembly and not finding the type.  We work
                    // around this by looking for the non-assembly qualified
                    // name, which causes the domain to raise a type 
                    // resolve event.
                    //
                    t = Type.GetType(typeName.Substring(0, commaIndex));
                }

                return t;
            }

            /// <summary>
            ///     Refreshes the contents of this type descriptor.  This does not
            ///     actually requery, but it will clear our state so the next
            ///     query re-populates.
            /// </summary>
            internal void Refresh()
            {
                _attributes = null;
                _events = null;
                _properties = null;
                _converter = null;
                _editors = null;
                _editorTypes = null;
                _editorCount = 0;

            }
        }
    }
}
