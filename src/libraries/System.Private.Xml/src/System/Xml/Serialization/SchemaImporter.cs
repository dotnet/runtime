// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Serialization
{
    using System;
    using System.Xml.Schema;
    using System.Collections;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Configuration;
    using System.Xml.Serialization.Configuration;

#if DEBUG
    using System.Diagnostics;
#endif

    public abstract class SchemaImporter
    {
        private XmlSchemas _schemas;
        private StructMapping? _root;
        private readonly CodeGenerationOptions _options;
        private TypeScope? _scope;
        private ImportContext _context;
        private bool _rootImported;
        private NameTable? _typesInUse;
        private NameTable? _groupsInUse;

        [RequiresUnreferencedCode("calls SetCache")]
        internal SchemaImporter(XmlSchemas schemas, CodeGenerationOptions options, ImportContext context)
        {
            if (!schemas.Contains(XmlSchema.Namespace))
            {
                schemas.AddReference(XmlSchemas.XsdSchema);
                schemas.SchemaSet.Add(XmlSchemas.XsdSchema);
            }
            if (!schemas.Contains(XmlReservedNs.NsXml))
            {
                schemas.AddReference(XmlSchemas.XmlSchema);
                schemas.SchemaSet.Add(XmlSchemas.XmlSchema);
            }
            _schemas = schemas;
            _options = options;
            _context = context;
            Schemas.SetCache(Context.Cache, Context.ShareTypes);
        }

        internal ImportContext Context
        {
            get
            {
                if (_context == null)
                    _context = new ImportContext();
                return _context;
            }
        }

        internal Hashtable ImportedElements
        {
            get { return Context.Elements; }
        }

        internal Hashtable ImportedMappings
        {
            get { return Context.Mappings; }
        }

        internal CodeIdentifiers TypeIdentifiers
        {
            get { return Context.TypeIdentifiers; }
        }

        internal XmlSchemas Schemas
        {
            get
            {
                if (_schemas == null)
                    _schemas = new XmlSchemas();
                return _schemas;
            }
        }

        internal TypeScope Scope
        {
            get
            {
                if (_scope == null)
                    _scope = new TypeScope();
                return _scope;
            }
        }

        internal NameTable GroupsInUse
        {
            get
            {
                if (_groupsInUse == null)
                    _groupsInUse = new NameTable();
                return _groupsInUse;
            }
        }

        internal NameTable TypesInUse
        {
            get
            {
                if (_typesInUse == null)
                    _typesInUse = new NameTable();
                return _typesInUse;
            }
        }

        internal CodeGenerationOptions Options
        {
            get { return _options; }
        }

        [RequiresUnreferencedCode("calls GetTypeDesc")]
        internal void MakeDerived(StructMapping structMapping, Type? baseType, bool baseTypeCanBeIndirect)
        {
            structMapping.ReferencedByTopLevelElement = true;
            TypeDesc baseTypeDesc;
            if (baseType != null)
            {
                baseTypeDesc = Scope.GetTypeDesc(baseType);
                if (baseTypeDesc != null)
                {
                    TypeDesc typeDescToChange = structMapping.TypeDesc!;
                    if (baseTypeCanBeIndirect)
                    {
                        // if baseTypeCanBeIndirect is true, we apply the supplied baseType to the top of the
                        // inheritance chain, not necessarily directly to the imported type.
                        while (typeDescToChange.BaseTypeDesc != null && typeDescToChange.BaseTypeDesc != baseTypeDesc)
                            typeDescToChange = typeDescToChange.BaseTypeDesc;
                    }
                    if (typeDescToChange.BaseTypeDesc != null && typeDescToChange.BaseTypeDesc != baseTypeDesc)
                        throw new InvalidOperationException(SR.Format(SR.XmlInvalidBaseType, structMapping.TypeDesc!.FullName, baseType.FullName, typeDescToChange.BaseTypeDesc.FullName));
                    typeDescToChange.BaseTypeDesc = baseTypeDesc;
                }
            }
        }

        internal string GenerateUniqueTypeName(string typeName)
        {
            typeName = CodeIdentifier.MakeValid(typeName);
            return TypeIdentifiers.AddUnique(typeName, typeName);
        }

        [RequiresUnreferencedCode("calls GetTypeDesc")]
        private StructMapping CreateRootMapping()
        {
            TypeDesc typeDesc = Scope.GetTypeDesc(typeof(object));
            StructMapping mapping = new StructMapping();
            mapping.TypeDesc = typeDesc;
            mapping.Members = Array.Empty<MemberMapping>();
            mapping.IncludeInSchema = false;
            mapping.TypeName = Soap.UrType;
            mapping.Namespace = XmlSchema.Namespace;

            return mapping;
        }

        [RequiresUnreferencedCode("calls CreateRootMapping")]
        internal StructMapping GetRootMapping()
        {
            if (_root == null)
                _root = CreateRootMapping();
            return _root;
        }

        [RequiresUnreferencedCode("calls GetRootMapping")]
        internal StructMapping ImportRootMapping()
        {
            if (!_rootImported)
            {
                _rootImported = true;
                ImportDerivedTypes(XmlQualifiedName.Empty);
            }
            return GetRootMapping();
        }

        [RequiresUnreferencedCode("calls ImportType")]
        internal abstract void ImportDerivedTypes(XmlQualifiedName baseName);

        internal static void AddReference(XmlQualifiedName name, NameTable references, string error)
        {
            if (name.Namespace == XmlSchema.Namespace)
                return;
            if (references[name] != null)
            {
                throw new InvalidOperationException(string.Format(error, name.Name, name.Namespace));
            }
            references[name] = name;
        }

        internal static void RemoveReference(XmlQualifiedName name, NameTable references)
        {
            references[name] = null;
        }

        internal void AddReservedIdentifiersForDataBinding(CodeIdentifiers scope)
        {
            if ((_options & CodeGenerationOptions.EnableDataBinding) != 0)
            {
                scope.AddReserved("PropertyChanged");
                scope.AddReserved("RaisePropertyChanged");
            }
        }
    }
}
