// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization
{
    public class ImportContext
    {
        private readonly bool _shareTypes;
        private SchemaObjectCache? _cache; // cached schema top-level items
        private Hashtable? _mappings; // XmlSchema -> SerializableMapping, XmlSchemaSimpleType -> EnumMapping, XmlSchemaComplexType -> StructMapping
        private Hashtable? _elements; // XmlSchemaElement -> ElementAccessor
        private CodeIdentifiers? _typeIdentifiers;

        public ImportContext(CodeIdentifiers? identifiers, bool shareTypes)
        {
            _typeIdentifiers = identifiers;
            _shareTypes = shareTypes;
        }

        internal ImportContext() : this(null, false) { }

        internal SchemaObjectCache Cache => _cache ??= new SchemaObjectCache();

        internal Hashtable Elements => _elements ??= new Hashtable();

        internal Hashtable Mappings => _mappings ??= new Hashtable();

        public CodeIdentifiers TypeIdentifiers => _typeIdentifiers ??= new CodeIdentifiers();

        public bool ShareTypes
        {
            get { return _shareTypes; }
        }

        public StringCollection Warnings
        {
            get { return Cache.Warnings; }
        }
    }

    internal sealed class SchemaObjectCache
    {
        private Hashtable? _graph;
        private Hashtable? _hash;
        private Hashtable? _objectCache;
        private StringCollection? _warnings;
        // UNDONE remove me soon, this is debug only code
        internal Hashtable looks = new Hashtable();
        private Hashtable Graph => _graph ??= new Hashtable();

        private Hashtable Hash => _hash ??= new Hashtable();

        private Hashtable ObjectCache => _objectCache ??= new Hashtable();

        internal StringCollection Warnings => _warnings ??= new StringCollection();

        internal XmlSchemaObject? AddItem(XmlSchemaObject? item, XmlQualifiedName? qname)
        {
            if (item == null)
                return null;
            if (qname == null || qname.IsEmpty)
                return null;

            string key = $"{item.GetType().Name}:{qname}";
            ArrayList? list = (ArrayList?)ObjectCache[key];
            if (list == null)
            {
                list = new ArrayList();
                ObjectCache[key] = list;
            }

            for (int i = 0; i < list.Count; i++)
            {
                XmlSchemaObject cachedItem = (XmlSchemaObject)list[i]!;
                if (cachedItem == item)
                    return cachedItem;

                if (Match(cachedItem, item, true))
                {
                    return cachedItem;
                }
                else
                {
                    Warnings.Add(SR.Format(SR.XmlMismatchSchemaObjects, item.GetType().Name, qname.Name, qname.Namespace));
                    Warnings.Add($"DEBUG:Cached item key:\r\n{(string?)looks[cachedItem]}\r\nnew item key:\r\n{(string?)looks[item]}");
                }
            }
            // no match found we need to insert the new type in the cache
            list.Add(item);
            return item;
        }

        internal bool Match(XmlSchemaObject o1, XmlSchemaObject o2, bool shareTypes)
        {
            if (o1 == o2)
                return true;
            if (o1.GetType() != o2.GetType())
                return false;

            Hash[o1] ??= GetHash(o1);
            int hash1 = (int)Hash[o1]!;
            int hash2 = GetHash(o2);
            if (hash1 != hash2)
                return false;

            if (shareTypes)
                return CompositeHash(o1) == CompositeHash(o2);
            return true;
        }

        private ArrayList GetDependencies(XmlSchemaObject o, ArrayList deps, Hashtable refs)
        {
            if (refs[o] == null)
            {
                refs[o] = o;
                deps.Add(o);
                ArrayList? list = Graph[o] as ArrayList;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        GetDependencies((XmlSchemaObject)list[i]!, deps, refs);
                    }
                }
            }
            return deps;
        }

        private int CompositeHash(XmlSchemaObject o)
        {
            ArrayList list = GetDependencies(o, new ArrayList(), new Hashtable());
            double tmp = 0;
            for (int i = 0; i < list.Count; i++)
            {
                object? cachedHash = Hash[list[i]!];
                if (cachedHash is int)
                {
                    tmp += (int)cachedHash / list.Count;
                }
            }
            return (int)tmp;
        }

        [RequiresUnreferencedCode("creates SchemaGraph")]
        internal void GenerateSchemaGraph(XmlSchemas schemas)
        {
            SchemaGraph graph = new SchemaGraph(Graph, schemas);
            ArrayList items = graph.GetItems();

            for (int i = 0; i < items.Count; i++)
            {
                GetHash((XmlSchemaObject)items[i]!);
            }
        }

        private int GetHash(XmlSchemaObject o)
        {
            object? hash = Hash[o];
            if (hash != null)
            {
                if (hash is XmlSchemaObject)
                {
                }
                else
                {
                    return (int)hash;
                }
            }
            // new object, generate the hash
            string hashString = ToString(o, new SchemaObjectWriter());
            looks[o] = hashString;
            int code = hashString.GetHashCode();
            Hash[o] = code;
            return code;
        }

        private static string ToString(XmlSchemaObject o, SchemaObjectWriter writer)
        {
            return writer.WriteXmlSchemaObject(o);
        }
    }

    internal sealed class SchemaGraph
    {
        private readonly ArrayList _empty = new ArrayList();
        private readonly XmlSchemas _schemas;
        private readonly Hashtable _scope;
        private readonly int _items;

        [RequiresUnreferencedCode("Calls Compile")]
        internal SchemaGraph(Hashtable scope, XmlSchemas schemas)
        {
            _scope = scope;
            schemas.Compile(null, false);
            _schemas = schemas;
            _items = 0;
            foreach (XmlSchema s in schemas)
            {
                _items += s.Items.Count;
                foreach (XmlSchemaObject item in s.Items)
                {
                    Depends(item);
                }
            }
        }

        internal ArrayList GetItems()
        {
            return new ArrayList(_scope.Keys);
        }

        internal void AddRef(ArrayList list, XmlSchemaObject? o)
        {
            if (o == null)
                return;
            if (_schemas.IsReference(o))
                return;
            if (o.Parent is XmlSchema parent)
            {
                string? ns = parent.TargetNamespace;
                if (ns == XmlSchema.Namespace)
                    return;
                if (list.Contains(o))
                    return;
                list.Add(o);
            }
        }

        internal ArrayList Depends(XmlSchemaObject item)
        {
            if (item.Parent is XmlSchema)
            {
                if (_scope[item] != null)
                    return (ArrayList)_scope[item]!;

                ArrayList refs = new ArrayList();
                Depends(item, refs);
                _scope.Add(item, refs);
                return refs;
            }
            return _empty;
        }

        internal void Depends(XmlSchemaObject? item, ArrayList refs)
        {
            if (item == null || _scope[item] != null)
                return;

            Type t = item.GetType();
            if (typeof(XmlSchemaType).IsAssignableFrom(t))
            {
                XmlQualifiedName baseName = XmlQualifiedName.Empty;
                XmlSchemaType? baseType = null;
                XmlSchemaParticle? particle = null;
                XmlSchemaObjectCollection? attributes = null;

                if (item is XmlSchemaComplexType ct)
                {
                    if (ct.ContentModel != null)
                    {
                        XmlSchemaContent? content = ct.ContentModel.Content;
                        if (content is XmlSchemaComplexContentRestriction)
                        {
                            baseName = ((XmlSchemaComplexContentRestriction)content).BaseTypeName;
                            attributes = ((XmlSchemaComplexContentRestriction)content).Attributes;
                        }
                        else if (content is XmlSchemaSimpleContentRestriction restriction)
                        {
                            if (restriction.BaseType != null)
                                baseType = restriction.BaseType;
                            else
                                baseName = restriction.BaseTypeName;
                            attributes = restriction.Attributes;
                        }
                        else if (content is XmlSchemaComplexContentExtension complex)
                        {
                            attributes = complex.Attributes;
                            particle = complex.Particle;
                            baseName = complex.BaseTypeName;
                        }
                        else if (content is XmlSchemaSimpleContentExtension simple)
                        {
                            attributes = simple.Attributes;
                            baseName = simple.BaseTypeName;
                        }
                    }
                    else
                    {
                        attributes = ct.Attributes;
                        particle = ct.Particle;
                    }
                    if (particle is XmlSchemaGroupRef refGroup)
                    {
                        particle = ((XmlSchemaGroup)_schemas.Find(refGroup.RefName, typeof(XmlSchemaGroup), false)!).Particle;
                    }
                    else if (particle is XmlSchemaGroupBase)
                    {
                        particle = (XmlSchemaGroupBase)particle;
                    }
                }
                else if (item is XmlSchemaSimpleType simpleType)
                {
                    XmlSchemaSimpleTypeContent? content = simpleType.Content;
                    if (content is XmlSchemaSimpleTypeRestriction)
                    {
                        baseType = ((XmlSchemaSimpleTypeRestriction)content).BaseType;
                        baseName = ((XmlSchemaSimpleTypeRestriction)content).BaseTypeName;
                    }
                    else if (content is XmlSchemaSimpleTypeList list)
                    {
                        if (list.ItemTypeName != null && !list.ItemTypeName.IsEmpty)
                            baseName = list.ItemTypeName;
                        if (list.ItemType != null)
                        {
                            baseType = list.ItemType;
                        }
                    }
                    else if (t == typeof(XmlSchemaSimpleTypeUnion))
                    {
                        XmlQualifiedName[]? memberTypes = ((XmlSchemaSimpleTypeUnion)item).MemberTypes;

                        if (memberTypes != null)
                        {
                            for (int i = 0; i < memberTypes.Length; i++)
                            {
                                XmlSchemaType? type = (XmlSchemaType?)_schemas.Find(memberTypes[i], typeof(XmlSchemaType), false);
                                AddRef(refs, type);
                            }
                        }
                    }
                }
                if (baseType == null && !baseName.IsEmpty && baseName.Namespace != XmlSchema.Namespace)
                    baseType = (XmlSchemaType?)_schemas.Find(baseName, typeof(XmlSchemaType), false);

                if (baseType != null)
                {
                    AddRef(refs, baseType);
                }
                if (particle != null)
                {
                    Depends(particle, refs);
                }
                if (attributes != null)
                {
                    for (int i = 0; i < attributes.Count; i++)
                    {
                        Depends(attributes[i], refs);
                    }
                }
            }
            else if (t == typeof(XmlSchemaElement))
            {
                XmlSchemaElement el = (XmlSchemaElement)item;
                if (!el.SubstitutionGroup.IsEmpty)
                {
                    if (el.SubstitutionGroup.Namespace != XmlSchema.Namespace)
                    {
                        XmlSchemaElement? head = (XmlSchemaElement?)_schemas.Find(el.SubstitutionGroup, typeof(XmlSchemaElement), false);
                        AddRef(refs, head);
                    }
                }
                if (!el.RefName.IsEmpty)
                {
                    el = (XmlSchemaElement)_schemas.Find(el.RefName, typeof(XmlSchemaElement), false)!;
                    AddRef(refs, el);
                }
                else if (!el.SchemaTypeName.IsEmpty)
                {
                    XmlSchemaType? type = (XmlSchemaType?)_schemas.Find(el.SchemaTypeName, typeof(XmlSchemaType), false);
                    AddRef(refs, type);
                }
                else
                {
                    Depends(el.SchemaType, refs);
                }
            }
            else if (t == typeof(XmlSchemaGroup))
            {
                Depends(((XmlSchemaGroup)item).Particle!);
            }
            else if (t == typeof(XmlSchemaGroupRef))
            {
                XmlSchemaGroup? group = (XmlSchemaGroup?)_schemas.Find(((XmlSchemaGroupRef)item).RefName, typeof(XmlSchemaGroup), false);
                AddRef(refs, group);
            }
            else if (typeof(XmlSchemaGroupBase).IsAssignableFrom(t))
            {
                foreach (XmlSchemaObject o in ((XmlSchemaGroupBase)item).Items)
                {
                    Depends(o, refs);
                }
            }
            else if (t == typeof(XmlSchemaAttributeGroupRef))
            {
                XmlSchemaAttributeGroup? group = (XmlSchemaAttributeGroup?)_schemas.Find(((XmlSchemaAttributeGroupRef)item).RefName, typeof(XmlSchemaAttributeGroup), false);
                AddRef(refs, group);
            }
            else if (t == typeof(XmlSchemaAttributeGroup))
            {
                foreach (XmlSchemaObject o in ((XmlSchemaAttributeGroup)item).Attributes)
                {
                    Depends(o, refs);
                }
            }
            else if (t == typeof(XmlSchemaAttribute))
            {
                XmlSchemaAttribute? at = (XmlSchemaAttribute)item;
                if (!at.RefName.IsEmpty)
                {
                    at = (XmlSchemaAttribute?)_schemas.Find(at.RefName, typeof(XmlSchemaAttribute), false);
                    AddRef(refs, at);
                }
                else if (!at.SchemaTypeName.IsEmpty)
                {
                    XmlSchemaType? type = (XmlSchemaType?)_schemas.Find(at.SchemaTypeName, typeof(XmlSchemaType), false);
                    AddRef(refs, type);
                }
                else
                {
                    Depends(at.SchemaType, refs);
                }
            }
            if (typeof(XmlSchemaAnnotated).IsAssignableFrom(t))
            {
                XmlAttribute[]? attrs = (XmlAttribute[]?)((XmlSchemaAnnotated)item).UnhandledAttributes;

                if (attrs != null)
                {
                    for (int i = 0; i < attrs.Length; i++)
                    {
                        XmlAttribute attribute = attrs[i];
                        if (attribute.LocalName == Wsdl.ArrayType && attribute.NamespaceURI == Wsdl.Namespace)
                        {
                            XmlQualifiedName qname = TypeScope.ParseWsdlArrayType(attribute.Value, out _, item);
                            XmlSchemaType? type = (XmlSchemaType?)_schemas.Find(qname, typeof(XmlSchemaType), false);
                            AddRef(refs, type);
                        }
                    }
                }
            }
        }
    }
}
