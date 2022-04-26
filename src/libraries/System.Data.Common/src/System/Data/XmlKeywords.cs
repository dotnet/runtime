// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Data
{
    internal static class Keywords
    {
        // Keywords for DataSet Namespace
        internal const string DFF = "diffgr";
        internal const string DFFNS = "urn:schemas-microsoft-com:xml-diffgram-v1";
        internal const string DIFFGRAM = "diffgram";
        internal const string DIFFID = "id";
        internal const string DIFFPID = "parentId";
        internal const string HASCHANGES = "hasChanges";
        internal const string HASERRORS = "hasErrors";
        internal const string ROWORDER = "rowOrder";
        internal const string MSD_ERRORS = "errors";
        internal const string CHANGES = "changes";
        internal const string MODIFIED = "modified";
        internal const string INSERTED = "inserted";
        //internal const string DESCENDENT            = "descendent";




        internal const string MSD = "msdata";
        internal const string MSDNS = "urn:schemas-microsoft-com:xml-msdata";
        internal const string MSD_ACCEPTREJECTRULE = "AcceptRejectRule";
        internal const string MSD_ALLOWDBNULL = "AllowDBNull";
        internal const string MSD_CHILD = "child";
        internal const string MSD_CHILDKEY = "childkey";
        internal const string MSD_CHILDTABLENS = "ChildTableNamespace";
        internal const string MSD_COLUMNNAME = "ColumnName";
        internal const string MSD_CONSTRAINTNAME = "ConstraintName";
        internal const string MSD_CONSTRAINTONLY = "ConstraintOnly";
        //     internal const string MSD_CREATECONSTRAINTS = "CreateConstraints";
        internal const string MSD_CASESENSITIVE = "CaseSensitive";
        internal const string MSD_DATASETNAME = "DataSetName";
        internal const string MSD_DATASETNAMESPACE = "DataSetNamespace";
        internal const string MSD_DATATYPE = "DataType";
        internal const string MSD_DEFAULTVALUE = "DefaultValue";
        internal const string MSD_DELETERULE = "DeleteRule";
        internal const string MSD_ERROR = "Error";
        internal const string MSD_ISDATASET = "IsDataSet";
        internal const string MSD_ISNESTED = "IsNested";
        internal const string MSD_LOCALE = "Locale";
        internal const string MSD_USECURRENTLOCALE = "UseCurrentLocale";
        internal const string MSD_ORDINAL = "Ordinal";
        internal const string MSD_PARENT = "parent";
        internal const string MSD_PARENTKEY = "parentkey";
        internal const string MSD_PRIMARYKEY = "PrimaryKey";
        internal const string MSD_RELATION = "Relationship";
        internal const string MSD_RELATIONNAME = "RelationName";
        internal const string MSD_UPDATERULE = "UpdateRule";
        internal const char MSD_KEYFIELDSEP = ' ';
        internal const char MSD_KEYFIELDOLDSEP = '+';
        internal const string MSD_REL_PREFIX = "rel_";
        internal const string MSD_FK_PREFIX = "fk_";
        internal const string MSD_MAINDATATABLE = "MainDataTable";
        internal const string MSD_TABLENS = "TableNamespace";
        internal const string MSD_PARENTTABLENS = "ParentTableNamespace";
        internal const string MSD_INSTANCETYPE = "InstanceType";

        internal const string MSD_EXCLUDESCHEMA = "ExcludeSchema";
        internal const string MSD_INCLUDESCHEMA = "IncludeSchema";

        internal const string MSD_FRAGMENTCOUNT = "schemafragmentcount";

        internal const string MSD_SCHEMASERIALIZATIONMODE = "SchemaSerializationMode";



        // Keywords for datatype namespace
        internal const string DTNS = "urn:schemas-microsoft-com:datatypes";
        internal const string DT_TYPE = "type";
        internal const string DT_VALUES = "values";

        // Keywords for schema namespace
        internal const string XDRNS = "urn:schemas-microsoft-com:xml-data";
        internal const string XDR_ATTRIBUTE = "attribute";
        internal const string XDR_ATTRIBUTETYPE = "AttributeType";
        internal const string XDR_DATATYPE = "datatype";
        internal const string XDR_DESCRIPTION = "description";
        internal const string XDR_ELEMENT = "element";
        internal const string XDR_ELEMENTTYPE = "ElementType";
        internal const string XDR_GROUP = "group";
        internal const string XDR_SCHEMA = "Schema";

        // Keywords for the xsd namespace

        internal const string XSDNS = "http://www.w3.org/2001/XMLSchema";

        internal const string XSD_NS_START = "http://www.w3.org/";
        internal const string XSD_XMLNS_NS = "http://www.w3.org/2000/xmlns/";
        internal const string XSD_PREFIX = "xs";
        internal const string XSD_PREFIXCOLON = "xs:";
        internal const string XSD_ANNOTATION = "annotation";
        internal const string XSD_APPINFO = "appinfo";
        internal const string XSD_ATTRIBUTE = "attribute";
        internal const string XSD_SIMPLETYPE = "simpleType";
        internal const string XSD_ELEMENT = "element";
        internal const string XSD_COMPLEXTYPE = "complexType";
        internal const string XSD_SCHEMA = "schema";
        internal const string XSD_PATTERN = "pattern";
        internal const string XSD_LENGTH = "length";
        internal const string XSD_MAXLENGTH = "maxLength";
        internal const string XSD_MINLENGTH = "minLength";
        internal const string XSD_ENUMERATION = "enumeration";
        internal const string XSD_MININCLUSIVE = "minInclusive";
        internal const string XSD_MINEXCLUSIVE = "minExclusive";
        internal const string XSD_MAXINCLUSIVE = "maxInclusive";
        internal const string XSD_MAXEXCLUSIVE = "maxExclusive";
        internal const string XSD_NAMESPACE = "namespace";
        internal const string XSD_NILLABLE = "nillable";
        internal const string XSD_IMPORT = "import";
        internal const string XSD_SELECTOR = "selector";
        internal const string XSD_FIELD = "field";
        internal const string XSD_UNIQUE = "unique";
        internal const string XSD_KEY = "key";
        internal const string XSD_KEYREF = "keyref";
        internal const string XSD_DATATYPE = "datatype";
        internal const string XSD_ALL = "all";
        internal const string XSD_SEQUENCE = "sequence";
        internal const string XSD_ENCODING = "encoding";
        internal const string XSD_EXTENSION = "extension";
        internal const string XSD_SIMPLECONTENT = "simpleContent";
        internal const string XSD_XPATH = "xpath";
        internal const string XSD_ATTRIBUTEFORMDEFAULT = "attributeFormDefault";
        internal const string XSD_ELEMENTFORMDEFAULT = "elementFormDefault";
        internal const string XSD_SCHEMALOCATION = "schemaLocation";
        internal const string XSD_CHOICE = "choice";
        internal const string XSD_RESTRICTION = "restriction";
        internal const string XSD_ANYTYPE = "anyType";

        internal const string XSINS = "http://www.w3.org/2001/XMLSchema-instance";
        internal const string XSI_NIL = "nil";
        internal const string XSI = "xsi";
        internal const string XML_XMLNS = "http://www.w3.org/XML/1998/namespace";

        // Keywords for sql Namespace
        internal const string UPDGNS = "urn:schemas-microsoft-com:xml-updategram";
        internal const string UPDG = "updg";
        internal const string SQL_SYNC = "sync";
        internal const string SQL_BEFORE = "before";
        internal const string SQL_AFTER = "after";
        internal const string SQL_ID = "id";
        internal const string SQL_UNCHANGED = "unchanged";

        // Keywords that don't have any namespace, but are atomized
        internal const string ATTRIBUTE = "attribute";
        internal const string CONTENT = "content";
        internal const string DEFAULT = "default";
        internal const string XSDID = "id";
        internal const string MINOCCURS = "minOccurs";
        internal const string MAXOCCURS = "maxOccurs";
        internal const string MODEL = "model";
        internal const string NAME = "name";
        internal const string NULLABLE = "nullable";
        internal const string ORDER = "order";
        internal const string REQUIRED = "required";
        internal const string REF = "ref";
        internal const string BASE = "base";
        internal const string TARGETNAMESPACE = "targetNamespace";
        internal const string TYPE = "type";
        internal const string XMLNS = "xmlns";
        internal const string XMLNS_XSD = "xmlns:xs";
        internal const string XMLNS_XSI = "xmlns:xsi";
        internal const string XMLNS_MSDATA = "xmlns:msdata";
        internal const string XMLNS_MSPROP = "xmlns:msprop";
        internal const string XMLNS_MSTNS = "xmlns:mstns";
        internal const string MSTNS_PREFIX = "mstns:";
        internal const string SPACE = "space";
        internal const string PRESERVE = "preserve";

        internal const string VALUE = "value";
        internal const string REFER = "refer";
        internal const string USE = "use";
        internal const string PROHIBITED = "prohibited";
        internal const string POSITIVEINFINITY = "INF";
        internal const string NEGATIVEINFINITY = "-INF";
        internal const string QUALIFIED = "qualified";
        internal const string UNQUALIFIED = "unqualified";


        // Keywords that are not atomized, just strings
        // they are mostly legal values for an attribute
        // NOTE: datatypes are enumerated in mapNameType table in XMLSchema.cs
        internal const string APP = "app";
        internal const string CLOSED = "closed";
        internal const string CURRENT = "Current";
        internal const string DOCUMENTELEMENT = "DocumentElement";
        internal const string FALSE = "false";
        internal const string FIXED = "fixed";
        internal const string FORM = "form";
        internal const string ENCODING = "encoding";
        internal const string ELEMENTONLY = "elementOnly";
        internal const string ELTONLY = "eltOnly";
        internal const string EMPTY = "empty";
        internal const string MANY = "many";
        internal const string MIXED = "mixed";
        internal const string NO = "no";
        internal const string NOTATION = "notation";
        internal const string OCCURS = "occurs";
        internal const string ONE_OR_MORE = "oneormore";
        internal const string ONE = "one";
        internal const string ONE_DIGIT = "1";
        internal const string ONCE = "once";
        internal const string OPTIONAL = "optional";
        internal const string OPEN = "open";
        internal const string ORIGINAL = "Original";
        internal const string RANGE = "range";
        internal const string SEQ = "seq";
        internal const string STAR = "*";
        internal const string TRUE = "true";
        internal const string TEXTONLY = "textOnly";
        internal const string VERSION = "version";
        internal const string XML = "xml";
        internal const string X_SCHEMA = "x-schema";
        internal const string YES = "yes";
        internal const string ZERO_DIGIT = "0";
        internal const string ZERO_OR_MORE = "unbounded";

        internal const string USEDATASETSCHEMAONLY = "UseDataSetSchemaOnly";
        internal const string UDTCOLUMNVALUEWRAPPED = "UDTColumnValueWrapped";
        internal const string TYPEINSTANCE = "Type";

        // Keywords for Msprop Namespace
        internal const string MSPROPNS = "urn:schemas-microsoft-com:xml-msprop";

        // Keywords in config file for  WebServices,related to publishing WSDL
        internal const string WS_DATASETFULLQNAME = "system.data.dataset";
        internal const string WS_VERSION = "WSDL_VERSION";
    } // Keywords
}
