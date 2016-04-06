// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;


namespace System.Runtime.Serialization.Json
{
    internal class JavaScriptSerializer
    {
        private Stream _outputStream;

        public JavaScriptSerializer(Stream stream)
        {
            _outputStream = stream;
        }

        public void SerializeObject(object obj)
        {
            StringBuilder sb = new StringBuilder();
            SerializeValue(obj, sb, 0, null);
            byte[] encodedBytes = Encoding.UTF8.GetBytes(sb.ToString());
            _outputStream.Write(encodedBytes, 0, encodedBytes.Length);
        }

        private static void SerializeBoolean(bool o, StringBuilder sb)
        {
            if (o)
            {
                sb.Append(Globals.True);
            }
            else
            {
                sb.Append(Globals.False);
            }
        }

        private static void SerializeUri(Uri uri, StringBuilder sb)
        {
            SerializeString(uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped), sb);
        }

        private static void SerializeGuid(Guid guid, StringBuilder sb)
        {
            sb.Append("\"").Append(guid.ToString()).Append("\"");
        }

        private static void SerializeDateTime(DateTime value, StringBuilder sb)
        {
            sb.Append(@"""\/Date(");
            DateTime valueUtc = value.ToUniversalTime();
            sb.Append((valueUtc.Ticks - JsonGlobals.unixEpochTicks) / 10000);

            switch (value.Kind)
            {
                case DateTimeKind.Unspecified:
                case DateTimeKind.Local:
                    TimeSpan ts = DateTime.SpecifyKind(value, DateTimeKind.Utc).Subtract(valueUtc);
                    if (ts.Ticks < 0)
                    {
                        sb.Append("-");
                    }
                    else
                    {
                        sb.Append("+");
                    }
                    int hours = Math.Abs(ts.Hours);
                    sb.Append((hours < 10) ? "0" + hours : hours.ToString(CultureInfo.InvariantCulture));
                    int minutes = Math.Abs(ts.Minutes);
                    sb.Append((minutes < 10) ? "0" + minutes : minutes.ToString(CultureInfo.InvariantCulture));
                    break;
                case DateTimeKind.Utc:
                    break;
            }
            sb.Append(@")\/""");
        }

        private void SerializeDictionary(IDictionary o, StringBuilder sb, int depth, Dictionary<object, bool> objectsInUse)
        {
            sb.Append('{');
            bool isFirstElement = true;
            // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
            IDictionaryEnumerator e = o.GetEnumerator();
            try
            {
                while (e.MoveNext())
                {
                    DictionaryEntry entry = e.Entry;
                    if (!isFirstElement)
                    {
                        sb.Append(',');
                    }
                    string key = entry.Key as string;
                    if (key == null)
                    {
                        throw new SerializationException(SR.Format(SR.ObjectSerializer_DictionaryNotSupported, o.GetType().FullName));
                    }
                    SerializeString(key, sb);
                    sb.Append(':');
                    SerializeValue(entry.Value, sb, depth, objectsInUse);
                    isFirstElement = false;
                }
            }
            finally
            {
                (e as IDisposable)?.Dispose();
            }
            sb.Append('}');
        }

        private void SerializeEnumerable(IEnumerable enumerable, StringBuilder sb, int depth, Dictionary<object, bool> objectsInUse)
        {
            sb.Append('[');
            bool isFirstElement = true;
            foreach (object o in enumerable)
            {
                if (!isFirstElement)
                {
                    sb.Append(',');
                }

                SerializeValue(o, sb, depth, objectsInUse);
                isFirstElement = false;
            }
            sb.Append(']');
        }

        private static void SerializeString(string input, StringBuilder sb)
        {
            sb.Append('"');
            sb.Append(JavaScriptString.QuoteString(input));
            sb.Append('"');
        }

        private void SerializeValue(object o, StringBuilder sb, int depth, Dictionary<object, bool> objectsInUse)
        {
            SerializeValueInternal(o, sb, depth, objectsInUse);
        }


        private class ReferenceComparer : IEqualityComparer<object>
        {
            bool IEqualityComparer<object>.Equals(object x, object y)
            {
                return x == y;
            }

            int IEqualityComparer<object>.GetHashCode(object obj)
            {
                return obj.GetHashCode();
            }
        }

        private void SerializeValueInternal(object o, StringBuilder sb, int depth, Dictionary<object, bool> objectsInUse)
        {
            if (o == null || Globals.IsDBNullValue(o))
            {
                sb.Append("null");
                return;
            }


            string os = o as String;
            if (os != null)
            {
                SerializeString(os, sb);
                return;
            }

            if (o is Char)
            {
                SerializeString(XmlConvert.ToString((char)o), sb);
                return;
            }

            if (o is bool)
            {
                SerializeBoolean((bool)o, sb);
                return;
            }

            if (o is DateTime)
            {
                SerializeDateTime((DateTime)o, sb);
                return;
            }

            if (o is Guid)
            {
                SerializeGuid((Guid)o, sb);
                return;
            }

            Uri uri = o as Uri;
            if (uri != null)
            {
                SerializeUri(uri, sb);
                return;
            }


            if (o is double)
            {
                double d = (double)o;
                if (double.IsInfinity(d))
                {
                    if (double.IsNegativeInfinity(d))
                        sb.Append(JsonGlobals.NegativeInf);
                    else
                        sb.Append(JsonGlobals.PositiveInf);
                }
                else
                {
                    sb.Append(d.ToString("r", CultureInfo.InvariantCulture));
                }
                return;
            }

            if (o is float)
            {
                float f = (float)o;
                if (float.IsInfinity(f))
                {
                    if (float.IsNegativeInfinity(f))
                        sb.Append(JsonGlobals.NegativeInf);
                    else
                        sb.Append(JsonGlobals.PositiveInf);
                }
                else
                {
                    sb.Append(f.ToString("r", CultureInfo.InvariantCulture));
                }
                return;
            }


            if (o.GetType().GetTypeInfo().IsPrimitive || o is Decimal)
            {
                sb.Append(Convert.ToString(o, CultureInfo.InvariantCulture));
                return;
            }

            if (o.GetType() == typeof(object))
            {
                sb.Append("{}");
                return;
            }


            Type type = o.GetType();
            if (type.GetTypeInfo().IsEnum)
            {
                sb.Append((int)o);
                return;
            }

            try
            {
                if (objectsInUse == null)
                {
                    objectsInUse = new Dictionary<object, bool>(new ReferenceComparer());
                }
                else if (objectsInUse.ContainsKey(o))
                {
                    throw new InvalidOperationException(SR.Format(SR.JsonCircularReferenceDetected, type.FullName));
                }

                objectsInUse.Add(o, true);

                IDictionary od = o as IDictionary;
                if (od != null)
                {
                    SerializeDictionary(od, sb, depth, objectsInUse);
                    return;
                }

                IEnumerable oenum = o as IEnumerable;
                if (oenum != null)
                {
                    SerializeEnumerable(oenum, sb, depth, objectsInUse);
                    return;
                }

                //SerializeCustomObject(o, sb, depth, objectsInUse);
            }
            finally
            {
                if (objectsInUse != null)
                {
                    objectsInUse.Remove(o);
                }
            }
        }
    }
}
