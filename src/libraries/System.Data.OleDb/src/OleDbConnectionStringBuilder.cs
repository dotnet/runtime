// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace System.Data.OleDb
{
    [DefaultProperty("Provider")]
    [RefreshProperties(RefreshProperties.All)]
    [TypeConverter(typeof(OleDbConnectionStringBuilder.OleDbConnectionStringBuilderConverter))]
    public sealed class OleDbConnectionStringBuilder : DbConnectionStringBuilder
    {
        private enum Keywords
        {
            FileName,
            Provider,
            DataSource,
            PersistSecurityInfo,
            OleDbServices,
        }
        private static readonly string[] s_validKeywords = new string[5]
        {
            DbConnectionStringKeywords.FileName,
            DbConnectionStringKeywords.Provider,
            DbConnectionStringKeywords.DataSource,
            DbConnectionStringKeywords.PersistSecurityInfo,
            DbConnectionStringKeywords.OleDbServices
        };
        private static readonly Dictionary<string, Keywords> s_keywords = new Dictionary<string, Keywords>(5, StringComparer.OrdinalIgnoreCase)
        {
            { DbConnectionStringKeywords.FileName, Keywords.FileName },
            { DbConnectionStringKeywords.Provider, Keywords.Provider },
            { DbConnectionStringKeywords.DataSource, Keywords.DataSource },
            { DbConnectionStringKeywords.PersistSecurityInfo, Keywords.PersistSecurityInfo },
            { DbConnectionStringKeywords.OleDbServices, Keywords.OleDbServices },
        };

        private string[]? _knownKeywords;
        private Dictionary<string, OleDbPropertyInfo>? _propertyInfo;

        private string _fileName = DbConnectionStringDefaults.FileName;

        private string _dataSource = DbConnectionStringDefaults.DataSource;
        private string _provider = DbConnectionStringDefaults.Provider;

        private int _oleDbServices = DbConnectionStringDefaults.OleDbServices;

        private bool _persistSecurityInfo = DbConnectionStringDefaults.PersistSecurityInfo;

        public OleDbConnectionStringBuilder() : this(null)
        {
            _knownKeywords = s_validKeywords;
        }

        public OleDbConnectionStringBuilder(string? connectionString) : base()
        {
            if (!ADP.IsEmpty(connectionString))
            {
                ConnectionString = connectionString;
            }
        }

        [AllowNull]
        public override object this[string keyword]
        {
            get
            {
                ADP.CheckArgumentNull(keyword, "keyword");
                object? value;
                Keywords index;
                if (s_keywords.TryGetValue(keyword, out index))
                {
                    value = GetAt(index);
                }
                else if (!base.TryGetValue(keyword, out value))
                {
                    Dictionary<string, OleDbPropertyInfo> dynamic = GetProviderInfo(Provider);
                    OleDbPropertyInfo info = dynamic[keyword];
                    value = info._defaultValue!;
                }
                return value;
            }
            set
            {
                if (null != value)
                {
                    ADP.CheckArgumentNull(keyword, "keyword");
                    Keywords index;
                    if (s_keywords.TryGetValue(keyword, out index))
                    {
                        switch (index)
                        {
                            case Keywords.DataSource:
                                DataSource = ConvertToString(value);
                                break;
                            case Keywords.FileName:
                                FileName = ConvertToString(value);
                                break;
                            case Keywords.Provider:
                                Provider = ConvertToString(value);
                                break;

                            case Keywords.OleDbServices:
                                OleDbServices = ConvertToInt32(value);
                                break;

                            case Keywords.PersistSecurityInfo:
                                PersistSecurityInfo = ConvertToBoolean(value);
                                break;
                            default:
                                Debug.Assert(false, "unexpected keyword");
                                throw ADP.KeywordNotSupported(keyword);
                        }
                    }
                    else
                    {
                        base[keyword] = value;
                        ClearPropertyDescriptors();
                    }
                }
                else
                {
                    Remove(keyword);
                }
            }
        }

        [DisplayName(DbConnectionStringKeywords.DataSource)]
        [RefreshProperties(RefreshProperties.All)]
        // TODO: hand off to editor VS, if SQL - do database names, if Jet do file picker
        public string DataSource
        {
            get { return _dataSource; }
            set
            {
                SetValue(DbConnectionStringKeywords.DataSource, value);
                _dataSource = value;
            }
        }

        [DisplayName(DbConnectionStringKeywords.FileName)]
        [RefreshProperties(RefreshProperties.All)]
        // TODO: hand off to VS, they derive from FileNameEditor and set the OpenDialogFilter to *.UDL
        public string FileName
        {
            get { return _fileName; }
            set
            {
                SetValue(DbConnectionStringKeywords.FileName, value);
                _fileName = value;
            }
        }

        [DisplayName(DbConnectionStringKeywords.OleDbServices)]
        [RefreshProperties(RefreshProperties.All)]
        [TypeConverter(typeof(OleDbConnectionStringBuilder.OleDbServicesConverter))]
        public int OleDbServices
        {
            get { return _oleDbServices; }
            set
            {
                SetValue(DbConnectionStringKeywords.OleDbServices, value);
                _oleDbServices = value;
            }
        }

        [DisplayName(DbConnectionStringKeywords.PersistSecurityInfo)]
        [RefreshProperties(RefreshProperties.All)]
        public bool PersistSecurityInfo
        {
            get { return _persistSecurityInfo; }
            set
            {
                SetValue(DbConnectionStringKeywords.PersistSecurityInfo, value);
                _persistSecurityInfo = value;
            }
        }

        [DisplayName(DbConnectionStringKeywords.Provider)]
        [RefreshProperties(RefreshProperties.All)]
        [TypeConverter(typeof(OleDbConnectionStringBuilder.OleDbProviderConverter))]
        public string Provider
        {
            get { return _provider; }
            set
            {
                SetValue(DbConnectionStringKeywords.Provider, value);
                _provider = value;
                RestartProvider();
            }
        }

        public override ICollection Keys
        {
            get
            {
                string[]? knownKeywords = _knownKeywords;
                if (null == knownKeywords)
                {
                    Dictionary<string, OleDbPropertyInfo> dynamic = GetProviderInfo(Provider);
                    if (0 < dynamic.Count)
                    {
                        knownKeywords = new string[s_validKeywords.Length + dynamic.Count];
                        s_validKeywords.CopyTo(knownKeywords, 0);
                        dynamic.Keys.CopyTo(knownKeywords, s_validKeywords.Length);
                    }
                    else
                    {
                        knownKeywords = s_validKeywords;
                    }

                    int count = 0;
                    foreach (string keyword in base.Keys)
                    {
                        bool flag = true;
                        foreach (string s in knownKeywords)
                        {
                            if (StringComparer.OrdinalIgnoreCase.Equals(s, keyword))
                            {
                                flag = false;
                                break;
                            }
                        }
                        if (flag)
                        {
                            count++;
                        }
                    }
                    if (0 < count)
                    {
                        string[] tmp = new string[knownKeywords.Length + count];
                        knownKeywords.CopyTo(tmp, 0);

                        int index = knownKeywords.Length;
                        foreach (string keyword in base.Keys)
                        {
                            bool flag = true;
                            foreach (string s in knownKeywords)
                            {
                                if (StringComparer.OrdinalIgnoreCase.Equals(s, keyword))
                                {
                                    flag = false;
                                    break;
                                }
                            }
                            if (flag)
                            {
                                tmp[index++] = keyword;
                            }
                        }
                        knownKeywords = tmp;
                    }
                    _knownKeywords = knownKeywords;
                }
                return new System.Data.Common.ReadOnlyCollection<string>(knownKeywords);
            }
        }

        public override bool ContainsKey(string keyword)
        {
            ADP.CheckArgumentNull(keyword, "keyword");
            return s_keywords.ContainsKey(keyword) || base.ContainsKey(keyword);
        }

        private static bool ConvertToBoolean(object value)
        {
            return DbConnectionStringBuilderUtil.ConvertToBoolean(value);
        }
        private static int ConvertToInt32(object value)
        {
            return DbConnectionStringBuilderUtil.ConvertToInt32(value);
        }
        private static string ConvertToString(object value)
        {
            return DbConnectionStringBuilderUtil.ConvertToString(value);
        }

        public override void Clear()
        {
            base.Clear();
            for (int i = 0; i < s_validKeywords.Length; ++i)
            {
                Reset((Keywords)i);
            }
            base.ClearPropertyDescriptors();
            _knownKeywords = s_validKeywords;
        }

        private object GetAt(Keywords index)
        {
            switch (index)
            {
                case Keywords.DataSource:
                    return DataSource;
                case Keywords.FileName:
                    return FileName;
                case Keywords.OleDbServices:
                    return OleDbServices;
                case Keywords.PersistSecurityInfo:
                    return PersistSecurityInfo;
                case Keywords.Provider:
                    return Provider;
                default:
                    Debug.Assert(false, "unexpected keyword");
                    throw ADP.KeywordNotSupported(s_validKeywords[(int)index]);
            }
        }

        public override bool Remove(string keyword)
        {
            ADP.CheckArgumentNull(keyword, "keyword");
            bool value = base.Remove(keyword);

            Keywords index;
            if (s_keywords.TryGetValue(keyword, out index))
            {
                Reset(index);
            }
            else if (value)
            {
                ClearPropertyDescriptors();
            }
            return value;
        }

        private void Reset(Keywords index)
        {
            switch (index)
            {
                case Keywords.DataSource:
                    _dataSource = DbConnectionStringDefaults.DataSource;
                    break;
                case Keywords.FileName:
                    _fileName = DbConnectionStringDefaults.FileName;
                    RestartProvider();
                    break;
                case Keywords.OleDbServices:
                    _oleDbServices = DbConnectionStringDefaults.OleDbServices;
                    break;
                case Keywords.PersistSecurityInfo:
                    _persistSecurityInfo = DbConnectionStringDefaults.PersistSecurityInfo;
                    break;
                case Keywords.Provider:
                    _provider = DbConnectionStringDefaults.Provider;
                    RestartProvider();
                    break;
                default:
                    Debug.Assert(false, "unexpected keyword");
                    throw ADP.KeywordNotSupported(s_validKeywords[(int)index]);
            }
        }

        private new void ClearPropertyDescriptors()
        {
            base.ClearPropertyDescriptors();
            _knownKeywords = null;
        }

        private void RestartProvider()
        {
            ClearPropertyDescriptors();
            _propertyInfo = null;
        }

        private void SetValue(string keyword, bool value)
        {
            base[keyword] = value.ToString(null);
        }
        private void SetValue(string keyword, int value)
        {
            base[keyword] = value.ToString((System.IFormatProvider?)null);
        }
        private void SetValue(string keyword, string value)
        {
            ADP.CheckArgumentNull(value, keyword);
            base[keyword] = value;
        }

        public override bool TryGetValue(string keyword, [NotNullWhen(true)] out object? value)
        {
            ADP.CheckArgumentNull(keyword, "keyword");
            Keywords index;
            if (s_keywords.TryGetValue(keyword, out index))
            {
                value = GetAt(index);
                return true;
            }
            else if (!base.TryGetValue(keyword, out value))
            {
                Dictionary<string, OleDbPropertyInfo> dynamic = GetProviderInfo(Provider);
                OleDbPropertyInfo? info;
                if (dynamic.TryGetValue(keyword, out info))
                {
                    value = info._defaultValue!;
                    return true;
                }
                return false;
            }
            return true;
        }

        private Dictionary<string, OleDbPropertyInfo> GetProviderInfo(string provider)
        {
            Dictionary<string, OleDbPropertyInfo>? providerInfo = _propertyInfo;
            if (null == providerInfo)
            {
                providerInfo = new Dictionary<string, OleDbPropertyInfo>(StringComparer.OrdinalIgnoreCase);
                if (!ADP.IsEmpty(provider))
                {
                    Dictionary<string, OleDbPropertyInfo>? hash = null;
                    try
                    {
                        StringBuilder builder = new StringBuilder();
                        AppendKeyValuePair(builder, DbConnectionStringKeywords.Provider, provider);
                        OleDbConnectionString constr = new OleDbConnectionString(builder.ToString(), true);

                        // load provider without calling Initialize or CreateDataSource
                        using (OleDbConnectionInternal connection = new OleDbConnectionInternal(constr, null))
                        {
                            // get all the init property information for the provider
                            hash = connection.GetPropertyInfo(new Guid[] { OleDbPropertySetGuid.DBInitAll })!;
                            foreach (KeyValuePair<string, OleDbPropertyInfo> entry in hash)
                            {
                                Keywords index;
                                OleDbPropertyInfo info = entry.Value;
                                if (!s_keywords.TryGetValue(info._description!, out index))
                                {
                                    if ((OleDbPropertySetGuid.DBInit == info._propertySet) &&
                                            ((ODB.DBPROP_INIT_ASYNCH == info._propertyID) ||
                                             (ODB.DBPROP_INIT_HWND == info._propertyID) ||
                                             (ODB.DBPROP_INIT_PROMPT == info._propertyID)))
                                    {
                                        continue; // skip this keyword
                                    }
                                    providerInfo[info._description!] = info;
                                }
                            }

                            // what are the unique propertysets?
                            List<Guid> listPropertySets = new List<Guid>();
                            foreach (KeyValuePair<string, OleDbPropertyInfo> entry in hash)
                            {
                                OleDbPropertyInfo info = entry.Value;
                                if (!listPropertySets.Contains(info._propertySet))
                                {
                                    listPropertySets.Add(info._propertySet);
                                }
                            }
                            Guid[] arrayPropertySets = new Guid[listPropertySets.Count];
                            listPropertySets.CopyTo(arrayPropertySets, 0);

                            // get all the init property values for the provider
                            using (PropertyIDSet propidset = new PropertyIDSet(arrayPropertySets))
                            {
                                using (IDBPropertiesWrapper idbProperties = connection.IDBProperties())
                                {
                                    OleDbHResult hr;
                                    using (DBPropSet propset = new DBPropSet(idbProperties.Value, propidset, out hr))
                                    {
                                        // OleDbConnectionStringBuilder is ignoring/hiding potential errors of OLEDB provider when reading its properties information
                                        if (0 <= (int)hr)
                                        {
                                            int count = propset.PropertySetCount;
                                            for (int i = 0; i < count; ++i)
                                            {
                                                Guid propertyset;
                                                ItagDBPROP[] props = propset.GetPropertySet(i, out propertyset);

                                                // attach the default property value to the property info
                                                foreach (ItagDBPROP prop in props)
                                                {
                                                    foreach (KeyValuePair<string, OleDbPropertyInfo> entry in hash)
                                                    {
                                                        OleDbPropertyInfo info = entry.Value;
                                                        if ((info._propertyID == prop.dwPropertyID) && (info._propertySet == propertyset))
                                                        {
                                                            info._defaultValue = prop.vValue;

                                                            if (null == info._defaultValue)
                                                            {
                                                                if (typeof(string) == info._type)
                                                                {
                                                                    info._defaultValue = "";
                                                                }
                                                                else if (typeof(int) == info._type)
                                                                {
                                                                    info._defaultValue = 0;
                                                                }
                                                                else if (typeof(bool) == info._type)
                                                                {
                                                                    info._defaultValue = false;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (System.InvalidOperationException e)
                    {
                        ADP.TraceExceptionWithoutRethrow(e);
                    }
                    catch (System.Data.OleDb.OleDbException e)
                    {
                        ADP.TraceExceptionWithoutRethrow(e);
                    }
                    catch (System.Security.SecurityException e)
                    {
                        ADP.TraceExceptionWithoutRethrow(e);
                    }
                }
                _propertyInfo = providerInfo;
            }
            return providerInfo;
        }

        private sealed class OleDbProviderConverter : StringConverter
        {
            private const int DBSOURCETYPE_DATASOURCE_TDP = 1;
            private const int DBSOURCETYPE_DATASOURCE_MDP = 3;

            private StandardValuesCollection? _standardValues;

            // converter classes should have public ctor
            public OleDbProviderConverter()
            {
            }

            public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            {
                return true;
            }

            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
            {
                return false;
            }

            public override StandardValuesCollection? GetStandardValues(ITypeDescriptorContext context)
            {
                StandardValuesCollection? dataSourceNames = _standardValues;
                if (null == _standardValues)
                {
                    // Get the sources rowset for the SQLOLEDB enumerator
                    DataTable table = (new OleDbEnumerator()).GetElements();

                    DataColumn column2 = table.Columns["SOURCES_NAME"]!;
                    DataColumn column5 = table.Columns["SOURCES_TYPE"]!;
                    //DataColumn column4 = table.Columns["SOURCES_DESCRIPTION"];

                    System.Collections.Generic.List<string> providerNames = new System.Collections.Generic.List<string>(table.Rows.Count);
                    foreach (DataRow row in table.Rows)
                    {
                        int sourceType = (int)row[column5];
                        if (DBSOURCETYPE_DATASOURCE_TDP == sourceType || DBSOURCETYPE_DATASOURCE_MDP == sourceType)
                        {
                            string progid = (string)row[column2];
                            if (!OleDbConnectionString.IsMSDASQL(progid.ToLowerInvariant()))
                            {
                                if (0 > providerNames.IndexOf(progid))
                                {
                                    providerNames.Add(progid);
                                }
                            }
                        }
                    }

                    // Create the standard values collection that contains the sources
                    dataSourceNames = new StandardValuesCollection(providerNames);
                    _standardValues = dataSourceNames;
                }
                return dataSourceNames;
            }
        }

        [Flags]
        internal enum OleDbServiceValues : int
        {
            DisableAll = unchecked((int)0x00000000),
            ResourcePooling = unchecked((int)0x00000001),
            TransactionEnlistment = unchecked((int)0x00000002),
            ClientCursor = unchecked((int)0x00000004),
            AggregationAfterSession = unchecked((int)0x00000008),
            EnableAll = unchecked((int)0xffffffff),
            Default = ~(ClientCursor | AggregationAfterSession),
        };

        internal sealed class OleDbServicesConverter : TypeConverter
        {
            private StandardValuesCollection? _standardValues;

            // converter classes should have public ctor
            public OleDbServicesConverter() : base()
            {
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                // Only know how to convert from a string
                return ((typeof(string) == sourceType) || base.CanConvertFrom(context, sourceType));
            }

            public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            {
                string? svalue = (value as string);
                if (null != svalue)
                {
                    int services;
                    if (int.TryParse(svalue, out services))
                    {
                        return services;
                    }
                    else
                    {
                        if (svalue.IndexOf(',') != -1)
                        {
                            int convertedValue = 0;
                            string[] values = svalue.Split(new char[] { ',' });
                            foreach (string v in values)
                            {
                                convertedValue |= (int)(OleDbServiceValues)Enum.Parse(typeof(OleDbServiceValues), v, true);
                            }
                            return (int)convertedValue;
                        }
                        else
                        {
                            return (int)(OleDbServiceValues)Enum.Parse(typeof(OleDbServiceValues), svalue, true);
                        }
                    }
                }
                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                // Only know how to convert to the NetworkLibrary enumeration
                return ((typeof(string) == destinationType) || base.CanConvertTo(context, destinationType));
            }

            public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
            {
                if ((typeof(string) == destinationType) && (null != value) && (typeof(int) == value.GetType()))
                {
                    return Enum.Format(typeof(OleDbServiceValues), ((OleDbServiceValues)(int)value), "G");
                }
                return base.ConvertTo(context, culture, value, destinationType);
            }

            public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            {
                return true;
            }

            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
            {
                return false;
            }

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                StandardValuesCollection? standardValues = _standardValues;
                if (null == standardValues)
                {
                    Array objValues = Enum.GetValues(typeof(OleDbServiceValues));
                    Array.Sort(objValues, 0, objValues.Length);
                    standardValues = new StandardValuesCollection(objValues);
                    _standardValues = standardValues;
                }
                return standardValues;
            }

            public override bool IsValid(ITypeDescriptorContext context, object value)
            {
                return true;
                //return Enum.IsDefined(type, value);
            }
        }

        internal sealed class OleDbConnectionStringBuilderConverter : ExpandableObjectConverter
        {
            // converter classes should have public ctor
            public OleDbConnectionStringBuilderConverter()
            {
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                if (typeof(System.ComponentModel.Design.Serialization.InstanceDescriptor) == destinationType)
                {
                    return true;
                }
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (destinationType == null)
                {
                    throw ADP.ArgumentNull("destinationType");
                }
                if (typeof(System.ComponentModel.Design.Serialization.InstanceDescriptor) == destinationType)
                {
                    OleDbConnectionStringBuilder? obj = (value as OleDbConnectionStringBuilder);
                    if (null != obj)
                    {
                        return ConvertToInstanceDescriptor(obj);
                    }
                }
                return base.ConvertTo(context, culture, value, destinationType);
            }

            private System.ComponentModel.Design.Serialization.InstanceDescriptor ConvertToInstanceDescriptor(OleDbConnectionStringBuilder options)
            {
                Type[] ctorParams = new Type[] { typeof(string) };
                object[] ctorValues = new object[] { options.ConnectionString };
                System.Reflection.ConstructorInfo ctor = typeof(OleDbConnectionStringBuilder).GetConstructor(ctorParams)!;
                return new System.ComponentModel.Design.Serialization.InstanceDescriptor(ctor, ctorValues);
            }
        }
    }
}
