//
// Mono Microsoft Error Reporting API
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mono {

#if MOBILE || XAMMAC_4_5
	public
#endif
	static class Runtime
	{
		static object exception_capture = new object ();

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern string ExceptionToState_internal (Exception exc, out ulong portable_hash, out ulong unportable_hash);

		static Tuple<String, ulong, ulong>
		ExceptionToState (Exception exc)
		{
			ulong portable_hash;
			ulong unportable_hash;
			string payload_str = ExceptionToState_internal (exc, out portable_hash, out unportable_hash);

			return new Tuple<String, ulong, ulong> (payload_str, portable_hash, unportable_hash);
		}


#if !MOBILE 
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern void DisableMicrosoftTelemetry ();

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern void EnableMicrosoftTelemetry_internal (IntPtr appBundleID, IntPtr appSignature, IntPtr appVersion, IntPtr merpGUIPath, IntPtr appPath, IntPtr configDir);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern void SendMicrosoftTelemetry_internal (IntPtr payload, ulong portable_hash, ulong unportable_hash);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern void WriteStateToFile_internal (IntPtr payload, ulong portable_hash, ulong unportable_hash);

		static void
		WriteStateToFile (Exception exc)
		{
			ulong portable_hash;
			ulong unportable_hash;
			string payload_str = ExceptionToState_internal (exc, out portable_hash, out unportable_hash);
			using (var payload_chars = RuntimeMarshal.MarshalString (payload_str))
			{
				WriteStateToFile_internal (payload_chars.Value, portable_hash, unportable_hash);
			}
		}

		static void SendMicrosoftTelemetry (string payload_str, ulong portable_hash, ulong unportable_hash)
		{
			if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX)) {
				using (var payload_chars = RuntimeMarshal.MarshalString (payload_str))
				{
					SendMicrosoftTelemetry_internal (payload_chars.Value, portable_hash, unportable_hash);
				}
			} else {
				throw new PlatformNotSupportedException("Merp support is currently only supported on OSX.");
			}
		}

		// Usage: 
		//
		// catch (Exception exc) {
		//   var monoType = Type.GetType ("Mono.Runtime", false);
		//   var m = monoType.GetMethod("SendExceptionToTelemetry", BindingFlags.NonPublic | BindingFlags.Static);
		//   m.Invoke(null, new object[] { exc });
		// }
		static void SendExceptionToTelemetry (Exception exc)
		{
			ulong portable_hash;
			ulong unportable_hash;
			lock (exception_capture)
			{
				string payload_str = ExceptionToState_internal (exc, out portable_hash, out unportable_hash);
				SendMicrosoftTelemetry (payload_str, portable_hash, unportable_hash);
			}
		}

		// All must be set except for configDir_str
		static void EnableMicrosoftTelemetry (string appBundleID_str, string appSignature_str, string appVersion_str, string merpGUIPath_str, string unused /* eventType_str */, string appPath_str, string configDir_str)
		{
			if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX)) {
				using (var appBundleID_chars = RuntimeMarshal.MarshalString (appBundleID_str))
				using (var appSignature_chars = RuntimeMarshal.MarshalString (appSignature_str))
				using (var appVersion_chars = RuntimeMarshal.MarshalString (appVersion_str))
				using (var merpGUIPath_chars = RuntimeMarshal.MarshalString (merpGUIPath_str))
				using (var appPath_chars = RuntimeMarshal.MarshalString (appPath_str))
				using (var configDir_chars = RuntimeMarshal.MarshalString (configDir_str))
				{
					EnableMicrosoftTelemetry_internal (appBundleID_chars.Value, appSignature_chars.Value, appVersion_chars.Value, merpGUIPath_chars.Value, appPath_chars.Value, configDir_chars.Value);
				}
			} else {
				throw new PlatformNotSupportedException("Merp support is currently only supported on OSX.");
			}
		}
#endif

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern string DumpStateSingle_internal (out ulong portable_hash, out ulong unportable_hash);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern string DumpStateTotal_internal (out ulong portable_hash, out ulong unportable_hash);

		static Tuple<String, ulong, ulong>
		DumpStateSingle ()
		{
			ulong portable_hash;
			ulong unportable_hash;
			string payload_str = DumpStateSingle_internal (out portable_hash, out unportable_hash);

			return new Tuple<String, ulong, ulong> (payload_str, portable_hash, unportable_hash);
		}

		static Tuple<String, ulong, ulong>
		DumpStateTotal ()
		{
			ulong portable_hash;
			ulong unportable_hash;
			string payload_str = DumpStateTotal_internal (out portable_hash, out unportable_hash);

			return new Tuple<String, ulong, ulong> (payload_str, portable_hash, unportable_hash);
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern void RegisterReportingForAllNativeLibs_internal ();

		static void RegisterReportingForAllNativeLibs ()
		{
			RegisterReportingForAllNativeLibs_internal ();
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern void RegisterReportingForNativeLib_internal (IntPtr modulePathSuffix, IntPtr moduleName);

		static void RegisterReportingForNativeLib (string modulePathSuffix_str, string moduleName_str)
		{
			using (var modulePathSuffix_chars = RuntimeMarshal.MarshalString (modulePathSuffix_str))
			using (var moduleName_chars = RuntimeMarshal.MarshalString (moduleName_str))
			{
				RegisterReportingForNativeLib_internal (modulePathSuffix_chars.Value, moduleName_chars.Value);
			}
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern void EnableCrashReportLog_internal (IntPtr directory);

		static void EnableCrashReportLog (string directory_str)
		{
			using (var directory_chars = RuntimeMarshal.MarshalString (directory_str))
			{
				EnableCrashReportLog_internal (directory_chars.Value);
			}
		}

		enum CrashReportLogLevel : int {
			MonoSummaryNone = 0,
			MonoSummarySetup,
			MonoSummarySuspendHandshake,
			MonoSummaryUnmanagedStacks,
			MonoSummaryManagedStacks,
			MonoSummaryStateWriter,
			MonoSummaryStateWriterDone,
			MonoSummaryMerpWriter,
			MonoSummaryMerpInvoke,
			MonoSummaryCleanup,
			MonoSummaryDone,

			MonoSummaryDoubleFault
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern int CheckCrashReportLog_internal (IntPtr directory, bool clear);

		static CrashReportLogLevel CheckCrashReportLog (string directory_str, bool clear)
		{
			using (var directory_chars = RuntimeMarshal.MarshalString (directory_str))
			{
				return (CrashReportLogLevel) CheckCrashReportLog_internal (directory_chars.Value, clear);
			}
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern void AnnotateMicrosoftTelemetry_internal (IntPtr key, IntPtr val);

		static void AnnotateMicrosoftTelemetry (string key, string val)
		{
			using (var key_chars = RuntimeMarshal.MarshalString (key))
			using (var val_chars = RuntimeMarshal.MarshalString (val))
			{
				AnnotateMicrosoftTelemetry_internal (key_chars.Value, val_chars.Value);
			}
		}
	}
}
