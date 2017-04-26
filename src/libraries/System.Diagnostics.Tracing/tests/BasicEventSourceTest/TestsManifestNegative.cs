// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics.Tracing;
using Xunit;

using Sdt = SdtEventSources;

namespace BasicEventSourceTests
{
    public class TestsManifestNegative
    {
        #region Message string building
        public static string GetResourceString(string key, params object[] args)
        {
            string fmt = GetResourceStringFromReflection(key);
            if (fmt != null)
                return string.Format(fmt, args);

            return key + " (" + string.Join(", ", args) + ")";
        }

        private static string GetResourceStringFromReflection(string key)
        {
            BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            if (!PlatformDetection.IsFullFramework)
            {
              Type sr = typeof(EventSource).Assembly.GetType("System.SR", throwOnError: true, ignoreCase: false);
              PropertyInfo resourceProp = sr.GetProperty(key, flags);
              return (string)resourceProp.GetValue(null);
            }

            Type[] paramsType = new Type[] { typeof(string) };
            MethodInfo getResourceString = typeof(Environment).GetMethod("GetResourceString", flags, null, paramsType, null);
            return (string)getResourceString.Invoke(null, new object[] { key });
        }
        #endregion

        /// <summary>
        /// These tests use the NuGet EventSource to validate *both* NuGet and BCL user-defined EventSources
        /// For NuGet EventSources we validate both "runtime" and "validation" behavior
        /// </summary>
        [Fact]
        public void Test_GenerateManifest_InvalidEventSources()
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");
            // specify AllowEventSourceOverride - this is needed for Sdt event sources and won't make a difference for Sdt ones
            var strictOptions = EventManifestOptions.Strict | EventManifestOptions.AllowEventSourceOverride;

            Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.UnsealedEventSource), string.Empty));

            Exception e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.UnsealedEventSource), string.Empty, strictOptions));
            Assert.Equal(GetResourceString("EventSource_TypeMustBeSealedOrAbstract"), e.Message);

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.UnsealedEventSource), string.Empty, strictOptions));
            Assert.Equal(GetResourceString("EventSource_TypeMustBeSealedOrAbstract"), e.Message);

            // starting with NuGet we allow non-void returning methods as long as they have the [Event] attribute
            Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.EventWithReturnEventSource), string.Empty));

            Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.EventWithReturnEventSource), string.Empty, strictOptions));

            Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.EventWithReturnEventSource), string.Empty, EventManifestOptions.AllowEventSourceOverride));

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.NegativeEventIdEventSource), string.Empty));
            Assert.Equal(GetResourceString("EventSource_NeedPositiveId", "WriteInteger"), e.Message);

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.NegativeEventIdEventSource), string.Empty, strictOptions));
            Assert.Equal(GetResourceString("EventSource_NeedPositiveId", "WriteInteger"), e.Message);

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.NegativeEventIdEventSource), string.Empty, EventManifestOptions.AllowEventSourceOverride));
            Assert.Equal(GetResourceString("EventSource_NeedPositiveId", "WriteInteger"), e.Message);

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.OutOfRangeKwdEventSource), string.Empty, strictOptions));
            Assert.Equal(String.Join(Environment.NewLine,
                                     GetResourceString("EventSource_IllegalKeywordsValue", "Kwd1", "0x100000000000"),
                                     GetResourceString("EventSource_KeywordCollision", "Session3", "Kwd1", "0x100000000000")),
                          e.Message);

#if FEATURE_ADVANCED_MANAGED_ETW_CHANNELS
            e = Assert.Throws<ArgumentException>(GetResourceString("EventSource_MaxChannelExceeded"),
                () => EventSource.GenerateManifest(typeof(Sdt.TooManyChannelsEventSource), string.Empty));
#endif

#if USE_ETW // TODO: Enable when TraceEvent is available on CoreCLR. GitHub issue #4864.
            e = Assert.Throws<ArgumentException>(GetResourceString("EventSource_EventWithAdminChannelMustHaveMessage", "WriteInteger", "Admin"),
                () => EventSource.GenerateManifest(typeof(Sdt.EventWithAdminChannelNoMessageEventSource), string.Empty, strictOptions));
#endif // USE_ETW

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.ReservedOpcodeEventSource), string.Empty, strictOptions));
            Assert.Equal(String.Join(Environment.NewLine,
                                     GetResourceString("EventSource_IllegalOpcodeValue", "Op1", 3),
                                     GetResourceString("EventSource_EventMustHaveTaskIfNonDefaultOpcode", "WriteInteger", 1)),
                          e.Message);

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.ReservedOpcodeEventSource), string.Empty, strictOptions));
            Assert.Equal(String.Join(Environment.NewLine,
                                     GetResourceString("EventSource_IllegalOpcodeValue", "Op1", 3),
                                     GetResourceString("EventSource_EventMustHaveTaskIfNonDefaultOpcode", "WriteInteger", 1)),
                          e.Message);

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.EnumKindMismatchEventSource), string.Empty));
            Assert.Equal(GetResourceString("EventSource_UndefinedKeyword", "0x1", "WriteInteger"), e.Message);

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.EnumKindMismatchEventSource), string.Empty, strictOptions));
            Assert.Equal(String.Join(Environment.NewLine,
                                     GetResourceString("EventSource_EnumKindMismatch", "Op1", "EventKeywords", "Opcodes"),
                                     GetResourceString("EventSource_UndefinedKeyword", "0x1", "WriteInteger")),
                          e.Message);

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.EnumKindMismatchEventSource), string.Empty, EventManifestOptions.AllowEventSourceOverride));
            Assert.Equal(GetResourceString("EventSource_UndefinedKeyword", "0x1", "WriteInteger"), e.Message);

            Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.MismatchIdEventSource), string.Empty));

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.MismatchIdEventSource), string.Empty, strictOptions));
            Assert.Equal(GetResourceString("EventSource_MismatchIdToWriteEvent", "WriteInteger", 10, 1), e.Message);

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.MismatchIdEventSource), string.Empty, strictOptions));
            Assert.Equal(GetResourceString("EventSource_MismatchIdToWriteEvent", "WriteInteger", 10, 1), e.Message);

            Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.EventIdReusedEventSource), string.Empty));

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.EventIdReusedEventSource), string.Empty, strictOptions));
            Assert.Equal(String.Join(Environment.NewLine,
                                     GetResourceString("EventSource_EventIdReused", "WriteInteger2", 1, "WriteInteger1"),
                                     GetResourceString("EventSource_TaskOpcodePairReused", "WriteInteger2", 1, "WriteInteger1", 1)),
                          e.Message);

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.EventIdReusedEventSource), string.Empty, strictOptions));
            Assert.Equal(String.Join(Environment.NewLine,
                                     GetResourceString("EventSource_EventIdReused", "WriteInteger2", 1, "WriteInteger1"),
                                     GetResourceString("EventSource_TaskOpcodePairReused", "WriteInteger2", 1, "WriteInteger1", 1)),
                          e.Message);

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.EventNameReusedEventSource), string.Empty, strictOptions));
            Assert.Equal(GetResourceString("EventSource_EventNameReused", "WriteInteger"), e.Message);

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.EventNameReusedEventSource), string.Empty, strictOptions));
            Assert.Equal(GetResourceString("EventSource_EventNameReused", "WriteInteger"), e.Message);

            Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.TaskOpcodePairReusedEventSource), string.Empty));

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.TaskOpcodePairReusedEventSource), string.Empty, strictOptions));
            Assert.Equal(GetResourceString("EventSource_TaskOpcodePairReused", "WriteInteger2", 2, "WriteInteger1", 1), e.Message);

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.TaskOpcodePairReusedEventSource), string.Empty, strictOptions));
            Assert.Equal(GetResourceString("EventSource_TaskOpcodePairReused", "WriteInteger2", 2, "WriteInteger1", 1), e.Message);

            Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.EventWithOpcodeNoTaskEventSource), string.Empty));

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.EventWithOpcodeNoTaskEventSource), string.Empty, strictOptions));
            Assert.Equal(GetResourceString("EventSource_EventMustHaveTaskIfNonDefaultOpcode", "WriteInteger", 1), e.Message);

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.EventWithOpcodeNoTaskEventSource), string.Empty, strictOptions));
            Assert.Equal(GetResourceString("EventSource_EventMustHaveTaskIfNonDefaultOpcode", "WriteInteger", 1), e.Message);

            Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.EventWithInvalidMessageEventSource), string.Empty));

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.EventWithInvalidMessageEventSource), string.Empty, strictOptions));
            Assert.Equal(GetResourceString("EventSource_UnsupportedMessageProperty", "WriteString", "Message = {0,12:G}"), e.Message);
            
            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.AbstractWithKwdTaskOpcodeEventSource), string.Empty, strictOptions));
            Assert.Equal(String.Join(Environment.NewLine,
                                     GetResourceString("EventSource_AbstractMustNotDeclareKTOC", "Keywords"),
                                     GetResourceString("EventSource_AbstractMustNotDeclareKTOC", "Tasks"),
                                     GetResourceString("EventSource_AbstractMustNotDeclareKTOC", "Opcodes")),
                          e.Message);

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.AbstractWithKwdTaskOpcodeEventSource), string.Empty, strictOptions));
            Assert.Equal(String.Join(Environment.NewLine,
                                     GetResourceString("EventSource_AbstractMustNotDeclareKTOC", "Keywords"),
                                     GetResourceString("EventSource_AbstractMustNotDeclareKTOC", "Tasks"),
                                     GetResourceString("EventSource_AbstractMustNotDeclareKTOC", "Opcodes")),
                          e.Message);

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.AbstractWithEventsEventSource), string.Empty, strictOptions));
            Assert.Equal(GetResourceString("EventSource_AbstractMustNotDeclareEventMethods", "WriteInteger", 1), e.Message);

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.AbstractWithEventsEventSource), string.Empty, strictOptions));
            Assert.Equal(GetResourceString("EventSource_AbstractMustNotDeclareEventMethods", "WriteInteger", 1), e.Message);

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.ImplementsInterfaceEventSource), string.Empty, strictOptions));
            Assert.Equal(GetResourceString("EventSource_EventMustNotBeExplicitImplementation", "SdtEventSources.ILogging.Error", 1), e.Message);

            e = Assert.Throws<ArgumentException>(() => EventSource.GenerateManifest(typeof(Sdt.ImplementsInterfaceEventSource), string.Empty, strictOptions));
            Assert.Equal(GetResourceString("EventSource_EventMustNotBeExplicitImplementation", "SdtEventSources.ILogging.Error", 1), e.Message);

            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }
    }
}
