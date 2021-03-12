// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Generators
{
    public partial class EventSourceGenerator
    {
        private class Emitter
        {
            private readonly StringBuilder _builder = new StringBuilder(1024);
            private readonly GeneratorExecutionContext _context;

            public Emitter(GeneratorExecutionContext context) => _context = context;

            public void Emit(EventSourceClass[] eventSources, CancellationToken cancellationToken, ITypeSymbol stringTypeSymbol)
            {
                foreach (EventSourceClass? ec in eventSources)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        // stop any additional work
                        break;
                    }

                    _builder.AppendLine("using System;");
                    GenType(ec, stringTypeSymbol);

                    _context.AddSource($"{ec.ClassName}.Generated", SourceText.From(_builder.ToString(), Encoding.UTF8));

                    _builder.Clear();
                }
            }

            private void GenType(EventSourceClass ec, ITypeSymbol stringTypeSymbol)
            {
                if (!string.IsNullOrWhiteSpace(ec.Namespace))
                {
                    _builder.AppendLine($@"
namespace {ec.Namespace}
{{");
                }

                _builder.AppendLine($@"
    partial class {ec.ClassName}
    {{");
                GenerateConstructor(ec);

                GenerateProviderMetadata(ec.SourceName);

                GenerateEventMetadata(ec);

                _builder.AppendLine($@"
    }}");

                if (!string.IsNullOrWhiteSpace(ec.Namespace))
                {
                    _builder.AppendLine($@"
}}");
                }

            }

            private void GenerateEventMetadata(EventSourceClass ec)
            {
                foreach (string debugStr in ec.DebugStrings)
                {
                    _builder.AppendLine(debugStr);
                }
            }

            private void GenerateConstructor(EventSourceClass ec)
            {
                _builder.AppendLine($@"
        private {ec.ClassName}() : base(new Guid({ec.Guid.ToString("x").Replace("{", "").Replace("}", "")}), ""{ec.SourceName}"") {{ }}");
            }

            private void GenerateProviderMetadata(string sourceName)
            {
                _builder.Append(@"
        private protected override ReadOnlySpan<byte> ProviderMetadata => new byte[] { ");

                byte[] metadataBytes = MetadataForString(sourceName);
                foreach (byte b in metadataBytes)
                {
                    _builder.Append($"0x{b:x}, ");
                }

                _builder.AppendLine(@"};");
            }

            // From System.Private.CoreLib
            private static byte[] MetadataForString(string name)
            {
                CheckName(name);
                int metadataSize = Encoding.UTF8.GetByteCount(name) + 3;
                byte[]? metadata = new byte[metadataSize];
                ushort totalSize = checked((ushort)(metadataSize));
                metadata[0] = unchecked((byte)totalSize);
                metadata[1] = unchecked((byte)(totalSize >> 8));
                Encoding.UTF8.GetBytes(name, 0, name.Length, metadata, 2);
                return metadata;
            }
            private void MetadataForProvider(string name, Guid guid, List<EventSourceEvent> events, ITypeSymbol stringTypeSymbol)
            {
                ManifestBuilder manifest = new ManifestBuilder(_builder, name, guid);
                                // Add an entry unconditionally for event ID 0 which will be for a string message.
                manifest.StartEvent("EventSourceMessage", new EventAttribute(0) { Level = EventLevel.LogAlways, Task = (EventTask)0xFFFE });
                manifest.AddEventParameter(stringTypeSymbol, "message");
                manifest.EndEvent("EventSourceMessage");

                // ensure we have keywords for the session-filtering reserved bits
                {
                    manifest.AddKeyword("Session3", (long)0x1000 << 32);
                    manifest.AddKeyword("Session2", (long)0x2000 << 32);
                    manifest.AddKeyword("Session1", (long)0x4000 << 32);
                    manifest.AddKeyword("Session0", (long)0x8000 << 32);
                }

                foreach (EventSourceEvent evt in events)
                {
                    EventAttribute eventAttribute = new EventAttribute(Int32.Parse(evt.Id));

                    eventAttribute.Level = (EventLevel)(Int32.Parse(evt.Level));
                    eventAttribute.Keywords = (EventKeywords)(Int64.Parse(evt.Keywords));

                    manifest.StartEvent(evt.Name, eventAttribute);
                    
                    if (evt.Parameters is not null)
                    {
                        foreach (EventParameter param in evt.Parameters)
                        {
                            manifest.AddEventParameter(param.Type, param.Name);
                        }
                    }
                }
            }

            /*
            private byte[] MetadataForProvider(string name, Guid guid, List<EventSourceEvent> events)
            {
                ManifestBuilder manifest = new ManifestBuilder(name, guid);
                                // Add an entry unconditionally for event ID 0 which will be for a string message.
                manifest.StartEvent("EventSourceMessage", new EventAttribute(0) { Level = EventLevel.LogAlways, Task = (EventTask)0xFFFE });
                manifest.AddEventParameter(typeof(string), "message");
                manifest.EndEvent();

                // ensure we have keywords for the session-filtering reserved bits
                {
                    manifest.AddKeyword("Session3", (long)0x1000 << 32);
                    manifest.AddKeyword("Session2", (long)0x2000 << 32);
                    manifest.AddKeyword("Session1", (long)0x4000 << 32);
                    manifest.AddKeyword("Session0", (long)0x8000 << 32);
                }

                foreach (EventSourceEvent evt in events)
                {
                    EventAttribute eventAttribute = new EventAttribute(Int32.Parse(evt.Id));

                    eventAttribute.Level = (EventLevel)(Int32.Parse(evt.Level));
                    eventAttribute.Keywords = (EventKeywords)(Int64.Parse(evt.Keywords));

                    manifest.StartEvent(evt.Name, eventAttribute);
                    
                    if (evt.Parameters is not null)
                    {
                        foreach (EventParameter param in evt.Parameters)
                        {
                            //manifest.AddEventParameter()


                        }
                    }
                }


                if (eventSourceType != typeof(EventSource))
                {
                    for (int i = 0; i < methods.Length; i++)
                    {
                        MethodInfo method = methods[i];
                        ParameterInfo[] args = method.GetParameters();

                        // Get the EventDescriptor (from the Custom attributes)
                        EventAttribute? eventAttribute = (EventAttribute?)GetCustomAttributeHelper(method, typeof(EventAttribute), flags);

                        {
                            continue;
                        }

                        if (eventSourceType.IsAbstract)
                        {
                            if (eventAttribute != null)
                            {
                                manifest.ManifestError(SR.Format(SR.EventSource_AbstractMustNotDeclareEventMethods, method.Name, eventAttribute.EventId));
                            }
                            continue;
                        }
                        else if (eventAttribute == null)
                        {
                            // Methods that don't return void can't be events, if they're NOT marked with [Event].
                            // (see Compat comment above)
                            if (method.ReturnType != typeof(void))
                            {
                                continue;
                            }

                            // Continue to ignore virtual methods if they do NOT have the [Event] attribute
                            // (see Compat comment above)
                            if (method.IsVirtual)
                            {
                                continue;
                            }

                            // If we explicitly mark the method as not being an event, then honor that.
                            if (IsCustomAttributeDefinedHelper(method, typeof(NonEventAttribute), flags))
                                continue;

                            defaultEventAttribute = new EventAttribute(eventId);
                            eventAttribute = defaultEventAttribute;
                        }
                        else if (eventAttribute.EventId <= 0)
                        {
                            manifest.ManifestError(SR.Format(SR.EventSource_NeedPositiveId, method.Name), true);
                            continue;   // don't validate anything else for this event
                        }
                        if (method.Name.LastIndexOf('.') >= 0)
                        {
                            manifest.ManifestError(SR.Format(SR.EventSource_EventMustNotBeExplicitImplementation, method.Name, eventAttribute.EventId));
                        }

                        eventId++;
                        string eventName = method.Name;

                        if (eventAttribute.Opcode == EventOpcode.Info)      // We are still using the default opcode.
                        {
                            // By default pick a task ID derived from the EventID, starting with the highest task number and working back
                            bool noTask = (eventAttribute.Task == EventTask.None);
                            if (noTask)
                                eventAttribute.Task = (EventTask)(0xFFFE - eventAttribute.EventId);

                            // Unless we explicitly set the opcode to Info (to override the auto-generate of Start or Stop opcodes,
                            // pick a default opcode based on the event name (either Info or start or stop if the name ends with that suffix).
                            if (!eventAttribute.IsOpcodeSet)
                                eventAttribute.Opcode = GetOpcodeWithDefault(EventOpcode.Info, eventName);

                            // Make the stop opcode have the same task as the start opcode.
                            if (noTask)
                            {
                                if (eventAttribute.Opcode == EventOpcode.Start)
                                {
                                    string taskName = eventName.Substring(0, eventName.Length - s_ActivityStartSuffix.Length); // Remove the Stop suffix to get the task name
                                    if (string.Compare(eventName, 0, taskName, 0, taskName.Length) == 0 &&
                                        string.Compare(eventName, taskName.Length, s_ActivityStartSuffix, 0, Math.Max(eventName.Length - taskName.Length, s_ActivityStartSuffix.Length)) == 0)
                                    {
                                        // Add a task that is just the task name for the start event.   This suppress the auto-task generation
                                        // That would otherwise happen (and create 'TaskName'Start as task name rather than just 'TaskName'
                                        manifest.AddTask(taskName, (int)eventAttribute.Task);
                                    }
                                }
                                else if (eventAttribute.Opcode == EventOpcode.Stop)
                                {
                                    // Find the start associated with this stop event.  We require start to be immediately before the stop
                                    int startEventId = eventAttribute.EventId - 1;
                                    if (eventData != null && startEventId < eventData.Length)
                                    {
                                        Debug.Assert(0 <= startEventId);                // Since we reserve id 0, we know that id-1 is <= 0
                                        EventMetadata startEventMetadata = eventData[startEventId];

                                        // If you remove the Stop and add a Start does that name match the Start Event's Name?
                                        // Ideally we would throw an error
                                        string taskName = eventName.Substring(0, eventName.Length - s_ActivityStopSuffix.Length); // Remove the Stop suffix to get the task name
                                        if (startEventMetadata.Descriptor.Opcode == (byte)EventOpcode.Start &&
                                            string.Compare(startEventMetadata.Name, 0, taskName, 0, taskName.Length) == 0 &&
                                            string.Compare(startEventMetadata.Name, taskName.Length, s_ActivityStartSuffix, 0, Math.Max(startEventMetadata.Name.Length - taskName.Length, s_ActivityStartSuffix.Length)) == 0)
                                        {
                                            // Make the stop event match the start event
                                            eventAttribute.Task = (EventTask)startEventMetadata.Descriptor.Task;
                                            noTask = false;
                                        }
                                    }
                                    if (noTask && (flags & EventManifestOptions.Strict) != 0)        // Throw an error if we can compatibly.
                                    {
                                        throw new ArgumentException(SR.EventSource_StopsFollowStarts);
                                    }
                                }
                            }
                        }

                        bool hasRelatedActivityID = RemoveFirstArgIfRelatedActivityId(ref args);
                        if (!(source != null && source.SelfDescribingEvents))
                        {
                            manifest.StartEvent(eventName, eventAttribute);
                            for (int fieldIdx = 0; fieldIdx < args.Length; fieldIdx++)
                            {
                                manifest.AddEventParameter(args[fieldIdx].ParameterType, args[fieldIdx].Name!);
                            }
                            manifest.EndEvent();
                        }

                        if (source != null || (flags & EventManifestOptions.Strict) != 0)
                        {
                            Debug.Assert(eventData != null);
                            // Do checking for user errors (optional, but not a big deal so we do it).
                            DebugCheckEvent(ref eventsByName, eventData, method, eventAttribute, manifest, flags);

#if FEATURE_MANAGED_ETW_CHANNELS
                            // add the channel keyword for Event Viewer channel based filters. This is added for creating the EventDescriptors only
                            // and is not required for the manifest
                            if (eventAttribute.Channel != EventChannel.None)
                            {
                                unchecked
                                {
                                    eventAttribute.Keywords |= (EventKeywords)manifest.GetChannelKeyword(eventAttribute.Channel, (ulong)eventAttribute.Keywords);
                                }
                            }
#endif
                            if (manifest.HasResources)
                            {
                                string eventKey = "event_" + eventName;
                                if (manifest.GetLocalizedMessage(eventKey, CultureInfo.CurrentUICulture, etwFormat: false) is string msg)
                                {
                                    // overwrite inline message with the localized message
                                    eventAttribute.Message = msg;
                                }
                            }

                            AddEventDescriptor(ref eventData, eventName, eventAttribute, args, hasRelatedActivityID);
                        }
                    }
                }

                // Tell the TraceLogging stuff where to start allocating its own IDs.
                NameInfo.ReserveEventIDsBelow(eventId);

                if (source != null)
                {
                    Debug.Assert(eventData != null);
                    TrimEventDescriptors(ref eventData);
                    source.m_eventData = eventData;     // officially initialize it. We do this at most once (it is racy otherwise).
#if FEATURE_MANAGED_ETW_CHANNELS
                    source.m_channelData = manifest.GetChannelData();
#endif
                }

                // if this is an abstract event source we've already performed all the validation we can
                if (!eventSourceType.IsAbstract && (source == null || !source.SelfDescribingEvents))
                {
                    bNeedsManifest = (flags & EventManifestOptions.OnlyIfNeededForRegistration) == 0
#if FEATURE_MANAGED_ETW_CHANNELS
                                            || manifest.GetChannelData().Length > 0
#endif
;

                    // if the manifest is not needed and we're not requested to validate the event source return early
                    if (!bNeedsManifest && (flags & EventManifestOptions.Strict) == 0)
                        return null;

                    return manifest.CreateManifest();
                }
                return null;
            }
            */

            private static void CheckName(string? name)
            {
                if (name != null && 0 <= name.IndexOf('\0'))
                {
                    throw new ArgumentOutOfRangeException(nameof(name));
                }
            }
        }
    }
}
