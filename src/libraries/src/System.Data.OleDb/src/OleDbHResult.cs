// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.OleDb
{
    internal enum OleDbHResult
    { // OLEDB Error codes
        CO_E_CLASSSTRING = unchecked((int)0x800401f3),
        REGDB_E_CLASSNOTREG = unchecked((int)0x80040154),
        CO_E_NOTINITIALIZED = unchecked((int)0x800401F0),

        S_OK = 0x00000000,
        S_FALSE = 0x00000001,

        E_UNEXPECTED = unchecked((int)0x8000FFFF),
        E_NOTIMPL = unchecked((int)0x80004001),
        E_OUTOFMEMORY = unchecked((int)0x8007000E),
        E_INVALIDARG = unchecked((int)0x80070057),
        E_NOINTERFACE = unchecked((int)0x80004002),
        E_POINTER = unchecked((int)0x80004003),
        E_HANDLE = unchecked((int)0x80070006),
        E_ABORT = unchecked((int)0x80004004),
        E_FAIL = unchecked((int)0x80004005),
        E_ACCESSDENIED = unchecked((int)0x80070005),

        // MessageId: DB_E_BADACCESSORHANDLE
        // MessageText:
        //  Accessor is invalid.
        DB_E_BADACCESSORHANDLE = unchecked((int)0x80040E00),

        // MessageId: DB_E_ROWLIMITEXCEEDED
        // MessageText:
        //  Row could not be inserted into the rowset without exceeding provider's maximum number of active rows.
        DB_E_ROWLIMITEXCEEDED = unchecked((int)0x80040E01),

        // MessageId: DB_E_REOLEDBNLYACCESSOR
        // MessageText:
        //  Accessor is read-only. Operation failed.
        DB_E_REOLEDBNLYACCESSOR = unchecked((int)0x80040E02),

        // MessageId: DB_E_SCHEMAVIOLATION
        // MessageText:
        //  Values violate the database schema.
        DB_E_SCHEMAVIOLATION = unchecked((int)0x80040E03),

        // MessageId: DB_E_BADROWHANDLE
        // MessageText:
        //  Row handle is invalid.
        DB_E_BADROWHANDLE = unchecked((int)0x80040E04),

        // MessageId: DB_E_OBJECTOPEN
        // MessageText:
        //  Object was open.
        DB_E_OBJECTOPEN = unchecked((int)0x80040E05),

        // MessageId: DB_E_BADCHAPTER
        // MessageText:
        //  Chapter is invalid.
        DB_E_BADCHAPTER = unchecked((int)0x80040E06),

        // MessageId: DB_E_CANTCONVERTVALUE
        // MessageText:
        //  Data or literal value could not be converted to the type of the column in the data source, and the provider was unable to determine which columns could not be converted.  Data overflow or sign mismatch was not the cause.
        DB_E_CANTCONVERTVALUE = unchecked((int)0x80040E07),

        // MessageId: DB_E_BADBINDINFO
        // MessageText:
        //  Binding information is invalid.
        DB_E_BADBINDINFO = unchecked((int)0x80040E08),

        // MessageId: DB_SEC_E_PERMISSIONDENIED
        // MessageText:
        //  Permission denied.
        DB_SEC_E_PERMISSIONDENIED = unchecked((int)0x80040E09),

        // MessageId: DB_E_NOTAREFERENCECOLUMN
        // MessageText:
        //  Column does not contain bookmarks or chapters.
        DB_E_NOTAREFERENCECOLUMN = unchecked((int)0x80040E0A),

        // MessageId: DB_E_LIMITREJECTED
        // MessageText:
        //  Cost limits were rejected.
        DB_E_LIMITREJECTED = unchecked((int)0x80040E0B),

        // MessageId: DB_E_NOCOMMAND
        // MessageText:
        //  Command text was not set for the command object.
        DB_E_NOCOMMAND = unchecked((int)0x80040E0C),

        // MessageId: DB_E_COSTLIMIT
        // MessageText:
        //  Query plan within the cost limit cannot be found.
        DB_E_COSTLIMIT = unchecked((int)0x80040E0D),

        // MessageId: DB_E_BADBOOKMARK
        // MessageText:
        //  Bookmark is invalid.
        DB_E_BADBOOKMARK = unchecked((int)0x80040E0E),

        // MessageId: DB_E_BADLOCKMODE
        // MessageText:
        //  Lock mode is invalid.
        DB_E_BADLOCKMODE = unchecked((int)0x80040E0F),

        // MessageId: DB_E_PARAMNOTOPTIONAL
        // MessageText:
        //  No value given for one or more required parameters.
        DB_E_PARAMNOTOPTIONAL = unchecked((int)0x80040E10),

        // MessageId: DB_E_BADCOLUMNID
        // MessageText:
        //  Column ID is invalid.
        DB_E_BADCOLUMNID = unchecked((int)0x80040E11),

        // MessageId: DB_E_BADRATIO
        // MessageText:
        //  Numerator was greater than denominator. Values must express ratio between zero and 1.
        DB_E_BADRATIO = unchecked((int)0x80040E12),

        // MessageId: DB_E_BADVALUES
        // MessageText:
        //  Value is invalid.
        DB_E_BADVALUES = unchecked((int)0x80040E13),

        // MessageId: DB_E_ERRORSINCOMMAND
        // MessageText:
        //  One or more errors occurred during processing of command.
        DB_E_ERRORSINCOMMAND = unchecked((int)0x80040E14),

        // MessageId: DB_E_CANTCANCEL
        // MessageText:
        //  Command cannot be canceled.
        DB_E_CANTCANCEL = unchecked((int)0x80040E15),

        // MessageId: DB_E_DIALECTNOTSUPPORTED
        // MessageText:
        //  Command dialect is not supported by this provider.
        DB_E_DIALECTNOTSUPPORTED = unchecked((int)0x80040E16),

        // MessageId: DB_E_DUPLICATEDATASOURCE
        // MessageText:
        //  Data source object could not be created because the named data source already exists.
        DB_E_DUPLICATEDATASOURCE = unchecked((int)0x80040E17),

        // MessageId: DB_E_CANNOTRESTART
        // MessageText:
        //  Rowset position cannot be restarted.
        DB_E_CANNOTRESTART = unchecked((int)0x80040E18),

        // MessageId: DB_E_NOTFOUND
        // MessageText:
        //  Object or data matching the name, range, or selection criteria was not found within the scope of this operation.
        DB_E_NOTFOUND = unchecked((int)0x80040E19),

        // MessageId: DB_E_NEWLYINSERTED
        // MessageText:
        //  Identity cannot be determined for newly inserted rows.
        DB_E_NEWLYINSERTED = unchecked((int)0x80040E1B),

        // MessageId: DB_E_CANNOTFREE
        // MessageText:
        //  Provider has ownership of this tree.
        DB_E_CANNOTFREE = unchecked((int)0x80040E1A),

        // MessageId: DB_E_GOALREJECTED
        // MessageText:
        //  Goal was rejected because no nonzero weights were specified for any goals supported. Current goal was not changed.
        DB_E_GOALREJECTED = unchecked((int)0x80040E1C),

        // MessageId: DB_E_UNSUPPORTEDCONVERSION
        // MessageText:
        //  Requested conversion is not supported.
        DB_E_UNSUPPORTEDCONVERSION = unchecked((int)0x80040E1D),

        // MessageId: DB_E_BADSTARTPOSITION
        // MessageText:
        //  No rows were returned because the offset value moves the position before the beginning or after the end of the rowset.
        DB_E_BADSTARTPOSITION = unchecked((int)0x80040E1E),

        // MessageId: DB_E_NOQUERY
        // MessageText:
        //  Information was requested for a query and the query was not set.
        DB_E_NOQUERY = unchecked((int)0x80040E1F),

        // MessageId: DB_E_NOTREENTRANT
        // MessageText:
        //  Consumer's event handler called a non-reentrant method in the provider.
        DB_E_NOTREENTRANT = unchecked((int)0x80040E20),

        // MessageId: DB_E_ERRORSOCCURRED
        // MessageText:
        //  Multiple-step operation generated errors. Check each status value. No work was done.
        DB_E_ERRORSOCCURRED = unchecked((int)0x80040E21),

        // MessageId: DB_E_NOAGGREGATION
        // MessageText:
        //  Non-NULL controlling IUnknown was specified, and either the requested interface was not
        //  IUnknown, or the provider does not support COM aggregation.
        DB_E_NOAGGREGATION = unchecked((int)0x80040E22),

        // MessageId: DB_E_DELETEDROW
        // MessageText:
        //  Row handle referred to a deleted row or a row marked for deletion.
        DB_E_DELETEDROW = unchecked((int)0x80040E23),

        // MessageId: DB_E_CANTFETCHBACKWARDS
        // MessageText:
        //  Rowset does not support fetching backward.
        DB_E_CANTFETCHBACKWARDS = unchecked((int)0x80040E24),

        // MessageId: DB_E_ROWSNOTRELEASED
        // MessageText:
        //  Row handles must all be released before new ones can be obtained.
        DB_E_ROWSNOTRELEASED = unchecked((int)0x80040E25),

        // MessageId: DB_E_BADSTORAGEFLAG
        // MessageText:
        //  One or more storage flags are not supported.
        DB_E_BADSTORAGEFLAG = unchecked((int)0x80040E26),

        // MessageId: DB_E_BADCOMPAREOP
        // MessageText:
        //  Comparison operator is invalid.
        DB_E_BADCOMPAREOP = unchecked((int)0x80040E27),

        // MessageId: DB_E_BADSTATUSVALUE
        // MessageText:
        //  Status flag was neither DBCOLUMNSTATUS_OK nor
        //  DBCOLUMNSTATUS_ISNULL.
        DB_E_BADSTATUSVALUE = unchecked((int)0x80040E28),

        // MessageId: DB_E_CANTSCROLLBACKWARDS
        // MessageText:
        //  Rowset does not support scrolling backward.
        DB_E_CANTSCROLLBACKWARDS = unchecked((int)0x80040E29),

        // MessageId: DB_E_BADREGIONHANDLE
        // MessageText:
        //  Region handle is invalid.
        DB_E_BADREGIONHANDLE = unchecked((int)0x80040E2A),

        // MessageId: DB_E_NONCONTIGUOUSRANGE
        // MessageText:
        //  Set of rows is not contiguous to, or does not overlap, the rows in the watch region.
        DB_E_NONCONTIGUOUSRANGE = unchecked((int)0x80040E2B),

        // MessageId: DB_E_INVALIDTRANSITION
        // MessageText:
        //  Transition from ALL* to MOVE* or EXTEND* was specified.
        DB_E_INVALIDTRANSITION = unchecked((int)0x80040E2C),

        // MessageId: DB_E_NOTASUBREGION
        // MessageText:
        //  Region is not a proper subregion of the region identified by the watch region handle.
        DB_E_NOTASUBREGION = unchecked((int)0x80040E2D),

        // MessageId: DB_E_MULTIPLESTATEMENTS
        // MessageText:
        //  Multiple-statement commands are not supported by this provider.
        DB_E_MULTIPLESTATEMENTS = unchecked((int)0x80040E2E),

        // MessageId: DB_E_INTEGRITYVIOLATION
        // MessageText:
        //  Value violated the integrity constraints for a column or table.
        DB_E_INTEGRITYVIOLATION = unchecked((int)0x80040E2F),

        // MessageId: DB_E_BADTYPENAME
        // MessageText:
        //  Type name is invalid.
        DB_E_BADTYPENAME = unchecked((int)0x80040E30),

        // MessageId: DB_E_ABORTLIMITREACHED
        // MessageText:
        //  Execution stopped because a resource limit was reached. No results were returned.
        DB_E_ABORTLIMITREACHED = unchecked((int)0x80040E31),

        // MessageId: DB_E_ROWSETINCOMMAND
        // MessageText:
        //  Command object whose command tree contains a rowset or rowsets cannot be cloned.
        DB_E_ROWSETINCOMMAND = unchecked((int)0x80040E32),

        // MessageId: DB_E_CANTTRANSLATE
        // MessageText:
        //  Current tree cannot be represented as text.
        DB_E_CANTTRANSLATE = unchecked((int)0x80040E33),

        // MessageId: DB_E_DUPLICATEINDEXID
        // MessageText:
        //  Index already exists.
        DB_E_DUPLICATEINDEXID = unchecked((int)0x80040E34),

        // MessageId: DB_E_NOINDEX
        // MessageText:
        //  Index does not exist.
        DB_E_NOINDEX = unchecked((int)0x80040E35),

        // MessageId: DB_E_INDEXINUSE
        // MessageText:
        //  Index is in use.
        DB_E_INDEXINUSE = unchecked((int)0x80040E36),

        // MessageId: DB_E_NOTABLE
        // MessageText:
        //  Table does not exist.
        DB_E_NOTABLE = unchecked((int)0x80040E37),

        // MessageId: DB_E_CONCURRENCYVIOLATION
        // MessageText:
        //  Rowset used optimistic concurrency and the value of a column has changed since it was last read.
        DB_E_CONCURRENCYVIOLATION = unchecked((int)0x80040E38),

        // MessageId: DB_E_BADCOPY
        // MessageText:
        //  Errors detected during the copy.
        DB_E_BADCOPY = unchecked((int)0x80040E39),

        // MessageId: DB_E_BADPRECISION
        // MessageText:
        //  Precision is invalid.
        DB_E_BADPRECISION = unchecked((int)0x80040E3A),

        // MessageId: DB_E_BADSCALE
        // MessageText:
        //  Scale is invalid.
        DB_E_BADSCALE = unchecked((int)0x80040E3B),

        // MessageId: DB_E_BADTABLEID
        // MessageText:
        //  Table ID is invalid.
        DB_E_BADTABLEID = unchecked((int)0x80040E3C),

        // MessageId: DB_E_BADTYPE
        // MessageText:
        //  Type is invalid.
        DB_E_BADTYPE = unchecked((int)0x80040E3D),

        // MessageId: DB_E_DUPLICATECOLUMNID
        // MessageText:
        //  Column ID already exists or occurred more than once in the array of columns.
        DB_E_DUPLICATECOLUMNID = unchecked((int)0x80040E3E),

        // MessageId: DB_E_DUPLICATETABLEID
        // MessageText:
        //  Table already exists.
        DB_E_DUPLICATETABLEID = unchecked((int)0x80040E3F),

        // MessageId: DB_E_TABLEINUSE
        // MessageText:
        //  Table is in use.
        DB_E_TABLEINUSE = unchecked((int)0x80040E40),

        // MessageId: DB_E_NOLOCALE
        // MessageText:
        //  Locale ID is not supported.
        DB_E_NOLOCALE = unchecked((int)0x80040E41),

        // MessageId: DB_E_BADRECORDNUM
        // MessageText:
        //  Record number is invalid.
        DB_E_BADRECORDNUM = unchecked((int)0x80040E42),

        // MessageId: DB_E_BOOKMARKSKIPPED
        // MessageText:
        //  Form of bookmark is valid, but no row was found to match it.
        DB_E_BOOKMARKSKIPPED = unchecked((int)0x80040E43),

        // MessageId: DB_E_BADPROPERTYVALUE
        // MessageText:
        //  Property value is invalid.
        DB_E_BADPROPERTYVALUE = unchecked((int)0x80040E44),

        // MessageId: DB_E_INVALID
        // MessageText:
        //  Rowset is not chaptered.
        DB_E_INVALID = unchecked((int)0x80040E45),

        // MessageId: DB_E_BADACCESSORFLAGS
        // MessageText:
        //  One or more accessor flags were invalid.
        DB_E_BADACCESSORFLAGS = unchecked((int)0x80040E46),

        // MessageId: DB_E_BADSTORAGEFLAGS
        // MessageText:
        //  One or more storage flags are invalid.
        DB_E_BADSTORAGEFLAGS = unchecked((int)0x80040E47),

        // MessageId: DB_E_BYREFACCESSORNOTSUPPORTED
        // MessageText:
        //  Reference accessors are not supported by this provider.
        DB_E_BYREFACCESSORNOTSUPPORTED = unchecked((int)0x80040E48),

        // MessageId: DB_E_NULLACCESSORNOTSUPPORTED
        // MessageText:
        //  Null accessors are not supported by this provider.
        DB_E_NULLACCESSORNOTSUPPORTED = unchecked((int)0x80040E49),

        // MessageId: DB_E_NOTPREPARED
        // MessageText:
        //  Command was not prepared.
        DB_E_NOTPREPARED = unchecked((int)0x80040E4A),

        // MessageId: DB_E_BADACCESSORTYPE
        // MessageText:
        //  Accessor is not a parameter accessor.
        DB_E_BADACCESSORTYPE = unchecked((int)0x80040E4B),

        // MessageId: DB_E_WRITEONLYACCESSOR
        // MessageText:
        //  Accessor is write-only.
        DB_E_WRITEONLYACCESSOR = unchecked((int)0x80040E4C),

        // MessageId: DB_SEC_E_AUTH_FAILED
        // MessageText:
        //  Authentication failed.
        DB_SEC_E_AUTH_FAILED = unchecked((int)0x80040E4D),

        // MessageId: DB_E_CANCELED
        // MessageText:
        //  Operation was canceled.
        DB_E_CANCELED = unchecked((int)0x80040E4E),

        // MessageId: DB_E_CHAPTERNOTRELEASED
        // MessageText:
        //  Rowset is single-chaptered. The chapter was not released.
        DB_E_CHAPTERNOTRELEASED = unchecked((int)0x80040E4F),

        // MessageId: DB_E_BADSOURCEHANDLE
        // MessageText:
        //  Source handle is invalid.
        DB_E_BADSOURCEHANDLE = unchecked((int)0x80040E50),

        // MessageId: DB_E_PARAMUNAVAILABLE
        // MessageText:
        //  Provider cannot derive parameter information and SetParameterInfo has not been called.
        DB_E_PARAMUNAVAILABLE = unchecked((int)0x80040E51),

        // MessageId: DB_E_ALREADYINITIALIZED
        // MessageText:
        //  Data source object is already initialized.
        DB_E_ALREADYINITIALIZED = unchecked((int)0x80040E52),

        // MessageId: DB_E_NOTSUPPORTED
        // MessageText:
        //  Method is not supported by this provider.
        DB_E_NOTSUPPORTED = unchecked((int)0x80040E53),

        // MessageId: DB_E_MAXPENDCHANGESEXCEEDED
        // MessageText:
        //  Number of rows with pending changes exceeded the limit.
        DB_E_MAXPENDCHANGESEXCEEDED = unchecked((int)0x80040E54),

        // MessageId: DB_E_BADORDINAL
        // MessageText:
        //  Column does not exist.
        DB_E_BADORDINAL = unchecked((int)0x80040E55),

        // MessageId: DB_E_PENDINGCHANGES
        // MessageText:
        //  Pending changes exist on a row with a reference count of zero.
        DB_E_PENDINGCHANGES = unchecked((int)0x80040E56),

        // MessageId: DB_E_DATAOVERFLOW
        // MessageText:
        //  Literal value in the command exceeded the range of the type of the associated column.
        DB_E_DATAOVERFLOW = unchecked((int)0x80040E57),

        // MessageId: DB_E_BADHRESULT
        // MessageText:
        //  HRESULT is invalid.
        DB_E_BADHRESULT = unchecked((int)0x80040E58),

        // MessageId: DB_E_BADLOOKUPID
        // MessageText:
        //  Lookup ID is invalid.
        DB_E_BADLOOKUPID = unchecked((int)0x80040E59),

        // MessageId: DB_E_BADDYNAMICERRORID
        // MessageText:
        //  DynamicError ID is invalid.
        DB_E_BADDYNAMICERRORID = unchecked((int)0x80040E5A),

        // MessageId: DB_E_PENDINGINSERT
        // MessageText:
        //  Most recent data for a newly inserted row could not be retrieved because the insert is pending.
        DB_E_PENDINGINSERT = unchecked((int)0x80040E5B),

        // MessageId: DB_E_BADCONVERTFLAG
        // MessageText:
        //  Conversion flag is invalid.
        DB_E_BADCONVERTFLAG = unchecked((int)0x80040E5C),

        // MessageId: DB_E_BADPARAMETERNAME
        // MessageText:
        //  Parameter name is unrecognized.
        DB_E_BADPARAMETERNAME = unchecked((int)0x80040E5D),

        // MessageId: DB_E_MULTIPLESTORAGE
        // MessageText:
        //  Multiple storage objects cannot be open simultaneously.
        DB_E_MULTIPLESTORAGE = unchecked((int)0x80040E5E),

        // MessageId: DB_E_CANTFILTER
        // MessageText:
        //  Filter cannot be opened.
        DB_E_CANTFILTER = unchecked((int)0x80040E5F),

        // MessageId: DB_E_CANTORDER
        // MessageText:
        //  Order cannot be opened.
        DB_E_CANTORDER = unchecked((int)0x80040E60),

        // MessageId: MD_E_BADTUPLE
        // MessageText:
        //  Tuple is invalid.
        MD_E_BADTUPLE = unchecked((int)0x80040E61),

        // MessageId: MD_E_BADCOORDINATE
        // MessageText:
        //  Coordinate is invalid.
        MD_E_BADCOORDINATE = unchecked((int)0x80040E62),

        // MessageId: MD_E_INVALIDAXIS
        // MessageText:
        //  Axis is invalid.
        MD_E_INVALIDAXIS = unchecked((int)0x80040E63),

        // MessageId: MD_E_INVALIDCELLRANGE
        // MessageText:
        //  One or more cell ordinals is invalid.
        MD_E_INVALIDCELLRANGE = unchecked((int)0x80040E64),

        // MessageId: DB_E_NOCOLUMN
        // MessageText:
        //  Column ID is invalid.
        DB_E_NOCOLUMN = unchecked((int)0x80040E65),

        // MessageId: DB_E_COMMANDNOTPERSISTED
        // MessageText:
        //  Command does not have a DBID.
        DB_E_COMMANDNOTPERSISTED = unchecked((int)0x80040E67),

        // MessageId: DB_E_DUPLICATEID
        // MessageText:
        //  DBID already exists.
        DB_E_DUPLICATEID = unchecked((int)0x80040E68),

        // MessageId: DB_E_OBJECTCREATIONLIMITREACHED
        // MessageText:
        //  Session cannot be created because maximum number of active sessions was already reached. Consumer must release one or more sessions before creating a new session object.
        DB_E_OBJECTCREATIONLIMITREACHED = unchecked((int)0x80040E69),

        // MessageId: DB_E_BADINDEXID
        // MessageText:
        //  Index ID is invalid.
        DB_E_BADINDEXID = unchecked((int)0x80040E72),

        // MessageId: DB_E_BADINITSTRING
        // MessageText:
        //  Format of the initialization string does not conform to the OLE DB specification.
        DB_E_BADINITSTRING = unchecked((int)0x80040E73),

        // MessageId: DB_E_NOPROVIDERSREGISTERED
        // MessageText:
        //  No OLE DB providers of this source type are registered.
        DB_E_NOPROVIDERSREGISTERED = unchecked((int)0x80040E74),

        // MessageId: DB_E_MISMATCHEDPROVIDER
        // MessageText:
        //  Initialization string specifies a provider that does not match the active provider.
        DB_E_MISMATCHEDPROVIDER = unchecked((int)0x80040E75),

        // MessageId: DB_E_BADCOMMANDID
        // MessageText:
        //  DBID is invalid.
        DB_E_BADCOMMANDID = unchecked((int)0x80040E76),

        // MessageId: SEC_E_BADTRUSTEEID
        // MessageText:
        //  Trustee is invalid.
        SEC_E_BADTRUSTEEID = unchecked((int)0x80040E6A),

        // MessageId: SEC_E_NOTRUSTEEID
        // MessageText:
        //  Trustee was not recognized for this data source.
        SEC_E_NOTRUSTEEID = unchecked((int)0x80040E6B),

        // MessageId: SEC_E_NOMEMBERSHIPSUPPORT
        // MessageText:
        //  Trustee does not support memberships or collections.
        SEC_E_NOMEMBERSHIPSUPPORT = unchecked((int)0x80040E6C),

        // MessageId: SEC_E_INVALIDOBJECT
        // MessageText:
        //  Object is invalid or unknown to the provider.
        SEC_E_INVALIDOBJECT = unchecked((int)0x80040E6D),

        // MessageId: SEC_E_NOOWNER
        // MessageText:
        //  Object does not have an owner.
        SEC_E_NOOWNER = unchecked((int)0x80040E6E),

        // MessageId: SEC_E_INVALIDACCESSENTRYLIST
        // MessageText:
        //  Access entry list is invalid.
        SEC_E_INVALIDACCESSENTRYLIST = unchecked((int)0x80040E6F),

        // MessageId: SEC_E_INVALIDOWNER
        // MessageText:
        //  Trustee supplied as owner is invalid or unknown to the provider.
        SEC_E_INVALIDOWNER = unchecked((int)0x80040E70),

        // MessageId: SEC_E_INVALIDACCESSENTRY
        // MessageText:
        //  Permission in the access entry list is invalid.
        SEC_E_INVALIDACCESSENTRY = unchecked((int)0x80040E71),

        // MessageId: DB_E_BADCONSTRAINTTYPE
        // MessageText:
        //  ConstraintType is invalid or not supported by the provider.
        DB_E_BADCONSTRAINTTYPE = unchecked((int)0x80040E77),

        // MessageId: DB_E_BADCONSTRAINTFORM
        // MessageText:
        //  ConstraintType is not DBCONSTRAINTTYPE_FOREIGNKEY and cForeignKeyColumns is not zero.
        DB_E_BADCONSTRAINTFORM = unchecked((int)0x80040E78),

        // MessageId: DB_E_BADDEFERRABILITY
        // MessageText:
        //  Specified deferrability flag is invalid or not supported by the provider.
        DB_E_BADDEFERRABILITY = unchecked((int)0x80040E79),

        // MessageId: DB_E_BADMATCHTYPE
        // MessageText:
        //  MatchType is invalid or the value is not supported by the provider.
        DB_E_BADMATCHTYPE = unchecked((int)0x80040E80),

        // MessageId: DB_E_BADUPDATEDELETERULE
        // MessageText:
        //  Constraint update rule or delete rule is invalid.
        DB_E_BADUPDATEDELETERULE = unchecked((int)0x80040E8A),

        // MessageId: DB_E_BADCONSTRAINTID
        // MessageText:
        //  Constraint does not exist.
        DB_E_BADCONSTRAINTID = unchecked((int)0x80040E8B),

        // MessageId: DB_E_BADCOMMANDFLAGS
        // MessageText:
        //  Command persistence flag is invalid.
        DB_E_BADCOMMANDFLAGS = unchecked((int)0x80040E8C),

        // MessageId: DB_E_OBJECTMISMATCH
        // MessageText:
        //  rguidColumnType points to a GUID that does not match the object type of this column, or this column was not set.
        DB_E_OBJECTMISMATCH = unchecked((int)0x80040E8D),

        // MessageId: DB_E_NOSOURCEOBJECT
        // MessageText:
        //  Source row does not exist.
        DB_E_NOSOURCEOBJECT = unchecked((int)0x80040E91),

        // MessageId: DB_E_RESOURCELOCKED
        // MessageText:
        //  OLE DB object represented by this URL is locked by one or more other processes.
        DB_E_RESOURCELOCKED = unchecked((int)0x80040E92),

        // MessageId: DB_E_NOTCOLLECTION
        // MessageText:
        //  Client requested an object type that is valid only for a collection.
        DB_E_NOTCOLLECTION = unchecked((int)0x80040E93),

        // MessageId: DB_E_REOLEDBNLY
        // MessageText:
        //  Caller requested write access to a read-only object.
        DB_E_REOLEDBNLY = unchecked((int)0x80040E94),

        // MessageId: DB_E_ASYNCNOTSUPPORTED
        // MessageText:
        //  Asynchronous binding is not supported by this provider.
        DB_E_ASYNCNOTSUPPORTED = unchecked((int)0x80040E95),

        // MessageId: DB_E_CANNOTCONNECT
        // MessageText:
        //  Connection to the server for this URL cannot be established.
        DB_E_CANNOTCONNECT = unchecked((int)0x80040E96),

        // MessageId: DB_E_TIMEOUT
        // MessageText:
        //  Timeout occurred when attempting to bind to the object.
        DB_E_TIMEOUT = unchecked((int)0x80040E97),

        // MessageId: DB_E_RESOURCEEXISTS
        // MessageText:
        //  Object cannot be created at this URL because an object named by this URL already exists.
        DB_E_RESOURCEEXISTS = unchecked((int)0x80040E98),

        // MessageId: DB_E_RESOURCEOUTOFSCOPE
        // MessageText:
        //  URL is outside of scope.
        DB_E_RESOURCEOUTOFSCOPE = unchecked((int)0x80040E8E),

        // MessageId: DB_E_DROPRESTRICTED
        // MessageText:
        //  Column or constraint could not be dropped because it is referenced by a dependent view or constraint.
        DB_E_DROPRESTRICTED = unchecked((int)0x80040E90),

        // MessageId: DB_E_DUPLICATECONSTRAINTID
        // MessageText:
        //  Constraint already exists.
        DB_E_DUPLICATECONSTRAINTID = unchecked((int)0x80040E99),

        // MessageId: DB_E_OUTOFSPACE
        // MessageText:
        //  Object cannot be created at this URL because the server is out of physical storage.
        DB_E_OUTOFSPACE = unchecked((int)0x80040E9A),

        // MessageId: DB_SEC_E_SAFEMODE_DENIED
        // MessageText:
        //  Safety settings on this computer prohibit accessing a data source on another domain.
        DB_SEC_E_SAFEMODE_DENIED = unchecked((int)0x80040E9B),

        // MessageId: DB_S_ROWLIMITEXCEEDED
        // MessageText:
        //  Fetching requested number of rows will exceed total number of active rows supported by the rowset.
        DB_S_ROWLIMITEXCEEDED = 0x00040EC0,

        // MessageId: DB_S_COLUMNTYPEMISMATCH
        // MessageText:
        //  One or more column types are incompatible. Conversion errors will occur during copying.
        DB_S_COLUMNTYPEMISMATCH = 0x00040EC1,

        // MessageId: DB_S_TYPEINFOOVERRIDDEN
        // MessageText:
        //  Parameter type information was overridden by caller.
        DB_S_TYPEINFOOVERRIDDEN = 0x00040EC2,

        // MessageId: DB_S_BOOKMARKSKIPPED
        // MessageText:
        //  Bookmark was skipped for deleted or nonmember row.
        DB_S_BOOKMARKSKIPPED = 0x00040EC3,

        // MessageId: DB_S_NONEXTROWSET
        // MessageText:
        //  No more rowsets.
        DB_S_NONEXTROWSET = 0x00040EC5,

        // MessageId: DB_S_ENDOFROWSET
        // MessageText:
        //  Start or end of rowset or chapter was reached.
        DB_S_ENDOFROWSET = 0x00040EC6,

        // MessageId: DB_S_COMMANDREEXECUTED
        // MessageText:
        //  Command was reexecuted.
        DB_S_COMMANDREEXECUTED = 0x00040EC7,

        // MessageId: DB_S_BUFFERFULL
        // MessageText:
        //  Operation succeeded, but status array or string buffer could not be allocated.
        DB_S_BUFFERFULL = 0x00040EC8,

        // MessageId: DB_S_NORESULT
        // MessageText:
        //  No more results.
        DB_S_NORESULT = 0x00040EC9,

        // MessageId: DB_S_CANTRELEASE
        // MessageText:
        //  Server cannot release or downgrade a lock until the end of the transaction.
        DB_S_CANTRELEASE = 0x00040ECA,

        // MessageId: DB_S_GOALCHANGED
        // MessageText:
        //  Weight is not supported or exceeded the supported limit, and was set to 0 or the supported limit.
        DB_S_GOALCHANGED = 0x00040ECB,

        // MessageId: DB_S_UNWANTEDOPERATION
        // MessageText:
        //  Consumer does not want to receive further notification calls for this operation.
        DB_S_UNWANTEDOPERATION = 0x00040ECC,

        // MessageId: DB_S_DIALECTIGNORED
        // MessageText:
        //  Input dialect was ignored and command was processed using default dialect.
        DB_S_DIALECTIGNORED = 0x00040ECD,

        // MessageId: DB_S_UNWANTEDPHASE
        // MessageText:
        //  Consumer does not want to receive further notification calls for this phase.
        DB_S_UNWANTEDPHASE = 0x00040ECE,

        // MessageId: DB_S_UNWANTEDREASON
        // MessageText:
        //  Consumer does not want to receive further notification calls for this reason.
        DB_S_UNWANTEDREASON = 0x00040ECF,

        // MessageId: DB_S_ASYNCHRONOUS
        // MessageText:
        //  Operation is being processed asynchronously.
        DB_S_ASYNCHRONOUS = 0x00040ED0,

        // MessageId: DB_S_COLUMNSCHANGED
        // MessageText:
        //  Command was executed to reposition to the start of the rowset. Either the order of the columns changed, or columns were added to or removed from the rowset.
        DB_S_COLUMNSCHANGED = 0x00040ED1,

        // MessageId: DB_S_ERRORSRETURNED
        // MessageText:
        //  Method had some errors, which were returned in the error array.
        DB_S_ERRORSRETURNED = 0x00040ED2,

        // MessageId: DB_S_BADROWHANDLE
        // MessageText:
        //  Row handle is invalid.
        DB_S_BADROWHANDLE = 0x00040ED3,

        // MessageId: DB_S_DELETEDROW
        // MessageText:
        //  Row handle referred to a deleted row.
        DB_S_DELETEDROW = 0x00040ED4,

        // MessageId: DB_S_TOOMANYCHANGES
        // MessageText:
        //  Provider cannot keep track of all the changes. Client must refetch the data associated with the watch region by using another method.
        DB_S_TOOMANYCHANGES = 0x00040ED5,

        // MessageId: DB_S_STOPLIMITREACHED
        // MessageText:
        //  Execution stopped because a resource limit was reached. Results obtained so far were returned, but execution cannot resume.
        DB_S_STOPLIMITREACHED = 0x00040ED6,

        // MessageId: DB_S_LOCKUPGRADED
        // MessageText:
        //  Lock was upgraded from the value specified.
        DB_S_LOCKUPGRADED = 0x00040ED8,

        // MessageId: DB_S_PROPERTIESCHANGED
        // MessageText:
        //  One or more properties were changed as allowed by provider.
        DB_S_PROPERTIESCHANGED = 0x00040ED9,

        // MessageId: DB_S_ERRORSOCCURRED
        // MessageText:
        //  Multiple-step operation completed with one or more errors. Check each status value.
        DB_S_ERRORSOCCURRED = 0x00040EDA,

        // MessageId: DB_S_PARAMUNAVAILABLE
        // MessageText:
        //  Parameter is invalid.
        DB_S_PARAMUNAVAILABLE = 0x00040EDB,

        // MessageId: DB_S_MULTIPLECHANGES
        // MessageText:
        //  Updating a row caused more than one row to be updated in the data source.
        DB_S_MULTIPLECHANGES = 0x00040EDC,

        // MessageId: DB_S_NOTSINGLETON
        // MessageText:
        //  Row object was requested on a non-singleton result. First row was returned.
        DB_S_NOTSINGLETON = 0x00040ED7,

        // MessageId: DB_S_NOROWSPECIFICCOLUMNS
        // MessageText:
        //  Row has no row-specific columns.
        DB_S_NOROWSPECIFICCOLUMNS = 0x00040EDD,

        XACT_E_FIRST = unchecked((int)0x8004d000),
        XACT_E_LAST = unchecked((int)0x8004d022),
        XACT_S_FIRST = 0x4d000,
        XACT_S_LAST = 0x4d009,
        XACT_E_ALREADYOTHERSINGLEPHASE = unchecked((int)0x8004d000),
        XACT_E_CANTRETAIN = unchecked((int)0x8004d001),
        XACT_E_COMMITFAILED = unchecked((int)0x8004d002),
        XACT_E_COMMITPREVENTED = unchecked((int)0x8004d003),
        XACT_E_HEURISTICABORT = unchecked((int)0x8004d004),
        XACT_E_HEURISTICCOMMIT = unchecked((int)0x8004d005),
        XACT_E_HEURISTICDAMAGE = unchecked((int)0x8004d006),
        XACT_E_HEURISTICDANGER = unchecked((int)0x8004d007),
        XACT_E_ISOLATIONLEVEL = unchecked((int)0x8004d008),
        XACT_E_NOASYNC = unchecked((int)0x8004d009),
        XACT_E_NOENLIST = unchecked((int)0x8004d00a),
        XACT_E_NOISORETAIN = unchecked((int)0x8004d00b),
        XACT_E_NORESOURCE = unchecked((int)0x8004d00c),
        XACT_E_NOTCURRENT = unchecked((int)0x8004d00d),
        XACT_E_NOTRANSACTION = unchecked((int)0x8004d00e),
        XACT_E_NOTSUPPORTED = unchecked((int)0x8004d00f),
        XACT_E_UNKNOWNRMGRID = unchecked((int)0x8004d010),
        XACT_E_WRONGSTATE = unchecked((int)0x8004d011),
        XACT_E_WRONGUOW = unchecked((int)0x8004d012),
        XACT_E_XTIONEXISTS = unchecked((int)0x8004d013),
        XACT_E_NOIMPORTOBJECT = unchecked((int)0x8004d014),
        XACT_E_INVALIDCOOKIE = unchecked((int)0x8004d015),
        XACT_E_INDOUBT = unchecked((int)0x8004d016),
        XACT_E_NOTIMEOUT = unchecked((int)0x8004d017),
        XACT_E_ALREADYINPROGRESS = unchecked((int)0x8004d018),
        XACT_E_ABORTED = unchecked((int)0x8004d019),
        XACT_E_LOGFULL = unchecked((int)0x8004d01a),
        XACT_E_TMNOTAVAILABLE = unchecked((int)0x8004d01b),
        XACT_E_CONNECTION_DOWN = unchecked((int)0x8004d01c),
        XACT_E_CONNECTION_DENIED = unchecked((int)0x8004d01d),
        XACT_E_REENLISTTIMEOUT = unchecked((int)0x8004d01e),
        XACT_E_TIP_CONNECT_FAILED = unchecked((int)0x8004d01f),
        XACT_E_TIP_PROTOCOL_ERROR = unchecked((int)0x8004d020),
        XACT_E_TIP_PULL_FAILED = unchecked((int)0x8004d021),
        XACT_E_DEST_TMNOTAVAILABLE = unchecked((int)0x8004d022),
        XACT_E_CLERKNOTFOUND = unchecked((int)0x8004d080),
        XACT_E_CLERKEXISTS = unchecked((int)0x8004d081),
        XACT_E_RECOVERYINPROGRESS = unchecked((int)0x8004d082),
        XACT_E_TRANSACTIONCLOSED = unchecked((int)0x8004d083),
        XACT_E_INVALIDLSN = unchecked((int)0x8004d084),
        XACT_E_REPLAYREQUEST = unchecked((int)0x8004d085),
        XACT_S_ASYNC = 0x4d000,
        XACT_S_DEFECT = 0x4d001,
        XACT_S_REOLEDBNLY = 0x4d002,
        XACT_S_SOMENORETAIN = 0x4d003,
        XACT_S_OKINFORM = 0x4d004,
        XACT_S_MADECHANGESCONTENT = 0x4d005,
        XACT_S_MADECHANGESINFORM = 0x4d006,
        XACT_S_ALLNORETAIN = 0x4d007,
        XACT_S_ABORTING = 0x4d008,
        XACT_S_SINGLEPHASE = 0x4d009,

        STG_E_INVALIDFUNCTION = unchecked((int)0x80030001),
        STG_E_FILENOTFOUND = unchecked((int)0x80030002),
        STG_E_PATHNOTFOUND = unchecked((int)0x80030003),
        STG_E_TOOMANYOPENFILES = unchecked((int)0x80030004),
        STG_E_ACCESSDENIED = unchecked((int)0x80030005),
        STG_E_INVALIDHANDLE = unchecked((int)0x80030006),
        STG_E_INSUFFICIENTMEMORY = unchecked((int)0x80030008),
        STG_E_INVALIDPOINTER = unchecked((int)0x80030009),
        STG_E_NOMOREFILES = unchecked((int)0x80030012),
        STG_E_DISKISWRITEPROTECTED = unchecked((int)0x80030013),
        STG_E_SEEKERROR = unchecked((int)0x80030019),
        STG_E_WRITEFAULT = unchecked((int)0x8003001D),
        STG_E_READFAULT = unchecked((int)0x8003001E),
        STG_E_SHAREVIOLATION = unchecked((int)0x80030020),
        STG_E_LOCKVIOLATION = unchecked((int)0x80030021),
        STG_E_FILEALREADYEXISTS = unchecked((int)0x80030050),
        STG_E_INVALIDPARAMETER = unchecked((int)0x80030057),
        STG_E_MEDIUMFULL = unchecked((int)0x80030070),
        STG_E_PROPSETMISMATCHED = unchecked((int)0x800300F0),
        STG_E_ABNORMALAPIEXIT = unchecked((int)0x800300FA),
        STG_E_INVALIDHEADER = unchecked((int)0x800300FB),
        STG_E_INVALIDNAME = unchecked((int)0x800300FC),
        STG_E_UNKNOWN = unchecked((int)0x800300FD),
        STG_E_UNIMPLEMENTEDFUNCTION = unchecked((int)0x800300FE),
        STG_E_INVALIDFLAG = unchecked((int)0x800300FF),
        STG_E_INUSE = unchecked((int)0x80030100),
        STG_E_NOTCURRENT = unchecked((int)0x80030101),
        STG_E_REVERTED = unchecked((int)0x80030102),
        STG_E_CANTSAVE = unchecked((int)0x80030103),
        STG_E_OLDFORMAT = unchecked((int)0x80030104),
        STG_E_OLDDLL = unchecked((int)0x80030105),
        STG_E_SHAREREQUIRED = unchecked((int)0x80030106),
        STG_E_NOTFILEBASEDSTORAGE = unchecked((int)0x80030107),
        STG_E_EXTANTMARSHALLINGS = unchecked((int)0x80030108),
        STG_E_DOCFILECORRUPT = unchecked((int)0x80030109),
        STG_E_BADBASEADDRESS = unchecked((int)0x80030110),
        STG_E_INCOMPLETE = unchecked((int)0x80030201),
        STG_E_TERMINATED = unchecked((int)0x80030202),
        STG_S_CONVERTED = 0x00030200,
        STG_S_BLOCK = 0x00030201,
        STG_S_RETRYNOW = 0x00030202,
        STG_S_MONITORING = 0x00030203,
    }
}
