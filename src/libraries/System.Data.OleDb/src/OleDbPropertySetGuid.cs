// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.OleDb
{
    sealed internal class OleDbPropertySetGuid
    {
        private OleDbPropertySetGuid()
        {
        }

        static internal readonly Guid Column = new Guid(0xc8b522b9, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid DataSource = new Guid(0xc8b522ba, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid DataSourceInfo = new Guid(0xc8b522bb, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid DBInit = new Guid(0xc8b522bc, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid Index = new Guid(0xc8b522bd, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid PropertiesInError = new Guid(0xc8b522d4, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid Rowset = new Guid(0xc8b522be, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid Session = new Guid(0xc8b522c6, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid Stream = new Guid(0xc8b522fd, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid Table = new Guid(0xc8b522bf, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid Trustee = new Guid(0xc8b522e1, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid View = new Guid(0xc8b522df, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static internal readonly Guid DataSourceAll = new Guid(0xc8b522c0, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid DataSourceInfoAll = new Guid(0xc8b522c1, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid DBInitAll = new Guid(0xc8b522ca, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid ColumnAll = new Guid(0xc8b522f0, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid ConstraintAll = new Guid(0xc8b522fa, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid IndexAll = new Guid(0xc8b522f1, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid RowsetAll = new Guid(0xc8b522c2, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid SessionAll = new Guid(0xc8b522c7, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid StreamAll = new Guid(0xc8b522fe, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid TableAll = new Guid(0xc8b522f2, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid TrusteeAll = new Guid(0xc8b522f3, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);
        static internal readonly Guid ViewAll = new Guid(0xc8b522fc, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        /*
        static internal string GetTextFromValue(Guid guid) {
            string value = ConvertToString(guid);
            if (null == value) {
                value = "{" + guid.ToString("D", CultureInfo.InvariantCulture) + "}";
            }
            return value;
        }

        static internal string ConvertToString(Guid guid) {
            if (guid == OleDbPropertySetGuid.DBInit) { return "DBInit"; }
            if (guid == OleDbPropertySetGuid.Rowset) { return "Rowset"; }
            if (guid == OleDbPropertySetGuid.DataSource) { return "DataSource"; }
            if (guid == OleDbPropertySetGuid.DataSourceInfo) { return "DataSourceInfo"; }
            if (guid == OleDbPropertySetGuid.PropertiesInError) { return "PropertiesInError"; }

            if (guid == OleDbPropertySetGuid.View) { return "View"; }
            if (guid == OleDbPropertySetGuid.Trustee) { return "Trustee"; }
            if (guid == OleDbPropertySetGuid.Column) { return "Column"; }
            if (guid == OleDbPropertySetGuid.Index) { return "Index"; }
            if (guid == OleDbPropertySetGuid.Table) { return "Table"; }
            if (guid == OleDbPropertySetGuid.Session) { return "Session"; }
            if (guid == OleDbPropertySetGuid.Stream) { return "Stream"; }

            if (guid == OleDbPropertySetGuid.ColumnAll) { return "ColumnAll"; }
            if (guid == OleDbPropertySetGuid.IndexAll) { return "IndexAll"; }
            if (guid == OleDbPropertySetGuid.TableAll) { return "TableAll"; }
            if (guid == OleDbPropertySetGuid.TrusteeAll) { return "TrusteeAll"; }
            if (guid == OleDbPropertySetGuid.ConstraintAll) { return "ConstraintAll"; }
            if (guid == OleDbPropertySetGuid.DataSourceAll) { return "DataSourceAll"; }
            if (guid == OleDbPropertySetGuid.DataSourceInfoAll) { return "DataSourceInfoAll"; }
            if (guid == OleDbPropertySetGuid.RowsetAll) { return "RowsetAll"; }
            if (guid == OleDbPropertySetGuid.SessionAll) { return "SessionAll"; }
            if (guid == OleDbPropertySetGuid.DBInitAll) { return "DBInitAll"; }
            if (guid == OleDbPropertySetGuid.ViewAll) { return "ViewAll"; }
            if (guid == OleDbPropertySetGuid.StreamAll) { return "StreamAll"; }

            return null;
        }
        */
    }
}

