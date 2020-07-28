// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.Odbc
{
    public sealed class OdbcConnectionStringBuilder : DbConnectionStringBuilder
    {
        private enum Keywords
        {
            // must maintain same ordering as s_validKeywords array
            Dsn,
            Driver,
        }

        private static readonly string[] s_validKeywords = new string[2]
        {
            DbConnectionStringKeywords.Dsn, // (int)Keywords.Dsn
            DbConnectionStringKeywords.Driver // (int)Keywords.Driver
        };
        private static readonly Dictionary<string, Keywords> s_keywords = new Dictionary<string, Keywords>(2, StringComparer.OrdinalIgnoreCase)
        {
            { DbConnectionStringKeywords.Driver, Keywords.Driver },
            { DbConnectionStringKeywords.Dsn, Keywords.Dsn }
        };

        private string[]? _knownKeywords;

        private string _dsn = DbConnectionStringDefaults.Dsn;
        private string _driver = DbConnectionStringDefaults.Driver;

        public OdbcConnectionStringBuilder() : this(null)
        {
        }

        public OdbcConnectionStringBuilder(string? connectionString) : base(true)
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                ConnectionString = connectionString;
            }
        }

        [AllowNull]
        public override object this[string keyword]
        {
            get
            {
                ADP.CheckArgumentNull(keyword, nameof(keyword));
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
                ADP.CheckArgumentNull(keyword, nameof(keyword));
                if (null != value)
                {
                    Keywords index;
                    if (s_keywords.TryGetValue(keyword, out index))
                    {
                        switch (index)
                        {
                            case Keywords.Driver: Driver = ConvertToString(value); break;
                            case Keywords.Dsn: Dsn = ConvertToString(value); break;
                            default:
                                Debug.Fail("unexpected keyword");
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

        public override ICollection Keys
        {
            get
            {
                string[]? knownKeywords = _knownKeywords;
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
            ADP.CheckArgumentNull(keyword, nameof(keyword));
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
                default:
                    Debug.Fail("unexpected keyword");
                    throw ADP.KeywordNotSupported(s_validKeywords[(int)index]);
            }
        }

        public override bool Remove(string keyword)
        {
            ADP.CheckArgumentNull(keyword, nameof(keyword));
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
                default:
                    Debug.Fail("unexpected keyword");
                    throw ADP.KeywordNotSupported(s_validKeywords[(int)index]);
            }
        }

        private void SetValue(string keyword, string value)
        {
            ADP.CheckArgumentNull(value, keyword);
            base[keyword] = value;
        }

        public override bool TryGetValue(string keyword, [NotNullWhen(true)] out object? value)
        {
            ADP.CheckArgumentNull(keyword, nameof(keyword));
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
