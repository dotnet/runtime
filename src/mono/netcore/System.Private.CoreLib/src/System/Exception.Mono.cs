// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
	[StructLayout (LayoutKind.Sequential)]
	partial class Exception
	{
		internal readonly struct DispatchState
		{
			public readonly MonoStackFrame[] StackFrames;

			public DispatchState (MonoStackFrame[] stackFrames)
			{
				StackFrames = stackFrames;
			}
		}

		# region Keep in sync with MonoException in object-internals.h
		string? _unused1;
		internal string _message;
		IDictionary _data;
		Exception _innerException;
		string _helpURL;
		object _traceIPs;
		string? _stackTraceString;
		string? _remoteStackTraceString;
		int _unused4;
		object _dynamicMethods; // Dynamic methods referenced by the stack trace
		int _HResult;
		string _source;
		object? _unused6;
		internal MonoStackFrame[] foreignExceptionsFrames;
		IntPtr[] native_trace_ips;
		int caught_in_unmanaged;
		#endregion

		public MethodBase? TargetSite {
			get {
				StackTrace st = new StackTrace (this, true);
				if (st.FrameCount > 0)
					return st.GetFrame (0)?.GetMethod ();

				return null;
			}
		}

		public virtual string? StackTrace => GetStackTrace (true);

		string? GetStackTrace (bool needFileInfo)
		{
			string? stackTraceString = _stackTraceString;
			string? remoteStackTraceString = _remoteStackTraceString;
				
			if (stackTraceString != null)
				return remoteStackTraceString + stackTraceString;
			if (_traceIPs == null)
				return remoteStackTraceString;

			return remoteStackTraceString + new StackTrace (this, needFileInfo).ToString (System.Diagnostics.StackTrace.TraceFormat.Normal);
		}

		internal DispatchState CaptureDispatchState ()
		{
			MonoStackFrame[] stackFrames;

			if (_traceIPs != null) {
				stackFrames = System.Diagnostics.StackTrace.get_trace (this, 0, true);
				stackFrames [stackFrames.Length - 1].isLastFrameFromForeignException = true;

				if (foreignExceptionsFrames != null) {
					var combinedStackFrames = new MonoStackFrame [stackFrames.Length + foreignExceptionsFrames.Length];
					Array.Copy (foreignExceptionsFrames, 0, combinedStackFrames, 0, foreignExceptionsFrames.Length);
					Array.Copy (stackFrames, 0, combinedStackFrames, foreignExceptionsFrames.Length, stackFrames.Length);

					stackFrames = combinedStackFrames;
				}
			} else {
				stackFrames = foreignExceptionsFrames;
			}

			return new DispatchState (stackFrames);
		}

		internal void RestoreDispatchState (in DispatchState state)
		{
			foreignExceptionsFrames = state.StackFrames;

			_stackTraceString = null;
		}

		[StackTraceHidden]
		internal void SetCurrentStackTrace ()
		{
			// Check to see if the exception already has a stack set in it.
			if (_traceIPs != null || _stackTraceString != null || _remoteStackTraceString != null) {
				ThrowHelper.ThrowInvalidOperationException ();
			}

			// Store the current stack trace into the "remote" stack trace, which was originally introduced to support
			// remoting of exceptions cross app-domain boundaries, and is thus concatenated into Exception.StackTrace
			// when it's retrieved.
			var sb = new StringBuilder (256);
			new StackTrace (fNeedFileInfo: true).ToString (System.Diagnostics.StackTrace.TraceFormat.TrailingNewLine, sb);
			sb.AppendLine (SR.Exception_EndStackTraceFromPreviousThrow);
			_remoteStackTraceString = sb.ToString ();
		}

		string? CreateSourceName ()
		{
			var st = new StackTrace (this, fNeedFileInfo: false);
			if (st.FrameCount > 0) {
				StackFrame sf = st.GetFrame (0)!;
				MethodBase method = sf.GetMethod ();

				Module module = method.Module;
				RuntimeModule rtModule = module as RuntimeModule;

				if (rtModule == null) {
					var moduleBuilder = module as System.Reflection.Emit.ModuleBuilder;
					if (moduleBuilder != null)
						throw new NotImplementedException (); // TODO: rtModule = moduleBuilder.InternalModule;
					else
						throw new ArgumentException (SR.Argument_MustBeRuntimeReflectionObject);
				}

				return rtModule.GetRuntimeAssembly ().GetName ().Name; // TODO: GetSimpleName ();
			}

			return null;
		}

		static IDictionary CreateDataContainer () => new ListDictionaryInternal ();

		static string? SerializationWatsonBuckets => null;
		string? SerializationRemoteStackTraceString => _remoteStackTraceString;
		string? SerializationStackTraceString => GetStackTrace (true);
	}
}
