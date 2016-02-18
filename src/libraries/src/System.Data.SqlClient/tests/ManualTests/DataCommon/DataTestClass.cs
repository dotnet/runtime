// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public abstract class DataTestClass
    {

        protected static StringBuilder globalBuilder = new StringBuilder();
        public static readonly string BinariesDropPath = (Environment.GetEnvironmentVariable("BVT_BinariesDropPath") ?? Environment.GetEnvironmentVariable("_NTPOSTBLD")) ?? Environment.GetEnvironmentVariable("DD_BuiltTarget");
        public static readonly string SuiteBinPath = (BinariesDropPath == null) ? string.Empty : System.IO.Path.Combine(BinariesDropPath, "SuiteBin");

        private static Dictionary<string, string> s_xmlConnectionStringMap = null;
        private static readonly object s_connStringMapLock = new object();

        protected abstract void RunDataTest();

        protected bool RunTest()
        {
            try
            {
                RunDataTest();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            return true;
        }

        public static string GetConnectionString(string connectionStringName)
        {
            lock (s_connStringMapLock)
            {
                return GetConnectionStringFromXml(connectionStringName);
            }
        }

        private static string GetConnectionStringFromXml(string key)
        {
            string connectionString = null;
            if (s_xmlConnectionStringMap == null)
            {
                PopulateConnectionStrings();
            }

            bool foundConnString = s_xmlConnectionStringMap.TryGetValue(key, out connectionString);
            if (!foundConnString || string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("Could not find a valid connection string for the key: " + key);
            }

            return connectionString;
        }

        private static void PopulateConnectionStrings()
        {
            s_xmlConnectionStringMap = new Dictionary<string, string>();

            TextReader connectionStringReader = File.OpenText(@"ConnectionString.xml");
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            settings.DtdProcessing = DtdProcessing.Ignore;

            XmlDocument document = new XmlDocument();
            string xmlString = connectionStringReader.ToString();

            using (StringReader reader = new StringReader(connectionStringReader.ReadToEnd()))
            {
                using (XmlReader xReader = XmlReader.Create(reader, settings))
                {
                    document.Load(xReader);
                    XmlNodeList nodeList = document.GetElementsByTagName("ConnectionString");
                    foreach (XmlNode node in nodeList)
                    {
                        string connectionStringKey = node.Attributes["id"].Value;
                        XmlText connectionStringXml = node.FirstChild as XmlText;
                        string connectionStringValue = connectionStringXml.Data;
                        s_xmlConnectionStringMap[connectionStringKey] = connectionStringValue;
                    }
                }
            }
        }

        public static string SQL2005_Master { get { return GetConnectionString("SQL2005_Master"); } }
        public static string SQL2005_Pubs { get { return GetConnectionString("SQL2005_Pubs"); } }
        public static string SQL2005_Northwind { get { return GetConnectionString("SQL2005_Northwind"); } }
        public static string SQL2005_Northwind_NamedPipes { get { return GetConnectionString("SQL2005_Northwind_NamedPipes"); } }
        public static string SQL2008_Master { get { return GetConnectionString("SQL2008_Master"); } }
        public static string SQL2008_Pubs { get { return GetConnectionString("SQL2008_Pubs"); } }
        public static string SQL2008_Northwind { get { return GetConnectionString("SQL2008_Northwind"); } }
        public static string SQL2008_Northwind_NamedPipes { get { return GetConnectionString("SQL2008_Northwind_NamedPipes"); } }
        public static string SQL2012_Northwind_NamedPipes { get { return GetConnectionString("SQL2012_Northwind_NamedPipes"); } }

        // the name length will be no more then (16 + prefix.Length + escapeLeft.Length + escapeRight.Length)
        // some providers does not support names (Oracle supports up to 30)
        public static string GetUniqueName(string prefix, string escapeLeft, string escapeRight)
        {
            string uniqueName = string.Format("{0}{1}_{2}_{3}{4}",
                escapeLeft,
                prefix,
                DateTime.Now.Ticks.ToString("X", CultureInfo.InvariantCulture), // up to 8 characters
                Guid.NewGuid().ToString().Substring(0, 6), // take the first 6 characters only
                escapeRight);
            return uniqueName;
        }

        // SQL Server supports long names (up to 128 characters), add extra info for troubleshooting
        public static string GetUniqueNameForSqlServer(string prefix)
        {
            string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            string hostName = System.Net.Dns.GetHostName();

            string extendedPrefix = string.Format(
                "{0}_{1}@{2}",
                prefix,
                userName,
                hostName,
                DateTime.Now.ToString("yyyy_MM_dd", CultureInfo.InvariantCulture));
            string name = GetUniqueName(extendedPrefix, "[", "]");
            if (name.Length > 128)
            {
                throw new ArgumentOutOfRangeException("the name is too long - SQL Server names are limited to 128");
            }
            return name;
        }

        // Oracle database does not support long names (names are limited to 30 characters)
        public static string GetUniqueNameForOracle(string prefix)
        {
            string name = GetUniqueName(prefix, string.Empty, string.Empty);
            if (name.Length > 30)
            {
                throw new ArgumentOutOfRangeException("prefix should be short - Oracle does not support long names");
            }
            return name;
        }

        // creates temporary table name for SQL Server
        public static string UniqueTempTableName
        {
            get
            {
                return GetUniqueNameForSqlServer("#T");
            }
        }

        public static void PrintException(Type expected, Exception e, params string[] values)
        {
            try
            {
                Debug.Assert(null != e, "PrintException: null exception");

                globalBuilder.Length = 0;
                globalBuilder.Append(e.GetType().Name).Append(": ");

                if (e is COMException)
                {
                    globalBuilder.Append("0x").Append((((COMException)e).HResult).ToString("X8"));
                    if (expected != e.GetType())
                    {
                        globalBuilder.Append(": ").Append(e.ToString());
                    }
                }
                else
                {
                    globalBuilder.Append(e.Message);
                }
                AssemblyFilter(globalBuilder);
                Console.WriteLine(globalBuilder.ToString());

                if (expected != e.GetType())
                {
                    Console.WriteLine(e.StackTrace);
                }
                if (null != values)
                {
                    foreach (string value in values)
                    {
                        Console.WriteLine(value);
                    }
                }
                if (null != e.InnerException)
                {
                    PrintException(e.InnerException.GetType(), e.InnerException);
                }
                Console.Out.Flush();
            }
            catch (Exception f)
            {
                Console.WriteLine(f);
            }
        }

        public static void DumpParameters(DbCommand cmd)
        {
            DumpParameters((SqlCommand)cmd);
        }

        public static void DumpParameters(SqlCommand cmd)
        {
            Debug.Assert(null != cmd, "DumpParameters: null SqlCommand");

            foreach (SqlParameter p in cmd.Parameters)
            {
                byte precision = p.Precision;
                byte scale = p.Scale;
                Console.WriteLine("\t\"" + p.ParameterName + "\" AS " + p.DbType.ToString("G") + " OF " + p.SqlDbType.ToString("G") + " FOR " + p.SourceColumn + "\"");
                Console.WriteLine("\t\t" + p.Size.ToString() + ", " + precision.ToString() + ", " + scale.ToString() + ", " + p.Direction.ToString("G") + ", " + DBConvertToString(p.Value));
            }
        }

        public static void WriteEntry(string entry)
        {
            Console.WriteLine(entry);
        }

        private static StringBuilder s_outputBuilder;
        private static string[] s_outputFilter;

        public static StreamWriter NewWriter()
        {
            return new StreamWriter(new MemoryStream(), System.Text.Encoding.UTF8);
            //return new StringWriter(OutputBuilder, System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string AssemblyFilter(StreamWriter writer)
        {
            if (null == s_outputBuilder)
            {
                s_outputBuilder = new StringBuilder();
            }
            s_outputBuilder.Length = 0;

            byte[] utf8 = ((MemoryStream)writer.BaseStream).ToArray();
            string value = System.Text.Encoding.UTF8.GetString(utf8, 3, utf8.Length - 3); // skip 0xEF, 0xBB, 0xBF
            s_outputBuilder.Append(value);
            AssemblyFilter(s_outputBuilder);
            return s_outputBuilder.ToString();
        }

        public static void AssemblyFilter(StringBuilder builder)
        {
            string[] filter = s_outputFilter;
            if (null == filter)
            {
                filter = new string[5];
                string tmp = typeof(System.Guid).AssemblyQualifiedName;
                filter[0] = tmp.Substring(tmp.IndexOf(','));
                filter[1] = filter[0].Replace("mscorlib", "System");
                filter[2] = filter[0].Replace("mscorlib", "System.Data");
                filter[3] = filter[0].Replace("mscorlib", "System.Data.OracleClient");
                filter[4] = filter[0].Replace("mscorlib", "System.Xml");
                s_outputFilter = filter;
            }

            for (int i = 0; i < filter.Length; ++i)
            {
                builder.Replace(filter[i], "");
            }
        }

        public static string ToInvariatString(object value)
        {
            return (
                (value is DateTime) ? ((DateTime)value).ToString("MM/dd/yyyy HH:mm:ss", DateTimeFormatInfo.InvariantInfo) :
                (value is decimal) ? ((decimal)value).ToString(NumberFormatInfo.InvariantInfo) :
                (value is double) ? ((double)value).ToString(NumberFormatInfo.InvariantInfo) :
                (value is float) ? ((float)value).ToString(NumberFormatInfo.InvariantInfo) :
                (value is short) ? ((short)value).ToString(NumberFormatInfo.InvariantInfo) :
                (value is int) ? ((int)value).ToString(NumberFormatInfo.InvariantInfo) :
                (value is long) ? ((Int64)value).ToString(NumberFormatInfo.InvariantInfo) :
                /*default: */ value.ToString()
            );
        }

        public static string DBConvertToString(object value)
        {
            StringWriter stringWriter = new StringWriter();
            WriteObject(stringWriter, value, CultureInfo.InvariantCulture, null, 0, int.MaxValue);
            return stringWriter.ToString();
        }

        public static void DumpValue(object value)
        {
            DumpValue(Console.Out, value, int.MaxValue, CultureInfo.InvariantCulture);
        }
        public static void DumpValue(object value, int recursionLimit)
        {
            DumpValue(Console.Out, value, recursionLimit, CultureInfo.InvariantCulture);
        }

        public static void DumpValue(TextWriter textWriter, object value, CultureInfo cultureInfo)
        {
            DumpValue(textWriter, value, int.MaxValue, cultureInfo);
        }

        public static void DumpValue(TextWriter textWriter, object value, int recursionLimit, CultureInfo cultureInfo)
        {
            if (value is DbDataReader)
            {
                WriteDbDataReader(textWriter, value as DbDataReader, cultureInfo, "", recursionLimit);
            }
            else
            {
                WriteObject(textWriter, value, recursionLimit, cultureInfo);
            }
        }

        private static void WriteDbDataReader(TextWriter textWriter, DbDataReader reader, CultureInfo cultureInfo, string prefix, int recursionLimit)
        {
            if (null == reader) { throw new ArgumentNullException("reader"); }
            if (null == textWriter) { throw new ArgumentNullException("textWriter"); }
            if (null == cultureInfo) { throw new ArgumentNullException("cultureInfo"); }

            if (0 > --recursionLimit)
            {
                return;
            }
            if (reader.IsClosed)
            {
                return;
            }

            int resultCount = 0;
            int lastRecordsAffected = 0;
            object value = null;
            do
            {
                try
                {
                    textWriter.WriteLine(prefix + "ResultSetIndex=" + resultCount);

                    int fieldCount = reader.FieldCount;
                    if (0 < fieldCount)
                    {
                        for (int i = 0; i < fieldCount; ++i)
                        {
                            textWriter.WriteLine(prefix + "Field[" + i + "] = " + reader.GetName(i) + "(" + reader.GetDataTypeName(i) + ")");
                        }
                        int rowCount = 0;
                        while (reader.Read())
                        {
                            textWriter.WriteLine(prefix + "RowIndex=" + rowCount);

                            for (int index = 0; index < fieldCount; ++index)
                            {
                                try
                                {
                                    value = reader.GetValue(index);
                                    if (value is DbDataReader)
                                    {
                                        DbDataReader hierarchialResult = (DbDataReader)value;
                                        textWriter.WriteLine(prefix + "Value[" + index + "] is " + value.GetType().Name + " Depth=" + hierarchialResult.Depth);
                                        WriteDbDataReader(textWriter, hierarchialResult, cultureInfo, prefix + "\t", recursionLimit);
                                        hierarchialResult.Dispose();
                                        value = null;
                                    }
                                    else
                                    {
                                        textWriter.Write(prefix + "Value[" + index + "] = ");
                                        WriteObject(textWriter, value, cultureInfo, null, 0, recursionLimit);
                                        textWriter.Write(Environment.NewLine);
                                    }
                                }
                                catch (Exception e)
                                {
                                    PrintException(null, e);
                                    if (value is IDisposable)
                                    {
                                        ((IDisposable)value).Dispose();
                                    }
                                    value = null;
                                }
                            }
                            ++rowCount;
                        }
                        int cumlativeRecordsAffected = reader.RecordsAffected;
                        textWriter.WriteLine(prefix + "RecordsAffected=" + (cumlativeRecordsAffected - lastRecordsAffected));
                        lastRecordsAffected = Math.Min(cumlativeRecordsAffected, 0);
                    }
                }
                catch (Exception e)
                {
                    PrintException(null, e);
                }
                resultCount++;
            } while (reader.NextResult());

            reader.Dispose();
        }

        public static void WriteObject(TextWriter textWriter, object value, CultureInfo cultureInfo)
        {
            WriteObject(textWriter, value, int.MaxValue, cultureInfo);
        }

        public static void WriteObject(TextWriter textWriter, object value, int recursionLimit, CultureInfo cultureInfo)
        {
            if (null == textWriter)
            {
                throw new ArgumentNullException("textWriter");
            }
            if (null == cultureInfo)
            {
                cultureInfo = CultureInfo.InvariantCulture;
            }
            WriteObject(textWriter, value, cultureInfo, new Hashtable(), 1, recursionLimit);
        }

        private static void WriteObject(TextWriter textWriter, object value, CultureInfo cultureInfo, Hashtable used, int indent, int recursionLimit)
        {
            if (0 > --recursionLimit)
            {
                return;
            }
            if (null == value)
            {
                textWriter.Write("DEFAULT");
            }
            else if (DBNull.Value.Equals(value))
            {
                textWriter.Write("ISNULL");
            }
            else
            {
                Type valuetype = value.GetType();

                if ((value is string) || (value is SqlString))
                {
                    if (value is SqlString)
                    {
                        value = ((SqlString)value).Value;
                    }
                    textWriter.Write(valuetype.Name);
                    textWriter.Write(":");
                    textWriter.Write(((string)value).Length);
                    textWriter.Write("<");
                    textWriter.Write((string)value);
                    textWriter.Write(">");
                }
                else if ((value is DateTime) || (value is SqlDateTime))
                {
                    if (value is SqlDateTime)
                    {
                        value = ((SqlDateTime)value).Value;
                    }
                    textWriter.Write(valuetype.Name);
                    textWriter.Write("<");
                    textWriter.Write(((DateTime)value).ToString("s", cultureInfo));
                    textWriter.Write(">");
                }
                else if ((value is Single) || (value is SqlSingle))
                {
                    if (value is SqlSingle)
                    {
                        value = ((SqlSingle)value).Value;
                    }
                    textWriter.Write(valuetype.Name);
                    textWriter.Write("<");
                    textWriter.Write(((float)value).ToString(cultureInfo));
                    textWriter.Write(">");
                }
                else if ((value is Double) || (value is SqlDouble))
                {
                    if (value is SqlDouble)
                    {
                        value = ((SqlDouble)value).Value;
                    }
                    textWriter.Write(valuetype.Name);
                    textWriter.Write("<");
                    textWriter.Write(((double)value).ToString(cultureInfo));
                    textWriter.Write(">");
                }
                else if ((value is decimal) || (value is SqlDecimal) || (value is SqlMoney))
                {
                    if (value is SqlDecimal)
                    {
                        value = ((SqlDecimal)value).Value;
                    }
                    else if (value is SqlMoney)
                    {
                        value = ((SqlMoney)value).Value;
                    }
                    textWriter.Write(valuetype.Name);
                    textWriter.Write("<");
                    textWriter.Write(((decimal)value).ToString(cultureInfo));
                    textWriter.Write(">");
                }
                else if (value is INullable && ((INullable)value).IsNull)
                {
                    textWriter.Write(valuetype.Name);
                    textWriter.Write(" ISNULL");
                }
                else if (valuetype.IsArray)
                {
                    textWriter.Write(valuetype.Name);
                    Array array = (Array)value;

                    if (1 < array.Rank)
                    {
                        textWriter.Write("{");
                    }

                    for (int i = 0; i < array.Rank; ++i)
                    {
                        int count = array.GetUpperBound(i);

                        textWriter.Write(' ');
                        textWriter.Write(count - array.GetLowerBound(i) + 1);
                        textWriter.Write("{ ");
                        for (int k = array.GetLowerBound(i); k <= count; ++k)
                        {
                            AppendNewLineIndent(textWriter, indent + 1);
                            textWriter.Write(',');
                            WriteObject(textWriter, array.GetValue(k), cultureInfo, used, 0, recursionLimit);
                            textWriter.Write(' ');
                        }
                        AppendNewLineIndent(textWriter, indent);
                        textWriter.Write("}");
                    }
                    if (1 < array.Rank)
                    {
                        textWriter.Write('}');
                    }
                }
                else if (value is ICollection)
                {
                    textWriter.Write(valuetype.Name);
                    ICollection collection = (ICollection)value;
                    object[] newvalue = new object[collection.Count];
                    collection.CopyTo(newvalue, 0);

                    textWriter.Write(' ');
                    textWriter.Write(newvalue.Length);
                    textWriter.Write('{');
                    for (int k = 0; k < newvalue.Length; ++k)
                    {
                        AppendNewLineIndent(textWriter, indent + 1);
                        textWriter.Write(',');
                        WriteObject(textWriter, newvalue[k], cultureInfo, used, indent + 1, recursionLimit);
                    }
                    AppendNewLineIndent(textWriter, indent);
                    textWriter.Write('}');
                }
                else if (value is Type)
                {
                    textWriter.Write(valuetype.Name);
                    textWriter.Write('<');
                    textWriter.Write((value as Type).FullName);
                    textWriter.Write('>');
                }
                else
                {
                    string fullName = valuetype.FullName;
                    if ("System.ComponentModel.ExtendedPropertyDescriptor" == fullName)
                    {
                        textWriter.Write(fullName);
                    }
                    else
                    {
                        FieldInfo[] fields = valuetype.GetFields(BindingFlags.Instance | BindingFlags.Public);
                        PropertyInfo[] properties = valuetype.GetProperties(BindingFlags.Instance | BindingFlags.Public);

                        bool hasinfo = false;
                        if ((null != fields) && (0 < fields.Length))
                        {
                            textWriter.Write(fullName);
                            fullName = null;

                            Array.Sort(fields, FieldInfoCompare.Default);
                            for (int i = 0; i < fields.Length; ++i)
                            {
                                FieldInfo field = fields[i];

                                AppendNewLineIndent(textWriter, indent + 1);
                                textWriter.Write(field.Name);
                                textWriter.Write('=');
                                object newvalue = field.GetValue(value);
                                WriteObject(textWriter, newvalue, cultureInfo, used, indent + 1, recursionLimit);
                            }
                            hasinfo = true;
                        }
                        if ((null != properties) && (0 < properties.Length))
                        {
                            if (null != fullName)
                            {
                                textWriter.Write(fullName);
                                fullName = null;
                            }

                            Array.Sort(properties, PropertyInfoCompare.Default);
                            for (int i = 0; i < properties.Length; ++i)
                            {
                                PropertyInfo property = properties[i];
                                if (property.CanRead)
                                {
                                    ParameterInfo[] parameters = property.GetIndexParameters();
                                    if ((null == parameters) || (0 == parameters.Length))
                                    {
                                        AppendNewLineIndent(textWriter, indent + 1);
                                        textWriter.Write(property.Name);
                                        textWriter.Write('=');
                                        object newvalue = null;
                                        bool haveValue = false;
                                        try
                                        {
                                            newvalue = property.GetValue(value);
                                            haveValue = true;
                                        }
                                        catch (TargetInvocationException e)
                                        {
                                            textWriter.Write(e.InnerException.GetType().Name);
                                            textWriter.Write(": ");
                                            textWriter.Write(e.InnerException.Message);
                                        }
                                        if (haveValue)
                                        {
                                            WriteObject(textWriter, newvalue, cultureInfo, used, indent + 1, recursionLimit);
                                        }
                                    }
                                }
                            }
                            hasinfo = true;
                        }
                        if (!hasinfo)
                        {
                            textWriter.Write(valuetype.Name);
                            textWriter.Write('<');
                            MethodInfo method = valuetype.GetMethod("ToString", new Type[] { typeof(IFormatProvider) });
                            if (null != method)
                            {
                                textWriter.Write((string)method.Invoke(value, new object[] { cultureInfo }));
                            }
                            else
                            {
                                string text = value.ToString();
                                textWriter.Write(text);
                            }
                            textWriter.Write('>');
                        }
                    }
                }
            }
        }

        private static char[] s_appendNewLineIndentBuffer = new char[0];
        private static void AppendNewLineIndent(TextWriter textWriter, int indent)
        {
            textWriter.Write(Environment.NewLine);
            char[] buf = s_appendNewLineIndentBuffer;
            if (buf.Length < indent * 4)
            {
                buf = new char[indent * 4];
                for (int i = 0; i < buf.Length; ++i)
                {
                    buf[i] = ' ';
                }
                s_appendNewLineIndentBuffer = buf;
            }
            textWriter.Write(buf, 0, indent * 4);
        }

        private class FieldInfoCompare : IComparer
        {
            internal static FieldInfoCompare Default = new FieldInfoCompare();

            private FieldInfoCompare()
            {
            }

            public int Compare(object x, object y)
            {
                string fieldInfoName1 = ((FieldInfo)x).Name;
                string fieldInfoName2 = ((FieldInfo)y).Name;

                return CultureInfo.InvariantCulture.CompareInfo.Compare(fieldInfoName1, fieldInfoName2, CompareOptions.IgnoreCase);
            }
        }

        private class PropertyInfoCompare : IComparer
        {
            internal static PropertyInfoCompare Default = new PropertyInfoCompare();

            private PropertyInfoCompare()
            {
            }

            public int Compare(object x, object y)
            {
                string propertyInfoName1 = ((PropertyInfo)x).Name;
                string propertyInfoName2 = ((PropertyInfo)y).Name;

                return CultureInfo.InvariantCulture.CompareInfo.Compare(propertyInfoName1, propertyInfoName2, CompareOptions.IgnoreCase);
            }
        }

        private static bool CheckException<TException>(Exception ex, string exceptionMessage, bool innerExceptionMustBeNull) where TException : Exception
        {
            return ((ex != null) && (ex is TException) &&
                ((string.IsNullOrEmpty(exceptionMessage)) || (ex.Message.Contains(exceptionMessage))) &&
                ((!innerExceptionMustBeNull) || (ex.InnerException == null)));
        }

        public static void AssertEqualsWithDescription(object expectedValue, object actualValue, string failMessage)
        {
            var msg = string.Format("{0}\nExpected: {1}\nActual: {2}", failMessage, expectedValue, actualValue);
            if (expectedValue == null || actualValue == null)
            {
                Assert.True(expectedValue == actualValue, msg);
            }
            else
            {
                Assert.True(expectedValue.Equals(actualValue), msg);
            }
        }

        public static TException AssertThrowsWrapper<TException>(Action actionThatFails, string exceptionMessage = null, bool innerExceptionMustBeNull = false, Func<TException, bool> customExceptionVerifier = null) where TException : Exception
        {
            TException ex = Assert.Throws<TException>(actionThatFails);
            if (exceptionMessage != null)
            {
                Assert.True(ex.Message.Contains(exceptionMessage),
                    string.Format("FAILED: Exception did not contain expected message.\nExpected: {0}\nActual: {1}", exceptionMessage, ex.Message));
            }

            if (innerExceptionMustBeNull)
            {
                Assert.True(ex.InnerException == null, "FAILED: Expected InnerException to be null.");
            }

            if (customExceptionVerifier != null)
            {
                Assert.True(customExceptionVerifier(ex), "FAILED: Custom exception verifier returned false for this exception.");
            }

            return ex;
        }

        public static TException AssertThrowsWrapper<TException, TInnerException>(Action actionThatFails, string exceptionMessage = null, string innerExceptionMessage = null, bool innerExceptionMustBeNull = false, Func<TException, bool> customExceptionVerifier = null) where TException : Exception
        {
            TException ex = AssertThrowsWrapper<TException>(actionThatFails, exceptionMessage, innerExceptionMustBeNull, customExceptionVerifier);

            if (innerExceptionMessage != null)
            {
                Assert.True(ex.InnerException != null, "FAILED: Cannot check innerExceptionMessage because InnerException is null.");
                Assert.True(ex.InnerException.Message.Contains(innerExceptionMessage),
                    string.Format("FAILED: Inner Exception did not contain expected message.\nExpected: {0}\nActual: {1}", innerExceptionMessage, ex.InnerException.Message));
            }

            return ex;
        }

        public static TException AssertThrowsWrapper<TException, TInnerException, TInnerInnerException>(Action actionThatFails, string exceptionMessage = null, string innerExceptionMessage = null, string innerInnerExceptionMessage = null, bool innerInnerInnerExceptionMustBeNull = false) where TException : Exception where TInnerException : Exception where TInnerInnerException : Exception
        {
            TException ex = AssertThrowsWrapper<TException, TInnerException>(actionThatFails, exceptionMessage, innerExceptionMessage);
            if (innerInnerInnerExceptionMustBeNull)
            {
                Assert.True(ex.InnerException != null, "FAILED: Cannot check innerInnerInnerExceptionMustBeNull since InnerException is null");
                Assert.True(ex.InnerException.InnerException == null, "FAILED: Expected InnerInnerException to be null.");
            }

            if (innerInnerExceptionMessage != null)
            {
                Assert.True(ex.InnerException != null, "FAILED: Cannot check innerInnerExceptionMessage since InnerException is null");
                Assert.True(ex.InnerException.InnerException != null, "FAILED: Cannot check innerInnerExceptionMessage since InnerInnerException is null");
                Assert.True(ex.InnerException.InnerException.Message.Contains(innerInnerExceptionMessage),
                    string.Format("FAILED: Inner Exception did not contain expected message.\nExpected: {0}\nActual: {1}", innerInnerExceptionMessage, ex.InnerException.InnerException.Message));
            }

            return ex;
        }

        public static TException ExpectFailure<TException>(Action actionThatFails, string exceptionMessage = null, bool innerExceptionMustBeNull = false, Func<TException, bool> customExceptionVerifier = null) where TException : Exception
        {
            try
            {
                actionThatFails();
                Console.WriteLine("ERROR: Did not get expected exception");
                return null;
            }
            catch (Exception ex)
            {
                if ((CheckException<TException>(ex, exceptionMessage, innerExceptionMustBeNull)) && ((customExceptionVerifier == null) || (customExceptionVerifier(ex as TException))))
                {
                    return (ex as TException);
                }
                else
                {
                    throw;
                }
            }
        }

        public static TException ExpectFailure<TException, TInnerException>(Action actionThatFails, string exceptionMessage = null, string innerExceptionMessage = null, bool innerInnerExceptionMustBeNull = false) where TException : Exception where TInnerException : Exception
        {
            try
            {
                actionThatFails();
                Console.WriteLine("ERROR: Did not get expected exception");
                return null;
            }
            catch (Exception ex)
            {
                if ((CheckException<TException>(ex, exceptionMessage, false)) && (CheckException<TInnerException>(ex.InnerException, innerExceptionMessage, innerInnerExceptionMustBeNull)))
                {
                    return (ex as TException);
                }
                else
                {
                    throw;
                }
            }
        }

        public static TException ExpectFailure<TException, TInnerException, TInnerInnerException>(Action actionThatFails, string exceptionMessage = null, string innerExceptionMessage = null, string innerInnerExceptionMessage = null, bool innerInnerInnerExceptionMustBeNull = false) where TException : Exception where TInnerException : Exception where TInnerInnerException : Exception
        {
            try
            {
                actionThatFails();
                Console.WriteLine("ERROR: Did not get expected exception");
                return null;
            }
            catch (Exception ex)
            {
                if ((CheckException<TException>(ex, exceptionMessage, false)) && (CheckException<TInnerException>(ex.InnerException, innerExceptionMessage, false)) && (CheckException<TInnerInnerException>(ex.InnerException.InnerException, innerInnerExceptionMessage, innerInnerInnerExceptionMustBeNull)))
                {
                    return (ex as TException);
                }
                else
                {
                    throw;
                }
            }
        }

        public static void ExpectAsyncFailure<TException>(Func<Task> actionThatFails, string exceptionMessage = null, bool innerExceptionMustBeNull = false) where TException : Exception
        {
            ExpectFailure<AggregateException, TException>(() => actionThatFails().Wait(), null, exceptionMessage, innerExceptionMustBeNull);
        }

        public static void ExpectAsyncFailure<TException, TInnerException>(Func<Task> actionThatFails, string exceptionMessage = null, string innerExceptionMessage = null, bool innerInnerExceptionMustBeNull = false) where TException : Exception where TInnerException : Exception
        {
            ExpectFailure<AggregateException, TException, TInnerException>(() => actionThatFails().Wait(), null, exceptionMessage, innerExceptionMessage, innerInnerExceptionMustBeNull);
        }

        private string GetTestName()
        {
            return GetType().Name;
        }

        public bool RunTestCoreAndCompareWithBaseline()
        {
            string outputPath = GetTestName() + ".out";
            string baselinePath = GetTestName() + ".bsl";

            var fstream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            var swriter = new StreamWriter(fstream, Encoding.UTF8);
            // Convert all string writes of '\n' to '\r\n' so output files can be 'text' not 'binary'
            var twriter = new CarriageReturnLineFeedReplacer(swriter);
            Console.SetOut(twriter); // "redirect" Console.Out

            // Run Test
            RunTest();

            Console.Out.Flush();
            Console.Out.Dispose();

            // Recover the standard output stream
            StreamWriter standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            // Compare output file
            var comparisonResult = FindDiffFromBaseline(baselinePath, outputPath);

            if (string.IsNullOrEmpty(comparisonResult))
            {
                return true;
            }

            Console.WriteLine("Test Failed!");
            Console.WriteLine("Please compare baseline : {0} with output :{1}", Path.GetFullPath(baselinePath), Path.GetFullPath(outputPath));
            Console.WriteLine("Comparison Results : ");
            Console.WriteLine(comparisonResult);
            return false;
        }

        private string FindDiffFromBaseline(string baselinePath, string outputPath)
        {
            var expectedLines = File.ReadAllLines(baselinePath);
            var outputLines = File.ReadAllLines(outputPath);

            var comparisonSb = new StringBuilder();

            // Start compare results
            var expectedLength = expectedLines.Length;
            var outputLength = outputLines.Length;
            var findDiffLength = Math.Min(expectedLength, outputLength);

            // Find diff for each lines
            for (var lineNo = 0; lineNo < findDiffLength; lineNo++)
            {
                if (!expectedLines[lineNo].Equals(outputLines[lineNo]))
                {
                    comparisonSb.AppendFormat("** DIFF at line {0} \n", lineNo);
                    comparisonSb.AppendFormat("A : {0} \n", outputLines[lineNo]);
                    comparisonSb.AppendFormat("E : {0} \n", expectedLines[lineNo]);
                }
            }

            var startIndex = findDiffLength - 1;
            if (startIndex < 0) startIndex = 0;

            if (findDiffLength < expectedLength)
            {
                comparisonSb.AppendFormat("** MISSING \n");
                for (var lineNo = startIndex; lineNo < expectedLength; lineNo++)
                {
                    comparisonSb.AppendFormat("{0} : {1}", lineNo, expectedLines[lineNo]);
                }
            }
            if (findDiffLength < outputLength)
            {
                comparisonSb.AppendFormat("** EXTRA \n");
                for (var lineNo = startIndex; lineNo < outputLength; lineNo++)
                {
                    comparisonSb.AppendFormat("{0} : {1}", lineNo, outputLines[lineNo]);
                }
            }

            return comparisonSb.ToString();
        }
    }
}
