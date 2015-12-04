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
            MethodInfo getResource = typeof(Environment).GetMethods(flags).Where(x => x.Name == "GetResourceString" && x.GetParameters().Count() == 1).First();
            object resource = getResource.Invoke(null, new object[] { key });

            return (string)resource;
        }
        #endregion

        /// <summary>
        /// These tests use the NuGet EventSource to validate *both* NuGet and BCL user-defined EventSources
        /// For NuGet EventSources we validate both "runtime" and "validation" behavior
        /// </summary>
        [Fact]
        public void Test_GenerateManifest_InvalidEventSources()
        {
            lock (TestUtilities.EventSourceTestLock)
            {
                TestUtilities.CheckNoEventSourcesRunning("Start");
                // specify AllowEventSourceOverride - this is needed for Sdt event sources and won't make a difference for Sdt ones
                var strictOptions = EventManifestOptions.Strict | EventManifestOptions.AllowEventSourceOverride;

                Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.UnsealedEventSource), string.Empty));

                ExceptionAssert.Throws<ArgumentException>(GetResourceString("EventSource_TypeMustBeSealedOrAbstract"),
                    () => EventSource.GenerateManifest(typeof(Sdt.UnsealedEventSource), string.Empty, strictOptions));

                ExceptionAssert.Throws<ArgumentException>(GetResourceString("EventSource_TypeMustBeSealedOrAbstract"),
                    () => EventSource.GenerateManifest(typeof(Sdt.UnsealedEventSource), string.Empty, strictOptions));

                // starting with NuGet we allow non-void returning methods as long as they have the [Event] attribute
                Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.EventWithReturnEventSource), string.Empty));

                Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.EventWithReturnEventSource), string.Empty, strictOptions));

                Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.EventWithReturnEventSource), string.Empty, EventManifestOptions.AllowEventSourceOverride));

                ExceptionAssert.Throws<ArgumentException>(GetResourceString("EventSource_NeedPositiveId", "WriteInteger"),
                    () => EventSource.GenerateManifest(typeof(Sdt.NegativeEventIdEventSource), string.Empty));

                ExceptionAssert.Throws<ArgumentException>(GetResourceString("EventSource_NeedPositiveId", "WriteInteger"),
                    () => EventSource.GenerateManifest(typeof(Sdt.NegativeEventIdEventSource), string.Empty, strictOptions));

                ExceptionAssert.Throws<ArgumentException>(GetResourceString("EventSource_NeedPositiveId", "WriteInteger"),
                    () => EventSource.GenerateManifest(typeof(Sdt.NegativeEventIdEventSource), string.Empty, EventManifestOptions.AllowEventSourceOverride));

                // This check was removed from EventSource
                // ExceptionAssert.Throws<ArgumentException>(GetResourceString("EventSource_ReservedKeywords", "WriteInteger"),
                //     () => EventSource.GenerateManifest(typeof(Sdt.OutOfRangeKwdEventSource), string.Empty));

                // ExceptionAssert.Throws<ArgumentException>(
                //     GetResourceString("EventSource_ReservedKeywords", "WriteInteger"),
                //     () => EventSource.GenerateManifest(typeof(Sdt.OutOfRangeKwdEventSource), string.Empty));

                ExceptionAssert.Throws<ArgumentException>(
                    GetResourceString("EventSource_IllegalKeywordsValue", "Kwd1", "0x100000000000"),
                    GetResourceString("EventSource_KeywordCollision", "Session3", "Kwd1", "0x100000000000"),
                    // GetResourceString("EventSource_ReservedKeywords", "WriteInteger"),
                    () => EventSource.GenerateManifest(typeof(Sdt.OutOfRangeKwdEventSource), string.Empty, strictOptions));

#if FEATURE_ADVANCED_MANAGED_ETW_CHANNELS
            ExceptionAssert.Throws<ArgumentException>(GetResourceString("EventSource_MaxChannelExceeded"),
                () => EventSource.GenerateManifest(typeof(Sdt.TooManyChannelsEventSource), string.Empty));
#endif

#if USE_ETW
            ExceptionAssert.Throws<ArgumentException>(GetResourceString("EventSource_EventWithAdminChannelMustHaveMessage", "WriteInteger", "Admin"),
                () => EventSource.GenerateManifest(typeof(Sdt.EventWithAdminChannelNoMessageEventSource), string.Empty, strictOptions));
#endif // USE_ETW

                ExceptionAssert.Throws<ArgumentException>(
                    GetResourceString("EventSource_IllegalOpcodeValue", "Op1", 3),
                    GetResourceString("EventSource_EventMustHaveTaskIfNonDefaultOpcode", "WriteInteger", 1),
                    () => EventSource.GenerateManifest(typeof(Sdt.ReservedOpcodeEventSource), string.Empty, strictOptions));

                ExceptionAssert.Throws<ArgumentException>(
                    GetResourceString("EventSource_IllegalOpcodeValue", "Op1", 3),
                    GetResourceString("EventSource_EventMustHaveTaskIfNonDefaultOpcode", "WriteInteger", 1),
                    () => EventSource.GenerateManifest(typeof(Sdt.ReservedOpcodeEventSource), string.Empty, strictOptions));

                ExceptionAssert.Throws<ArgumentException>(GetResourceString("EventSource_UndefinedKeyword", "0x1", "WriteInteger"),
                    () => EventSource.GenerateManifest(typeof(Sdt.EnumKindMismatchEventSource), string.Empty));

                ExceptionAssert.Throws<ArgumentException>(
                    GetResourceString("EventSource_EnumKindMismatch", "Op1", "EventKeywords", "Opcodes"),
                    GetResourceString("EventSource_UndefinedKeyword", "0x1", "WriteInteger"),
                    () => EventSource.GenerateManifest(typeof(Sdt.EnumKindMismatchEventSource), string.Empty, strictOptions));

                ExceptionAssert.Throws<ArgumentException>(GetResourceString("EventSource_UndefinedKeyword", "0x1", "WriteInteger"),
                    () => EventSource.GenerateManifest(typeof(Sdt.EnumKindMismatchEventSource), string.Empty, EventManifestOptions.AllowEventSourceOverride));

                Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.MismatchIdEventSource), string.Empty));

                ExceptionAssert.Throws<ArgumentException>(GetResourceString("EventSource_MismatchIdToWriteEvent", "WriteInteger", 10, 1),
                    () => EventSource.GenerateManifest(typeof(Sdt.MismatchIdEventSource), string.Empty, strictOptions));

                ExceptionAssert.Throws<ArgumentException>(GetResourceString("EventSource_MismatchIdToWriteEvent", "WriteInteger", 10, 1),
                    () => EventSource.GenerateManifest(typeof(Sdt.MismatchIdEventSource), string.Empty, strictOptions));

                Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.EventIdReusedEventSource), string.Empty));

                ExceptionAssert.Throws<ArgumentException>(
                    GetResourceString("EventSource_EventIdReused", "WriteInteger2", 1, "WriteInteger1"),
                    GetResourceString("EventSource_TaskOpcodePairReused", "WriteInteger2", 1, "WriteInteger1", 1),
                    () => EventSource.GenerateManifest(typeof(Sdt.EventIdReusedEventSource), string.Empty, strictOptions));

                ExceptionAssert.Throws<ArgumentException>(
                    GetResourceString("EventSource_EventIdReused", "WriteInteger2", 1, "WriteInteger1"),
                    GetResourceString("EventSource_TaskOpcodePairReused", "WriteInteger2", 1, "WriteInteger1", 1),
                    () => EventSource.GenerateManifest(typeof(Sdt.EventIdReusedEventSource), string.Empty, strictOptions));

                ExceptionAssert.Throws<ArgumentException>(GetResourceString("EventSource_EventNameReused", "WriteInteger"),
                    () => EventSource.GenerateManifest(typeof(Sdt.EventNameReusedEventSource), string.Empty, strictOptions));

                ExceptionAssert.Throws<ArgumentException>(
                    GetResourceString("EventSource_EventNameReused", "WriteInteger"),
                    () => EventSource.GenerateManifest(typeof(Sdt.EventNameReusedEventSource), string.Empty, strictOptions));

                Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.TaskOpcodePairReusedEventSource), string.Empty));

                ExceptionAssert.Throws<ArgumentException>(GetResourceString("EventSource_TaskOpcodePairReused", "WriteInteger2", 2, "WriteInteger1", 1),
                    () => EventSource.GenerateManifest(typeof(Sdt.TaskOpcodePairReusedEventSource), string.Empty, strictOptions));

                ExceptionAssert.Throws<ArgumentException>(GetResourceString("EventSource_TaskOpcodePairReused", "WriteInteger2", 2, "WriteInteger1", 1),
                    () => EventSource.GenerateManifest(typeof(Sdt.TaskOpcodePairReusedEventSource), string.Empty, strictOptions));

                Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.EventWithOpcodeNoTaskEventSource), string.Empty));

                ExceptionAssert.Throws<ArgumentException>(GetResourceString("EventSource_EventMustHaveTaskIfNonDefaultOpcode", "WriteInteger", 1),
                    () => EventSource.GenerateManifest(typeof(Sdt.EventWithOpcodeNoTaskEventSource), string.Empty, strictOptions));

                ExceptionAssert.Throws<ArgumentException>(GetResourceString("EventSource_EventMustHaveTaskIfNonDefaultOpcode", "WriteInteger", 1),
                    () => EventSource.GenerateManifest(typeof(Sdt.EventWithOpcodeNoTaskEventSource), string.Empty, strictOptions));

                Assert.NotNull(EventSource.GenerateManifest(typeof(Sdt.EventWithInvalidMessageEventSource), string.Empty));

                ExceptionAssert.Throws<ArgumentException>(GetResourceString("EventSource_UnsupportedMessageProperty", "WriteString", "Message = {0,12:G}"),
                    () => EventSource.GenerateManifest(typeof(Sdt.EventWithInvalidMessageEventSource), string.Empty, strictOptions));

#if FALSE
            // some checks are only performed when an event source instance is actually initialized and
            // it attempts to send its manifest... for that you need something like:
            ExceptionAssert.Throws<ArgumentException>(GetResourceString("EventSource_MismatchIdToWriteEvent", "WriteInteger", 10, 1),
                () => 
                {
                    using (var es = new Sdt.MismatchIdEventSource())
                    using (var el = new LoudListener()) 
                    { /* OnCreateSource calls: el.EnableEvents(es, EventLevel.Verbose); */ }
                });
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); // ensure event source is collected
#endif

                ExceptionAssert.Throws<ArgumentException>(
                    GetResourceString("EventSource_AbstractMustNotDeclareKTOC", "Keywords"),
                    GetResourceString("EventSource_AbstractMustNotDeclareKTOC", "Tasks"),
                    GetResourceString("EventSource_AbstractMustNotDeclareKTOC", "Opcodes"),
                    () => EventSource.GenerateManifest(typeof(Sdt.AbstractWithKwdTaskOpcodeEventSource), string.Empty, strictOptions));

                ExceptionAssert.Throws<ArgumentException>(
                    GetResourceString("EventSource_AbstractMustNotDeclareKTOC", "Keywords"),
                    GetResourceString("EventSource_AbstractMustNotDeclareKTOC", "Tasks"),
                    GetResourceString("EventSource_AbstractMustNotDeclareKTOC", "Opcodes"),
                    () => EventSource.GenerateManifest(typeof(Sdt.AbstractWithKwdTaskOpcodeEventSource), string.Empty, strictOptions));

                ExceptionAssert.Throws<ArgumentException>(
                    GetResourceString("EventSource_AbstractMustNotDeclareEventMethods", "WriteInteger", 1),
                    () => EventSource.GenerateManifest(typeof(Sdt.AbstractWithEventsEventSource), string.Empty, strictOptions));

                ExceptionAssert.Throws<ArgumentException>(
                    GetResourceString("EventSource_AbstractMustNotDeclareEventMethods", "WriteInteger", 1),
                    () => EventSource.GenerateManifest(typeof(Sdt.AbstractWithEventsEventSource), string.Empty, strictOptions));

                ExceptionAssert.Throws<ArgumentException>(
                    GetResourceString("EventSource_EventMustNotBeExplicitImplementation", "SdtEventSources.ILogging.Error", 1),
                    () => EventSource.GenerateManifest(typeof(Sdt.ImplementsInterfaceEventSource), string.Empty, strictOptions));

                ExceptionAssert.Throws<ArgumentException>(
                    GetResourceString("EventSource_EventMustNotBeExplicitImplementation", "SdtEventSources.ILogging.Error", 1),
                    () => EventSource.GenerateManifest(typeof(Sdt.ImplementsInterfaceEventSource), string.Empty, strictOptions));
                TestUtilities.CheckNoEventSourcesRunning("Stop");
            }
        }
    }
}
