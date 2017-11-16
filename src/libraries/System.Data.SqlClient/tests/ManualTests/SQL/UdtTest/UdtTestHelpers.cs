// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    internal static class UdtTestHelpers
    {
        internal static string DumpReaderString(SqlDataReader reader)
        {
            return DumpReaderString(reader, true);
        }

        internal static string DumpReaderString(SqlDataReader reader, bool showMetaData)
        {
            int i;
            int x = 0, y = 0;
            double d;
            object o = 0;
            bool fNull;
            StringBuilder builder = new StringBuilder();

            if (showMetaData)
            {
                for (i = 0; i < reader.FieldCount; i++)
                {
                    builder.AppendLine($"ColumnName[{i}] = {reader.GetName(i)}");
                    builder.AppendLine($"DataType[{i}] = {reader.GetDataTypeName(i)}");
                    builder.AppendLine($"FieldType[{i}] = {reader.GetFieldType(i)}");
                }
            }

            while (reader.Read())
            {
                for (i = 0; i < reader.FieldCount; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    object fieldValue = reader.GetValue(i);

                    if (fieldValue is Point)
                    {
                        fNull = (bool)fieldValue.GetType().InvokeMember("IsNull", BindingFlags.GetProperty, null, fieldValue, null);
                        if (!fNull)
                        {
                            x = (int)fieldValue.GetType().InvokeMember("X", BindingFlags.GetProperty, null, fieldValue, null);
                            y = (int)fieldValue.GetType().InvokeMember("X", BindingFlags.GetProperty, null, fieldValue, null);
                            d = (double)fieldValue.GetType().InvokeMember("Distance", BindingFlags.Public | BindingFlags.Default | BindingFlags.Instance | BindingFlags.InvokeMethod, null, fieldValue, new Object[] { });
                            builder.Append(string.Format("p.X = {0,3}, p.Y = {1,3}, p.Distance() = {2}", x, y, d));
                        }
                        else
                        {
                            builder.Append("null");
                        }
                    }
                    else if (fieldValue is Circle)
                    {
                        fNull = (bool)fieldValue.GetType().InvokeMember("IsNull", BindingFlags.GetProperty, null, fieldValue, null);
                        if (!fNull)
                        {
                            o = fieldValue.GetType().InvokeMember("Center", BindingFlags.Public | BindingFlags.Default | BindingFlags.Instance | BindingFlags.GetProperty, null, fieldValue, new Object[] { });
                            builder.Append("Center = " + o);
                        }
                    }
                    else
                    {
                        builder.Append(string.Format("{0,10}", fieldValue.ToString()));
                    }
                }
                builder.AppendLine();
            }
            return builder.ToString();
        }
    }
}
