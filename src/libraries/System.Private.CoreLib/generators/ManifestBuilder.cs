// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Numerics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Generators
{
    /// <summary>
    /// ManifestBuilder is designed to isolate the details of the message of the event from the
    /// rest of EventSource.  This one happens to create XML.
    /// </summary>
    public class ManifestBuilder
    {
        //private const string dllName = "System.Private.CoreLib";
        private StringBuilder _builder;

        /// <summary>
        /// Build a manifest for 'providerName' with the given GUID, which will be packaged into 'dllName'.
        /// 'resources, is a resource manager.  If specified all messages are localized using that manager.
        /// </summary>
        public ManifestBuilder(StringBuilder builder, string providerName, Guid providerGuid, Dictionary<ulong, string>? keywordMap, Dictionary<int, string>? taskMap)
        {
            this.providerName = providerName;
            this._builder = builder;
            sb = new StringBuilder();
            events = new StringBuilder();
            templates = new StringBuilder();
            opcodeTab = new Dictionary<int, string>();
            stringTab = new Dictionary<string, string>();
            errors = new List<string>();
            perEventByteArrayArgIndices = new Dictionary<string, List<int>>();

            sb.AppendLine("<instrumentationManifest xmlns=\"http://schemas.microsoft.com/win/2004/08/events\">");
            sb.AppendLine(" <instrumentation xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:win=\"http://manifests.microsoft.com/win/2004/08/windows/events\">");
            sb.AppendLine("  <events xmlns=\"http://schemas.microsoft.com/win/2004/08/events\">");
            sb.Append("<provider name=\"").Append(providerName).
               Append("\" guid=\"{").Append(providerGuid.ToString()).Append("}");
            string symbolsName = providerName.Replace("-", "").Replace('.', '_');  // Period and - are illegal replace them.
            sb.Append("\" symbol=\"").Append(symbolsName);
            sb.AppendLine("\">");

            keywordTab = keywordMap;
            taskTab = taskMap;

            // TODO: Remove these once we figure out a long-term localization replacement solution
            stringTab.Add("event_TaskCompleted", "Task {2} completed.");
            stringTab.Add("event_TaskScheduled", "Task {2} scheduled to TaskScheduler {0}.");
            stringTab.Add("event_TaskStarted", "Task {2} executing.");
            stringTab.Add("event_TaskWaitBegin", "Beginning wait ({3}) on Task {2}.");
            stringTab.Add("event_TaskWaitEnd", "Ending wait on Task {2}.");
        }

        public void AddOpcode(string name, int value)
        {
            opcodeTab[value] = name;
        }

        public void AddTask(string name, int value)
        {
            taskTab ??= new Dictionary<int, string>();
            if (taskTab.ContainsValue(name))
            {
                return;
            }

            taskTab[value] = name;
            stringTab.Add($"keyword_{name}", name);
        }

        public void AddKeyword(string name, ulong value)
        {
            if ((value & (value - 1)) != 0)   // Is it a power of 2?
            {
                return;
            }
            keywordTab ??= new Dictionary<ulong, string>();
            keywordTab[value] = name;
        }
        public void AddMap(Dictionary<string, Dictionary<string, int>> ecMap)
        {
            maps ??= new Dictionary<string, Dictionary<string, int>>();

            foreach (string enumName in ecMap.Keys)
            {
                if (!maps.ContainsKey(enumName))
                {
                    maps.Add(enumName, new Dictionary<string, int>());
                    
                    foreach (string fieldName in ecMap[enumName].Keys)
                    {
                        if (!maps[enumName].ContainsKey(fieldName))
                        {
                            maps[enumName][fieldName] = ecMap[enumName][fieldName];
                        }
                    }
                }
            }
        }

        public void StartEvent(string eventName, EventAttribute eventAttribute)
        {
            this.eventName = eventName;
            this.numParams = 0;
            byteArrArgIndices = null;
            string taskName = eventName;

            // TODO: Add additional logic here for Start/Stop events
            if (eventAttribute.Task == EventTask.None)
            {
                eventAttribute.Task = (EventTask)(0xFFFE - eventAttribute.EventId);
            }
            events.Append("  <event value=\"").Append(eventAttribute.EventId).
                 Append("\" version=\"").Append(eventAttribute.Version).
                 Append("\" level=\"");
            AppendLevelName(events, eventAttribute.Level);
            events.Append("\" symbol=\"").Append(eventName).Append('"');
            // at this point we add to the manifest's stringTab a message that is as-of-yet
            // "untranslated to manifest convention", b/c we don't have the number or position
            // of any byte[] args (which require string format index updates)
            WriteMessageAttrib(events, "event", eventName, eventAttribute.Message);

            if (eventAttribute.Keywords != 0)
            {
                events.Append(" keywords=\"");
                AppendKeywords(events, (ulong)eventAttribute.Keywords, eventName);
                events.Append('"');
            }

            if (eventAttribute.Task != 0)
            {
                events.Append(" task=\"").Append(taskName).Append('"');

                AddTask(taskName, (int)eventAttribute.Task);
            }

            /*
            if (eventAttribute.Opcode != 0)
            {
                events.Append(" opcode=\"").Append(GetOpcodeName(eventAttribute.Opcode, eventName)).Append('"');
            }

            if (eventAttribute.Task != 0)
            {
                events.Append(" task=\"").Append(GetTaskName(eventAttribute.Task, eventName)).Append('"');
            }

            if (eventAttribute.Channel != 0)
            {
                events.Append(" channel=\"").Append(GetChannelName(eventAttribute.Channel, eventName, eventAttribute.Message)).Append('"');
            }
            */
        }

        public void AddEventParameter(ITypeSymbol? type, string name)
        {
            if (type is null)
            {
                // ???
                return;
            }
            if (this.numParams == 0)
                templates.Append("  <template tid=\"").Append(this.eventName).AppendLine("Args\">");
            if (type.ToDisplayString() == "byte[]")
            {
                // mark this index as "extraneous" (it has no parallel in the managed signature)
                // we use these values in TranslateToManifestConvention()
                byteArrArgIndices ??= new List<int>(4);
                byteArrArgIndices.Add(numParams);

                // add an extra field to the template representing the length of the binary blob
                numParams++;
                templates.Append("   <data name=\"").Append(name).AppendLine("Size\" inType=\"win:UInt32\"/>");
            }
            numParams++;
            templates.Append("   <data name=\"").Append(name).Append("\" inType=\"").Append(GetTypeName(type)).Append('"');
            // TODO: for 'byte*' types it assumes the user provided length is named using the same naming convention
            //       as for 'byte[]' args (blob_arg_name + "Size")
            if ((type.TypeKind == TypeKind.Array && ((IArrayTypeSymbol)type).ElementType.SpecialType == SpecialType.System_Byte) ||
                (type.TypeKind == TypeKind.Pointer && ((IPointerTypeSymbol)type).PointedAtType.SpecialType == SpecialType.System_Byte))
            {
                 // add "length" attribute to the "blob" field in the template (referencing the field added above)
                templates.Append(" length=\"").Append(name).Append("Size\"");
            }

            // ETW does not support 64-bit value maps, so we don't specify these as ETW maps
            if (type.TypeKind == TypeKind.Enum)
            {
                INamedTypeSymbol? underlyingEnumType = ((INamedTypeSymbol)type).EnumUnderlyingType;
                if (underlyingEnumType is not null)
                {
                    if (underlyingEnumType.SpecialType != SpecialType.System_Int64)
                    {
                        templates.Append(" map=\"").Append(type.Name).Append('"');
                    }
                }
            }
            templates.AppendLine("/>");
        }
        private void WriteMessageAttrib(StringBuilder stringBuilder, string elementName, string name, string? value)
        {
            string? key = null;

            // See if the user wants things localized.
            /*
            if (resources != null)
            {
                // resource fallback: strings in the neutral culture will take precedence over inline strings
                key = elementName + "_" + name;
                if (resources.GetString(key, CultureInfo.InvariantCulture) is string localizedString)
                    value = localizedString;
            }
            */

            if (value == null)
                return;

            key ??= elementName + "_" + name;
            stringBuilder.Append(" message=\"$(string.").Append(key).Append(")\"");

            if (stringTab.TryGetValue(key, out string? prevValue) && !prevValue.Equals(value))
            {
                return;
            }

            stringTab[key] = value;
        }

        private static void AppendLevelName(StringBuilder sb, EventLevel level)
        {
            if ((int)level < 16)
            {
                sb.Append("win:");
            }

            sb.Append(level switch // avoid boxing that comes from level.ToString()
            {
                EventLevel.LogAlways => nameof(EventLevel.LogAlways),
                EventLevel.Critical => nameof(EventLevel.Critical),
                EventLevel.Error => nameof(EventLevel.Error),
                EventLevel.Warning => nameof(EventLevel.Warning),
                EventLevel.Informational => nameof(EventLevel.Informational),
                EventLevel.Verbose => nameof(EventLevel.Verbose),
                _ => ((int)level).ToString()
            });
        }


        public void EndEvent(string eventName)
        {
            if (numParams > 0)
            {
                templates.AppendLine("  </template>");
                events.Append(" template=\"").Append(eventName).Append("Args\"");
            }
            events.AppendLine("/>");

            if (byteArrArgIndices != null)
                perEventByteArrayArgIndices[eventName] = byteArrArgIndices;

            // at this point we have all the information we need to translate the C# Message
            // to the manifest string we'll put in the stringTab
            string prefixedEventName = "event_" + eventName;
            if (stringTab.TryGetValue(prefixedEventName, out string? msg))
            {
                msg = TranslateToManifestConvention(msg, eventName);
                stringTab[prefixedEventName] = msg;
            }
        }
        private string TranslateToManifestConvention(string eventMessage, string evtName)
        {
            StringBuilder? stringBuilder = null;        // We lazily create this
            int writtenSoFar = 0;
            for (int i = 0; ;)
            {
                if (i >= eventMessage.Length)
                {
                    if (stringBuilder is null)
                        return eventMessage;
                    UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, i - writtenSoFar);
                    return stringBuilder!.ToString();
                }

                int chIdx;
                if (eventMessage[i] == '%')
                {
                    // handle format message escaping character '%' by escaping it
                    UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, i - writtenSoFar);
                    stringBuilder!.Append("%%");
                    i++;
                    writtenSoFar = i;
                }
                else if (i < eventMessage.Length - 1 &&
                    (eventMessage[i] == '{' && eventMessage[i + 1] == '{' || eventMessage[i] == '}' && eventMessage[i + 1] == '}'))
                {
                    // handle C# escaped '{" and '}'
                    UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, i - writtenSoFar);
                    stringBuilder!.Append(eventMessage[i]);
                    i++; i++;
                    writtenSoFar = i;
                }
                else if (eventMessage[i] == '{')
                {
                    int leftBracket = i;
                    i++;
                    int argNum = 0;
                    while (i < eventMessage.Length && char.IsDigit(eventMessage[i]))
                    {
                        argNum = argNum * 10 + eventMessage[i] - '0';
                        i++;
                    }
                    if (i < eventMessage.Length && eventMessage[i] == '}')
                    {
                        i++;
                        UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, leftBracket - writtenSoFar);
                        int manIndex = TranslateIndexToManifestConvention(argNum, evtName);
                        stringBuilder!.Append('%').Append(manIndex);
                        // An '!' after the insert specifier {n} will be interpreted as a literal.
                        // We'll escape it so that mc.exe does not attempt to consider it the
                        // beginning of a format string.
                        if (i < eventMessage.Length && eventMessage[i] == '!')
                        {
                            i++;
                            stringBuilder.Append("%!");
                        }
                        writtenSoFar = i;
                    }
                }
                else if ((chIdx = "&<>'\"\r\n\t".IndexOf(eventMessage[i])) >= 0)
                {
                    UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, i - writtenSoFar);
                    i++;
                    stringBuilder!.Append(s_escapes[chIdx]);
                    writtenSoFar = i;
                }
                else
                    i++;
            }
        }
        private int TranslateIndexToManifestConvention(int idx, string evtName)
        {
            if (perEventByteArrayArgIndices.TryGetValue(evtName, out List<int>? byteArrArgIndices))
            {
                foreach (int byArrIdx in byteArrArgIndices)
                {
                    if (idx >= byArrIdx)
                        ++idx;
                    else
                        break;
                }
            }
            return idx + 1;
        }


        private static void UpdateStringBuilder(ref StringBuilder? stringBuilder, string eventMessage, int startIndex, int count)
        {
            stringBuilder ??= new StringBuilder();
            stringBuilder.Append(eventMessage, startIndex, count);
        }


        public byte[] CreateManifest()
        {
            string str = CreateManifestString();
            return Encoding.UTF8.GetBytes(str);
        }

        public string CreateManifestString()
        {
            Span<char> ulongHexScratch = stackalloc char[16]; // long enough for ulong.MaxValue formatted as hex
            // Write out the tasks
            if (taskTab != null)
            {
                sb.AppendLine(" <tasks>");
                var sortedTasks = new List<int>(taskTab.Keys);
                sortedTasks.Sort();
                foreach (int task in sortedTasks)
                {
                    sb.Append("  <task");
                    WriteNameAndMessageAttribs(sb, "task", taskTab[task]);
                    sb.Append(" value=\"").Append(task).AppendLine("\"/>");
                }
                sb.AppendLine(" </tasks>");
            }

            // Write out the maps
            sb.AppendLine(" <maps>");
            foreach (string enumName in maps.Keys)
            {
                sb.Append("  <").Append("valueMap").Append(" name=\"").Append(enumName).AppendLine("\">");
                foreach (string fieldName in maps[enumName].Keys)
                {
                    ulong hexValue = (ulong)Convert.ToInt64(maps[enumName][fieldName]);
                    string hexValueFormatted = hexValue.ToString("x", CultureInfo.InvariantCulture);
                    sb.Append("   <").Append("map value=\"0x").Append(hexValueFormatted).Append('"');
                    WriteMessageAttrib(sb, "map", enumName + "." + fieldName, fieldName);
                    sb.AppendLine("/>");
                }
                sb.AppendLine("  </valueMap>");
            }
            sb.AppendLine(" </maps>");

            // TODO: WRITE OUT OPCODE

            // Write out the keywords
            if (keywordTab != null)
            {
                sb.AppendLine(" <keywords>");
                var sortedKeywords = new List<ulong>(keywordTab.Keys);
                sortedKeywords.Sort();
                foreach (ulong keyword in sortedKeywords)
                {
                    sb.Append("  <keyword");
                    WriteNameAndMessageAttribs(sb, "keyword", keywordTab[keyword]);
                    string hexValueFormatted = keyword.ToString("x", CultureInfo.InvariantCulture);
                    sb.Append(" mask=\"0x").Append(hexValueFormatted).AppendLine("\"/>");
                }
                sb.AppendLine(" </keywords>");
            }


            sb.AppendLine(" <events>");
            sb.Append(events.ToString());
            sb.AppendLine(" </events>");
            sb.AppendLine(" <templates>");
            sb.Append(templates.ToString());
            sb.AppendLine(" </templates>");
            sb.AppendLine("</provider>");
            sb.AppendLine("</events>");
            sb.AppendLine("</instrumentation>");

            // TODO: StringTable? (localization)
            // Output the localization information.

            sb.AppendLine("<localization>");

            var sortedStrings = new string[stringTab.Keys.Count];
            stringTab.Keys.CopyTo(sortedStrings, 0);
            Array.Sort<string>(sortedStrings, 0, sortedStrings.Length);

            CultureInfo ci = CultureInfo.CurrentUICulture;
            sb.Append(" <resources culture=\"").Append(ci.Name).AppendLine("\">");
            sb.AppendLine("  <stringTable>");
            foreach (string stringKey in sortedStrings)
            {
                stringTab.TryGetValue(stringKey, out string val);
                if (val != null)
                {
                    sb.Append("   <string id=\"").Append(stringKey).Append("\" value=\"").Append(val).AppendLine("\"/>");
                }
            }
            sb.AppendLine("  </stringTable>");
            sb.AppendLine(" </resources>");

            sb.AppendLine("</localization>");
            sb.AppendLine("</instrumentationManifest>");

            return sb.ToString();
        }
        private void WriteNameAndMessageAttribs(StringBuilder stringBuilder, string elementName, string name)
        {
            stringBuilder.Append(" name=\"").Append(name).Append('"');
            WriteMessageAttrib(sb, elementName, name, name);
        }
        private void AppendKeywords(StringBuilder sb, ulong keywords, string eventName)
        {
            // ignore keywords associate with channels
            // See ValidPredefinedChannelKeywords def for more.
            keywords &= ~ValidPredefinedChannelKeywords;
            bool appended = false;
            for (ulong bit = 1; bit != 0; bit <<= 1)
            {
                if ((keywords & bit) != 0)
                {
                    string? keyword = null;
                    if ((keywordTab == null || !keywordTab.TryGetValue(bit, out keyword)) &&
                        (bit >= (ulong)0x1000000000000))
                    {
                        // do not report Windows reserved keywords in the manifest (this allows the code
                        // to be resilient to potential renaming of these keywords)
                        keyword = string.Empty;
                    }
                    if (keyword == null)
                    {
                        keyword = string.Empty;
                    }

                    if (keyword.Length != 0)
                    {
                        if (appended)
                        {
                            sb.Append(' ');
                        }

                        sb.Append(keyword);
                        appended = true;
                    }
                }
            }
        }
        private string? GetLocalizedMessage(string key, CultureInfo ci, bool etwFormat)
        {
            string? value = null;
            if (etwFormat && value == null)
                stringTab.TryGetValue(key, out value);

            return value;
        }


        private string GetTypeName(ITypeSymbol type)
        {
            if (type is null) return string.Empty;
            if (type.TypeKind == TypeKind.Enum)
            {
                ITypeSymbol? enumType = ((INamedTypeSymbol)type).EnumUnderlyingType;
                if (enumType is not null)
                {
                    string typeName = GetTypeName(enumType);
                    return typeName.Replace("win:Int", "win:UInt"); // ETW requires enums to be unsigned.
                }
                else
                {
                    // TODO: error?
                    return string.Empty;
                }
            }

            ITypeSymbol underlyingType = type.OriginalDefinition;

            switch (underlyingType.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return "win:Boolean";
                case SpecialType.System_Byte:
                    return "win:UInt8";
                case SpecialType.System_Char:
                case SpecialType.System_UInt16:
                    return "win:UInt16";
                case SpecialType.System_UInt32:
                    return "win:UInt32";
                case SpecialType.System_UInt64:
                    return "win:UInt64";
                case SpecialType.System_SByte:
                    return "win:Int8";
                case SpecialType.System_Int16:
                    return "win:Int16";
                case SpecialType.System_Int32:
                    return "win:Int32";
                case SpecialType.System_Int64:
                    return "win:Int64";
                case SpecialType.System_String:
                    return "win:UnicodeString";
                case SpecialType.System_Single:
                    return "win:Float";
                case SpecialType.System_Double:
                    return "win:Double";
                case SpecialType.System_DateTime:
                    return "win:FILETIME";
                default:
                    if (type.ToDisplayString() == "Guid")
                        return "win:GUID";
                    else if (type.SpecialType == SpecialType.System_IntPtr)
                        return "win:Pointer";
                    else if (type.TypeKind == TypeKind.Array && ((IArrayTypeSymbol)type).ElementType.SpecialType == SpecialType.System_Byte)
                    {
                        return "win:Binary";
                    }
                    else if (type.TypeKind == TypeKind.Pointer &&  ((IPointerTypeSymbol)type).PointedAtType.SpecialType == SpecialType.System_Byte)
                    {
                        return "win:Binary";
                    }

                    // ManifestError(SR.Format(SR.EventSource_UnsupportedEventTypeInManifest, type.Name), true);
                    // TODO: ERROR
                    return string.Empty;
            }
        }

        private static readonly string[] s_escapes = { "&amp;", "&lt;", "&gt;", "&apos;", "&quot;", "%r", "%n", "%t" };
        // Manifest messages use %N conventions for their message substitutions.   Translate from
        // .NET conventions.   We can't use RegEx for this (we are in mscorlib), so we do it 'by hand'

        private readonly Dictionary<int, string> opcodeTab;
        private Dictionary<int, string>? taskTab;
        private Dictionary<ulong, string>? keywordTab;
        private readonly Dictionary<string, string> stringTab;       // Maps unlocalized strings to localized ones

        // WCF used EventSource to mimic a existing ETW manifest.   To support this
        // in just their case, we allowed them to specify the keywords associated
        // with their channels explicitly.   ValidPredefinedChannelKeywords is
        // this set of channel keywords that we allow to be explicitly set.  You
        // can ignore these bits otherwise.
        internal const ulong ValidPredefinedChannelKeywords = 0xF000000000000000;

        private readonly StringBuilder sb;               // Holds the provider information.
        private readonly StringBuilder events;           // Holds the events.
        private readonly StringBuilder templates;

        private readonly string providerName;
        private readonly IList<string> errors;           // list of currently encountered errors
        private readonly Dictionary<string, List<int>> perEventByteArrayArgIndices;  // "event_name" -> List_of_Indices_of_Byte[]_Arg

        // State we track between StartEvent and EndEvent.
        private string? eventName;               // Name of the event currently being processed.
        private int numParams;                  // keeps track of the number of args the event has.
        private List<int>? byteArrArgIndices;   // keeps track of the index of each byte[] argument

        private Dictionary<string, Dictionary<string, int>> maps;
    }

    /// <summary>
    /// Used to send the m_rawManifest into the event dispatcher as a series of events.
    /// </summary>
    internal struct ManifestEnvelope
    {
        public const int MaxChunkSize = 0xFF00;
        public enum ManifestFormats : byte
        {
            SimpleXmlFormat = 1,          // simply dump the XML manifest as UTF8
        }

#if FEATURE_MANAGED_ETW
        public ManifestFormats Format;
        public byte MajorVersion;
        public byte MinorVersion;
        public byte Magic;
        public ushort TotalChunks;
        public ushort ChunkNumber;
#endif
    }


}
