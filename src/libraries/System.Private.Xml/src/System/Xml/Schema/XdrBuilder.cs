// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Schema
{
    /*
     * The XdrBuilder class parses the XDR Schema and
     * builds internal validation information
     */
    internal sealed class XdrBuilder : SchemaBuilder
    {
        private const int XdrSchema = 1;
        private const int XdrElementType = 2;
        private const int XdrAttributeType = 3;
        private const int XdrElement = 4;
        private const int XdrAttribute = 5;
        private const int XdrGroup = 6;
        private const int XdrElementDatatype = 7;
        private const int XdrAttributeDatatype = 8;

        private const int SchemaFlagsNs = 0x0100;

        private const int StackIncrement = 10;


        private const int SchemaOrderNone = 0;
        private const int SchemaOrderMany = 1;
        private const int SchemaOrderSequence = 2;
        private const int SchemaOrderChoice = 3;
        private const int SchemaOrderAll = 4;

        private const int SchemaContentNone = 0;
        private const int SchemaContentEmpty = 1;
        private const int SchemaContentText = 2;
        private const int SchemaContentMixed = 3;
        private const int SchemaContentElement = 4;


        private sealed class DeclBaseInfo
        {
            // used for <element... or <attribute...
            internal XmlQualifiedName _Name;
            internal string? _Prefix;
            internal XmlQualifiedName _TypeName;
            internal string? _TypePrefix;
            internal object? _Default;
            internal object? _Revises;
            internal uint _MaxOccurs;
            internal uint _MinOccurs;

            // used for checking undeclared attribute type
            internal bool _Checking;
            internal SchemaElementDecl? _ElementDecl;
            internal SchemaAttDef? _Attdef;
            internal DeclBaseInfo? _Next;

            internal DeclBaseInfo()
            {
                Reset();
            }

            [MemberNotNull(nameof(_Name))]
            [MemberNotNull(nameof(_TypeName))]
            internal void Reset()
            {
                _Name = XmlQualifiedName.Empty;
                _Prefix = null;
                _TypeName = XmlQualifiedName.Empty;
                _TypePrefix = null;
                _Default = null;
                _Revises = null;
                _MaxOccurs = 1;
                _MinOccurs = 1;
                _Checking = false;
                _ElementDecl = null;
                _Next = null;
                _Attdef = null;
            }
        };

        private sealed class GroupContent
        {
            internal uint _MinVal;
            internal uint _MaxVal;
            internal bool _HasMaxAttr;
            internal bool _HasMinAttr;
            internal int _Order;

            internal static void Copy(GroupContent from, GroupContent to)
            {
                to._MinVal = from._MinVal;
                to._MaxVal = from._MaxVal;
                to._Order = from._Order;
            }

            internal static GroupContent Copy(GroupContent other)
            {
                GroupContent g = new GroupContent();
                Copy(other, g);
                return g;
            }
        };

        private sealed class ElementContent
        {
            // for <ElementType ...
            internal SchemaElementDecl? _ElementDecl;          // Element Information

            internal int _ContentAttr;              // content attribute
            internal int _OrderAttr;                // order attribute

            internal bool _MasterGroupRequired;      // In a situation like <!ELEMENT root (e1)> e1 has to have a ()
            internal bool _ExistTerminal;            // when there exist a terminal, we need to addOrder before

            // we can add another terminal
            internal bool _AllowDataType;            // must have textOnly if we have datatype
            internal bool _HasDataType;              // got data type

            // for <element ...
            internal bool _HasType;                  // user must have a type attribute in <element ...
            internal bool _EnumerationRequired;
            internal uint _MinVal;
            internal uint _MaxVal;                   // -1 means infinity

            internal uint _MaxLength;                // dt:maxLength
            internal uint _MinLength;                // dt:minLength

            internal Hashtable? _AttDefList;               // a list of current AttDefs for the <ElementType ...
            // only the used one will be added
        };

        private sealed class AttributeContent
        {
            // used for <AttributeType ...
            internal SchemaAttDef? _AttDef;

            // used to store name & prefix for the AttributeType
            internal XmlQualifiedName? _Name;
            internal string? _Prefix;
            internal bool _Required;                  // true:  when the attribute required="yes"

            // used for both AttributeType and attribute
            internal uint _MinVal;
            internal uint _MaxVal;                   // -1 means infinity

            internal uint _MaxLength;                 // dt:maxLength
            internal uint _MinLength;                 // dt:minLength

            // used for datatype
            internal bool _EnumerationRequired;       // when we have dt:value then we must have dt:type="enumeration"
            internal bool _HasDataType;

            // used for <attribute ...
            internal bool _Global;

            internal object? _Default;
        };

        private delegate void XdrBuildFunction(XdrBuilder builder, object obj, string prefix);
        private delegate void XdrInitFunction(XdrBuilder builder, object obj);
        private delegate void XdrBeginChildFunction(XdrBuilder builder);
        private delegate void XdrEndChildFunction(XdrBuilder builder);

        private sealed class XdrAttributeEntry
        {
            internal SchemaNames.Token _Attribute;     // possible attribute names
            internal int _SchemaFlags;
            internal XmlSchemaDatatype? _Datatype;
            internal XdrBuildFunction _BuildFunc;     // Corresponding build functions for attribute value

            internal XdrAttributeEntry(SchemaNames.Token a, XmlTokenizedType ttype, XdrBuildFunction build)
            {
                _Attribute = a;
                _Datatype = XmlSchemaDatatype.FromXmlTokenizedType(ttype);
                _SchemaFlags = 0;
                _BuildFunc = build;
            }
            internal XdrAttributeEntry(SchemaNames.Token a, XmlTokenizedType ttype, int schemaFlags, XdrBuildFunction build)
            {
                _Attribute = a;
                _Datatype = XmlSchemaDatatype.FromXmlTokenizedType(ttype);
                _SchemaFlags = schemaFlags;
                _BuildFunc = build;
            }
        };

        //
        // XdrEntry controls the states of parsing a schema document
        // and calls the corresponding "init", "end" and "build" functions when necessary
        //
        private sealed class XdrEntry
        {
            internal SchemaNames.Token _Name;               // the name of the object it is comparing to
            internal int[]? _NextStates;         // possible next states
            internal XdrAttributeEntry[]? _Attributes;         // allowed attributes
            internal XdrInitFunction? _InitFunc;           // "init" functions in XdrBuilder
            internal XdrBeginChildFunction? _BeginChildFunc;     // "begin" functions in XdrBuilder for BeginChildren
            internal XdrEndChildFunction? _EndChildFunc;       // "end" functions in XdrBuilder for EndChildren
            internal bool _AllowText;          // whether text content is allowed

            internal XdrEntry(SchemaNames.Token n,
                              int[]? states,
                              XdrAttributeEntry[]? attributes,
                              XdrInitFunction? init,
                              XdrBeginChildFunction? begin,
                              XdrEndChildFunction? end,
                              bool fText)
            {
                _Name = n;
                _NextStates = states;
                _Attributes = attributes;
                _InitFunc = init;
                _BeginChildFunc = begin;
                _EndChildFunc = end;
                _AllowText = fText;
            }
        };


        /////////////////////////////////////////////////////////////////////////////
        // Data structures for XML-Data Reduced (XDR Schema)
        //

        //
        // Elements
        //
        private static readonly int[] s_XDR_Root_Element = { XdrSchema };
        private static readonly int[] s_XDR_Root_SubElements = { XdrElementType, XdrAttributeType };
        private static readonly int[] s_XDR_ElementType_SubElements = { XdrElement, XdrGroup, XdrAttributeType, XdrAttribute, XdrElementDatatype };
        private static readonly int[] s_XDR_AttributeType_SubElements = { XdrAttributeDatatype };
        private static readonly int[] s_XDR_Group_SubElements = { XdrElement, XdrGroup };

        //
        // Attributes
        //
        private static readonly XdrAttributeEntry[] s_XDR_Root_Attributes =
        {
            new XdrAttributeEntry(SchemaNames.Token.SchemaName, XmlTokenizedType.CDATA, new XdrBuildFunction(XDR_BuildRoot_Name) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaId,   XmlTokenizedType.QName, new XdrBuildFunction(XDR_BuildRoot_ID) )
        };

        private static readonly XdrAttributeEntry[] s_XDR_ElementType_Attributes =
        {
            new XdrAttributeEntry(SchemaNames.Token.SchemaName,         XmlTokenizedType.QName, SchemaFlagsNs,  new XdrBuildFunction(XDR_BuildElementType_Name) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaContent,      XmlTokenizedType.QName,    new XdrBuildFunction(XDR_BuildElementType_Content) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaModel,        XmlTokenizedType.QName,    new XdrBuildFunction(XDR_BuildElementType_Model) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaOrder,        XmlTokenizedType.QName,    new XdrBuildFunction(XDR_BuildElementType_Order) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaDtType,       XmlTokenizedType.CDATA,    new XdrBuildFunction(XDR_BuildElementType_DtType) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaDtValues,     XmlTokenizedType.NMTOKENS, new XdrBuildFunction(XDR_BuildElementType_DtValues) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaDtMaxLength,  XmlTokenizedType.CDATA,    new XdrBuildFunction(XDR_BuildElementType_DtMaxLength) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaDtMinLength,  XmlTokenizedType.CDATA,    new XdrBuildFunction(XDR_BuildElementType_DtMinLength) )
        };

        private static readonly XdrAttributeEntry[] s_XDR_AttributeType_Attributes =
        {
            new XdrAttributeEntry(SchemaNames.Token.SchemaName,         XmlTokenizedType.QName,     new XdrBuildFunction(XDR_BuildAttributeType_Name) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaRequired,     XmlTokenizedType.QName,     new XdrBuildFunction(XDR_BuildAttributeType_Required) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaDefault,      XmlTokenizedType.CDATA,     new XdrBuildFunction(XDR_BuildAttributeType_Default) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaDtType,       XmlTokenizedType.QName,     new XdrBuildFunction(XDR_BuildAttributeType_DtType) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaDtValues,     XmlTokenizedType.NMTOKENS,  new XdrBuildFunction(XDR_BuildAttributeType_DtValues) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaDtMaxLength,  XmlTokenizedType.CDATA,     new XdrBuildFunction(XDR_BuildAttributeType_DtMaxLength) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaDtMinLength,  XmlTokenizedType.CDATA,     new XdrBuildFunction(XDR_BuildAttributeType_DtMinLength) )
        };

        private static readonly XdrAttributeEntry[] s_XDR_Element_Attributes =
        {
            new XdrAttributeEntry(SchemaNames.Token.SchemaType,      XmlTokenizedType.QName, SchemaFlagsNs,  new XdrBuildFunction(XDR_BuildElement_Type) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaMinOccurs, XmlTokenizedType.CDATA,     new XdrBuildFunction(XDR_BuildElement_MinOccurs) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaMaxOccurs, XmlTokenizedType.CDATA,     new XdrBuildFunction(XDR_BuildElement_MaxOccurs) )
        };

        private static readonly XdrAttributeEntry[] s_XDR_Attribute_Attributes =
        {
            new XdrAttributeEntry(SchemaNames.Token.SchemaType,       XmlTokenizedType.QName,   new XdrBuildFunction(XDR_BuildAttribute_Type) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaRequired,   XmlTokenizedType.QName,   new XdrBuildFunction(XDR_BuildAttribute_Required) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaDefault,    XmlTokenizedType.CDATA,   new XdrBuildFunction(XDR_BuildAttribute_Default) )
        };

        private static readonly XdrAttributeEntry[] s_XDR_Group_Attributes =
        {
            new XdrAttributeEntry(SchemaNames.Token.SchemaOrder,     XmlTokenizedType.QName,   new XdrBuildFunction(XDR_BuildGroup_Order) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaMinOccurs, XmlTokenizedType.CDATA,   new XdrBuildFunction(XDR_BuildGroup_MinOccurs) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaMaxOccurs, XmlTokenizedType.CDATA,   new XdrBuildFunction(XDR_BuildGroup_MaxOccurs) )
        };

        private static readonly XdrAttributeEntry[] s_XDR_ElementDataType_Attributes =
        {
            new XdrAttributeEntry(SchemaNames.Token.SchemaDtType,        XmlTokenizedType.CDATA,    new XdrBuildFunction(XDR_BuildElementType_DtType) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaDtValues,      XmlTokenizedType.NMTOKENS, new XdrBuildFunction(XDR_BuildElementType_DtValues) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaDtMaxLength,   XmlTokenizedType.CDATA,    new XdrBuildFunction(XDR_BuildElementType_DtMaxLength) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaDtMinLength,   XmlTokenizedType.CDATA,    new XdrBuildFunction(XDR_BuildElementType_DtMinLength) )
        };

        private static readonly XdrAttributeEntry[] s_XDR_AttributeDataType_Attributes =
        {
            new XdrAttributeEntry(SchemaNames.Token.SchemaDtType,        XmlTokenizedType.QName,    new XdrBuildFunction(XDR_BuildAttributeType_DtType) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaDtValues,      XmlTokenizedType.NMTOKENS, new XdrBuildFunction(XDR_BuildAttributeType_DtValues) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaDtMaxLength,   XmlTokenizedType.CDATA,    new XdrBuildFunction(XDR_BuildAttributeType_DtMaxLength) ),
            new XdrAttributeEntry(SchemaNames.Token.SchemaDtMinLength,   XmlTokenizedType.CDATA,    new XdrBuildFunction(XDR_BuildAttributeType_DtMinLength) )
};
        //
        // Schema entries
        //
        private static readonly XdrEntry[] s_schemaEntries =
        {
            new XdrEntry( SchemaNames.Token.Empty,     s_XDR_Root_Element, null,
                          null,
                          null,
                          null,
                          false),
            new XdrEntry( SchemaNames.Token.XdrRoot,     s_XDR_Root_SubElements, s_XDR_Root_Attributes,
                          new XdrInitFunction(XDR_InitRoot),
                          new XdrBeginChildFunction(XDR_BeginRoot),
                          new XdrEndChildFunction(XDR_EndRoot),
                          false),
            new XdrEntry( SchemaNames.Token.XdrElementType,    s_XDR_ElementType_SubElements, s_XDR_ElementType_Attributes,
                          new XdrInitFunction(XDR_InitElementType),
                          new XdrBeginChildFunction(XDR_BeginElementType),
                          new XdrEndChildFunction(XDR_EndElementType),
                          false),
            new XdrEntry( SchemaNames.Token.XdrAttributeType,  s_XDR_AttributeType_SubElements, s_XDR_AttributeType_Attributes,
                          new XdrInitFunction(XDR_InitAttributeType),
                          new XdrBeginChildFunction(XDR_BeginAttributeType),
                          new XdrEndChildFunction(XDR_EndAttributeType),
                          false),
            new XdrEntry( SchemaNames.Token.XdrElement,        null, s_XDR_Element_Attributes,
                          new XdrInitFunction(XDR_InitElement),
                          null,
                          new XdrEndChildFunction(XDR_EndElement),
                          false),
            new XdrEntry( SchemaNames.Token.XdrAttribute,      null, s_XDR_Attribute_Attributes,
                          new XdrInitFunction(XDR_InitAttribute),
                          new XdrBeginChildFunction(XDR_BeginAttribute),
                          new XdrEndChildFunction(XDR_EndAttribute),
                          false),
            new XdrEntry( SchemaNames.Token.XdrGroup,          s_XDR_Group_SubElements, s_XDR_Group_Attributes,
                          new XdrInitFunction(XDR_InitGroup),
                          null,
                          new XdrEndChildFunction(XDR_EndGroup),
                          false),
            new XdrEntry( SchemaNames.Token.XdrDatatype,       null, s_XDR_ElementDataType_Attributes,
                          new XdrInitFunction(XDR_InitElementDtType),
                          null,
                          new XdrEndChildFunction(XDR_EndElementDtType),
                          true),
            new XdrEntry( SchemaNames.Token.XdrDatatype,       null, s_XDR_AttributeDataType_Attributes,
                          new XdrInitFunction(XDR_InitAttributeDtType),
                          null,
                          new XdrEndChildFunction(XDR_EndAttributeDtType),
                          true)
        };

        private readonly SchemaInfo _SchemaInfo;
        private string? _TargetNamespace;
        private readonly XmlReader _reader;
        private readonly PositionInfo _positionInfo;
        private ParticleContentValidator? _contentValidator;

        private XdrEntry _CurState;
        private XdrEntry? _NextState;

        private readonly HWStack _StateHistory;
        private readonly HWStack _GroupStack;
        private string? _XdrName;
        private string? _XdrPrefix;

        private readonly ElementContent _ElementDef;
        private GroupContent _GroupDef;
        private readonly AttributeContent _AttributeDef;

        private DeclBaseInfo? _UndefinedAttributeTypes;
        private DeclBaseInfo? _BaseDecl;

        private readonly XmlNameTable _NameTable;
        private readonly SchemaNames _SchemaNames;

        private readonly XmlNamespaceManager _CurNsMgr;
        private string? _Text;

        private readonly ValidationEventHandler? _validationEventHandler;
        private readonly Hashtable _UndeclaredElements = new Hashtable();

        private const string x_schema = "x-schema:";

        private XmlResolver? _xmlResolver;

        internal XdrBuilder(
                           XmlReader reader,
                           XmlNamespaceManager curmgr,
                           SchemaInfo sinfo,
                           string? targetNamspace,
                           XmlNameTable nameTable,
                           SchemaNames schemaNames,
                           ValidationEventHandler? eventhandler)
        {
            _SchemaInfo = sinfo;
            _TargetNamespace = targetNamspace;
            _reader = reader;
            _CurNsMgr = curmgr;
            _validationEventHandler = eventhandler;
            _StateHistory = new HWStack(StackIncrement);
            _ElementDef = new ElementContent();
            _AttributeDef = new AttributeContent();
            _GroupStack = new HWStack(StackIncrement);
            _GroupDef = new GroupContent();
            _NameTable = nameTable;
            _SchemaNames = schemaNames;
            _CurState = s_schemaEntries[0];
            _positionInfo = PositionInfo.GetPositionInfo(_reader);
            _xmlResolver = null;
        }

        internal override bool ProcessElement(string prefix, string name, string ns)
        {
            XmlQualifiedName qname = new XmlQualifiedName(name, XmlSchemaDatatype.XdrCanonizeUri(ns, _NameTable, _SchemaNames));
            if (GetNextState(qname))
            {
                Push();
                if (_CurState._InitFunc != null)
                {
                    (this._CurState._InitFunc)(this, qname);
                }
                return true;
            }
            else
            {
                if (!IsSkipableElement(qname))
                {
                    SendValidationEvent(SR.Sch_UnsupportedElement, XmlQualifiedName.ToString(name, prefix));
                }
                return false;
            }
        }

        // SxS: This method processes attribute from the source document and does not expose any resources to the caller
        // It is fine to suppress the SxS warning.
        internal override void ProcessAttribute(string prefix, string name, string ns, string value)
        {
            XmlQualifiedName qname = new XmlQualifiedName(name, XmlSchemaDatatype.XdrCanonizeUri(ns, _NameTable, _SchemaNames));
            for (int i = 0; i < _CurState._Attributes!.Length; i++)
            {
                XdrAttributeEntry a = _CurState._Attributes[i];
                if (_SchemaNames.TokenToQName[(int)a._Attribute].Equals(qname))
                {
                    XdrBuildFunction buildFunc = a._BuildFunc;
                    if (a._Datatype!.TokenizedType == XmlTokenizedType.QName)
                    {
                        string prefixValue;
                        XmlQualifiedName qnameValue = XmlQualifiedName.Parse(value, _CurNsMgr, out prefixValue);
                        qnameValue.Atomize(_NameTable);
                        if (prefixValue.Length != 0)
                        {
                            if (a._Attribute != SchemaNames.Token.SchemaType)
                            {    // <attribute type= || <element type=
                                throw new XmlException(SR.Xml_UnexpectedToken, "NAME");
                            }
                        }
                        else if (IsGlobal(a._SchemaFlags))
                        {
                            qnameValue = new XmlQualifiedName(qnameValue.Name, _TargetNamespace);
                        }
                        else
                        {
                            qnameValue = new XmlQualifiedName(qnameValue.Name);
                        }
                        buildFunc(this, qnameValue, prefixValue);
                    }
                    else
                    {
                        buildFunc(this, a._Datatype.ParseValue(value, _NameTable, _CurNsMgr), string.Empty);
                    }
                    return;
                }
            }

            if ((object)ns == (object)_SchemaNames.NsXmlNs && IsXdrSchema(value))
            {
                LoadSchema(value);
                return;
            }

            // Check non-supported attribute
            if (!IsSkipableAttribute(qname))
            {
                SendValidationEvent(SR.Sch_UnsupportedAttribute,
                                    XmlQualifiedName.ToString(qname.Name, prefix));
            }
        }

        internal XmlResolver? XmlResolver
        {
            set
            {
                _xmlResolver = value;
            }
        }

        private bool LoadSchema(string uri)
        {
            if (_xmlResolver == null)
            {
                return false;
            }
            uri = _NameTable.Add(uri);
            if (_SchemaInfo.TargetNamespaces.ContainsKey(uri))
            {
                return false;
            }
            SchemaInfo? schemaInfo = null;
            Uri _baseUri = _xmlResolver.ResolveUri(null, _reader.BaseURI);
            XmlReader? reader = null;
            try
            {
                Uri ruri = _xmlResolver.ResolveUri(_baseUri, uri.Substring(x_schema.Length));
                Stream stm = (Stream)_xmlResolver.GetEntity(ruri, null, null)!;
                reader = new XmlTextReader(ruri.ToString(), stm, _NameTable);
                schemaInfo = new SchemaInfo();
                Parser parser = new Parser(SchemaType.XDR, _NameTable, _SchemaNames, _validationEventHandler);
                parser.XmlResolver = _xmlResolver;
                parser.Parse(reader, uri);
                schemaInfo = parser.XdrSchema;
            }
            catch (XmlException e)
            {
                SendValidationEvent(SR.Sch_CannotLoadSchema, new string[] { uri, e.Message }, XmlSeverityType.Warning);
                schemaInfo = null;
            }
            finally
            {
                reader?.Close();
            }
            if (schemaInfo != null && schemaInfo.ErrorCount == 0)
            {
                _SchemaInfo.Add(schemaInfo, _validationEventHandler);
                return true;
            }
            return false;
        }

        internal static bool IsXdrSchema(string uri)
        {
            return uri.Length >= x_schema.Length &&
                   0 == string.Compare(uri, 0, x_schema, 0, x_schema.Length, StringComparison.Ordinal) &&
                   !uri.StartsWith("x-schema:#", StringComparison.Ordinal);
        }

        internal override bool IsContentParsed()
        {
            return true;
        }

        internal override void ProcessMarkup(XmlNode?[] markup)
        {
            throw new InvalidOperationException(SR.Xml_InvalidOperation); // should never be called
        }

        internal override void ProcessCData(string value)
        {
            if (_CurState._AllowText)
            {
                _Text = value;
            }
            else
            {
                SendValidationEvent(SR.Sch_TextNotAllowed, value);
            }
        }

        internal override void StartChildren()
        {
            if (_CurState._BeginChildFunc != null)
            {
                (this._CurState._BeginChildFunc)(this);
            }
        }

        internal override void EndChildren()
        {
            if (_CurState._EndChildFunc != null)
            {
                (this._CurState._EndChildFunc)(this);
            }

            Pop();
        }

        //
        // State stack push & pop
        //
        private void Push()
        {
            _StateHistory.Push();
            _StateHistory[_StateHistory.Length - 1] = _CurState;
            _CurState = _NextState!;
        }

        private void Pop()
        {
            _CurState = (XdrEntry)_StateHistory.Pop()!;
        }

        //
        // Group stack push & pop
        //
        private void PushGroupInfo()
        {
            _GroupStack.Push();
            _GroupStack[_GroupStack.Length - 1] = GroupContent.Copy(_GroupDef);
        }

        private void PopGroupInfo()
        {
            _GroupDef = (GroupContent)_GroupStack.Pop()!;
            Debug.Assert(_GroupDef != null);
        }

        //
        // XDR Schema
        //
        private static void XDR_InitRoot(XdrBuilder builder, object obj)
        {
            builder._SchemaInfo.SchemaType = SchemaType.XDR;
            builder._ElementDef._ElementDecl = null;
            builder._ElementDef._AttDefList = null;
            builder._AttributeDef._AttDef = null;
        }

        private static void XDR_BuildRoot_Name(XdrBuilder builder, object obj, string prefix)
        {
            builder._XdrName = (string)obj;
            builder._XdrPrefix = prefix;
        }

        private static void XDR_BuildRoot_ID(XdrBuilder builder, object obj, string prefix)
        {
        }

        private static void XDR_BeginRoot(XdrBuilder builder)
        {
            if (builder._TargetNamespace == null)
            { // inline xdr schema
                if (builder._XdrName != null)
                {
                    builder._TargetNamespace = builder._NameTable.Add($"x-schema:#{builder._XdrName}");
                }
                else
                {
                    builder._TargetNamespace = string.Empty;
                }
            }
            builder._SchemaInfo.TargetNamespaces.Add(builder._TargetNamespace, true);
        }

        private static void XDR_EndRoot(XdrBuilder builder)
        {
            //
            // check undefined attribute types
            // We already checked local attribute types, so only need to check global attribute types here
            //
            while (builder._UndefinedAttributeTypes != null)
            {
                XmlQualifiedName gname = builder._UndefinedAttributeTypes._TypeName;

                // if there is no URN in this name then the name is local to the
                // schema, but the global attribute was still URN qualified, so
                // we need to qualify this name now.
                if (gname.Namespace.Length == 0)
                {
                    gname = new XmlQualifiedName(gname.Name, builder._TargetNamespace);
                }
                SchemaAttDef? ad;
                if (builder._SchemaInfo.AttributeDecls.TryGetValue(gname, out ad))
                {
                    builder._UndefinedAttributeTypes._Attdef = (SchemaAttDef)ad.Clone();
                    builder._UndefinedAttributeTypes._Attdef.Name = gname;
                    builder.XDR_CheckAttributeDefault(builder._UndefinedAttributeTypes, builder._UndefinedAttributeTypes._Attdef);
                }
                else
                {
                    builder.SendValidationEvent(SR.Sch_UndeclaredAttribute, gname.Name);
                }

                builder._UndefinedAttributeTypes = builder._UndefinedAttributeTypes._Next;
            }

            foreach (SchemaElementDecl? ed in builder._UndeclaredElements.Values)
            {
                builder.SendValidationEvent(SR.Sch_UndeclaredElement, XmlQualifiedName.ToString(ed!.Name.Name, ed.Prefix));
            }
        }


        //
        // XDR ElementType
        //

        private static void XDR_InitElementType(XdrBuilder builder, object obj)
        {
            builder._ElementDef._ElementDecl = new SchemaElementDecl();
            builder._contentValidator = new ParticleContentValidator(XmlSchemaContentType.Mixed);
            builder._contentValidator.IsOpen = true;

            builder._ElementDef._ContentAttr = SchemaContentNone;
            builder._ElementDef._OrderAttr = SchemaOrderNone;
            builder._ElementDef._MasterGroupRequired = false;
            builder._ElementDef._ExistTerminal = false;
            builder._ElementDef._AllowDataType = true;
            builder._ElementDef._HasDataType = false;
            builder._ElementDef._EnumerationRequired = false;
            builder._ElementDef._AttDefList = new Hashtable();
            builder._ElementDef._MaxLength = uint.MaxValue;
            builder._ElementDef._MinLength = uint.MaxValue;

            //        builder._AttributeDef._HasDataType = false;
            //        builder._AttributeDef._Default = null;
        }

        private static void XDR_BuildElementType_Name(XdrBuilder builder, object obj, string prefix)
        {
            XmlQualifiedName qname = (XmlQualifiedName)obj;

            if (builder._SchemaInfo.ElementDecls.ContainsKey(qname))
            {
                builder.SendValidationEvent(SR.Sch_DupElementDecl, XmlQualifiedName.ToString(qname.Name, prefix));
            }

            builder._ElementDef._ElementDecl!.Name = qname;
            builder._ElementDef._ElementDecl!.Prefix = prefix;
            builder._SchemaInfo.ElementDecls.Add(qname, builder._ElementDef._ElementDecl);
            if (builder._UndeclaredElements[qname] != null)
            {
                builder._UndeclaredElements.Remove(qname);
            }
        }

        private static void XDR_BuildElementType_Content(XdrBuilder builder, object obj, string prefix)
        {
            builder._ElementDef._ContentAttr = builder.GetContent((XmlQualifiedName)obj);
        }

        private static void XDR_BuildElementType_Model(XdrBuilder builder, object obj, string prefix)
        {
            builder._contentValidator!.IsOpen = builder.GetModel((XmlQualifiedName)obj);
        }

        private static void XDR_BuildElementType_Order(XdrBuilder builder, object obj, string prefix)
        {
            builder._ElementDef._OrderAttr = builder._GroupDef._Order = builder.GetOrder((XmlQualifiedName)obj);
        }

        private static void XDR_BuildElementType_DtType(XdrBuilder builder, object obj, string prefix)
        {
            builder._ElementDef._HasDataType = true;
            string s = ((string)obj).Trim();
            if (s.Length == 0)
            {
                builder.SendValidationEvent(SR.Sch_MissDtvalue);
            }
            else
            {
                XmlSchemaDatatype? dtype = XmlSchemaDatatype.FromXdrName(s);
                if (dtype == null)
                {
                    builder.SendValidationEvent(SR.Sch_UnknownDtType, s);
                }

                builder._ElementDef._ElementDecl!.Datatype = dtype!;
            }
        }

        private static void XDR_BuildElementType_DtValues(XdrBuilder builder, object obj, string prefix)
        {
            builder._ElementDef._EnumerationRequired = true;
            builder._ElementDef._ElementDecl!.Values = new List<string>((string[])obj);
        }

        private static void XDR_BuildElementType_DtMaxLength(XdrBuilder builder, object obj, string prefix)
        {
            ParseDtMaxLength(ref builder._ElementDef._MaxLength, obj, builder);
        }

        private static void XDR_BuildElementType_DtMinLength(XdrBuilder builder, object obj, string prefix)
        {
            ParseDtMinLength(ref builder._ElementDef._MinLength, obj, builder);
        }

        private static void XDR_BeginElementType(XdrBuilder builder)
        {
            string? code = null;
            string? msg = null;

            //
            // check name attribute
            //
            if (builder._ElementDef._ElementDecl!.Name.IsEmpty)
            {
                code = SR.Sch_MissAttribute;
                msg = "name";
                goto cleanup;
            }

            //
            // check dt:type attribute
            //
            if (builder._ElementDef._HasDataType)
            {
                if (!builder._ElementDef._AllowDataType)
                {
                    code = SR.Sch_DataTypeTextOnly;
                    goto cleanup;
                }
                else
                {
                    builder._ElementDef._ContentAttr = SchemaContentText;
                }
            }
            else if (builder._ElementDef._ContentAttr == SchemaContentNone)
            {
                switch (builder._ElementDef._OrderAttr)
                {
                    case SchemaOrderNone:
                        builder._ElementDef._ContentAttr = SchemaContentMixed;
                        builder._ElementDef._OrderAttr = SchemaOrderMany;
                        break;
                    case SchemaOrderSequence:
                        builder._ElementDef._ContentAttr = SchemaContentElement;
                        break;
                    case SchemaOrderChoice:
                        builder._ElementDef._ContentAttr = SchemaContentElement;
                        break;
                    case SchemaOrderMany:
                        builder._ElementDef._ContentAttr = SchemaContentMixed;
                        break;
                }
            }


            //save the model value from the base
            bool tempOpen = builder._contentValidator!.IsOpen;
            ElementContent def = builder._ElementDef;
            switch (builder._ElementDef._ContentAttr)
            {
                case SchemaContentText:
                    builder._ElementDef._ElementDecl.ContentValidator = ContentValidator.TextOnly;
                    builder._GroupDef._Order = SchemaOrderMany;
                    builder._contentValidator = null;
                    break;
                case SchemaContentElement:
                    builder._contentValidator = new ParticleContentValidator(XmlSchemaContentType.ElementOnly);
                    if (def._OrderAttr == SchemaOrderNone)
                    {
                        builder._GroupDef._Order = SchemaOrderSequence;
                    }
                    def._MasterGroupRequired = true;
                    builder._contentValidator.IsOpen = tempOpen;
                    break;

                case SchemaContentEmpty:
                    builder._ElementDef._ElementDecl.ContentValidator = ContentValidator.Empty;
                    builder._contentValidator = null;
                    break;

                case SchemaContentMixed:
                    if (def._OrderAttr == SchemaOrderNone || def._OrderAttr == SchemaOrderMany)
                    {
                        builder._GroupDef._Order = SchemaOrderMany;
                    }
                    else
                    {
                        code = SR.Sch_MixedMany;
                        goto cleanup;
                    }
                    def._MasterGroupRequired = true;
                    builder._contentValidator.IsOpen = tempOpen;
                    break;
            }


            if (def._ContentAttr == SchemaContentMixed || def._ContentAttr == SchemaContentElement)
            {
                builder._contentValidator!.Start();
                builder._contentValidator.OpenGroup();
            }
        cleanup:
            if (code != null)
            {
                builder.SendValidationEvent(code, msg);
            }
        }

        private static void XDR_EndElementType(XdrBuilder builder)
        {
            SchemaElementDecl? ed = builder._ElementDef._ElementDecl;

            // check undefined attribute types first
            if (builder._UndefinedAttributeTypes != null && builder._ElementDef._AttDefList != null)
            {
                DeclBaseInfo? patt = builder._UndefinedAttributeTypes;
                DeclBaseInfo? p1 = patt;

                while (patt != null)
                {
                    SchemaAttDef? pAttdef = null;

                    if (patt._ElementDecl == ed)
                    {
                        XmlQualifiedName pName = patt._TypeName;
                        pAttdef = (SchemaAttDef?)builder._ElementDef._AttDefList[pName];
                        if (pAttdef != null)
                        {
                            patt._Attdef = (SchemaAttDef)pAttdef.Clone();
                            patt._Attdef.Name = pName;
                            builder.XDR_CheckAttributeDefault(patt, pAttdef);

                            // remove it from _pUndefinedAttributeTypes
                            if (patt == builder._UndefinedAttributeTypes)
                            {
                                patt = builder._UndefinedAttributeTypes = patt._Next;
                                p1 = patt;
                            }
                            else
                            {
                                p1!._Next = patt._Next;
                                patt = p1._Next;
                            }
                        }
                    }

                    if (pAttdef == null)
                    {
                        if (patt != builder._UndefinedAttributeTypes)
                            p1 = p1!._Next;
                        patt = patt!._Next;
                    }
                }
            }

            if (builder._ElementDef._MasterGroupRequired)
            {
                // if the content is mixed, there is a group that need to be closed
                builder._contentValidator!.CloseGroup();

                if (!builder._ElementDef._ExistTerminal)
                {
                    if (builder._contentValidator.IsOpen)
                    {
                        builder._ElementDef._ElementDecl!.ContentValidator = ContentValidator.Any;
                        builder._contentValidator = null;
                    }
                    else
                    {
                        if (builder._ElementDef._ContentAttr != SchemaContentMixed)
                            builder.SendValidationEvent(SR.Sch_ElementMissing);
                    }
                }
                else
                {
                    if (builder._GroupDef._Order == SchemaOrderMany)
                    {
                        builder._contentValidator.AddStar();
                    }
                }
            }

            if (ed!.Datatype != null)
            {
                XmlTokenizedType ttype = ed.Datatype.TokenizedType;
                if (ttype == XmlTokenizedType.ENUMERATION &&
                    !builder._ElementDef._EnumerationRequired)
                {
                    builder.SendValidationEvent(SR.Sch_MissDtvaluesAttribute);
                }

                if (ttype != XmlTokenizedType.ENUMERATION &&
                    builder._ElementDef._EnumerationRequired)
                {
                    builder.SendValidationEvent(SR.Sch_RequireEnumeration);
                }
            }

            CompareMinMaxLength(builder._ElementDef._MinLength, builder._ElementDef._MaxLength, builder);
            ed.MaxLength = (long)builder._ElementDef._MaxLength;
            ed.MinLength = (long)builder._ElementDef._MinLength;

            if (builder._contentValidator != null)
            {
                builder._ElementDef._ElementDecl!.ContentValidator = builder._contentValidator.Finish(true);
                builder._contentValidator = null;
            }

            builder._ElementDef._ElementDecl = null;
            builder._ElementDef._AttDefList = null;
        }


        //
        // XDR AttributeType
        //

        private static void XDR_InitAttributeType(XdrBuilder builder, object obj)
        {
            AttributeContent ad = builder._AttributeDef;
            ad._AttDef = new SchemaAttDef(XmlQualifiedName.Empty, null);

            ad._Required = false;
            ad._Prefix = null;

            ad._Default = null;
            ad._MinVal = 0; // optional by default.
            ad._MaxVal = 1;

            // used for datatype
            ad._EnumerationRequired = false;
            ad._HasDataType = false;
            ad._Global = (builder._StateHistory.Length == 2);

            ad._MaxLength = uint.MaxValue;
            ad._MinLength = uint.MaxValue;
        }

        private static void XDR_BuildAttributeType_Name(XdrBuilder builder, object obj, string prefix)
        {
            XmlQualifiedName qname = (XmlQualifiedName)obj;

            builder._AttributeDef._Name = qname;
            builder._AttributeDef._Prefix = prefix;
            builder._AttributeDef._AttDef!.Name = qname;

            if (builder._ElementDef._ElementDecl != null)
            {  // Local AttributeType
                if (builder._ElementDef._AttDefList![qname] == null)
                {
                    builder._ElementDef._AttDefList.Add(qname, builder._AttributeDef._AttDef);
                }
                else
                {
                    builder.SendValidationEvent(SR.Sch_DupAttribute, XmlQualifiedName.ToString(qname.Name, prefix));
                }
            }
            else
            {  // Global AttributeType
                // Global AttributeTypes are URN qualified so that we can look them up across schemas.
                qname = new XmlQualifiedName(qname.Name, builder._TargetNamespace);
                builder._AttributeDef._AttDef.Name = qname;
                if (!builder._SchemaInfo.AttributeDecls.ContainsKey(qname))
                {
                    builder._SchemaInfo.AttributeDecls.Add(qname, builder._AttributeDef._AttDef);
                }
                else
                {
                    builder.SendValidationEvent(SR.Sch_DupAttribute, XmlQualifiedName.ToString(qname.Name, prefix));
                }
            }
        }

        private static void XDR_BuildAttributeType_Required(XdrBuilder builder, object obj, string prefix)
        {
            builder._AttributeDef._Required = IsYes(obj, builder);
        }

        private static void XDR_BuildAttributeType_Default(XdrBuilder builder, object obj, string prefix)
        {
            builder._AttributeDef._Default = obj;
        }

        private static void XDR_BuildAttributeType_DtType(XdrBuilder builder, object obj, string prefix)
        {
            XmlQualifiedName qname = (XmlQualifiedName)obj;
            builder._AttributeDef._HasDataType = true;
            builder._AttributeDef._AttDef!.Datatype = builder.CheckDatatype(qname.Name)!;
        }

        private static void XDR_BuildAttributeType_DtValues(XdrBuilder builder, object obj, string prefix)
        {
            builder._AttributeDef._EnumerationRequired = true;
            builder._AttributeDef._AttDef!.Values = new List<string>((string[])obj);
        }

        private static void XDR_BuildAttributeType_DtMaxLength(XdrBuilder builder, object obj, string prefix)
        {
            ParseDtMaxLength(ref builder._AttributeDef._MaxLength, obj, builder);
        }

        private static void XDR_BuildAttributeType_DtMinLength(XdrBuilder builder, object obj, string prefix)
        {
            ParseDtMinLength(ref builder._AttributeDef._MinLength, obj, builder);
        }

        private static void XDR_BeginAttributeType(XdrBuilder builder)
        {
            if (builder._AttributeDef._Name!.IsEmpty)
            {
                builder.SendValidationEvent(SR.Sch_MissAttribute);
            }
        }

        private static void XDR_EndAttributeType(XdrBuilder builder)
        {
            string? code = null;
            if (builder._AttributeDef._HasDataType && builder._AttributeDef._AttDef!.Datatype != null)
            {
                XmlTokenizedType ttype = builder._AttributeDef._AttDef.Datatype.TokenizedType;

                if (ttype == XmlTokenizedType.ENUMERATION && !builder._AttributeDef._EnumerationRequired)
                {
                    code = SR.Sch_MissDtvaluesAttribute;
                    goto cleanup;
                }

                if (ttype != XmlTokenizedType.ENUMERATION && builder._AttributeDef._EnumerationRequired)
                {
                    code = SR.Sch_RequireEnumeration;
                    goto cleanup;
                }

                // an attribute of type id is not supposed to have a default value
                if (builder._AttributeDef._Default != null && ttype == XmlTokenizedType.ID)
                {
                    code = SR.Sch_DefaultIdValue;
                    goto cleanup;
                }
            }
            else
            {
                builder._AttributeDef._AttDef!.Datatype = XmlSchemaDatatype.FromXmlTokenizedType(XmlTokenizedType.CDATA)!;
            }

            //
            // constraints
            //
            CompareMinMaxLength(builder._AttributeDef._MinLength, builder._AttributeDef._MaxLength, builder);
            builder._AttributeDef._AttDef.MaxLength = builder._AttributeDef._MaxLength;
            builder._AttributeDef._AttDef.MinLength = builder._AttributeDef._MinLength;

            //
            // checkAttributeType
            //
            if (builder._AttributeDef._Default != null)
            {
                builder._AttributeDef._AttDef.DefaultValueRaw = builder._AttributeDef._AttDef.DefaultValueExpanded = (string)builder._AttributeDef._Default;
                builder.CheckDefaultAttValue(builder._AttributeDef._AttDef);
            }

            XdrBuilder.SetAttributePresence(builder._AttributeDef._AttDef, builder._AttributeDef._Required);

        cleanup:
            if (code != null)
            {
                builder.SendValidationEvent(code);
            }
        }


        //
        // XDR Element
        //

        private static void XDR_InitElement(XdrBuilder builder, object obj)
        {
            if (builder._ElementDef._HasDataType ||
                (builder._ElementDef._ContentAttr == SchemaContentEmpty) ||
                (builder._ElementDef._ContentAttr == SchemaContentText))
            {
                builder.SendValidationEvent(SR.Sch_ElementNotAllowed);
            }

            builder._ElementDef._AllowDataType = false;

            builder._ElementDef._HasType = false;
            builder._ElementDef._MinVal = 1;
            builder._ElementDef._MaxVal = 1;
        }

        private static void XDR_BuildElement_Type(XdrBuilder builder, object obj, string prefix)
        {
            XmlQualifiedName qname = (XmlQualifiedName)obj;

            if (!builder._SchemaInfo.ElementDecls.ContainsKey(qname))
            {
                SchemaElementDecl? ed = (SchemaElementDecl?)builder._UndeclaredElements[qname];
                if (ed == null)
                {
                    ed = new SchemaElementDecl(qname, prefix);
                    builder._UndeclaredElements.Add(qname, ed);
                }
            }

            builder._ElementDef._HasType = true;
            if (builder._ElementDef._ExistTerminal)
                builder.AddOrder();
            else
                builder._ElementDef._ExistTerminal = true;

            builder._contentValidator!.AddName(qname, null);
        }

        private static void XDR_BuildElement_MinOccurs(XdrBuilder builder, object obj, string prefix)
        {
            builder._ElementDef._MinVal = ParseMinOccurs(obj, builder);
        }

        private static void XDR_BuildElement_MaxOccurs(XdrBuilder builder, object obj, string prefix)
        {
            builder._ElementDef._MaxVal = ParseMaxOccurs(obj, builder);
        }


        //    private static void XDR_BeginElement(XdrBuilder builder)
        //  {
        //
        //  }


        private static void XDR_EndElement(XdrBuilder builder)
        {
            if (builder._ElementDef._HasType)
            {
                HandleMinMax(builder._contentValidator,
                             builder._ElementDef._MinVal,
                             builder._ElementDef._MaxVal);
            }
            else
            {
                builder.SendValidationEvent(SR.Sch_MissAttribute);
            }
        }


        //
        // XDR Attribute
        //

        private static void XDR_InitAttribute(XdrBuilder builder, object obj)
        {
            builder._BaseDecl ??= new DeclBaseInfo();
            builder._BaseDecl._MinOccurs = 0;
        }

        private static void XDR_BuildAttribute_Type(XdrBuilder builder, object obj, string prefix)
        {
            builder._BaseDecl!._TypeName = (XmlQualifiedName)obj;
            builder._BaseDecl._Prefix = prefix;
        }

        private static void XDR_BuildAttribute_Required(XdrBuilder builder, object obj, string prefix)
        {
            if (IsYes(obj, builder))
            {
                builder._BaseDecl!._MinOccurs = 1;
            }
        }

        private static void XDR_BuildAttribute_Default(XdrBuilder builder, object obj, string prefix)
        {
            builder._BaseDecl!._Default = obj;
        }

        private static void XDR_BeginAttribute(XdrBuilder builder)
        {
            if (builder._BaseDecl!._TypeName.IsEmpty)
            {
                builder.SendValidationEvent(SR.Sch_MissAttribute);
            }

            SchemaAttDef? attdef = null;
            XmlQualifiedName qname = builder._BaseDecl._TypeName;
            string prefix = builder._BaseDecl._Prefix!;

            // local?
            if (builder._ElementDef._AttDefList != null)
            {
                attdef = (SchemaAttDef?)builder._ElementDef._AttDefList[qname];
            }

            // global?
            if (attdef == null)
            {
                // if there is no URN in this name then the name is local to the
                // schema, but the global attribute was still URN qualified, so
                // we need to qualify this name now.
                XmlQualifiedName gname = qname;
                if (prefix.Length == 0)
                    gname = new XmlQualifiedName(qname.Name, builder._TargetNamespace);
                SchemaAttDef? ad;
                if (builder._SchemaInfo.AttributeDecls.TryGetValue(gname, out ad))
                {
                    attdef = (SchemaAttDef)ad.Clone();
                    attdef.Name = qname;
                }
                else if (prefix.Length != 0)
                {
                    builder.SendValidationEvent(SR.Sch_UndeclaredAttribute, XmlQualifiedName.ToString(qname.Name, prefix));
                }
            }

            if (attdef != null)
            {
                builder.XDR_CheckAttributeDefault(builder._BaseDecl, attdef);
            }
            else
            {
                // will process undeclared types later
                attdef = new SchemaAttDef(qname, prefix);
                DeclBaseInfo decl = new DeclBaseInfo();
                decl._Checking = true;
                decl._Attdef = attdef;
                decl._TypeName = builder._BaseDecl._TypeName;
                decl._ElementDecl = builder._ElementDef._ElementDecl;
                decl._MinOccurs = builder._BaseDecl._MinOccurs;
                decl._Default = builder._BaseDecl._Default;

                // add undefined attribute types
                decl._Next = builder._UndefinedAttributeTypes;
                builder._UndefinedAttributeTypes = decl;
            }

            builder._ElementDef._ElementDecl!.AddAttDef(attdef);
        }

        private static void XDR_EndAttribute(XdrBuilder builder)
        {
            builder._BaseDecl!.Reset();
        }


        //
        // XDR Group
        //

        private static void XDR_InitGroup(XdrBuilder builder, object obj)
        {
            if (builder._ElementDef._ContentAttr == SchemaContentEmpty ||
                builder._ElementDef._ContentAttr == SchemaContentText)
            {
                builder.SendValidationEvent(SR.Sch_GroupDisabled);
            }

            builder.PushGroupInfo();

            builder._GroupDef._MinVal = 1;
            builder._GroupDef._MaxVal = 1;
            builder._GroupDef._HasMaxAttr = false;
            builder._GroupDef._HasMinAttr = false;

            if (builder._ElementDef._ExistTerminal)
                builder.AddOrder();

            // now we are in a group so we reset fExistTerminal
            builder._ElementDef._ExistTerminal = false;

            builder._contentValidator!.OpenGroup();
        }

        private static void XDR_BuildGroup_Order(XdrBuilder builder, object obj, string prefix)
        {
            builder._GroupDef._Order = builder.GetOrder((XmlQualifiedName)obj);
            if (builder._ElementDef._ContentAttr == SchemaContentMixed && builder._GroupDef._Order != SchemaOrderMany)
            {
                builder.SendValidationEvent(SR.Sch_MixedMany);
            }
        }

        private static void XDR_BuildGroup_MinOccurs(XdrBuilder builder, object obj, string prefix)
        {
            builder._GroupDef._MinVal = ParseMinOccurs(obj, builder);
            builder._GroupDef._HasMinAttr = true;
        }

        private static void XDR_BuildGroup_MaxOccurs(XdrBuilder builder, object obj, string prefix)
        {
            builder._GroupDef._MaxVal = ParseMaxOccurs(obj, builder);
            builder._GroupDef._HasMaxAttr = true;
        }


        //    private static void XDR_BeginGroup(XdrBuilder builder)
        //    {
        //
        //    }


        private static void XDR_EndGroup(XdrBuilder builder)
        {
            if (!builder._ElementDef._ExistTerminal)
            {
                builder.SendValidationEvent(SR.Sch_ElementMissing);
            }

            builder._contentValidator!.CloseGroup();

            if (builder._GroupDef._Order == SchemaOrderMany)
            {
                builder._contentValidator.AddStar();
            }

            if (SchemaOrderMany == builder._GroupDef._Order &&
                builder._GroupDef._HasMaxAttr &&
                builder._GroupDef._MaxVal != uint.MaxValue)
            {
                builder.SendValidationEvent(SR.Sch_ManyMaxOccurs);
            }

            HandleMinMax(builder._contentValidator,
                         builder._GroupDef._MinVal,
                         builder._GroupDef._MaxVal);

            builder.PopGroupInfo();
        }


        //
        // DataType
        //

        private static void XDR_InitElementDtType(XdrBuilder builder, object obj)
        {
            if (builder._ElementDef._HasDataType)
            {
                builder.SendValidationEvent(SR.Sch_DupDtType);
            }

            if (!builder._ElementDef._AllowDataType)
            {
                builder.SendValidationEvent(SR.Sch_DataTypeTextOnly);
            }
        }

        private static void XDR_EndElementDtType(XdrBuilder builder)
        {
            if (!builder._ElementDef._HasDataType)
            {
                builder.SendValidationEvent(SR.Sch_MissAttribute);
            }
            builder._ElementDef._ElementDecl!.ContentValidator = ContentValidator.TextOnly;
            builder._ElementDef._ContentAttr = SchemaContentText;
            builder._ElementDef._MasterGroupRequired = false;
            builder._contentValidator = null;
        }

        private static void XDR_InitAttributeDtType(XdrBuilder builder, object obj)
        {
            if (builder._AttributeDef._HasDataType)
            {
                builder.SendValidationEvent(SR.Sch_DupDtType);
            }
        }

        private static void XDR_EndAttributeDtType(XdrBuilder builder)
        {
            string? code = null;

            if (!builder._AttributeDef._HasDataType)
            {
                code = SR.Sch_MissAttribute;
            }
            else
            {
                if (builder._AttributeDef._AttDef!.Datatype != null)
                {
                    XmlTokenizedType ttype = builder._AttributeDef._AttDef.Datatype.TokenizedType;

                    if (ttype == XmlTokenizedType.ENUMERATION && !builder._AttributeDef._EnumerationRequired)
                    {
                        code = SR.Sch_MissDtvaluesAttribute;
                    }
                    else if (ttype != XmlTokenizedType.ENUMERATION && builder._AttributeDef._EnumerationRequired)
                    {
                        code = SR.Sch_RequireEnumeration;
                    }
                }
            }
            if (code != null)
            {
                builder.SendValidationEvent(code);
            }
        }

        //
        // private utility methods
        //

        private bool GetNextState(XmlQualifiedName qname)
        {
            if (_CurState._NextStates != null)
            {
                for (int i = 0; i < _CurState._NextStates.Length; i++)
                {
                    if (_SchemaNames.TokenToQName[(int)s_schemaEntries[_CurState._NextStates[i]]._Name].Equals(qname))
                    {
                        _NextState = s_schemaEntries[_CurState._NextStates[i]];
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsSkipableElement(XmlQualifiedName qname)
        {
            string ns = qname.Namespace;
            if (ns != null && !Ref.Equal(ns, _SchemaNames.NsXdr))
                return true;

            // skip description && extends
            if (_SchemaNames.TokenToQName[(int)SchemaNames.Token.XdrDescription].Equals(qname) ||
                _SchemaNames.TokenToQName[(int)SchemaNames.Token.XdrExtends].Equals(qname))
                return true;

            return false;
        }

        private bool IsSkipableAttribute(XmlQualifiedName qname)
        {
            string ns = qname.Namespace;
            if (
                ns.Length != 0 &&
                !Ref.Equal(ns, _SchemaNames.NsXdr) &&
                !Ref.Equal(ns, _SchemaNames.NsDataType)
             )
            {
                return true;
            }

            if (Ref.Equal(ns, _SchemaNames.NsDataType) &&
                _CurState._Name == SchemaNames.Token.XdrDatatype &&
                (_SchemaNames.QnDtMax.Equals(qname) ||
                 _SchemaNames.QnDtMin.Equals(qname) ||
                 _SchemaNames.QnDtMaxExclusive.Equals(qname) ||
                 _SchemaNames.QnDtMinExclusive.Equals(qname)))
            {
                return true;
            }

            return false;
        }

        private int GetOrder(XmlQualifiedName qname)
        {
            int order = 0;
            if (_SchemaNames.TokenToQName[(int)SchemaNames.Token.SchemaSeq].Equals(qname))
            {
                order = SchemaOrderSequence;
            }
            else if (_SchemaNames.TokenToQName[(int)SchemaNames.Token.SchemaOne].Equals(qname))
            {
                order = SchemaOrderChoice;
            }
            else if (_SchemaNames.TokenToQName[(int)SchemaNames.Token.SchemaMany].Equals(qname))
            {
                order = SchemaOrderMany;
            }
            else
            {
                SendValidationEvent(SR.Sch_UnknownOrder, qname.Name);
            }

            return order;
        }

        private void AddOrder()
        {
            // additional order can be add on by changing the setOrder and addOrder
            switch (_GroupDef._Order)
            {
                case SchemaOrderSequence:
                    _contentValidator!.AddSequence();
                    break;
                case SchemaOrderChoice:
                case SchemaOrderMany:
                    _contentValidator!.AddChoice();
                    break;
                default:
                case SchemaOrderAll:
                    throw new XmlException(SR.Xml_UnexpectedToken, "NAME");
            }
        }

        private static bool IsYes(object obj, XdrBuilder builder)
        {
            XmlQualifiedName qname = (XmlQualifiedName)obj;
            bool fYes = false;

            if (qname.Name == "yes")
            {
                fYes = true;
            }
            else if (qname.Name != "no")
            {
                builder.SendValidationEvent(SR.Sch_UnknownRequired);
            }

            return fYes;
        }

        private static uint ParseMinOccurs(object obj, XdrBuilder builder)
        {
            uint cVal = 1;

            if (!ParseInteger((string)obj, ref cVal) || (cVal != 0 && cVal != 1))
            {
                builder.SendValidationEvent(SR.Sch_MinOccursInvalid);
            }
            return cVal;
        }

        private static uint ParseMaxOccurs(object obj, XdrBuilder builder)
        {
            uint cVal = uint.MaxValue;
            string s = (string)obj;

            if (!s.Equals("*") &&
                (!ParseInteger(s, ref cVal) || (cVal != uint.MaxValue && cVal != 1)))
            {
                builder.SendValidationEvent(SR.Sch_MaxOccursInvalid);
            }
            return cVal;
        }

        private static void HandleMinMax(ParticleContentValidator? pContent, uint cMin, uint cMax)
        {
            if (pContent != null)
            {
                if (cMax == uint.MaxValue)
                {
                    if (cMin == 0)
                        pContent.AddStar();           // minOccurs="0" and maxOccurs="infinite"
                    else
                        pContent.AddPlus();           // minOccurs="1" and maxOccurs="infinite"
                }
                else if (cMin == 0)
                {                 // minOccurs="0" and maxOccurs="1")
                    pContent.AddQMark();
                }
            }
        }

        private static void ParseDtMaxLength(ref uint cVal, object obj, XdrBuilder builder)
        {
            if (uint.MaxValue != cVal)
            {
                builder.SendValidationEvent(SR.Sch_DupDtMaxLength);
            }

            if (!ParseInteger((string)obj, ref cVal))
            {
                builder.SendValidationEvent(SR.Sch_DtMaxLengthInvalid, obj.ToString());
            }
        }

        private static void ParseDtMinLength(ref uint cVal, object obj, XdrBuilder builder)
        {
            if (uint.MaxValue != cVal)
            {
                builder.SendValidationEvent(SR.Sch_DupDtMinLength);
            }

            if (!ParseInteger((string)obj, ref cVal))
            {
                builder.SendValidationEvent(SR.Sch_DtMinLengthInvalid, obj.ToString());
            }
        }

        private static void CompareMinMaxLength(uint cMin, uint cMax, XdrBuilder builder)
        {
            if (cMin != uint.MaxValue && cMax != uint.MaxValue && cMin > cMax)
            {
                builder.SendValidationEvent(SR.Sch_DtMinMaxLength);
            }
        }

        private static bool ParseInteger(string str, ref uint n)
        {
            return uint.TryParse(str, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo, out n);
        }

        private void XDR_CheckAttributeDefault(DeclBaseInfo decl, SchemaAttDef pAttdef)
        {
            if (decl._Default != null || pAttdef.DefaultValueTyped != null)
            {
                if (decl._Default != null)
                {
                    pAttdef.DefaultValueRaw = pAttdef.DefaultValueExpanded = (string)decl._Default;
                    CheckDefaultAttValue(pAttdef);
                }
            }

            SetAttributePresence(pAttdef, 1 == decl._MinOccurs);
        }

        private static void SetAttributePresence(SchemaAttDef pAttdef, bool fRequired)
        {
            if (SchemaDeclBase.Use.Fixed != pAttdef.Presence)
            {
                if (fRequired || SchemaDeclBase.Use.Required == pAttdef.Presence)
                {
                    // If it is required and it has a default value then it is a FIXED attribute.
                    if (pAttdef.DefaultValueTyped != null)
                        pAttdef.Presence = SchemaDeclBase.Use.Fixed;
                    else
                        pAttdef.Presence = SchemaDeclBase.Use.Required;
                }
                else if (pAttdef.DefaultValueTyped != null)
                {
                    pAttdef.Presence = SchemaDeclBase.Use.Default;
                }
                else
                {
                    pAttdef.Presence = SchemaDeclBase.Use.Implied;
                }
            }
        }

        private int GetContent(XmlQualifiedName qname)
        {
            int content = 0;
            if (_SchemaNames.TokenToQName[(int)SchemaNames.Token.SchemaEmpty].Equals(qname))
            {
                content = SchemaContentEmpty;
                _ElementDef._AllowDataType = false;
            }
            else if (_SchemaNames.TokenToQName[(int)SchemaNames.Token.SchemaElementOnly].Equals(qname))
            {
                content = SchemaContentElement;
                _ElementDef._AllowDataType = false;
            }
            else if (_SchemaNames.TokenToQName[(int)SchemaNames.Token.SchemaMixed].Equals(qname))
            {
                content = SchemaContentMixed;
                _ElementDef._AllowDataType = false;
            }
            else if (_SchemaNames.TokenToQName[(int)SchemaNames.Token.SchemaTextOnly].Equals(qname))
            {
                content = SchemaContentText;
            }
            else
            {
                SendValidationEvent(SR.Sch_UnknownContent, qname.Name);
            }
            return content;
        }

        private bool GetModel(XmlQualifiedName qname)
        {
            bool fOpen = false;
            if (_SchemaNames.TokenToQName[(int)SchemaNames.Token.SchemaOpen].Equals(qname))
                fOpen = true;
            else if (_SchemaNames.TokenToQName[(int)SchemaNames.Token.SchemaClosed].Equals(qname))
                fOpen = false;
            else
                SendValidationEvent(SR.Sch_UnknownModel, qname.Name);
            return fOpen;
        }

        private XmlSchemaDatatype? CheckDatatype(string str)
        {
            XmlSchemaDatatype? dtype = XmlSchemaDatatype.FromXdrName(str);
            if (dtype == null)
            {
                SendValidationEvent(SR.Sch_UnknownDtType, str);
            }
            else if (dtype.TokenizedType == XmlTokenizedType.ID)
            {
                if (!_AttributeDef._Global)
                {
                    if (_ElementDef._ElementDecl!.IsIdDeclared)
                    {
                        SendValidationEvent(SR.Sch_IdAttrDeclared,
                                            XmlQualifiedName.ToString(_ElementDef._ElementDecl.Name.Name, _ElementDef._ElementDecl.Prefix));
                    }
                    _ElementDef._ElementDecl.IsIdDeclared = true;
                }
            }

            return dtype;
        }

        private void CheckDefaultAttValue(SchemaAttDef attDef)
        {
            string str = (attDef.DefaultValueRaw).Trim();
            XdrValidator.CheckDefaultValue(str, attDef, _SchemaInfo, _CurNsMgr, _NameTable, null, _validationEventHandler, _reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition);
        }

        private static bool IsGlobal(int flags)
        {
            return flags == SchemaFlagsNs;
        }

        private void SendValidationEvent(string? code, string?[]? args, XmlSeverityType severity)
        {
            SendValidationEvent(new XmlSchemaException(code, args, _reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition), severity);
        }

        private void SendValidationEvent(string? code)
        {
            SendValidationEvent(code, string.Empty);
        }

        private void SendValidationEvent(string? code, string? msg)
        {
            SendValidationEvent(new XmlSchemaException(code, msg, _reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition), XmlSeverityType.Error);
        }

        private void SendValidationEvent(XmlSchemaException e, XmlSeverityType severity)
        {
            _SchemaInfo.ErrorCount++;
            if (_validationEventHandler != null)
            {
                _validationEventHandler(this, new ValidationEventArgs(e, severity));
            }
            else if (severity == XmlSeverityType.Error)
            {
                throw e;
            }
        }
    }; // class XdrBuilder
} // namespace System.Xml
