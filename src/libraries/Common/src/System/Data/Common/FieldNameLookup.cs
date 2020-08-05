// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;

namespace System.Data.ProviderBase
{
    internal sealed class FieldNameLookup : BasicFieldNameLookup
    {
        private readonly int _defaultLocaleID;

        public FieldNameLookup(string[] fieldNames, int defaultLocaleID) : base(fieldNames)
        {
            _defaultLocaleID = defaultLocaleID;
        }

        public FieldNameLookup(System.Collections.ObjectModel.ReadOnlyCollection<string> columnNames, int defaultLocaleID) : base(columnNames)
        {
            _defaultLocaleID = defaultLocaleID;
        }

        public FieldNameLookup(IDataReader reader, int defaultLocaleID) : base(reader)
        {
            _defaultLocaleID = defaultLocaleID;
        }

        //The compare info is specified by the server by specifying the default LocaleId.
        protected override CompareInfo GetCompareInfo()
        {
            CompareInfo? compareInfo = null;
            if (-1 != _defaultLocaleID)
            {
                compareInfo = CompareInfo.GetCompareInfo(_defaultLocaleID);
            }
            if (null == compareInfo)
            {
                compareInfo = base.GetCompareInfo();
            }
            return compareInfo;
        }
    }
}
