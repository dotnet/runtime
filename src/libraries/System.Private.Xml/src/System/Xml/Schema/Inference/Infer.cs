// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Diagnostics;
using System.Globalization;

namespace System.Xml.Schema
{
    /// <summary>
    /// Infer class serves for inferring XML Schema from given XML instance document.
    /// </summary>
    public sealed class XmlSchemaInference
    {
        internal static XmlQualifiedName ST_boolean = new XmlQualifiedName("boolean", XmlSchema.Namespace);
        internal static XmlQualifiedName ST_byte = new XmlQualifiedName("byte", XmlSchema.Namespace);
        internal static XmlQualifiedName ST_unsignedByte = new XmlQualifiedName("unsignedByte", XmlSchema.Namespace);
        internal static XmlQualifiedName ST_short = new XmlQualifiedName("short", XmlSchema.Namespace);
        internal static XmlQualifiedName ST_unsignedShort = new XmlQualifiedName("unsignedShort", XmlSchema.Namespace);
        internal static XmlQualifiedName ST_int = new XmlQualifiedName("int", XmlSchema.Namespace);
        internal static XmlQualifiedName ST_unsignedInt = new XmlQualifiedName("unsignedInt", XmlSchema.Namespace);
        internal static XmlQualifiedName ST_long = new XmlQualifiedName("long", XmlSchema.Namespace);
        internal static XmlQualifiedName ST_unsignedLong = new XmlQualifiedName("unsignedLong", XmlSchema.Namespace);
        internal static XmlQualifiedName ST_integer = new XmlQualifiedName("integer", XmlSchema.Namespace);
        internal static XmlQualifiedName ST_decimal = new XmlQualifiedName("decimal", XmlSchema.Namespace);
        internal static XmlQualifiedName ST_float = new XmlQualifiedName("float", XmlSchema.Namespace);
        internal static XmlQualifiedName ST_double = new XmlQualifiedName("double", XmlSchema.Namespace);
        internal static XmlQualifiedName ST_duration = new XmlQualifiedName("duration", XmlSchema.Namespace);
        internal static XmlQualifiedName ST_dateTime = new XmlQualifiedName("dateTime", XmlSchema.Namespace);
        internal static XmlQualifiedName ST_time = new XmlQualifiedName("time", XmlSchema.Namespace);
        internal static XmlQualifiedName ST_date = new XmlQualifiedName("date", XmlSchema.Namespace);
        internal static XmlQualifiedName ST_gYearMonth = new XmlQualifiedName("gYearMonth", XmlSchema.Namespace);
        internal static XmlQualifiedName ST_string = new XmlQualifiedName("string", XmlSchema.Namespace);
        internal static XmlQualifiedName ST_anySimpleType = new XmlQualifiedName("anySimpleType", XmlSchema.Namespace);

        internal static XmlQualifiedName[] SimpleTypes =
        {
                ST_boolean,
                ST_byte,
                ST_unsignedByte,
                ST_short,
                ST_unsignedShort,
                ST_int,
                ST_unsignedInt,
                ST_long,
                ST_unsignedLong,
                ST_integer,
                ST_decimal,
                ST_float,
                ST_double,
                ST_duration,
                ST_dateTime,
                ST_time,
                ST_date,
                ST_gYearMonth,
                ST_string
            };

        internal const short HC_ST_boolean = 0;
        internal const short HC_ST_byte = 1;
        internal const short HC_ST_unsignedByte = 2;
        internal const short HC_ST_short = 3;
        internal const short HC_ST_unsignedShort = 4;
        internal const short HC_ST_int = 5;
        internal const short HC_ST_unsignedInt = 6;
        internal const short HC_ST_long = 7;
        internal const short HC_ST_unsignedLong = 8;
        internal const short HC_ST_integer = 9;
        internal const short HC_ST_decimal = 10;
        internal const short HC_ST_float = 11;
        internal const short HC_ST_double = 12;
        internal const short HC_ST_duration = 13;
        internal const short HC_ST_dateTime = 14;
        internal const short HC_ST_time = 15;
        internal const short HC_ST_date = 16;
        internal const short HC_ST_gYearMonth = 17;
        internal const short HC_ST_string = 18;
        internal const short HC_ST_Count = HC_ST_string + 1;


        internal const int TF_boolean = 1 << HC_ST_boolean;
        internal const int TF_byte = 1 << HC_ST_byte;
        internal const int TF_unsignedByte = 1 << HC_ST_unsignedByte;
        internal const int TF_short = 1 << HC_ST_short;
        internal const int TF_unsignedShort = 1 << HC_ST_unsignedShort;
        internal const int TF_int = 1 << HC_ST_int;
        internal const int TF_unsignedInt = 1 << HC_ST_unsignedInt;
        internal const int TF_long = 1 << HC_ST_long;
        internal const int TF_unsignedLong = 1 << HC_ST_unsignedLong;
        internal const int TF_integer = 1 << HC_ST_integer;
        internal const int TF_decimal = 1 << HC_ST_decimal;
        internal const int TF_float = 1 << HC_ST_float;
        internal const int TF_double = 1 << HC_ST_double;
        internal const int TF_duration = 1 << HC_ST_duration;
        internal const int TF_dateTime = 1 << HC_ST_dateTime;
        internal const int TF_time = 1 << HC_ST_time;
        internal const int TF_date = 1 << HC_ST_date;
        internal const int TF_gYearMonth = 1 << HC_ST_gYearMonth;
        internal const int TF_string = 1 << HC_ST_string;

        private XmlSchema? _rootSchema; //(XmlSchema) xsc[TargetNamespace];
        private XmlSchemaSet? _schemaSet;
        private XmlReader? _xtr;
        private readonly NameTable _nametable;
        private string? _targetNamespace;
        private readonly XmlNamespaceManager _namespaceManager;
        //private Hashtable schemas;    //contains collection of schemas before they get added to the XmlSchemaSet xsc
        //private bool bRefine = false; //indicates if we are going to infer or refine schema when InferSchema is called
        private InferenceOption _occurrence = InferenceOption.Restricted;
        private InferenceOption _typeInference = InferenceOption.Restricted;

        /*  internal struct ReplaceList
          {
              internal XmlSchemaObjectCollection col;
              internal int position;

              internal ReplaceList(XmlSchemaObjectCollection col, int position)
              {
                  this.col = col;
                  this.position = position;
              }
          }*/

        public enum InferenceOption
        {
            Restricted,
            Relaxed
        };

        public InferenceOption Occurrence
        {
            get
            {
                return _occurrence;
            }
            set
            {
                _occurrence = value;
            }
        }

        public InferenceOption TypeInference
        {
            get
            {
                return _typeInference;
            }
            set
            {
                _typeInference = value;
            }
        }

        public XmlSchemaInference()
        {
            _nametable = new NameTable();
            _namespaceManager = new XmlNamespaceManager(_nametable);
            _namespaceManager.AddNamespace("xs", XmlSchema.Namespace);
        }

        public XmlSchemaSet InferSchema(XmlReader instanceDocument)
        {
            return InferSchema1(instanceDocument, new XmlSchemaSet(_nametable));
        }

        public XmlSchemaSet InferSchema(XmlReader instanceDocument, XmlSchemaSet schemas)
        {
            if (schemas == null)
            {
                schemas = new XmlSchemaSet(_nametable);
            }
            return InferSchema1(instanceDocument, schemas);
        }

        internal XmlSchemaSet InferSchema1(XmlReader instanceDocument!!, XmlSchemaSet schemas)
        {
            _rootSchema = null;
            _xtr = instanceDocument;
            schemas.Compile();
            _schemaSet = schemas;
            //schemas = new Hashtable();
            //while(xtr.Read())

            while (_xtr.NodeType != XmlNodeType.Element && _xtr.Read()) ;


            if (_xtr.NodeType == XmlNodeType.Element)
            {
                //Create and process the root element
                _targetNamespace = _xtr.NamespaceURI;
                if (_xtr.NamespaceURI == XmlSchema.Namespace)
                {
                    throw new XmlSchemaInferenceException(SR.SchInf_schema, 0, 0);
                }
                XmlSchemaElement? xse = null;
                foreach (XmlSchemaElement? elem in schemas.GlobalElements.Values)
                {
                    if (elem!.Name == _xtr.LocalName && elem.QualifiedName.Namespace == _xtr.NamespaceURI)
                    {
                        _rootSchema = elem.Parent as XmlSchema;
                        xse = elem;
                        break;
                    }
                }

                if (_rootSchema == null)
                {
                    //rootSchema = CreateXmlSchema(xtr.NamespaceURI);
                    AddElement(_xtr.LocalName, _xtr.Prefix, _xtr.NamespaceURI, null, null, -1);
                }
                else
                {
                    //bRefine = true;
                    InferElement(xse!, false, _rootSchema);
                }

                /*  foreach (ReplaceList listItem in schemaList)
                  {
                      if (listItem.position < listItem.col.Count)
                      {
                          XmlSchemaElement particle = listItem.col[listItem.position] as XmlSchemaElement;
                          if (particle != null && (particle.RefName.Namespace == XmlSchema.Namespace))
                          {
                              XmlSchemaAny any = new XmlSchemaAny();
                              if (particle.MaxOccurs != 1)
                              {
                                  any.MaxOccurs = particle.MaxOccurs;
                              }
                              if (particle.MinOccurs != 1)
                              {
                                  any.MinOccurs = particle.MinOccurs;
                              }
                              any.ProcessContents = XmlSchemaContentProcessing.Skip;
                              any.MinOccurs = decimal.Zero;
                              any.Namespace = particle.RefName.Namespace;
                              listItem.col[listItem.position] = any;
                          }
                      }
                  }*/
                foreach (string? prefix in _namespaceManager)
                {
                    if (!prefix!.Equals("xml") && !prefix.Equals("xmlns"))
                    {
                        string ns = _namespaceManager.LookupNamespace(_nametable.Get(prefix)!)!;
                        if (ns.Length != 0)
                        { //Do not add xmlns=""
                            _rootSchema!.Namespaces.Add(prefix, ns);
                        }
                    }
                }
                Debug.Assert(_rootSchema != null, "rootSchema is null");
                schemas.Reprocess(_rootSchema);
                schemas.Compile();
                //break;
            }
            else
            {
                throw new XmlSchemaInferenceException(SR.SchInf_NoElement, 0, 0);
            }
            return schemas;
        }

        private XmlSchemaAttribute AddAttribute(string localName, string prefix, string childURI, string attrValue, bool bCreatingNewType, XmlSchema parentSchema, XmlSchemaObjectCollection addLocation, XmlSchemaObjectTable compiledAttributes)
        {
            if (childURI == XmlSchema.Namespace)
            {
                throw new XmlSchemaInferenceException(SR.SchInf_schema, 0, 0);
            }

            XmlSchemaAttribute? xsa;
            int AttributeType = -1;
            XmlSchemaAttribute? returnedAttribute;    //this value will change to attributeReference if childURI!= parentURI
            XmlSchema? xs = null;
            bool add = true;

            Debug.Assert(compiledAttributes != null); //AttributeUses is never null
                                                      // First we need to look into the already compiled attributes
                                                      //   (they come from the schemaset which we got on input)
                                                      // If there are none or we don't find it there, then we must search the list of attributes
                                                      //   where we are going to add a new one (if it doesn't exist).
                                                      //   This is necessary to avoid adding duplicate attribute declarations.
            ICollection searchCollectionPrimary;
            ICollection? searchCollectionSecondary;
            if (compiledAttributes.Count > 0)
            {
                searchCollectionPrimary = compiledAttributes.Values;
                searchCollectionSecondary = addLocation;
            }
            else
            {
                searchCollectionPrimary = addLocation;
                searchCollectionSecondary = null;
            }
            if (childURI == XmlReservedNs.NsXml)
            {
                XmlSchemaAttribute? attributeReference;
                //see if the reference exists
                attributeReference = FindAttributeRef(searchCollectionPrimary, localName, childURI);
                if (attributeReference == null && searchCollectionSecondary != null)
                {
                    attributeReference = FindAttributeRef(searchCollectionSecondary, localName, childURI);
                }
                if (attributeReference == null)
                {
                    attributeReference = new XmlSchemaAttribute();
                    attributeReference.RefName = new XmlQualifiedName(localName, childURI);
                    if (bCreatingNewType && this.Occurrence == InferenceOption.Restricted)
                    {
                        attributeReference.Use = XmlSchemaUse.Required;
                    }
                    else
                    {
                        attributeReference.Use = XmlSchemaUse.Optional;
                    }

                    addLocation.Add(attributeReference);
                }
                returnedAttribute = attributeReference;
            }
            else
            {
                if (childURI.Length == 0)
                {
                    xs = parentSchema;
                    add = false;
                }
                else if (childURI != null && !_schemaSet!.Contains(childURI))
                {
                    /*if (parentSchema.AttributeFormDefault = XmlSchemaForm.Unqualified && childURI.Length == 0)
                {
                    xs = parentSchema;
                    add = false;
                    break;
                }*/
                    xs = new XmlSchema();
                    xs.AttributeFormDefault = XmlSchemaForm.Unqualified;
                    xs.ElementFormDefault = XmlSchemaForm.Qualified;
                    if (childURI.Length != 0)
                        xs.TargetNamespace = childURI;
                    //schemas.Add(childURI, xs);
                    _schemaSet.Add(xs);
                    if (prefix.Length != 0 && !string.Equals(prefix, "xml", StringComparison.OrdinalIgnoreCase))
                        _namespaceManager.AddNamespace(prefix, childURI);
                }
                else
                {
                    ArrayList? col = _schemaSet!.Schemas(childURI) as ArrayList;
                    if (col != null && col.Count > 0)
                    {
                        xs = col[0] as XmlSchema;
                    }
                }

                if (childURI!.Length != 0) //BUGBUG It need not be an attribute reference if there is a namespace, it can be attribute with attributeFormDefault = qualified
                {
                    XmlSchemaAttribute? attributeReference;
                    //see if the reference exists
                    attributeReference = FindAttributeRef(searchCollectionPrimary, localName, childURI);
                    if (attributeReference == null & searchCollectionSecondary != null)
                    {
                        attributeReference = FindAttributeRef(searchCollectionSecondary!, localName, childURI);
                    }
                    if (attributeReference == null)
                    {
                        attributeReference = new XmlSchemaAttribute();
                        attributeReference.RefName = new XmlQualifiedName(localName, childURI);
                        if (bCreatingNewType && this.Occurrence == InferenceOption.Restricted)
                        {
                            attributeReference.Use = XmlSchemaUse.Required;
                        }
                        else
                        {
                            attributeReference.Use = XmlSchemaUse.Optional;
                        }

                        addLocation.Add(attributeReference);
                    }
                    returnedAttribute = attributeReference;

                    //see if the attribute exists on the global level
                    xsa = FindAttribute(xs!.Items, localName);
                    if (xsa == null)
                    {
                        xsa = new XmlSchemaAttribute();
                        xsa.Name = localName;
                        xsa.SchemaTypeName = RefineSimpleType(attrValue, ref AttributeType);
                        xsa.LineNumber = AttributeType; //we use LineNumber to store flags of valid types
                        xs.Items.Add(xsa);
                    }
                    else
                    {
                        if (xsa.Parent == null)
                        {
                            AttributeType = xsa.LineNumber; // we use LineNumber to store flags of valid types
                        }
                        else
                        {
                            AttributeType = GetSchemaType(xsa.SchemaTypeName);
                            xsa.Parent = null;
                        }
                        xsa.SchemaTypeName = RefineSimpleType(attrValue, ref AttributeType);
                        xsa.LineNumber = AttributeType; // we use LineNumber to store flags of valid types
                    }
                }
                else
                {
                    xsa = FindAttribute(searchCollectionPrimary, localName);
                    if (xsa == null && searchCollectionSecondary != null)
                    {
                        xsa = FindAttribute(searchCollectionSecondary, localName);
                    }
                    if (xsa == null)
                    {
                        xsa = new XmlSchemaAttribute();
                        xsa.Name = localName;
                        xsa.SchemaTypeName = RefineSimpleType(attrValue, ref AttributeType);
                        xsa.LineNumber = AttributeType; // we use LineNumber to store flags of valid types
                        if (bCreatingNewType && this.Occurrence == InferenceOption.Restricted)
                            xsa.Use = XmlSchemaUse.Required;
                        else
                            xsa.Use = XmlSchemaUse.Optional;
                        addLocation.Add(xsa);
                        if (xs!.AttributeFormDefault != XmlSchemaForm.Unqualified)
                        {
                            xsa.Form = XmlSchemaForm.Unqualified;
                        }
                    }
                    else
                    {
                        if (xsa.Parent == null)
                        {
                            AttributeType = xsa.LineNumber; // we use LineNumber to store flags of valid types
                        }
                        else
                        {
                            AttributeType = GetSchemaType(xsa.SchemaTypeName);
                            xsa.Parent = null;
                        }
                        xsa.SchemaTypeName = RefineSimpleType(attrValue, ref AttributeType);
                        xsa.LineNumber = AttributeType; // we use LineNumber to store flags of valid types
                    }
                    returnedAttribute = xsa;
                }
            }
            string? nullString = null;
            if (add && childURI != parentSchema.TargetNamespace)
            {
                for (int i = 0; i < parentSchema.Includes.Count; ++i)
                {
                    XmlSchemaImport? import = parentSchema.Includes[i] as XmlSchemaImport;
                    if (import == null)
                    {
                        continue;
                    }

                    if (import.Namespace == childURI)
                    {
                        add = false;
                    }
                }

                if (add)
                {
                    XmlSchemaImport import = new XmlSchemaImport();
                    import.Schema = xs;
                    if (childURI.Length != 0)
                    {
                        nullString = childURI;
                    }
                    import.Namespace = nullString;
                    parentSchema.Includes.Add(import);
                }
            }


            return returnedAttribute;
        }

        private XmlSchema CreateXmlSchema(string targetNS)
        {
            Debug.Assert(targetNS == null || targetNS.Length > 0, "targetns for schema is empty");
            XmlSchema xs = new XmlSchema();
            xs.AttributeFormDefault = XmlSchemaForm.Unqualified;
            xs.ElementFormDefault = XmlSchemaForm.Qualified;
            xs.TargetNamespace = targetNS;
            _schemaSet!.Add(xs);
            return xs;
        }

        private XmlSchemaElement AddElement(string localName, string prefix, string? childURI, XmlSchema? parentSchema, XmlSchemaObjectCollection? addLocation, int positionWithinCollection)
        {
            if (childURI == XmlSchema.Namespace)
            {
                throw new XmlSchemaInferenceException(SR.SchInf_schema, 0, 0);
            }

            XmlSchemaElement? xse;
            XmlSchemaElement? returnedElement; //this value will change to elementReference if childURI!= parentURI
            XmlSchema? xs;
            bool bCreatingNewType = true;
            if (childURI == string.Empty)
            {
                childURI = null;
            }

            // The new element belongs to the same ns as parent and addlocation is not null
            if (parentSchema != null && childURI == parentSchema.TargetNamespace)
            {
                xse = new XmlSchemaElement();
                xse.Name = localName;
                xs = parentSchema;
                if (xs.ElementFormDefault != XmlSchemaForm.Qualified && addLocation != null)
                {
                    xse.Form = XmlSchemaForm.Qualified;
                }
            }
            else if (_schemaSet!.Contains(childURI))
            {
                xse = this.FindGlobalElement(childURI, localName, out xs);
                if (xse == null)
                {
                    ArrayList? col = _schemaSet.Schemas(childURI) as ArrayList;
                    if (col != null && col.Count > 0)
                    {
                        xs = col[0] as XmlSchema;
                    }
                    xse = new XmlSchemaElement();
                    xse.Name = localName;
                    xs!.Items.Add(xse);
                }
                else
                    bCreatingNewType = false;
            }
            else
            {
                xs = CreateXmlSchema(childURI!);
                if (prefix.Length != 0)
                    _namespaceManager.AddNamespace(prefix, childURI!);
                xse = new XmlSchemaElement();
                xse.Name = localName;
                xs.Items.Add(xse);  //add global element declaration only when creating new schema
            }
            if (parentSchema == null)
            {
                parentSchema = xs;
                _rootSchema = parentSchema;
            }

            if (childURI != parentSchema!.TargetNamespace)
            {
                bool add = true;

                for (int i = 0; i < parentSchema.Includes.Count; ++i)
                {
                    XmlSchemaImport? import = parentSchema.Includes[i] as XmlSchemaImport;
                    if (import == null)
                    {
                        continue;
                    }
                    //Debug.WriteLine(import.Schema.TargetNamespace);

                    if (import.Namespace == childURI)
                    {
                        add = false;
                    }
                }
                if (add)
                {
                    XmlSchemaImport import = new XmlSchemaImport();
                    import.Schema = xs;
                    import.Namespace = childURI;
                    parentSchema.Includes.Add(import);
                }
            }
            returnedElement = xse;
            if (addLocation != null)
            {
                if (childURI == parentSchema.TargetNamespace)
                {
                    if (this.Occurrence == InferenceOption.Relaxed /*&& parentSchema.Items != addLocation*/)
                    {
                        xse.MinOccurs = 0;
                    }
                    if (positionWithinCollection == -1)
                    {
                        addLocation.Add(xse);
                    }
                    else
                    {
                        addLocation.Insert(positionWithinCollection, xse);
                    }
                }
                else
                {
                    XmlSchemaElement elementReference = new XmlSchemaElement();
                    elementReference.RefName = new XmlQualifiedName(localName, childURI);
                    if (this.Occurrence == InferenceOption.Relaxed)
                    {
                        elementReference.MinOccurs = 0;
                    }
                    if (positionWithinCollection == -1)
                    {
                        addLocation.Add(elementReference);
                    }
                    else
                    {
                        addLocation.Insert(positionWithinCollection, elementReference);
                    }
                    returnedElement = elementReference;
                    /* if (childURI == XmlSchema.Namespace)
                     {
                         schemaList.Add(new ReplaceList(addLocation, positionWithinCollection));
                     }*/
                }
            }


            InferElement(xse, bCreatingNewType, xs!);

            return returnedElement;
        }

        /// <summary>
        /// Sets type of the xse based on the currently read element.
        /// If the type is already set, verifies that it matches the instance and if not, updates the type to validate the instance.
        /// </summary>
        /// <param name="xse">XmlSchemaElement corresponding to the element just read by the xtr XmlTextReader</param>
        /// <param name="bCreatingNewType">true if the type is newly created, false if the type already existed and matches the current element name</param>
        /// <param name="parentSchema">namespaceURI of the parent element. Used to distinguish if ref= should be used when parent is in different ns than child.</param>
        internal void InferElement(XmlSchemaElement xse, bool bCreatingNewType, XmlSchema parentSchema)
        {
            bool bEmptyElement = _xtr!.IsEmptyElement;
            int lastUsedSeqItem = -1;

            Hashtable table = new Hashtable();
            XmlSchemaType? schemaType = GetEffectiveSchemaType(xse, bCreatingNewType);
            XmlSchemaComplexType? ct = schemaType as XmlSchemaComplexType;

            //infer type based on content of the current element
            if (_xtr.MoveToFirstAttribute())
            {
                ProcessAttributes(ref xse, schemaType, bCreatingNewType, parentSchema);
            }
            else
            {
                if (!bCreatingNewType && ct != null)
                {   //if type already exists and can potentially have attributes
                    MakeExistingAttributesOptional(ct, null);
                }
            }
            if (ct == null || ct == XmlSchemaComplexType.AnyType)
            { //It was null or simple type, after processing attributes, this might have been set
                ct = xse.SchemaType as XmlSchemaComplexType;
            }
            //xse's type is set either to complex type if attributes exist or null
            if (bEmptyElement)  //<element attr="3232" />
            {
                if (!bCreatingNewType)
                {
                    if (null != ct)
                    {
                        if (null != ct.Particle)

                        {
                            ct.Particle.MinOccurs = 0;
                        }
                        else if (null != ct.ContentModel)
                        {
                            XmlSchemaSimpleContentExtension sce = CheckSimpleContentExtension(ct);
                            sce.BaseTypeName = ST_string;
                            sce.LineNumber = TF_string;
                        }
                    }
                    else if (!xse.SchemaTypeName.IsEmpty)
                    {
                        xse.LineNumber = TF_string;
                        xse.SchemaTypeName = ST_string;
                    }
                }
                else
                {
                    xse.LineNumber = TF_string;
                    //xse.SchemaTypeName = ST_string; //My change
                }
                return; //We are done processing this element - all attributes are already added
            }
            bool bWhiteSpace = false;
            do
            {
                _xtr.Read();
                if (_xtr.NodeType == XmlNodeType.Whitespace)
                {
                    bWhiteSpace = true;
                }
                if (_xtr.NodeType == XmlNodeType.EntityReference)
                {
                    throw new XmlSchemaInferenceException(SR.SchInf_entity, 0, 0);
                }
            } while ((!_xtr.EOF) && (_xtr.NodeType != XmlNodeType.EndElement) && (_xtr.NodeType != XmlNodeType.CDATA) && (_xtr.NodeType != XmlNodeType.Element) && (_xtr.NodeType != XmlNodeType.Text));

            if (_xtr.NodeType == XmlNodeType.EndElement)
            {
                if (bWhiteSpace)
                {
                    if (ct != null)
                    {
                        if (null != ct.ContentModel)
                        {
                            XmlSchemaSimpleContentExtension sce = CheckSimpleContentExtension(ct);
                            sce.BaseTypeName = ST_string;
                            sce.LineNumber = TF_string;
                        }
                        else if (bCreatingNewType)
                        {
                            //attributes exist, but both Particle and ContentModel == null - this must be complex type with simpleContent extension
                            XmlSchemaSimpleContent sc = new XmlSchemaSimpleContent();
                            ct.ContentModel = sc;
                            XmlSchemaSimpleContentExtension sce = new XmlSchemaSimpleContentExtension();
                            sc.Content = sce;

                            MoveAttributes(ct, sce, bCreatingNewType);

                            sce.BaseTypeName = ST_string;
                            sce.LineNumber = TF_string;
                        }
                        else
                            ct.IsMixed = true;
                    }
                    else
                    {
                        xse.SchemaTypeName = ST_string;
                        xse.LineNumber = TF_string;
                    }
                }
                if (bCreatingNewType)
                {
                    xse.LineNumber = TF_string;
                    //xse.SchemaTypeName = ST_string; //my change
                }
                else
                {
                    if (null != ct)
                    {
                        if (null != ct.Particle)
                        {
                            ct.Particle.MinOccurs = 0;
                        }
                        else if (null != ct.ContentModel)
                        {
                            XmlSchemaSimpleContentExtension sce = CheckSimpleContentExtension(ct);
                            sce.BaseTypeName = ST_string;
                            sce.LineNumber = TF_string;
                        }
                    }
                    else if (!xse.SchemaTypeName.IsEmpty)
                    {
                        xse.LineNumber = TF_string;
                        xse.SchemaTypeName = ST_string;
                    }
                }

                return; //<element attr="232"></element>
            }
            int iChildNumber = 0;
            bool bCreatingNewSequence = false;
            while (!_xtr.EOF && (_xtr.NodeType != XmlNodeType.EndElement))
            {
                bool bNextNodeAlreadyRead = false;  //In some cases we have to look ahead one node. If true means that we did look ahead.
                iChildNumber++;
                if ((_xtr.NodeType == XmlNodeType.Text) || (_xtr.NodeType == XmlNodeType.CDATA)) //node can be simple type, complex with simple content or complex with mixed content
                {
                    if (null != ct)
                    {
                        if (null != ct.Particle)
                        {
                            ct.IsMixed = true;
                            if (iChildNumber == 1)
                            {
                                //if this is the only child and other elements do not follow, we must set particle minOccurs="0"
                                do { _xtr.Read(); } while ((!_xtr.EOF) && ((_xtr.NodeType == XmlNodeType.CDATA) || (_xtr.NodeType == XmlNodeType.Text) || (_xtr.NodeType == XmlNodeType.Comment) || (_xtr.NodeType == XmlNodeType.ProcessingInstruction) || (_xtr.NodeType == XmlNodeType.Whitespace) || (_xtr.NodeType == XmlNodeType.SignificantWhitespace) || (_xtr.NodeType == XmlNodeType.XmlDeclaration)));
                                bNextNodeAlreadyRead = true;
                                if (_xtr.NodeType == XmlNodeType.EndElement)
                                    ct.Particle.MinOccurs = decimal.Zero;
                            }
                        }
                        else if (null != ct.ContentModel)
                        {   //complexType with simpleContent
                            XmlSchemaSimpleContentExtension sce = CheckSimpleContentExtension(ct);
                            if ((_xtr.NodeType == XmlNodeType.Text) && (iChildNumber == 1))
                            {
                                int SimpleType = -1;
                                if (xse.Parent == null)
                                {
                                    SimpleType = sce.LineNumber; // we use LineNumber to represent valid type flags
                                }
                                else
                                {
                                    SimpleType = GetSchemaType(sce.BaseTypeName);
                                    xse.Parent = null;
                                }
                                sce.BaseTypeName = RefineSimpleType(_xtr.Value, ref SimpleType);
                                sce.LineNumber = SimpleType; // we use LineNumber to represent valid type flags
                            }
                            else
                            {
                                sce.BaseTypeName = ST_string;
                                sce.LineNumber = TF_string;
                            }
                        }
                        else
                        {
                            //attributes exist, but both Particle and ContentModel == null - this must be complex type with simpleContent extension
                            XmlSchemaSimpleContent sc = new XmlSchemaSimpleContent();
                            ct.ContentModel = sc;
                            XmlSchemaSimpleContentExtension sce = new XmlSchemaSimpleContentExtension();
                            sc.Content = sce;

                            MoveAttributes(ct, sce, bCreatingNewType);

                            if (_xtr.NodeType == XmlNodeType.Text)
                            {
                                int TypeFlags;
                                if (!bCreatingNewType)
                                    //previously this was empty element
                                    TypeFlags = TF_string;
                                else
                                    TypeFlags = -1;
                                sce.BaseTypeName = RefineSimpleType(_xtr.Value, ref TypeFlags);
                                sce.LineNumber = TypeFlags; // we use LineNumber to store flags of valid types
                            }
                            else
                            {
                                sce.BaseTypeName = ST_string;
                                sce.LineNumber = TF_string;
                            }
                        }
                    }
                    else
                    {   //node is currently empty or with SimpleType
                        //node will become simple type
                        if (iChildNumber > 1)
                        {
                            //more than one consecutive text nodes probably with PI in between
                            xse.SchemaTypeName = ST_string;
                            xse.LineNumber = TF_string; // we use LineNumber to store flags of valid types
                        }
                        else
                        {
                            int TypeFlags = -1;
                            if (bCreatingNewType)
                                if (_xtr.NodeType == XmlNodeType.Text)
                                {
                                    xse.SchemaTypeName = RefineSimpleType(_xtr.Value, ref TypeFlags);
                                    xse.LineNumber = TypeFlags; // we use LineNumber to store flags of valid types
                                }
                                else
                                {
                                    xse.SchemaTypeName = ST_string;
                                    xse.LineNumber = TF_string; // we use LineNumber to store flags of valid types
                                }
                            else if (_xtr.NodeType == XmlNodeType.Text)
                            {
                                if (xse.Parent == null)
                                {
                                    TypeFlags = xse.LineNumber;
                                }
                                else
                                {
                                    TypeFlags = GetSchemaType(xse.SchemaTypeName);
                                    if (TypeFlags == -1 && xse.LineNumber == TF_string)
                                    { //Since schemaTypeName is not set for empty elements (<e></e>)
                                        TypeFlags = TF_string;
                                    }
                                    xse.Parent = null;
                                }
                                xse.SchemaTypeName = RefineSimpleType(_xtr.Value, ref TypeFlags);    //simple type
                                xse.LineNumber = TypeFlags; // we use LineNumber to store flags of valid types
                            }
                            else
                            {
                                xse.SchemaTypeName = ST_string;
                                xse.LineNumber = TF_string; // we use LineNumber to store flags of valid types
                            }
                        }
                    }
                }
                else if (_xtr.NodeType == XmlNodeType.Element)
                {
                    XmlQualifiedName qname = new XmlQualifiedName(_xtr.LocalName, _xtr.NamespaceURI);
                    bool Maxoccursflag = false;
                    if (table.Contains(qname))
                    {
                        Maxoccursflag = true;
                    }
                    else
                    {
                        table.Add(qname, null);
                    }
                    if (ct == null)
                    { //untill now the element was empty or SimpleType - it now becomes complex type
                        ct = new XmlSchemaComplexType();
                        xse.SchemaType = ct;
                        if (!xse.SchemaTypeName.IsEmpty) //BUGBUG, This assumption is wrong
                        {
                            ct.IsMixed = true;
                            xse.SchemaTypeName = XmlQualifiedName.Empty;
                        }
                    }
                    if (ct.ContentModel != null)
                    {   //type was previously identified as simple content extension - we need to convert it to sequence
                        XmlSchemaSimpleContentExtension sce = CheckSimpleContentExtension(ct);
                        MoveAttributes(sce, ct);
                        ct.ContentModel = null;
                        ct.IsMixed = true;
                        if (ct.Particle != null)
                            throw new XmlSchemaInferenceException(SR.SchInf_particle, 0, 0);
                        ct.Particle = new XmlSchemaSequence();
                        bCreatingNewSequence = true;
                        AddElement(_xtr.LocalName, _xtr.Prefix, _xtr.NamespaceURI, parentSchema, ((XmlSchemaSequence)ct.Particle).Items, -1);
                        lastUsedSeqItem = 0;
                        if (!bCreatingNewType)
                            ct.Particle.MinOccurs = 0;    //previously this was simple type so subelements did not exist
                    }
                    else if (ct.Particle == null)
                    {
                        ct.Particle = new XmlSchemaSequence();
                        bCreatingNewSequence = true;
                        AddElement(_xtr.LocalName, _xtr.Prefix, _xtr.NamespaceURI, parentSchema, ((XmlSchemaSequence)ct.Particle).Items, -1);
                        if (!bCreatingNewType)
                        {
                            ((XmlSchemaSequence)ct.Particle).MinOccurs = decimal.Zero;
                            //  subelement.MinOccurs = decimal.Zero;
                        }

                        lastUsedSeqItem = 0;
                    }
                    else
                    {
                        bool bParticleChanged = false;
                        FindMatchingElement(bCreatingNewType || bCreatingNewSequence, _xtr, ct, ref lastUsedSeqItem, ref bParticleChanged, parentSchema, Maxoccursflag);
                    }
                }
                else if (_xtr.NodeType == XmlNodeType.Text)
                {
                    if (ct == null)
                        throw new XmlSchemaInferenceException(SR.SchInf_ct, 0, 0);
                    ct.IsMixed = true;
                }
                do
                {
                    if (_xtr.NodeType == XmlNodeType.EntityReference)
                    {
                        throw new XmlSchemaInferenceException(SR.SchInf_entity, 0, 0);
                    }
                    if (!bNextNodeAlreadyRead)
                    {
                        _xtr.Read();
                    }
                    else
                    {
                        bNextNodeAlreadyRead = false;
                    }
                } while ((!_xtr.EOF) && (_xtr.NodeType != XmlNodeType.EndElement) && (_xtr.NodeType != XmlNodeType.CDATA) && (_xtr.NodeType != XmlNodeType.Element) && (_xtr.NodeType != XmlNodeType.Text));
            }
            if (lastUsedSeqItem != -1)
            {
                //Verify if all elements in a sequence exist, if not set MinOccurs=0
                while (++lastUsedSeqItem < ((XmlSchemaSequence)ct!.Particle!).Items.Count)
                {
                    if (((XmlSchemaSequence)ct.Particle).Items[lastUsedSeqItem].GetType() != typeof(XmlSchemaElement))
                        throw new XmlSchemaInferenceException(SR.SchInf_seq, 0, 0);
                    XmlSchemaElement subElement = (XmlSchemaElement)((XmlSchemaSequence)ct.Particle).Items[lastUsedSeqItem];
                    subElement.MinOccurs = 0;
                }
            }
        }

        private static XmlSchemaSimpleContentExtension CheckSimpleContentExtension(XmlSchemaComplexType ct)
        {
            XmlSchemaSimpleContent? sc = ct.ContentModel as XmlSchemaSimpleContent;
            if (sc == null)
            {
                throw new XmlSchemaInferenceException(SR.SchInf_simplecontent, 0, 0);
            }
            XmlSchemaSimpleContentExtension? sce = sc.Content as XmlSchemaSimpleContentExtension;
            if (sce == null)
            {
                throw new XmlSchemaInferenceException(SR.SchInf_extension, 0, 0);
            }
            return sce;
        }

        private XmlSchemaType? GetEffectiveSchemaType(XmlSchemaElement elem, bool bCreatingNewType)
        {
            XmlSchemaType? effectiveSchemaType = null;
            if (!bCreatingNewType && elem.ElementSchemaType != null)
            {
                effectiveSchemaType = elem.ElementSchemaType;
            }
            else
            { //creating new type, hence look up pre-compiled info
                Debug.Assert(elem.ElementDecl == null);
                if (elem.SchemaType != null)
                {
                    effectiveSchemaType = elem.SchemaType;
                }
                else if (elem.SchemaTypeName != XmlQualifiedName.Empty)
                {
                    effectiveSchemaType = _schemaSet!.GlobalTypes[elem.SchemaTypeName] as XmlSchemaType;
                    if (effectiveSchemaType == null)
                    {
                        effectiveSchemaType = XmlSchemaType.GetBuiltInSimpleType(elem.SchemaTypeName);
                    }
                    if (effectiveSchemaType == null)
                    {
                        effectiveSchemaType = XmlSchemaType.GetBuiltInComplexType(elem.SchemaTypeName);
                    }
                }
            }
            return effectiveSchemaType;
        }
        /// <summary>
        /// Verifies that the current element has its corresponding element in the sequence and order is the same.
        /// If the order is not the same, it changes the particle from Sequence to Sequence with Choice.
        /// If there is more elements of the same kind in the sequence, sets maxOccurs to unbounded
        /// </summary>
        /// <param name="bCreatingNewType">True if this is a new type. This is important for setting minOccurs=0 for elements that did not exist in a particle.</param>
        /// <param name="xtr">text reader positioned to the current element</param>
        /// <param name="ct">complex type with Sequence or Choice Particle</param>
        /// <param name="lastUsedSeqItem">ordinal number in the sequence to indicate current sequence position</param>
        /// <param name="bParticleChanged">This indicates to the caller if Sequence was changed to Choice</param>
        /// <param name="parentSchema">The parent schema.</param>
        /// <param name="setMaxoccurs">Whether set max occurs.</param>
        internal XmlSchemaElement FindMatchingElement(bool bCreatingNewType, XmlReader xtr, XmlSchemaComplexType ct, ref int lastUsedSeqItem, ref bool bParticleChanged, XmlSchema? parentSchema, bool setMaxoccurs)
        {
            if (xtr.NamespaceURI == XmlSchema.Namespace)
            {
                throw new XmlSchemaInferenceException(SR.SchInf_schema, 0, 0);
            }

            bool bItemNotUsedYet = ((lastUsedSeqItem == -1) ? true : false);
            XmlSchemaObjectCollection minOccursCandidates = new XmlSchemaObjectCollection(); //elements that are skipped in the sequence and need minOccurs modified.
            if (ct.Particle!.GetType() == typeof(XmlSchemaSequence))
            {
                string? childURI = xtr.NamespaceURI;
                if (childURI.Length == 0)
                {
                    childURI = null;
                }

                XmlSchemaSequence xss = (XmlSchemaSequence)ct.Particle;
                if (xss.Items.Count < 1 && !bCreatingNewType)
                {
                    lastUsedSeqItem = 0;
                    XmlSchemaElement e = AddElement(xtr.LocalName, xtr.Prefix, xtr.NamespaceURI, parentSchema, xss.Items, -1);
                    e.MinOccurs = 0;
                    return e;
                }
                if (xss.Items[0].GetType() == typeof(XmlSchemaChoice))
                {   // <sequence minOccurs="0" maxOccurs="unbounded"><choice><element>...</choice></sequence>
                    XmlSchemaChoice xsch = (XmlSchemaChoice)xss.Items[0];
                    for (int i = 0; i < xsch.Items.Count; ++i)
                    {
                        XmlSchemaElement? el = xsch.Items[i] as XmlSchemaElement;
                        if (el == null)
                        {
                            throw new XmlSchemaInferenceException(SR.SchInf_UnknownParticle, 0, 0);
                        }
                        if ((el.Name == xtr.LocalName) && (parentSchema!.TargetNamespace == childURI))
                        {   // element is in the same namespace
                            InferElement(el, false, parentSchema);
                            SetMinMaxOccurs(el, setMaxoccurs);
                            return el;
                        }
                        else if ((el.RefName.Name == xtr.LocalName) && (el.RefName.Namespace == xtr.NamespaceURI))
                        {
                            XmlSchemaElement referencedElement = FindGlobalElement(childURI, xtr.LocalName, out parentSchema)!;
                            InferElement(referencedElement, false, parentSchema!);
                            SetMinMaxOccurs(el, setMaxoccurs);
                            return referencedElement;
                        }
                    }
                    XmlSchemaElement subElement = AddElement(xtr.LocalName, xtr.Prefix, xtr.NamespaceURI, parentSchema, xsch.Items, -1);
                    return subElement;
                }
                else
                {   //this should be sequence of elements
                    int iSeqItem = 0;   //iterator through schema sequence items
                    if (lastUsedSeqItem >= 0)
                        iSeqItem = lastUsedSeqItem;
                    XmlSchemaParticle? particle = xss.Items[iSeqItem] as XmlSchemaParticle;
                    XmlSchemaElement? el = particle as XmlSchemaElement;
                    if (el == null)
                    {
                        throw new XmlSchemaInferenceException(SR.SchInf_UnknownParticle, 0, 0);
                    }
                    if (el.Name == xtr.LocalName && parentSchema!.TargetNamespace == childURI)
                    {
                        if (!bItemNotUsedYet)   //read: if item was already used one or more times
                            el.MaxOccurs = decimal.MaxValue;    //set it to unbounded
                        lastUsedSeqItem = iSeqItem;
                        InferElement(el, false, parentSchema);
                        SetMinMaxOccurs(el, false);
                        return el;
                    }
                    else if (el.RefName.Name == xtr.LocalName && el.RefName.Namespace == xtr.NamespaceURI)
                    {
                        if (!bItemNotUsedYet)   //read: if item was already used one or more times
                            el.MaxOccurs = decimal.MaxValue;    //set it to unbounded
                        lastUsedSeqItem = iSeqItem;
                        XmlSchemaElement referencedElement = FindGlobalElement(childURI, xtr.LocalName, out parentSchema)!;
                        InferElement(referencedElement, false, parentSchema!);
                        SetMinMaxOccurs(el, false);
                        return el;
                    }
                    if (bItemNotUsedYet && el.MinOccurs != decimal.Zero)
                        minOccursCandidates.Add(el);
                    iSeqItem++;
                    while (iSeqItem < xss.Items.Count)
                    {
                        particle = xss.Items[iSeqItem] as XmlSchemaParticle;
                        el = particle as XmlSchemaElement;
                        if (el == null)
                        {
                            throw new XmlSchemaInferenceException(SR.SchInf_UnknownParticle, 0, 0);
                        }
                        if (el.Name == xtr.LocalName && parentSchema!.TargetNamespace == childURI)
                        {
                            lastUsedSeqItem = iSeqItem;
                            for (int i = 0; i < minOccursCandidates.Count; ++i)
                            {
                                ((XmlSchemaElement)minOccursCandidates[i]).MinOccurs = decimal.Zero;
                            }
                            InferElement(el, false, parentSchema);
                            SetMinMaxOccurs(el, setMaxoccurs);
                            return el;
                        }
                        else if (el.RefName.Name == xtr.LocalName && el.RefName.Namespace == xtr.NamespaceURI)
                        {
                            lastUsedSeqItem = iSeqItem;
                            for (int i = 0; i < minOccursCandidates.Count; ++i)
                            {
                                ((XmlSchemaElement)minOccursCandidates[i]).MinOccurs = decimal.Zero;
                            }
                            XmlSchemaElement referencedElement = FindGlobalElement(childURI, xtr.LocalName, out parentSchema)!;
                            InferElement(referencedElement!, false, parentSchema!);
                            SetMinMaxOccurs(el, setMaxoccurs);
                            return referencedElement;
                        }


                        minOccursCandidates.Add(el);
                        iSeqItem++;
                    }

                    //element not found in the sequence order, if it is found out of order change Sequence of elements to Sequence of Choices otherwise insert into sequence as optional
                    XmlSchemaElement? subElement;
                    XmlSchemaElement? actualElement = null;
                    //BUGBUG - is this logic correct - if there is a sequence of elements should they be int he parent's namespace.

                    if (parentSchema!.TargetNamespace == childURI)
                    {
                        subElement = FindElement(xss.Items, xtr.LocalName);
                        actualElement = subElement;
                    }
                    else
                    {
                        subElement = FindElementRef(xss.Items, xtr.LocalName, xtr.NamespaceURI);
                        if (subElement != null)
                        {
                            actualElement = FindGlobalElement(childURI, xtr.LocalName, out parentSchema);
                        }
                    }
                    if (null != subElement)
                    {
                        XmlSchemaChoice xsc = new XmlSchemaChoice();
                        xsc.MaxOccurs = decimal.MaxValue;
                        SetMinMaxOccurs(subElement, setMaxoccurs);
                        InferElement(actualElement!, false, parentSchema!);
                        for (int i = 0; i < xss.Items.Count; ++i)
                        {
                            xsc.Items.Add(CreateNewElementforChoice((XmlSchemaElement)xss.Items[i]));
                        }
                        xss.Items.Clear();
                        xss.Items.Add(xsc);
                        return subElement;
                    }
                    else
                    {
                        subElement = AddElement(xtr.LocalName, xtr.Prefix, xtr.NamespaceURI, parentSchema, xss.Items, ++lastUsedSeqItem);
                        if (!bCreatingNewType)
                            subElement.MinOccurs = decimal.Zero;
                        return subElement;
                    }
                }
            }
            else
            {
                throw new XmlSchemaInferenceException(SR.SchInf_noseq, 0, 0);
            }
        }
        internal void ProcessAttributes(ref XmlSchemaElement xse, XmlSchemaType? effectiveSchemaType, bool bCreatingNewType, XmlSchema parentSchema)
        {
            XmlSchemaObjectCollection attributesSeen = new XmlSchemaObjectCollection();
            XmlSchemaComplexType? ct = effectiveSchemaType as XmlSchemaComplexType;

            Debug.Assert(_xtr!.NodeType == XmlNodeType.Attribute);
            do
            {
                if (_xtr.NamespaceURI == XmlSchema.Namespace)
                {
                    throw new XmlSchemaInferenceException(SR.SchInf_schema, 0, 0);
                }

                if (_xtr.NamespaceURI == XmlReservedNs.NsXmlNs)
                {
                    if (_xtr.Prefix == "xmlns")
                        _namespaceManager.AddNamespace(_xtr.LocalName, _xtr.Value);
                }
                else if (_xtr.NamespaceURI == XmlReservedNs.NsXsi)
                {
                    string localName = _xtr.LocalName;
                    if (localName == "nil")
                    {
                        xse.IsNillable = true;
                    }
                    else if (localName != "type" && localName != "schemaLocation" && localName != "noNamespaceSchemaLocation")
                    {
                        throw new XmlSchemaInferenceException(SR.Sch_NotXsiAttribute, localName);
                    }
                }
                else
                {
                    if (ct == null || ct == XmlSchemaComplexType.AnyType)
                    {
                        ct = new XmlSchemaComplexType();
                        xse.SchemaType = ct;
                    }

                    XmlSchemaAttribute? xsa;
                    //The earlier assumption of checking just schemaTypeName !Empty is not correct for schemas that are not generated by us, schemaTypeName can point to any complex type as well
                    //Check that it is a simple type by checking typeCode
                    //Switch to complex type simple content extension
                    if (effectiveSchemaType != null && effectiveSchemaType.Datatype != null && !xse.SchemaTypeName.IsEmpty)
                    {
                        //type was previously simple type, now it will become complex with simple type extension
                        Debug.Assert(ct != null);
                        XmlSchemaSimpleContent sc = new XmlSchemaSimpleContent();
                        ct.ContentModel = sc;
                        XmlSchemaSimpleContentExtension sce = new XmlSchemaSimpleContentExtension();
                        sc.Content = sce;
                        sce.BaseTypeName = xse.SchemaTypeName;
                        sce.LineNumber = xse.LineNumber;
                        xse.LineNumber = 0;
                        xse.SchemaTypeName = XmlQualifiedName.Empty; //re-set the name
                    }

                    Debug.Assert(ct != null); //either the user-defined type itself is a complex type or we switched from a simple type to a complex type
                    if (ct.ContentModel != null)
                    {
                        XmlSchemaSimpleContentExtension sce = CheckSimpleContentExtension(ct);
                        Debug.Assert(sce != null);
                        xsa = AddAttribute(_xtr.LocalName, _xtr.Prefix, _xtr.NamespaceURI, _xtr.Value, bCreatingNewType, parentSchema, sce.Attributes, ct.AttributeUses);
                    }
                    else //add attributes directly to complex type
                    {
                        xsa = AddAttribute(_xtr.LocalName, _xtr.Prefix, _xtr.NamespaceURI, _xtr.Value, bCreatingNewType, parentSchema, ct.Attributes, ct.AttributeUses);
                    }
                    if (xsa != null)
                    {
                        attributesSeen.Add(xsa);
                    }
                }
            } while (_xtr.MoveToNextAttribute());
            if (!bCreatingNewType)
            {
                //make attributes that did not appear this time optional
                if (ct != null)
                {
                    MakeExistingAttributesOptional(ct, attributesSeen);
                }
            }
        }

        private static void MoveAttributes(XmlSchemaSimpleContentExtension scExtension, XmlSchemaComplexType ct)
        {
            //copy all attributes from the simple content to the complex type
            //This is ok since when we move from complex type to simple content extension we copy from AttributeUses property
            for (int i = 0; i < scExtension.Attributes.Count; ++i)  //since simpleContent is being cleared
            {
                ct.Attributes.Add(scExtension.Attributes[i]);
            }
        }

        private static void MoveAttributes(XmlSchemaComplexType ct, XmlSchemaSimpleContentExtension simpleContentExtension, bool bCreatingNewType)
        {
            //copy all attributes from the complex type to the simple content

            ICollection sourceCollection;
            if (!bCreatingNewType && ct.AttributeUses.Count > 0)
            {
                sourceCollection = ct.AttributeUses.Values;
            }
            else
            {
                sourceCollection = ct.Attributes;
            }

            foreach (XmlSchemaAttribute? attr in sourceCollection)
            {
                simpleContentExtension.Attributes.Add(attr!);
            }

            ct.Attributes.Clear(); //Clear from pre-compiled property, post compiled will be cleared on Re-process and Compile()
        }

        internal static XmlSchemaAttribute? FindAttribute(ICollection attributes, string attrName)
        {
            foreach (XmlSchemaObject? xsa in attributes)
            {
                XmlSchemaAttribute? schemaAttribute = xsa as XmlSchemaAttribute;
                if (schemaAttribute != null)
                {
                    if (schemaAttribute.Name == attrName)
                    {
                        return schemaAttribute;
                    }
                }
            }
            return null;
        }

        internal XmlSchemaElement? FindGlobalElement(string? namespaceURI, string localName, out XmlSchema? parentSchema)
        {
            ICollection col = _schemaSet!.Schemas(namespaceURI);
            XmlSchemaElement? xse;
            parentSchema = null;
            foreach (XmlSchema? schema in col)
            {
                xse = FindElement(schema!.Items, localName);
                if (xse != null)
                {
                    parentSchema = schema;
                    return xse;
                }
            }
            return null;
        }


        internal static XmlSchemaElement? FindElement(XmlSchemaObjectCollection elements, string elementName)
        {
            for (int i = 0; i < elements.Count; ++i)
            {
                XmlSchemaElement? xse = elements[i] as XmlSchemaElement;
                if (xse != null && xse.RefName != null)
                {
                    if (xse.Name == elementName)
                    {
                        return xse;
                    }
                }
            }

            return null;
        }

        internal static XmlSchemaAttribute? FindAttributeRef(ICollection attributes, string attributeName, string nsURI)
        {
            foreach (XmlSchemaObject? xsa in attributes)
            {
                XmlSchemaAttribute? schemaAttribute = xsa as XmlSchemaAttribute;
                if (schemaAttribute != null)
                {
                    if (schemaAttribute.RefName.Name == attributeName && schemaAttribute.RefName.Namespace == nsURI)
                    {
                        return schemaAttribute;
                    }
                }
            }

            return null;
        }

        internal static XmlSchemaElement? FindElementRef(XmlSchemaObjectCollection elements, string elementName, string nsURI)
        {
            for (int i = 0; i < elements.Count; ++i)
            {
                XmlSchemaElement? xse = elements[i] as XmlSchemaElement;
                if (xse != null && xse.RefName != null)
                {
                    if (xse.RefName.Name == elementName && xse.RefName.Namespace == nsURI)
                    {
                        return xse;
                    }
                }
            }

            return null;
        }

        internal static void MakeExistingAttributesOptional(XmlSchemaComplexType ct, XmlSchemaObjectCollection? attributesInInstance)
        {
            if (ct == null)
            {
                throw new XmlSchemaInferenceException(SR.SchInf_noct, 0, 0);
            }
            if (ct.ContentModel != null)
            {
                XmlSchemaSimpleContentExtension xssce = CheckSimpleContentExtension(ct);
                SwitchUseToOptional(xssce.Attributes, attributesInInstance);
            }
            else
            { //either <xs:attribute> as child of xs:complexType or the attributes are within the content model
                SwitchUseToOptional(ct.Attributes, attributesInInstance);
            }
        }

        private static void SwitchUseToOptional(XmlSchemaObjectCollection attributes, XmlSchemaObjectCollection? attributesInInstance)
        {
            for (int i = 0; i < attributes.Count; ++i)
            {
                XmlSchemaAttribute? attr = attributes[i] as XmlSchemaAttribute;
                if (attr != null)
                {
                    if (attributesInInstance != null)
                    {
                        if (attr.RefName.Name.Length == 0)
                        { //If the attribute is not present in this instance, make it optional
                            if (null == FindAttribute(attributesInInstance, attr.Name!))
                            {
                                attr.Use = XmlSchemaUse.Optional;
                            }
                        }
                        else
                        {
                            if (null == FindAttributeRef(attributesInInstance, attr.RefName.Name, attr.RefName.Namespace))
                            {
                                attr.Use = XmlSchemaUse.Optional;
                            }
                        }
                    }
                    else
                    {
                        attr.Use = XmlSchemaUse.Optional;
                    }
                }
            }
        }

        internal XmlQualifiedName RefineSimpleType(string s, ref int iTypeFlags)
        {
            bool bNeedsRangeCheck = false;
            s = s.Trim();
            if (iTypeFlags == TF_string || _typeInference == InferenceOption.Relaxed)
                return ST_string;
            iTypeFlags &= InferSimpleType(s, ref bNeedsRangeCheck);
            if (iTypeFlags == TF_string)
                return ST_string;
            if (bNeedsRangeCheck)
            {
                if ((iTypeFlags & TF_byte) != 0)
                {
                    try
                    {
                        XmlConvert.ToSByte(s);
                        //sbyte.Parse(s);
                        if ((iTypeFlags & TF_unsignedByte) != 0)
                            return ST_unsignedByte; //number is positive and fits byte -> it also fits unsignedByte
                        else
                            return ST_byte;
                    }
                    catch (FormatException)
                    { }
                    catch (OverflowException)
                    { }
                    iTypeFlags &= (~TF_byte);
                }
                if ((iTypeFlags & TF_unsignedByte) != 0)
                {
                    try
                    {
                        XmlConvert.ToByte(s);
                        //byte.Parse(s);
                        return ST_unsignedByte;
                    }
                    catch (FormatException)
                    { }
                    catch (OverflowException)
                    { }
                    iTypeFlags &= (~TF_unsignedByte);
                }
                if ((iTypeFlags & TF_short) != 0)
                {
                    try
                    {
                        XmlConvert.ToInt16(s);
                        //short.Parse(s);
                        if ((iTypeFlags & TF_unsignedShort) != 0)
                            return ST_unsignedShort;    //number is positive and fits short -> it also fits unsignedShort
                        else
                            return ST_short;
                    }
                    catch (FormatException)
                    { }
                    catch (OverflowException)
                    { }
                    iTypeFlags &= (~TF_short);
                }
                if ((iTypeFlags & TF_unsignedShort) != 0)
                {
                    try
                    {
                        XmlConvert.ToUInt16(s);
                        //ushort.Parse(s);
                        return ST_unsignedShort;
                    }
                    catch (FormatException)
                    { }
                    catch (OverflowException)
                    { }
                    iTypeFlags &= (~TF_unsignedShort);
                }
                if ((iTypeFlags & TF_int) != 0)
                {
                    try
                    {
                        XmlConvert.ToInt32(s);
                        //int.Parse(s);
                        if ((iTypeFlags & TF_unsignedInt) != 0)
                            return ST_unsignedInt;  //number is positive and fits int -> it also fits unsignedInt
                        else
                            return ST_int;
                    }
                    catch (FormatException)
                    { }
                    catch (OverflowException)
                    { }
                    iTypeFlags &= (~TF_int);
                }
                if ((iTypeFlags & TF_unsignedInt) != 0)
                {
                    try
                    {
                        XmlConvert.ToUInt32(s);
                        //uint.Parse(s);
                        return ST_unsignedInt;
                    }
                    catch (FormatException)
                    { }
                    catch (OverflowException)
                    { }
                    iTypeFlags &= (~TF_unsignedInt);
                }
                if ((iTypeFlags & TF_long) != 0)
                {
                    try
                    {
                        XmlConvert.ToInt64(s);
                        //long.Parse(s);
                        if ((iTypeFlags & TF_unsignedLong) != 0)
                            return ST_unsignedLong; //number is positive and fits long -> it also fits unsignedLong
                        else
                            return ST_long;
                    }
                    catch (FormatException)
                    { }
                    catch (OverflowException)
                    { }
                    iTypeFlags &= (~TF_long);
                }
                if ((iTypeFlags & TF_unsignedLong) != 0)
                {
                    try
                    {
                        XmlConvert.ToUInt64(s);
                        //ulong.Parse(s);
                        return ST_unsignedLong;
                    }
                    catch (FormatException)
                    { }
                    catch (OverflowException)
                    { }
                    iTypeFlags &= (~TF_unsignedLong);
                }
                if ((iTypeFlags & TF_double) != 0)
                {
                    try
                    {
                        double dbValue = XmlConvert.ToDouble(s);
                        if ((iTypeFlags & TF_integer) != 0)
                            return ST_integer;
                        else if ((iTypeFlags & TF_decimal) != 0)
                            return ST_decimal;
                        else
                        {
                            // The value fits into double, but it could be float as well
                            if ((iTypeFlags & TF_float) != 0)
                            {
                                // We used to default to float in this case, so try to fit it into float first
                                try
                                {
                                    float flValue = XmlConvert.ToSingle(s);
                                    // Compare the float and double values. We can't do simple value comparison
                                    //   as conversion from float to double introduces imprecissions which cause problems.
                                    // Instead we will convert both back to string and compare the strings.
                                    if (string.Equals(XmlConvert.ToString(flValue), XmlConvert.ToString(dbValue), StringComparison.OrdinalIgnoreCase))
                                    {
                                        // If we can convert the original string to the exact same value
                                        //   and it still fits into float then we treat it as float
                                        return ST_float;
                                    }
                                }
                                catch (FormatException)
                                { }
                                catch (OverflowException)
                                { }
                            }
                            iTypeFlags &= (~TF_float);
                            return ST_double;
                        }
                    }
                    catch (FormatException)
                    { }
                    catch (OverflowException)
                    { }
                    iTypeFlags &= (~TF_double);
                }
                if ((iTypeFlags & TF_float) != 0)
                {
                    try
                    {
                        XmlConvert.ToSingle(s);
                        if ((iTypeFlags & TF_integer) != 0)
                            return ST_integer;
                        else if ((iTypeFlags & TF_decimal) != 0)
                            return ST_decimal;
                        else
                            return ST_float;
                    }
                    catch (FormatException) { }
                    catch (OverflowException) { }
                    iTypeFlags &= (~TF_float);
                }
                if ((iTypeFlags & TF_integer) != 0)
                    return ST_integer;
                else if ((iTypeFlags & TF_decimal) != 0)
                    return ST_decimal;
                else if (iTypeFlags == (TF_gYearMonth | TF_string))
                {
                    try
                    {
                        XmlConvert.ToDateTime(s, XmlDateTimeSerializationMode.RoundtripKind);
                        return ST_gYearMonth;
                    }
                    catch (FormatException)
                    { }
                    catch (OverflowException)
                    { }
                    iTypeFlags = TF_string;
                    return ST_string;
                }
                else if (iTypeFlags == (TF_duration | TF_string))
                {
                    try
                    {
                        XmlConvert.ToTimeSpan(s);
                        return ST_duration;
                    }
                    catch (FormatException)
                    { }
                    catch (OverflowException)
                    { }
                    iTypeFlags = TF_string;
                    return ST_string;
                }
                else if (iTypeFlags == (TF_boolean | TF_string))
                {
                    return ST_boolean;
                }
            }

            switch (iTypeFlags)
            {
                case TF_string:
                    return ST_string;
                case TF_boolean:
                    return ST_boolean;
                case TF_byte:
                    return ST_byte;
                case TF_unsignedByte:
                    return ST_unsignedByte;
                case TF_short:
                    return ST_short;
                case TF_unsignedShort:
                    return ST_unsignedShort;
                case TF_int:
                    return ST_int;
                case TF_unsignedInt:
                    return ST_unsignedInt;
                case TF_long:
                    return ST_long;
                case TF_unsignedLong:
                    return ST_unsignedLong;
                case TF_integer:
                    return ST_integer;
                case TF_decimal:
                    return ST_decimal;
                case TF_float:
                    return ST_float;
                case TF_double:
                    return ST_double;
                case TF_duration:
                    return ST_duration;
                case TF_dateTime:
                    return ST_dateTime;
                case TF_time:
                    return ST_time;
                case TF_date:
                    return ST_date;
                case TF_gYearMonth:
                    return ST_gYearMonth;

                case TF_boolean | TF_string:
                    return ST_boolean;
                case TF_dateTime | TF_string:
                    return ST_dateTime;
                case TF_date | TF_string:
                    return ST_date;
                case TF_time | TF_string:
                    return ST_time;
                case TF_float | TF_double | TF_string:
                    return ST_float;
                case TF_double | TF_string:
                    return ST_double;

                default:
                    Debug.Fail("Expected type not matched");
                    return ST_string;
            }
            /*          if (currentType == null)
                            return SimpleTypes[newType];
                        else
                            return SimpleTypes[ST_Map[newType,(short) ST_Codes[currentType]]];
                            */
        }

        internal static int InferSimpleType(string s, ref bool bNeedsRangeCheck)
        {
            bool bNegative = false;
            bool bPositive = false;
            bool bDate = false;
            bool bTime = false;
            bool bMissingDay = false;

            if (s.Length == 0) return TF_string;
            int i = 0;
            switch (s[i])
            {
                case 't':
                case 'f':
                    if (s == "true")
                        return TF_boolean | TF_string;
                    else if (s == "false")
                        return TF_boolean | TF_string;
                    else
                        return TF_string;
                case 'N':       //try to match "NaN"
                    if (s == "NaN")
                        return TF_float | TF_double | TF_string;
                    else
                        return TF_string;
                //else
                case 'I':       //try to match "INF"
                INF:
                    if (s.AsSpan(i).SequenceEqual("INF"))
                        return TF_float | TF_double | TF_string;
                    else return TF_string;
                case '.':       //try to match ".9999"  decimal/float/double
                FRACTION:
                    bNeedsRangeCheck = true;
                    i++;
                    if (i == s.Length)
                    {
                        if ((i == 1) || (i == 2 && (bPositive || bNegative)))   //"." "-." "+."
                            return TF_string;
                        else
                            return TF_decimal | TF_float | TF_double | TF_string;
                    }
                    switch (s[i])
                    {
                        case 'e':
                        case 'E':
                            goto EXPONENT;
                        default:
                            if (s[i] >= '0' && s[i] <= '9')
                                goto DEC_PART;
                            else
                                return TF_string;
                    }
                DEC_PART:
                    i++; if (i == s.Length) return TF_decimal | TF_float | TF_double | TF_string; //"9999.9" was matched
                    switch (s[i])
                    {
                        case 'e':
                        case 'E':
                            goto EXPONENT;
                        default:
                            if (s[i] >= '0' && s[i] <= '9')
                                goto DEC_PART;
                            else
                                return TF_string;
                    }
                EXPONENT:
                    i++; if (i == s.Length) return TF_string;
                    switch (s[i])
                    {
                        case '+':
                        case '-':
                            goto E1;
                        default:
                            if (s[i] >= '0' && s[i] <= '9')
                                goto EXP_PART;
                            else
                                return TF_string;
                    }
                E1:
                    i++; if (i == s.Length) return TF_string; //".9999e+" was matched
                    if (s[i] >= '0' && s[i] <= '9')
                        goto EXP_PART;
                    else
                        return TF_string;   //".999e+X was matched
                    EXP_PART:
                    i++; if (i == s.Length) return TF_float | TF_double | TF_string;  //".9999e+99" was matched
                    if (s[i] >= '0' && s[i] <= '9') //".9999e+9
                        goto EXP_PART;
                    else
                        return TF_string;   //".9999e+999X" was matched
                case '-':
                    bNegative = true;
                    i++; if (i == s.Length) return TF_string;
                    switch (s[i])
                    {
                        case 'I':   //try to match "-INF"
                            goto INF;
                        case '.':   //try to match "-.9999"
                            goto FRACTION;
                        case 'P':
                            goto DURATION;
                        default:
                            if (s[i] >= '0' && s[i] <= '9') //-9
                                goto NUMBER;
                            else return TF_string;
                    }
                case '+':
                    bPositive = true;
                    i++; if (i == s.Length) return TF_string;
                    switch (s[i])
                    {
                        case '.':   //try to match "+.9999"
                            goto FRACTION;
                        case 'P':
                            goto DURATION;
                        default:
                            if (s[i] >= '0' && s[i] <= '9') //"+9
                                goto NUMBER;
                            else return TF_string;
                    }
                case 'P':       //try to match duration
                DURATION:
                    i++; if (i == s.Length) return TF_string;
                    switch (s[i])
                    {
                        case 'T':
                            goto D7;
                        default:
                            if (s[i] >= '0' && s[i] <= '9') //"P9"
                                goto D1;
                            else return TF_string;
                    }
                D1:
                    i++; if (i == s.Length) return TF_string; //"P999" was matched
                    switch (s[i])
                    {
                        case 'Y':
                            goto D2;
                        case 'M':
                            goto D4;
                        case 'D':
                            goto D6;
                        default:
                            if (s[i] >= '0' && s[i] <= '9')
                                goto D1;
                            else
                                return TF_string;
                    }
                D2:
                    i++;
                    if (i == s.Length)
                    {
                        bNeedsRangeCheck = true;
                        return TF_duration | TF_string; //"P999Y" was matched
                    }
                    switch (s[i])
                    {
                        case 'T':
                            goto D7;
                        default:
                            if (s[i] >= '0' && s[i] <= '9')
                                goto D3;
                            else
                                return TF_string;
                    }
                D3:
                    i++; if (i == s.Length) return TF_string; //"P999Y9" was matched
                    switch (s[i])
                    {
                        case 'M':
                            goto D4;
                        case 'D':
                            goto D6;
                        default:
                            if (s[i] >= '0' && s[i] <= '9')
                                goto D3;
                            else
                                return TF_string;
                    }
                D4:
                    i++;
                    if (i == s.Length)
                    {
                        bNeedsRangeCheck = true;
                        return TF_duration | TF_string; //"P999Y999M" was matched
                    }
                    switch (s[i])
                    {
                        case 'T':
                            goto D7;
                        default:
                            if (s[i] >= '0' && s[i] <= '9')
                                goto D5;
                            else
                                return TF_string;
                    }
                D5:
                    i++; if (i == s.Length) return TF_string; //"P999Y999M9" was matched
                    switch (s[i])
                    {
                        case 'D':
                            goto D6;
                        default:
                            if (s[i] >= '0' && s[i] <= '9')
                                goto D5;
                            else
                                return TF_string;
                    }
                D6:
                    i++;
                    if (i == s.Length)
                    {
                        bNeedsRangeCheck = true;
                        return TF_duration | TF_string; //"P999Y999M999D" was matched
                    }
                    switch (s[i])
                    {
                        case 'T':
                            goto D7;
                        default:
                            return TF_string;
                    }
                D7:
                    i++; if (i == s.Length) return TF_string; //"P999Y999M9999DT" was matched
                    if (s[i] >= '0' && s[i] <= '9')
                        goto D8;
                    else
                        return TF_string;
                    D8:
                    i++; if (i == s.Length) return TF_string; //"___T9" was matched
                    switch (s[i])
                    {
                        case 'H':
                            goto D9;
                        case 'M':
                            goto D11;
                        case '.':
                            goto D13;
                        case 'S':
                            goto D15;
                        default:
                            if (s[i] >= '0' && s[i] <= '9')
                                goto D8;
                            else
                                return TF_string;
                    }
                D9:
                    i++;
                    if (i == s.Length)
                    {
                        bNeedsRangeCheck = true;
                        return TF_duration | TF_string; //"___T999H" was matched
                    }
                    if (s[i] >= '0' && s[i] <= '9')
                        goto D10;
                    else
                        return TF_string;
                    D10:
                    i++; if (i == s.Length) return TF_string; //"___T999H9" was matched
                    switch (s[i])
                    {
                        case 'M':
                            goto D11;
                        case '.':
                            goto D13;
                        case 'S':
                            goto D15;
                        default:
                            if (s[i] >= '0' && s[i] <= '9')
                                goto D10;
                            else
                                return TF_string;
                    }
                D11:
                    i++;
                    if (i == s.Length)
                    {
                        bNeedsRangeCheck = true;
                        return TF_duration | TF_string; //"___T999H999M" was matched
                    }
                    if (s[i] >= '0' && s[i] <= '9')
                        goto D12;
                    else
                        return TF_string;
                    D12:
                    i++; if (i == s.Length) return TF_string; //"___T999H999M9" was matched
                    switch (s[i])
                    {
                        case '.':
                            goto D13;
                        case 'S':
                            goto D15;
                        default:
                            if (s[i] >= '0' && s[i] <= '9')
                                goto D12;
                            else
                                return TF_string;
                    }
                D13:
                    i++;
                    if (i == s.Length)
                    {
                        bNeedsRangeCheck = true;
                        return TF_duration | TF_string; //"___T999H999M999." was matched
                    }
                    if (s[i] >= '0' && s[i] <= '9')
                        goto D14;
                    else
                        return TF_string;
                    D14:
                    i++; if (i == s.Length) return TF_string; //"___T999H999M999.9" was matched
                    switch (s[i])
                    {
                        case 'S':
                            goto D15;
                        default:
                            if (s[i] >= '0' && s[i] <= '9')
                                goto D14;
                            else
                                return TF_string;
                    }
                D15:
                    i++;
                    if (i == s.Length)
                    {
                        bNeedsRangeCheck = true;
                        return TF_duration | TF_string; //"___T999H999M999.999S" was matched
                    }
                    else return TF_string;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                NUMBER:
                    i++;
                    if (i == s.Length)
                    {
                        bNeedsRangeCheck = true;
                        if (bNegative || bPositive)
                            return TF_byte | TF_short | TF_int | TF_long | TF_integer | TF_decimal | TF_float | TF_double | TF_string;  //"-9"
                        else
                        {
                            if (s == "0" || s == "1")
                                return TF_boolean | TF_byte | TF_short | TF_int | TF_long | TF_integer | TF_decimal | TF_float | TF_double |
                                    TF_unsignedByte | TF_unsignedShort | TF_unsignedInt | TF_unsignedLong | TF_string;
                            else
                                return TF_byte | TF_short | TF_int | TF_long | TF_integer | TF_decimal | TF_float | TF_double |
                                    TF_unsignedByte | TF_unsignedShort | TF_unsignedInt | TF_unsignedLong | TF_string;
                        }
                    }
                    switch (s[i])
                    {
                        case '.':
                            goto FRACTION;
                        case 'e':
                        case 'E':
                            bNeedsRangeCheck = true;
                            return TF_float | TF_double | TF_string;
                        default:
                            if (s[i] >= '0' && s[i] <= '9')
                                goto N2;
                            else
                                return TF_string;
                    }
                N2:
                    i++;
                    if (i == s.Length)
                    {
                        bNeedsRangeCheck = true;
                        if (bNegative || bPositive)
                            return TF_byte | TF_short | TF_int | TF_long | TF_integer | TF_decimal | TF_float | TF_double | TF_string;  //"-9"
                        else
                            return TF_byte | TF_short | TF_int | TF_long | TF_integer | TF_decimal | TF_float | TF_double |
                                TF_unsignedByte | TF_unsignedShort | TF_unsignedInt | TF_unsignedLong | TF_string;
                    }
                    switch (s[i])
                    {
                        case '.':
                            goto FRACTION;
                        case ':':
                            bTime = true;
                            goto MINUTE;
                        case 'e':
                        case 'E':
                            bNeedsRangeCheck = true;
                            return TF_float | TF_double | TF_string;
                        default:
                            if (s[i] >= '0' && s[i] <= '9')
                                goto N3;
                            else
                                return TF_string;
                    }

                N3:
                    i++;
                    if (i == s.Length)
                    {
                        bNeedsRangeCheck = true; //three digits may not fit byte and unsignedByte
                        if (bNegative || bPositive)
                            return TF_byte | TF_short | TF_int | TF_long | TF_integer | TF_decimal | TF_float | TF_double | TF_string;  //"-9"
                        else
                            return TF_byte | TF_short | TF_int | TF_long | TF_integer | TF_decimal | TF_float | TF_double |
                                TF_unsignedByte | TF_unsignedShort | TF_unsignedInt | TF_unsignedLong | TF_string;
                    }
                    switch (s[i])
                    {
                        case '.':
                            goto FRACTION;
                        case 'e':
                        case 'E':
                            bNeedsRangeCheck = true;
                            return TF_float | TF_double | TF_string;
                        default:
                            if (s[i] >= '0' && s[i] <= '9')
                                goto N4;
                            else
                                return TF_string;
                    }
                N4:
                    i++;
                    if (i == s.Length)
                    {
                        bNeedsRangeCheck = true;
                        if (bNegative || bPositive)
                            return TF_byte | TF_short | TF_int | TF_long | TF_integer | TF_decimal | TF_float | TF_double | TF_string;  //"-9"
                        else
                            return TF_byte | TF_short | TF_int | TF_long | TF_integer | TF_decimal | TF_float | TF_double |
                                TF_unsignedByte | TF_unsignedShort | TF_unsignedInt | TF_unsignedLong | TF_string;
                    }

                    switch (s[i])
                    {
                        case '-':
                            bDate = true;
                            goto DATE;
                        case '.':
                            goto FRACTION;
                        case 'e':
                        case 'E':
                            bNeedsRangeCheck = true;
                            return TF_float | TF_double | TF_string;
                        default:
                            if (s[i] >= '0' && s[i] <= '9')
                                goto N4;
                            else
                                return TF_string;
                    }
                DATE:
                    i++; if (i == s.Length) return TF_string; //"9999-"
                    if (s[i] < '0' || s[i] > '9')
                        return TF_string;
                    i++; if (i == s.Length) return TF_string; //"9999-9"
                    if (s[i] < '0' || s[i] > '9')
                        return TF_string;
                    i++;
                    if (i == s.Length)
                    {
                        bNeedsRangeCheck = true;
                        return TF_gYearMonth | TF_string;   //"9999-99"
                    }
                    switch (s[i])
                    {
                        case '-':
                            goto DAY;
                        case 'Z':
                        case 'z':
                            bMissingDay = true;
                            goto ZULU;
                        case '+':
                            bMissingDay = true;
                            goto ZONE_SHIFT;
                        default:
                            return TF_string;
                    }
                DAY:
                    i++; if (i == s.Length) return TF_string; //"9999-99-"
                    if (s[i] < '0' || s[i] > '9')
                        return TF_string;
                    i++; if (i == s.Length) return TF_string; //"9999-99-9"
                    if (s[i] < '0' || s[i] > '9')
                        return TF_string;
                    i++; if (i == s.Length) return DateTime(s, bDate, bTime); //"9999-99-99"
                    switch (s[i])
                    {
                        case 'Z':
                        case 'z':
                            goto ZULU;
                        case '+':
                        case '-':
                            goto ZONE_SHIFT;
                        case 'T':
                            bTime = true;
                            goto TIME;
                        case ':':
                            bMissingDay = true;
                            goto ZONE_SHIFT_MINUTE;
                        default:
                            return TF_string;
                    }
                ZULU:
                    i++;
                    if (i == s.Length)
                    {
                        if (bMissingDay)
                        {
                            bNeedsRangeCheck = true;
                            return TF_gYearMonth | TF_string;
                        }
                        else
                        {
                            return DateTime(s, bDate, bTime);
                        }
                    }
                    else
                        return TF_string;
                    ZONE_SHIFT:
                    i++; if (i == s.Length) return TF_string;
                    if (s[i] < '0' || s[i] > '9')
                        return TF_string;
                    i++; if (i == s.Length) return TF_string;
                    if (s[i] < '0' || s[i] > '9')
                        return TF_string;
                    i++; if (i == s.Length) return TF_string;
                    if (s[i] != ':')
                        return TF_string;
                    ZONE_SHIFT_MINUTE:
                    i++; if (i == s.Length) return TF_string;
                    if (s[i] < '0' || s[i] > '9')
                        return TF_string;
                    i++; if (i == s.Length) return TF_string;
                    if (s[i] < '0' || s[i] > '9')
                        return TF_string;
                    i++;
                    if (i == s.Length)
                    {
                        if (bMissingDay)
                        {
                            bNeedsRangeCheck = true;
                            return TF_gYearMonth | TF_string;
                        }
                        else
                        {
                            return DateTime(s, bDate, bTime);
                        }
                    }
                    else return TF_string;
                    TIME:
                    i++; if (i == s.Length) return TF_string;
                    if (s[i] < '0' || s[i] > '9')
                        return TF_string;
                    i++; if (i == s.Length) return TF_string;
                    if (s[i] < '0' || s[i] > '9')
                        return TF_string;
                    i++; if (i == s.Length) return TF_string;
                    if (s[i] != ':')
                        return TF_string;
                    MINUTE:
                    i++; if (i == s.Length) return TF_string;
                    if (s[i] < '0' || s[i] > '9')
                        return TF_string;
                    i++; if (i == s.Length) return TF_string;
                    if (s[i] < '0' || s[i] > '9')
                        return TF_string;
                    i++; if (i == s.Length) return TF_string;
                    if (s[i] != ':')
                        return TF_string;
                    i++; if (i == s.Length) return TF_string;
                    if (s[i] < '0' || s[i] > '9')
                        return TF_string;
                    i++; if (i == s.Length) return TF_string;
                    if (s[i] < '0' || s[i] > '9')
                        return TF_string;
                    i++; if (i == s.Length) return DateTime(s, bDate, bTime);
                    switch (s[i])
                    {
                        case 'Z':
                        case 'z':
                            goto ZULU;
                        case '+':
                        case '-':
                            goto ZONE_SHIFT;
                        case '.':
                            goto SECOND_FRACTION;
                        default:
                            return TF_string;
                    }
                SECOND_FRACTION:
                    i++; if (i == s.Length) return TF_string;
                    if (s[i] < '0' || s[i] > '9')
                        return TF_string;
                    FRACT_DIGITS:
                    i++; if (i == s.Length) return DateTime(s, bDate, bTime);
                    switch (s[i])
                    {
                        case 'Z':
                        case 'z':
                            goto ZULU;
                        case '+':
                        case '-':
                            goto ZONE_SHIFT;
                        default:
                            if (s[i] >= '0' && s[i] <= '9')
                                goto FRACT_DIGITS;
                            else
                                return TF_string;
                    }
                default:
                    return TF_string;
            }
        }

        internal static int DateTime(string s, bool bDate, bool bTime)
        {
            try
            {
                XmlConvert.ToDateTime(s, XmlDateTimeSerializationMode.RoundtripKind);
            }
            catch (FormatException)
            {
                return TF_string;
            }
            if (bDate && bTime)
                return TF_dateTime | TF_string;
            else if (bDate)
                return TF_date | TF_string;
            else if (bTime)
                return TF_time | TF_string;
            else
            {
                Debug.Fail("Expected date, time or dateTime");
                return TF_string;
            }
        }
        private XmlSchemaElement CreateNewElementforChoice(XmlSchemaElement copyElement)
        {
            XmlSchemaElement newElement = new XmlSchemaElement();
            newElement.Annotation = copyElement.Annotation;
            newElement.Block = copyElement.Block;
            newElement.DefaultValue = copyElement.DefaultValue;
            newElement.Final = copyElement.Final;
            newElement.FixedValue = copyElement.FixedValue;
            newElement.Form = copyElement.Form;
            newElement.Id = copyElement.Id;
            //                newElement.IsAbstract = copyElement.IsAbstract;
            if (copyElement.IsNillable)
            {
                newElement.IsNillable = copyElement.IsNillable;
            }
            newElement.LineNumber = copyElement.LineNumber;
            newElement.LinePosition = copyElement.LinePosition;
            newElement.Name = copyElement.Name;
            newElement.Namespaces = copyElement.Namespaces;
            newElement.RefName = copyElement.RefName;
            newElement.SchemaType = copyElement.SchemaType;
            newElement.SchemaTypeName = copyElement.SchemaTypeName;
            newElement.SourceUri = copyElement.SourceUri;
            newElement.SubstitutionGroup = copyElement.SubstitutionGroup;
            newElement.UnhandledAttributes = copyElement.UnhandledAttributes;
            if (copyElement.MinOccurs != decimal.One && this.Occurrence == InferenceOption.Relaxed)
            {
                newElement.MinOccurs = copyElement.MinOccurs;
            }
            if (copyElement.MaxOccurs != decimal.One)
            {
                newElement.MaxOccurs = copyElement.MaxOccurs;
            }
            return newElement;
        }

        private static int GetSchemaType(XmlQualifiedName qname)
        {
            if (qname == SimpleTypes[HC_ST_boolean])
            {
                return TF_boolean | TF_string;
            }
            if (qname == SimpleTypes[HC_ST_byte])
            {
                return TF_byte | TF_short | TF_int | TF_long | TF_integer | TF_decimal | TF_float | TF_double | TF_string;
            }
            if (qname == SimpleTypes[HC_ST_unsignedByte])
            {
                return TF_byte | TF_short | TF_int | TF_long | TF_integer | TF_decimal | TF_float | TF_double |
                                    TF_unsignedByte | TF_unsignedShort | TF_unsignedInt | TF_unsignedLong | TF_string;
            }
            if (qname == SimpleTypes[HC_ST_short])
            {
                return TF_short | TF_int | TF_long | TF_integer | TF_decimal | TF_float | TF_double | TF_string;
            }
            if (qname == SimpleTypes[HC_ST_unsignedShort])
            {
                return TF_short | TF_int | TF_long | TF_integer | TF_decimal | TF_float | TF_double |
                                     TF_unsignedShort | TF_unsignedInt | TF_unsignedLong | TF_string;
            }
            if (qname == SimpleTypes[HC_ST_int])
            {
                return TF_int | TF_long | TF_integer | TF_decimal | TF_float | TF_double | TF_string;
            }
            if (qname == SimpleTypes[HC_ST_unsignedInt])
            {
                return TF_int | TF_long | TF_integer | TF_decimal | TF_float | TF_double |
                                      TF_unsignedInt | TF_unsignedLong | TF_string;
            }
            if (qname == SimpleTypes[HC_ST_long])
            {
                return TF_long | TF_integer | TF_decimal | TF_float | TF_double | TF_string;
            }
            if (qname == SimpleTypes[HC_ST_unsignedLong])
            {
                return TF_long | TF_integer | TF_decimal | TF_float | TF_double |
                                      TF_unsignedLong | TF_string;
            }
            if (qname == SimpleTypes[HC_ST_integer])
            {
                return TF_integer | TF_decimal | TF_float | TF_double | TF_string;
            }
            if (qname == SimpleTypes[HC_ST_decimal])
            {
                return TF_decimal | TF_float | TF_double | TF_string;
            }
            if (qname == SimpleTypes[HC_ST_float])
            {
                return TF_float | TF_double | TF_string;
            }
            if (qname == SimpleTypes[HC_ST_double])
            {
                return TF_double | TF_string;
            }
            if (qname == SimpleTypes[HC_ST_duration])
            {
                return TF_duration | TF_string;
            }
            if (qname == SimpleTypes[HC_ST_dateTime])
            {
                return TF_dateTime | TF_string;
            }
            if (qname == SimpleTypes[HC_ST_time])
            {
                return TF_time | TF_string;
            }
            if (qname == SimpleTypes[HC_ST_date])
            {
                return TF_date;
            }
            if (qname == SimpleTypes[HC_ST_gYearMonth])
            {
                return TF_gYearMonth;
            }
            if (qname == SimpleTypes[HC_ST_string])
            {
                return TF_string;
            }
            if (qname == null || qname.IsEmpty)
            {
                return -1;
            }
            throw new XmlSchemaInferenceException(SR.SchInf_schematype, 0, 0);
        }

        internal void SetMinMaxOccurs(XmlSchemaElement el, bool setMaxOccurs)
        {
            if (this.Occurrence == InferenceOption.Relaxed)
            {
                if (setMaxOccurs || el.MaxOccurs > 1)
                {
                    el.MaxOccurs = decimal.MaxValue;    //set it to unbounded
                }
                el.MinOccurs = 0;
            }
            else if (el.MinOccurs > 1)
            {
                el.MinOccurs = 1;
            }
        }
    }
}
