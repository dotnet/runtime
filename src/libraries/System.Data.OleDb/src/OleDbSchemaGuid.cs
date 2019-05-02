// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.Data.OleDb
{
    public sealed class OleDbSchemaGuid
    {
        // V1 shipped with default ctor which is public
        // so we can NOT remove the now explict public ctor
        public OleDbSchemaGuid() { }

        // MDAC 2.0

        static public readonly Guid Tables_Info = new Guid(0xc8b522e0, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        // MDAC 2.1

        static public readonly Guid Trustee = new Guid(0xc8b522ef, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Assertions = new Guid(0xc8b52210, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Catalogs = new Guid(0xc8b52211, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Character_Sets = new Guid(0xc8b52212, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Collations = new Guid(0xc8b52213, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Columns = new Guid(0xc8b52214, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Check_Constraints = new Guid(0xc8b52215, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Constraint_Column_Usage = new Guid(0xc8b52216, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Constraint_Table_Usage = new Guid(0xc8b52217, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Key_Column_Usage = new Guid(0xc8b52218, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Referential_Constraints = new Guid(0xc8b52219, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Table_Constraints = new Guid(0xc8b5221a, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Column_Domain_Usage = new Guid(0xc8b5221b, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Indexes = new Guid(0xc8b5221e, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Column_Privileges = new Guid(0xc8b52221, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Table_Privileges = new Guid(0xc8b52222, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Usage_Privileges = new Guid(0xc8b52223, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Procedures = new Guid(0xc8b52224, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Schemata = new Guid(0xc8b52225, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Sql_Languages = new Guid(0xc8b52226, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Statistics = new Guid(0xc8b52227, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Tables = new Guid(0xc8b52229, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Translations = new Guid(0xc8b5222a, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Provider_Types = new Guid(0xc8b5222c, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Views = new Guid(0xc8b5222d, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid View_Column_Usage = new Guid(0xc8b5222e, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid View_Table_Usage = new Guid(0xc8b5222f, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Procedure_Parameters = new Guid(0xc8b522b8, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Foreign_Keys = new Guid(0xc8b522c4, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Primary_Keys = new Guid(0xc8b522c5, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Procedure_Columns = new Guid(0xc8b522c9, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        // MDAC 2.6

        static public readonly Guid Table_Statistics = new Guid(0xc8b522ff, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid Check_Constraints_By_Table = new Guid(0xc8b52301, 0x5cf3, 0x11ce, 0xad, 0xe5, 0x00, 0xaa, 0x00, 0x44, 0x77, 0x3d);

        static public readonly Guid SchemaGuids = new Guid(0xf3264c9b, 0x1860, 0x4dfe, 0xb7, 0x1b, 0x29, 0x61, 0xb2, 0xea, 0x91, 0xbd);

        static public readonly Guid DbInfoKeywords = new Guid(0xf3264c9c, 0x1860, 0x4dfe, 0xb7, 0x1b, 0x29, 0x61, 0xb2, 0xea, 0x91, 0xbd);

        static public readonly Guid DbInfoLiterals = new Guid(0xf3264c9d, 0x1860, 0x4dfe, 0xb7, 0x1b, 0x29, 0x61, 0xb2, 0xea, 0x91, 0xbd);

        static internal string GetTextFromValue(Guid guid)
        {
            // it is correct that SchemaGuids, DbInfoKeywords, DbInfoLiterals don't appear below
            // those are manufactured guids for calling methods other than IDBSchemaRowset.GetRowset
            // when the user calls OleDbConnection.GetOleDbSchemaTable

            if (guid == Primary_Keys)
            { return "Primary_Keys"; }
            if (guid == Indexes)
            { return "Indexes"; }
            if (guid == Procedure_Parameters)
            { return "Procedure_Parameters"; }
            if (guid == Procedures)
            { return "Procedures"; }

            if (guid == Tables_Info)
            { return "Tables_Info"; }

            if (guid == Trustee)
            { return "Trustee"; }
            if (guid == Assertions)
            { return "Assertions"; }
            if (guid == Catalogs)
            { return "Catalogs"; }
            if (guid == Character_Sets)
            { return "Character_Sets"; }
            if (guid == Collations)
            { return "Collations"; }
            if (guid == Columns)
            { return "Columns"; }
            if (guid == Check_Constraints)
            { return "Check_Constraints"; }
            if (guid == Constraint_Column_Usage)
            { return "Constraint_Column_Usage"; }
            if (guid == Constraint_Table_Usage)
            { return "Constraint_Table_Usage"; }
            if (guid == Key_Column_Usage)
            { return "Key_Column_Usage"; }
            if (guid == Referential_Constraints)
            { return "Referential_Constraints"; }
            if (guid == Table_Constraints)
            { return "Table_Constraints"; }
            if (guid == Column_Domain_Usage)
            { return "Column_Domain_Usage"; }
            if (guid == Column_Privileges)
            { return "Column_Privileges"; }
            if (guid == Table_Privileges)
            { return "Table_Privileges"; }
            if (guid == Usage_Privileges)
            { return "Usage_Privileges"; }
            if (guid == Schemata)
            { return "Schemata"; }
            if (guid == Sql_Languages)
            { return "Sql_Languages"; }
            if (guid == Statistics)
            { return "Statistics"; }
            if (guid == Tables)
            { return "Tables"; }
            if (guid == Translations)
            { return "Translations"; }
            if (guid == Provider_Types)
            { return "Provider_Types"; }
            if (guid == Views)
            { return "Views"; }
            if (guid == View_Column_Usage)
            { return "View_Column_Usage"; }
            if (guid == View_Table_Usage)
            { return "View_Table_Usage"; }
            if (guid == Foreign_Keys)
            { return "Foreign_Keys"; }
            if (guid == Procedure_Columns)
            { return "Procedure_Columns"; }

            if (guid == Table_Statistics)
            { return "Table_Statistics"; }
            if (guid == Check_Constraints_By_Table)
            { return "Check_Constraints_By_Table"; }

            return "{" + guid.ToString("D", CultureInfo.InvariantCulture) + ")";
        }
    }
}
