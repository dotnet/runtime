// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;

namespace System.Data.Odbc
{
    public sealed class OdbcConnectionStringBuilder : DbConnectionStringBuilder
    {
        private enum Keywords
        { // must maintain same ordering as _validKeywords array
          //            NamedConnection,
            Dsn,

            Driver,
        }

        private static readonly string[] s_validKeywords;
        private static readonly Dictionary<string, Keywords> s_keywords;

        private string[] _knownKeywords;

        private string _dsn = DbConnectionStringDefaults.Dsn;
        //        private string _namedConnection  = DbConnectionStringDefaults.NamedConnection;

        private string _driver = DbConnectionStringDefaults.Driver;

        static OdbcConnectionStringBuilder()
        {
            string[] validKeywords = new string[2];
            validKeywords[(int)Keywords.Driver] = DbConnectionStringKeywords.Driver;
            validKeywords[(int)Keywords.Dsn] = DbConnectionStringKeywords.Dsn;
            //            validKeywords[(int)Keywords.NamedConnection] = DbConnectionStringKeywords.NamedConnection;
            s_validKeywords = validKeywords;

            Dictionary<string, Keywords> hash = new Dictionary<string, Keywords>(2, StringComparer.OrdinalIgnoreCase);
            hash.Add(DbConnectionStringKeywords.Driver, Keywords.Driver);
            hash.Add(DbConnectionStringKeywords.Dsn, Keywords.Dsn);
            //            hash.Add(DbConnectionStringKeywords.NamedConnection, Keywords.NamedConnection);
            Debug.Assert(2 == hash.Count, "initial expected size is incorrect");
            s_keywords = hash;
        }

        public OdbcConnectionStringBuilder() : this((string)null)
        {
        }

        public OdbcConnectionStringBuilder(string connectionString) : base(true)
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                ConnectionString = connectionString;
            }
        }

        public override object this[string keyword]
        {
            get
            {
                ADP.CheckArgumentNull(keyword, "keyword");
                Keywords index;
                if (s_keywords.TryGetValue(keyword, out index))
                {
                    return GetAt(index);
                }
                else
                {
                    return base[keyword];
                }
            }
            set
            {
                ADP.CheckArgumentNull(keyword, "keyword");
                if (null != value)
                {
                    Keywords index;
                    if (s_keywords.TryGetValue(keyword, out index))
                    {
                        switch (index)
                        {
                            case Keywords.Driver: Driver = ConvertToString(value); break;
                            case Keywords.Dsn: Dsn = ConvertToString(value); break;
                            //                      case Keywords.NamedConnection: NamedConnection = ConvertToString(value); break;
                            default:
                                Debug.Assert(false, "unexpected keyword");
                                throw ADP.KeywordNotSupported(keyword);
                        }
                    }
                    else
                    {
                        base[keyword] = value;
                        ClearPropertyDescriptors();
                        _knownKeywords = null;
                    }
                }
                else
                {
                    Remove(keyword);
                }
            }
        }

        [DisplayName(DbConnectionStringKeywords.Driver)]
        public string Driver
        {
            get { return _driver; }
            set
            {
                SetValue(DbConnectionStringKeywords.Driver, value);
                _driver = value;
            }
        }

        [DisplayName(DbConnectionStringKeywords.Dsn)]
        public string Dsn
        {
            get { return _dsn; }
            set
            {
                SetValue(DbConnectionStringKeywords.Dsn, value);
                _dsn = value;
            }
        }
        /*
                [DisplayName(DbConnectionStringKeywords.NamedConnection)]
                [ResCategoryAttribute(Res.DataCategory_NamedConnectionString)]
                [ResDescriptionAttribute(Res.DbConnectionString_NamedConnection)]
                [RefreshPropertiesAttribute(RefreshProperties.All)]
                [TypeConverter(typeof(NamedConnectionStringConverter))]
                public string NamedConnection {
                    get { return _namedConnection; }
                    set {
                        SetValue(DbConnectionStringKeywords.NamedConnection, value);
                        _namedConnection = value;
                    }
                }
        */
        public override ICollection Keys
        {
            get
            {
                string[] knownKeywords = _knownKeywords;
                if (null == knownKeywords)
                {
                    knownKeywords = s_validKeywords;

                    int count = 0;
                    foreach (string keyword in base.Keys)
                    {
                        bool flag = true;
                        foreach (string s in knownKeywords)
                        {
                            if (s == keyword)
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
                                if (s == keyword)
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
                return new ReadOnlyCollection<string>(knownKeywords);
            }
        }

        public override void Clear()
        {
            base.Clear();
            for (int i = 0; i < s_validKeywords.Length; ++i)
            {
                Reset((Keywords)i);
            }
            _knownKeywords = s_validKeywords;
        }

        public override bool ContainsKey(string keyword)
        {
            ADP.CheckArgumentNull(keyword, "keyword");
            return s_keywords.ContainsKey(keyword) || base.ContainsKey(keyword);
        }

        private static string ConvertToString(object value)
        {
            return DbConnectionStringBuilderUtil.ConvertToString(value);
        }

        private object GetAt(Keywords index)
        {
            switch (index)
            {
                case Keywords.Driver: return Driver;
                case Keywords.Dsn: return Dsn;
                //          case Keywords.NamedConnection: return NamedConnection;
                default:
                    Debug.Assert(false, "unexpected keyword");
                    throw ADP.KeywordNotSupported(s_validKeywords[(int)index]);
            }
        }

        /*
        protected override void GetProperties(Hashtable propertyDescriptors) {
            object value;
            if (TryGetValue(DbConnectionStringSynonyms.TRUSTEDCONNECTION, out value)) {
                bool trusted = false;
                if (value is bool) {
                    trusted = (bool)value;
                }
                else if ((value is string) && !Boolean.TryParse((string)value, out trusted)) {
                    trusted = false;
                }

                if (trusted) {
                   Attribute[] attributes = new Attribute[] {
                        BrowsableAttribute.Yes,
                        RefreshPropertiesAttribute.All,
                    };
                    DbConnectionStringBuilderDescriptor descriptor;
                    descriptor = new DbConnectionStringBuilderDescriptor(DbConnectionStringSynonyms.TRUSTEDCONNECTION,
                                        this.GetType(), typeof(bool), false, attributes);
                    descriptor.RefreshOnChange = true;
                    propertyDescriptors[DbConnectionStringSynonyms.TRUSTEDCONNECTION] = descriptor;

                    if (ContainsKey(DbConnectionStringSynonyms.Pwd)) {
                        descriptor = new DbConnectionStringBuilderDescriptor(DbConnectionStringSynonyms.Pwd,
                                            this.GetType(), typeof(string), true, attributes);
                        propertyDescriptors[DbConnectionStringSynonyms.Pwd] = descriptor;
                    }
                    if (ContainsKey(DbConnectionStringSynonyms.UID)) {
                        descriptor = new DbConnectionStringBuilderDescriptor(DbConnectionStringSynonyms.UID,
                                            this.GetType(), typeof(string), true, attributes);
                        propertyDescriptors[DbConnectionStringSynonyms.UID] = descriptor;
                    }
                }
            }
            base.GetProperties(propertyDescriptors);
        }
        */

        public override bool Remove(string keyword)
        {
            ADP.CheckArgumentNull(keyword, "keyword");
            if (base.Remove(keyword))
            {
                Keywords index;
                if (s_keywords.TryGetValue(keyword, out index))
                {
                    Reset(index);
                }
                else
                {
                    ClearPropertyDescriptors();
                    _knownKeywords = null;
                }
                return true;
            }
            return false;
        }
        private void Reset(Keywords index)
        {
            switch (index)
            {
                case Keywords.Driver:
                    _driver = DbConnectionStringDefaults.Driver;
                    break;
                case Keywords.Dsn:
                    _dsn = DbConnectionStringDefaults.Dsn;
                    break;
                //            case Keywords.NamedConnection:
                //               _namedConnection = DbConnectionStringDefaults.NamedConnection;
                //                break;
                default:
                    Debug.Assert(false, "unexpected keyword");
                    throw ADP.KeywordNotSupported(s_validKeywords[(int)index]);
            }
        }

        private void SetValue(string keyword, string value)
        {
            ADP.CheckArgumentNull(value, keyword);
            base[keyword] = value;
        }

        public override bool TryGetValue(string keyword, out object value)
        {
            ADP.CheckArgumentNull(keyword, "keyword");
            Keywords index;
            if (s_keywords.TryGetValue(keyword, out index))
            {
                value = GetAt(index);
                return true;
            }
            return base.TryGetValue(keyword, out value);
        }
    }
}
