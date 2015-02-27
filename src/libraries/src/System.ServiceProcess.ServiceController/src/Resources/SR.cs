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
        private const string s_resourcesName = "System.ServiceProcess.ServiceController.resources"; // assembly Name + .resources
#pragma warning restore 0414

#if !DEBUGRESOURCES
        internal static string ArgsCantBeNull {
              get { return SR.GetResourceString("ArgsCantBeNull", null); }
        }
        internal static string BadMachineName {
              get { return SR.GetResourceString("BadMachineName", null); }
        }
        internal static string CannotStart {
              get { return SR.GetResourceString("CannotStart", null); }
        }
        internal static string DisplayName {
              get { return SR.GetResourceString("DisplayName", null); }
        }
        internal static string InvalidEnumArgument {
              get { return SR.GetResourceString("InvalidEnumArgument", null); }
        }
        internal static string InvalidParameter {
              get { return SR.GetResourceString("InvalidParameter", null); }
        }
        internal static string NoDisplayName {
              get { return SR.GetResourceString("NoDisplayName", null); }
        }
        internal static string NoMachineName {
              get { return SR.GetResourceString("NoMachineName", null); }
        }
        internal static string NoService {
              get { return SR.GetResourceString("NoService", null); }
        }
        internal static string OpenSC {
              get { return SR.GetResourceString("OpenSC", null); }
        }
        internal static string OpenService {
              get { return SR.GetResourceString("OpenService", null); }
        }
        internal static string PauseService {
              get { return SR.GetResourceString("PauseService", null); }
        }
        internal static string ResumeService {
              get { return SR.GetResourceString("ResumeService", null); }
        }
        internal static string ServiceName {
              get { return SR.GetResourceString("ServiceName", null); }
        }
        internal static string StopService {
              get { return SR.GetResourceString("StopService", null); }
        }
        internal static string Timeout {
              get { return SR.GetResourceString("Timeout", null); }
        }
#else
        internal static string ArgsCantBeNull {
              get { return SR.GetResourceString("ArgsCantBeNull", @"Arguments within the 'args' array passed to Start cannot be null."); }
        }
        internal static string BadMachineName {
              get { return SR.GetResourceString("BadMachineName", @"MachineName value {0} is invalid."); }
        }
        internal static string CannotStart {
              get { return SR.GetResourceString("CannotStart", @"Cannot start service {0} on computer '{1}'."); }
        }
        internal static string DisplayName {
              get { return SR.GetResourceString("DisplayName", @"Display name cannot be null or empty"); }
        }
        internal static string InvalidEnumArgument {
              get { return SR.GetResourceString("InvalidEnumArgument", @"The value of argument '{0}' ({1}) is invalid for Enum type '{2}'."); }
        }
        internal static string InvalidParameter {
              get { return SR.GetResourceString("InvalidParameter", @"Invalid value {1} for parameter {0}."); }
        }
        internal static string NoDisplayName {
              get { return SR.GetResourceString("NoDisplayName", @"Display name could not be retrieved for service {0} on computer '{1}'."); }
        }
        internal static string NoMachineName {
              get { return SR.GetResourceString("NoMachineName", @"MachineName was not set."); }
        }
        internal static string NoService {
              get { return SR.GetResourceString("NoService", @"Service {0} was not found on computer '{1}'."); }
        }
        internal static string OpenSC {
              get { return SR.GetResourceString("OpenSC", @"Cannot open Service Control Manager on computer '{0}'. This operation might require other privileges."); }
        }
        internal static string OpenService {
              get { return SR.GetResourceString("OpenService", @"Cannot open {0} service on computer '{1}'."); }
        }
        internal static string PauseService {
              get { return SR.GetResourceString("PauseService", @"Cannot pause {0} service on computer '{1}'."); }
        }
        internal static string ResumeService {
              get { return SR.GetResourceString("ResumeService", @"Cannot resume {0} service on computer '{1}'."); }
        }
        internal static string ServiceName {
              get { return SR.GetResourceString("ServiceName", @"Service name {0} contains invalid characters, is empty, or is too long (max length = {1})."); }
        }
        internal static string StopService {
              get { return SR.GetResourceString("StopService", @"Cannot stop {0} service on computer '{1}'."); }
        }
        internal static string Timeout {
              get { return SR.GetResourceString("Timeout", @"Time out has expired and the operation has not been completed."); }
        }

#endif
    }
}
