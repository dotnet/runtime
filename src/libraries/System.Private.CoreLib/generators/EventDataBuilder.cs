// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;

namespace Generators
{
    public partial class EventSourceGenerator
    {
        /// <summary>
        /// Used for generating the m_EventData field for EventSource
        /// </summary>
        private class EventDataBuilder
        {
            public EventDataBuilder() {}
            
            public static void BuildEventDescriptor(StringBuilder builder, List<EventSourceEvent> events)
            {
                StringBuilder _builder = builder;
                int eventDataSize = events.Count + 1;

                // check if any events ID went "out of sync" and is bigger than total # of events.
                foreach (var esEvent in events)
                {
                    if (Int32.Parse(esEvent.Id) > eventDataSize)
                    {
                        eventDataSize = Int32.Parse(esEvent.Id) + 1;
                    }
                }

                _builder.AppendLine("        m_EventDataInitializer = () => ");
                _builder.AppendLine("        {");
                

                _builder.Append("            EventMetadata[] eventData = new EventMetadata[").Append(eventDataSize).AppendLine("];");
                _builder.AppendLine("            eventData[0].Name = \"\";");

                foreach (var esEvent in events)
                {
                    int task = (int)(0xFFFE - Int32.Parse(esEvent.Id));

                    _builder.Append("            eventData[").Append(esEvent.Id).AppendLine("].Descriptor = new EventDescriptor(");
                    _builder.Append("                ").Append(esEvent.Id).AppendLine(",");
                    _builder.Append("                ").Append(esEvent.Version).AppendLine(",");
                    _builder.AppendLine("                (byte)0,"); // TODO: ADD CHANNELS SUPPORT
                    _builder.Append("                (byte)").Append(esEvent.Level).AppendLine(",");
                    _builder.AppendLine("                (byte)0,"); // TODO: ADD OPCODE SUPPORT
                    _builder.Append("                ").Append(task).AppendLine(",");

                    ulong kwMask;
                    if (esEvent.Keywords == "")
                    {
                        kwMask = 0;
                    }
                    else
                    {
                        kwMask = (ulong)(Int64.Parse(esEvent.Keywords));
                    }
                    ulong sessionMaskAllKeywords = (ulong)0x0fU << 44; // SessionMask.All.ToEventKeywords() in EventSource.cs
                    _builder.Append("                unchecked((long)").Append(kwMask | sessionMaskAllKeywords).AppendLine("));");

                    _builder.Append("            eventData[").Append(esEvent.Id).AppendLine("].Tags=EventTags.None;");
                    _builder.Append("            eventData[").Append(esEvent.Id).Append("].Name=\"").Append(esEvent.Name).AppendLine("\";");
                    _builder.Append("            eventData[").Append(esEvent.Id).AppendLine("].Parameters=null;");
                    _builder.Append("            eventData[").Append(esEvent.Id).AppendLine("].Message=\"\";");
                    _builder.Append("            eventData[").Append(esEvent.Id).AppendLine("].ActivityOptions=0;");
                    _builder.Append("            eventData[").Append(esEvent.Id).Append("].HasRelatedActivityID").AppendLine(" = false;");
                    _builder.Append("            eventData[").Append(esEvent.Id).Append("].EventHandle").AppendLine(" = IntPtr.Zero;");
                    _builder.AppendLine("");
                }
                _builder.AppendLine("            return eventData;");
                _builder.AppendLine("        };");
            }


                

                /*

                                EventMetadata[]? eventData = null;
                    Dictionary<string, string>? eventsByName = null;
                
                
                    if (source != null || (flags & EventManifestOptions.Strict) != 0)
                    {
                        eventData = new EventMetadata[methods.Length + 1];
                        eventData[0].Name = "";         // Event 0 is the 'write messages string' event, and has an empty name.
                    }
                */

        }

    }
}
