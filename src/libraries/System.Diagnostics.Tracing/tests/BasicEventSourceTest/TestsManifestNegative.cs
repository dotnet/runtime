// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using Xunit;

using Sdt = SdtEventSources;

namespace BasicEventSourceTests
{
    public class TestsManifestNegative
    {
        private static void AssertExceptionStringsEqual(Func<string> expectedStrFunc, Exception ex)
        {
            string expectedStr = expectedStrFunc();
            Assert.Equal(ex.Message, expectedStr);
        }

        #region Message string building
        private static string GetResourceString(string key, params object[] args)
        {
            string fmt = GetResourceStringFromReflection(key);
            if (fmt != null)
                return string.Format(fmt, args);

            return key + " (" + string.Join(", ", args) + ")";
        }

        private static string GetResourceStringFromReflection(string key)
        {
            BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            if (!PlatformDetection.IsNetFramework)
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

            Exception e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.UnsealedEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_TypeMustBeSealedOrAbstract"), e);

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.UnsealedEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_TypeMustBeSealedOrAbstract"), e);

            // starting with NuGet we allow non-void returning methods as long as they have the [Event] attribute
            Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.EventWithReturnEventSource), string.Empty));

            Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.EventWithReturnEventSource), string.Empty, strictOptions));

            Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.EventWithReturnEventSource), string.Empty, EventManifestOptions.AllowEventSourceOverride));

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.NegativeEventIdEventSource), string.Empty));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_NeedPositiveId"), e);

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.NegativeEventIdEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_NeedPositiveId"), e);

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.NegativeEventIdEventSource), string.Empty, EventManifestOptions.AllowEventSourceOverride));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_NeedPositiveId"), e);

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.OutOfRangeKwdEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => string.Join(Environment.NewLine,
                    GetResourceString("EventSource_IllegalKeywordsValue", "Kwd1", "0x100000000000"),
                    GetResourceString("EventSource_KeywordCollision", "Session3", "Kwd1", "0x100000000000")),
                e);

            if (PlatformDetection.IsWindows)
            {
                e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.EventWithAdminChannelNoMessageEventSource), string.Empty, strictOptions));
                AssertExceptionStringsEqual(() => GetResourceString("EventSource_EventWithAdminChannelMustHaveMessage", "WriteInteger", "Admin"), e);
            }

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.ReservedOpcodeEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => string.Join(Environment.NewLine,
                                     GetResourceString("EventSource_IllegalOpcodeValue", "Op1", 3),
                                     GetResourceString("EventSource_EventMustHaveTaskIfNonDefaultOpcode", "WriteInteger", 1)),
                          e);

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.ReservedOpcodeEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => string.Join(Environment.NewLine,
                                     GetResourceString("EventSource_IllegalOpcodeValue", "Op1", 3),
                                     GetResourceString("EventSource_EventMustHaveTaskIfNonDefaultOpcode", "WriteInteger", 1)),
                          e);

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.EnumKindMismatchEventSource), string.Empty));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_UndefinedKeyword", "0x1", "WriteInteger"), e);

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.EnumKindMismatchEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => string.Join(Environment.NewLine,
                                     GetResourceString("EventSource_EnumKindMismatch", "EventKeywords", "Opcodes"),
                                     GetResourceString("EventSource_UndefinedKeyword", "0x1", "WriteInteger")),
                          e);

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.EnumKindMismatchEventSource), string.Empty, EventManifestOptions.AllowEventSourceOverride));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_UndefinedKeyword", "0x1", "WriteInteger"), e);

            Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.MismatchIdEventSource), string.Empty));

            // These tests require the IL to be present for inspection.
            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.MismatchIdEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_MismatchIdToWriteEvent", "WriteInteger", 10, 1), e);

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.MismatchIdEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_MismatchIdToWriteEvent", "WriteInteger", 10, 1), e);

            Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.EventIdReusedEventSource), string.Empty));

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.EventIdReusedEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => string.Join(Environment.NewLine,
                                     GetResourceString("EventSource_EventIdReused", "WriteInteger2", 1),
                                     GetResourceString("EventSource_TaskOpcodePairReused", "WriteInteger2", 1, "WriteInteger1", 1)),
                          e);


            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.EventIdReusedEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => string.Join(Environment.NewLine,
                                     GetResourceString("EventSource_EventIdReused", "WriteInteger2", 1),
                                     GetResourceString("EventSource_TaskOpcodePairReused", "WriteInteger2", 1, "WriteInteger1", 1)),
                          e);

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.EventNameReusedEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_EventNameReused", "WriteInteger"), e);

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.EventNameReusedEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_EventNameReused", "WriteInteger"), e);

            Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.TaskOpcodePairReusedEventSource), string.Empty));

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.TaskOpcodePairReusedEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_TaskOpcodePairReused", "WriteInteger2", 2, "WriteInteger1", 1), e);

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.TaskOpcodePairReusedEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_TaskOpcodePairReused", "WriteInteger2", 2, "WriteInteger1", 1), e);

            Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.EventWithOpcodeNoTaskEventSource), string.Empty));

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.EventWithOpcodeNoTaskEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_EventMustHaveTaskIfNonDefaultOpcode", "WriteInteger", 1), e);

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.EventWithOpcodeNoTaskEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_EventMustHaveTaskIfNonDefaultOpcode", "WriteInteger", 1), e);

            Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.EventWithInvalidMessageEventSource), string.Empty));

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.EventWithInvalidMessageEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_UnsupportedMessageProperty", "WriteString", "Message = {0,12:G}"), e);

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.AbstractWithKwdTaskOpcodeEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => string.Join(Environment.NewLine,
                                     GetResourceString("EventSource_AbstractMustNotDeclareKTOC", "Keywords"),
                                     GetResourceString("EventSource_AbstractMustNotDeclareKTOC", "Tasks"),
                                     GetResourceString("EventSource_AbstractMustNotDeclareKTOC", "Opcodes")),
                          e);

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.AbstractWithKwdTaskOpcodeEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => string.Join(Environment.NewLine,
                                     GetResourceString("EventSource_AbstractMustNotDeclareKTOC", "Keywords"),
                                     GetResourceString("EventSource_AbstractMustNotDeclareKTOC", "Tasks"),
                                     GetResourceString("EventSource_AbstractMustNotDeclareKTOC", "Opcodes")),
                          e);

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.AbstractWithEventsEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_AbstractMustNotDeclareEventMethods", "WriteInteger", 1), e);

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.AbstractWithEventsEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_AbstractMustNotDeclareEventMethods", "WriteInteger", 1), e);

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.ImplementsInterfaceEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_EventMustNotBeExplicitImplementation", "SdtEventSources.ILogging.Error", 1), e);

            e = AssertExtensions.Throws<ArgumentException>(null, () => EventSource.GenerateManifest(typeof(Sdt.ImplementsInterfaceEventSource), string.Empty, strictOptions));
            AssertExceptionStringsEqual(() => GetResourceString("EventSource_EventMustNotBeExplicitImplementation", "SdtEventSources.ILogging.Error", 1), e);

            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }
    }
}
