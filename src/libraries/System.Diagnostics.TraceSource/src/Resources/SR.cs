// This is auto generated file. Please don’t modify manually.
// The file is generated as part of the build through the ResourceGenerator tool 
// which takes the project resx resource file and generated this source code file.
// By default the tool will use Resources\Strings.resx but projects can customize
// that by overriding the StringResourcesPath property group.
namespace System
{
    internal static partial class SR
    {
#pragma warning disable 0414
        private const string s_resourcesName = "System.Diagnostics.TraceSource.resources"; // assembly Name + .resources
#pragma warning restore 0414

#if !DEBUGRESOURCES
        internal static string MustAddListener {
              get { return SR.GetResourceString("MustAddListener", null); }
        }
        internal static string IndentSize {
              get { return SR.GetResourceString("IndentSize", null); }
        }
        internal static string TraceListenerFail {
              get { return SR.GetResourceString("TraceListenerFail", null); }
        }
        internal static string TraceListenerIndentSize {
              get { return SR.GetResourceString("TraceListenerIndentSize", null); }
        }
        internal static string DebugAssertBanner {
              get { return SR.GetResourceString("DebugAssertBanner", null); }
        }
        internal static string DebugAssertShortMessage {
              get { return SR.GetResourceString("DebugAssertShortMessage", null); }
        }
        internal static string DebugAssertLongMessage {
              get { return SR.GetResourceString("DebugAssertLongMessage", null); }
        }
        internal static string ExceptionOccurred {
              get { return SR.GetResourceString("ExceptionOccurred", null); }
        }
        internal static string TraceSwitchLevelTooLow {
              get { return SR.GetResourceString("TraceSwitchLevelTooLow", null); }
        }
        internal static string TraceSwitchInvalidLevel {
              get { return SR.GetResourceString("TraceSwitchInvalidLevel", null); }
        }
        internal static string TraceSwitchLevelTooHigh {
              get { return SR.GetResourceString("TraceSwitchLevelTooHigh", null); }
        }
        internal static string InvalidNullEmptyArgument {
              get { return SR.GetResourceString("InvalidNullEmptyArgument", null); }
        }
        internal static string DebugAssertTitle {
              get { return SR.GetResourceString("DebugAssertTitle", null); }
        }
        internal static string RTL {
              get { return SR.GetResourceString("RTL", null); }
        }
#else
        internal static string MustAddListener {
              get { return SR.GetResourceString("MustAddListener", @"Only TraceListeners can be added to a TraceListenerCollection."); }
        }
        internal static string IndentSize {
              get { return SR.GetResourceString("IndentSize", @"The IndentSize property must be non-negative."); }
        }
        internal static string TraceListenerFail {
              get { return SR.GetResourceString("TraceListenerFail", @"Fail:"); }
        }
        internal static string TraceListenerIndentSize {
              get { return SR.GetResourceString("TraceListenerIndentSize", @"The IndentSize property must be non-negative."); }
        }
        internal static string DebugAssertBanner {
              get { return SR.GetResourceString("DebugAssertBanner", @"---- DEBUG ASSERTION FAILED ----"); }
        }
        internal static string DebugAssertShortMessage {
              get { return SR.GetResourceString("DebugAssertShortMessage", @"---- Assert Short Message ----"); }
        }
        internal static string DebugAssertLongMessage {
              get { return SR.GetResourceString("DebugAssertLongMessage", @"---- Assert Long Message ----"); }
        }
        internal static string ExceptionOccurred {
              get { return SR.GetResourceString("ExceptionOccurred", @"An exception occurred writing trace output to log file '{0}'. {1}"); }
        }
        internal static string TraceSwitchLevelTooLow {
              get { return SR.GetResourceString("TraceSwitchLevelTooLow", @"Attempted to set {0} to a value that is too low.  Setting level to TraceLevel.Off"); }
        }
        internal static string TraceSwitchInvalidLevel {
              get { return SR.GetResourceString("TraceSwitchInvalidLevel", @"The Level must be set to a value in the enumeration TraceLevel."); }
        }
        internal static string TraceSwitchLevelTooHigh {
              get { return SR.GetResourceString("TraceSwitchLevelTooHigh", @"Attempted to set {0} to a value that is too high.  Setting level to TraceLevel.Verbose"); }
        }
        internal static string InvalidNullEmptyArgument {
              get { return SR.GetResourceString("InvalidNullEmptyArgument", @"Argument {0} cannot be null or zero-length."); }
        }
        internal static string DebugAssertTitle {
              get { return SR.GetResourceString("DebugAssertTitle", @"Assertion Failed: Cancel=Debug, OK=Continue"); }
        }
        internal static string RTL {
              get { return SR.GetResourceString("RTL", @"RTL_False"); }
        }

#endif
    }
}
