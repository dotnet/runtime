// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class SteAttributeKey
    {
        public static readonly SteAttributeKey SqlDbType = new SteAttributeKey();
        public static readonly SteAttributeKey MultiValued = new SteAttributeKey();
        public static readonly SteAttributeKey Value = new SteAttributeKey();
        public static readonly SteAttributeKey MaxLength = new SteAttributeKey();
        public static readonly SteAttributeKey Precision = new SteAttributeKey();
        public static readonly SteAttributeKey Scale = new SteAttributeKey();
        public static readonly SteAttributeKey LocaleId = new SteAttributeKey();
        public static readonly SteAttributeKey CompareOptions = new SteAttributeKey();
        public static readonly SteAttributeKey TypeName = new SteAttributeKey();
        public static readonly SteAttributeKey Type = new SteAttributeKey();
        public static readonly SteAttributeKey Fields = new SteAttributeKey();
        public static readonly SteAttributeKey Offset = new SteAttributeKey();
        public static readonly SteAttributeKey Length = new SteAttributeKey();

        public static readonly IList<SteAttributeKey> MetaDataKeys = new List<SteAttributeKey>(
                new SteAttributeKey[] {
                    SteAttributeKey.SqlDbType,
                    SteAttributeKey.MultiValued,
                    SteAttributeKey.MaxLength,
                    SteAttributeKey.Precision,
                    SteAttributeKey.Scale,
                    SteAttributeKey.LocaleId,
                    SteAttributeKey.CompareOptions,
                    SteAttributeKey.TypeName,
                    SteAttributeKey.Type,
                    SteAttributeKey.Fields,
                }
            ).AsReadOnly();

        public static IList<SteAttributeKey> ValueKeys = new List<SteAttributeKey>(
                new SteAttributeKey[] { SteAttributeKey.Value }
            ).AsReadOnly();
    }

    public class SteAttribute
    {
        private SteAttributeKey _key;
        private object _value;

        public SteAttribute(SteAttributeKey key, object value)
        {
            _key = key;
            _value = value;
        }

        public SteAttributeKey Key
        {
            get
            {
                return _key;
            }
        }

        public object Value
        {
            get
            {
                return _value;
            }
        }
    }

}
