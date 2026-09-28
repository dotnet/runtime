// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;

namespace System.Threading
{
    public partial class EventWaitHandle : WaitHandle
    {
        public EventWaitHandle(bool initialState, EventResetMode mode)
        {
            CreateEventCore(initialState, mode);
        }

        /// <summary>
        /// Creates a named or unnamed event, or opens a named event if a event with the name already exists.
        /// </summary>
        /// <param name="initialState">True to initially set the event to a signaled state; false otherwise.</param>
        /// <param name="mode">Indicates whether the event resets automatically or manually.</param>
        /// <param name="name">
        /// The name, if the event is to be shared with other processes; otherwise, null or an empty string.
        /// </param>
        /// <param name="options">
        /// Options for the named event. Defaulted options, such as when passing 'options: default' in C#, are
        /// 'CurrentUserOnly = true' and 'CurrentSessionOnly = true'. For more information, see 'NamedWaitHandleOptions'. The
        /// specified options may affect the namespace for the name, and access to the underlying event object.
        /// </param>
        public EventWaitHandle(bool initialState, EventResetMode mode, string? name, NamedWaitHandleOptions options)
        {
            CreateEventCore(initialState, mode, name, new(options), out _);
        }

        public EventWaitHandle(bool initialState, EventResetMode mode, string? name)
        {
            CreateEventCore(initialState, mode, name, options: default, out _);
        }

        /// <summary>
        /// Creates a named or unnamed event, or opens a named event if a event with the name already exists.
        /// </summary>
        /// <param name="initialState">True to initially set the event to a signaled state; false otherwise.</param>
        /// <param name="mode">Indicates whether the event resets automatically or manually.</param>
        /// <param name="name">
        /// The name, if the event is to be shared with other processes; otherwise, null or an empty string.
        /// </param>
        /// <param name="options">
        /// Options for the named event. Defaulted options, such as when passing 'options: default' in C#, are
        /// 'CurrentUserOnly = true' and 'CurrentSessionOnly = true'. For more information, see 'NamedWaitHandleOptions'. The
        /// specified options may affect the namespace for the name, and access to the underlying event object.
        /// </param>
        /// <param name="createdNew">
        /// True if the event was created; false if an existing named event was opened.
        /// </param>
        public EventWaitHandle(bool initialState, EventResetMode mode, string? name, NamedWaitHandleOptions options, out bool createdNew)
        {
            CreateEventCore(initialState, mode, name, new(options), out createdNew);
        }

        public EventWaitHandle(bool initialState, EventResetMode mode, string? name, out bool createdNew)
        {
            CreateEventCore(initialState, mode, name, options: default, out createdNew);
        }

        private static void ValidateMode(EventResetMode mode)
        {
            if (mode != EventResetMode.AutoReset && mode != EventResetMode.ManualReset)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(mode));
            }
        }

        /// <summary>
        /// Opens an existing named event.
        /// </summary>
        /// <param name="name">The name of the event to be shared with other processes.</param>
        /// <param name="options">
        /// Options for the named event. Defaulted options, such as when passing 'options: default' in C#, are
        /// 'CurrentUserOnly = true' and 'CurrentSessionOnly = true'. For more information, see 'NamedWaitHandleOptions'. The
        /// specified options may affect the namespace for the name, and access to the underlying event object.
        /// </param>
        /// <returns>An object that represents the named event.</returns>
        [SupportedOSPlatform("windows")]
        public static EventWaitHandle OpenExisting(string name, NamedWaitHandleOptions options)
        {
            OpenExistingResult openExistingResult = OpenExistingWorker(name, new(options), out EventWaitHandle? result);
            if (openExistingResult != OpenExistingResult.Success)
            {
                ThrowForOpenExistingFailure(openExistingResult, name);
            }

            Debug.Assert(result != null, "result should be non-null on success");
            return result;
        }

        [SupportedOSPlatform("windows")]
        public static EventWaitHandle OpenExisting(string name)
        {
            OpenExistingResult openExistingResult = OpenExistingWorker(name, options: default, out EventWaitHandle? result);
            if (openExistingResult != OpenExistingResult.Success)
            {
                ThrowForOpenExistingFailure(openExistingResult, name);
            }

            Debug.Assert(result != null, "result should be non-null on success");
            return result;
        }

        [DoesNotReturn]
        private static void ThrowForOpenExistingFailure(OpenExistingResult openExistingResult, string name)
        {
            Debug.Assert(openExistingResult != OpenExistingResult.Success);

            switch (openExistingResult)
            {
                case OpenExistingResult.NameNotFound:
                    throw new WaitHandleCannotBeOpenedException();
                case OpenExistingResult.NameInvalid:
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));
                case OpenExistingResult.PathNotFound:
                    throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, name));
                default:
                    Debug.Assert(openExistingResult == OpenExistingResult.ObjectIncompatibleWithCurrentUserOnly);
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.NamedWaitHandles_ExistingObjectIncompatibleWithCurrentUserOnly, name));
            }
        }

        /// <summary>
        /// Tries to open an existing named event and returns a value indicating whether it was successful.
        /// </summary>
        /// <param name="name">The name of the event to be shared with other processes.</param>
        /// <param name="options">
        /// Options for the named event. Defaulted options, such as when passing 'options: default' in C#, are
        /// 'CurrentUserOnly = true' and 'CurrentSessionOnly = true'. For more information, see 'NamedWaitHandleOptions'. The
        /// specified options may affect the namespace for the name, and access to the underlying event object.
        /// </param>
        /// <param name="result">
        /// An object that represents the named event if the method returns true; otherwise, null.
        /// </param>
        /// <returns>True if the named event was opened successfully; otherwise, false.</returns>
        [SupportedOSPlatform("windows")]
        public static bool TryOpenExisting(string name, NamedWaitHandleOptions options, [NotNullWhen(true)] out EventWaitHandle? result) =>
            OpenExistingWorker(name, new(options), out result!) == OpenExistingResult.Success;

        [SupportedOSPlatform("windows")]
        public static bool TryOpenExisting(string name, [NotNullWhen(true)] out EventWaitHandle? result) =>
            OpenExistingWorker(name, options: default, out result!) == OpenExistingResult.Success;
    }
}
