// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    /// <devdoc>
    ///     A set of values used to specify a process to start.  This is
    ///     used in conjunction with the <see cref='System.Diagnostics.Process'/>
    ///     component.
    /// </devdoc>
    [DebuggerDisplay("FileName = {FileName}, Arguments = {BuildArguments()}, WorkingDirectory = {WorkingDirectory}")]
    public sealed partial class ProcessStartInfo
    {
        private string? _fileName;
        private string? _arguments;
        private string? _directory;
        private string? _userName;
        private string? _verb;
        private Collection<string>? _argumentList;
        private ProcessWindowStyle _windowStyle;

        internal DictionaryWrapper? _environmentVariables;

        /// <devdoc>
        ///     Default constructor.  At least the <see cref='System.Diagnostics.ProcessStartInfo.FileName'/>
        ///     property must be set before starting the process.
        /// </devdoc>
        public ProcessStartInfo()
        {
        }

        /// <devdoc>
        ///     Specifies the name of the application or document that is to be started.
        /// </devdoc>
        public ProcessStartInfo(string fileName)
        {
            _fileName = fileName;
        }

        /// <devdoc>
        ///     Specifies the name of the application that is to be started, as well as a set
        ///     of command line arguments to pass to the application.
        /// </devdoc>
        public ProcessStartInfo(string fileName, string? arguments)
        {
            _fileName = fileName;
            _arguments = arguments;
        }

        /// <summary>
        /// Specifies the name of the application that is to be started, as well as a set
        /// of command line arguments to pass to the application.
        /// </summary>
        public ProcessStartInfo(string fileName, IEnumerable<string> arguments)
        {
            ArgumentNullException.ThrowIfNull(fileName);
            ArgumentNullException.ThrowIfNull(arguments);

            _fileName = fileName;
            _argumentList = new Collection<string>(new List<string>(arguments));
        }

        /// <devdoc>
        ///     Specifies the set of command line arguments to use when starting the application.
        /// </devdoc>
        [AllowNull]
        public string Arguments
        {
            get => _arguments ?? string.Empty;
            set => _arguments = value;
        }

        public Collection<string> ArgumentList => _argumentList ??= new Collection<string>();

        internal bool HasArgumentList => _argumentList is not null && _argumentList.Count != 0;

        public bool CreateNoWindow { get; set; }

        [Editor("System.Diagnostics.Design.StringDictionaryEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public StringDictionary EnvironmentVariables => new StringDictionaryWrapper((Environment as DictionaryWrapper)!);

        public IDictionary<string, string?> Environment
        {
            get
            {
                if (_environmentVariables == null)
                {
                    IDictionary envVars = System.Environment.GetEnvironmentVariables();

                    _environmentVariables = new DictionaryWrapper(new Dictionary<string, string?>(
                        envVars.Count,
                        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal));

                    // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
                    IDictionaryEnumerator e = envVars.GetEnumerator();
                    Debug.Assert(!(e is IDisposable), "Environment.GetEnvironmentVariables should not be IDisposable.");
                    while (e.MoveNext())
                    {
                        DictionaryEntry entry = e.Entry;
                        _environmentVariables.Add((string)entry.Key, (string?)entry.Value);
                    }
                }
                return _environmentVariables;
            }
        }

        public bool RedirectStandardInput { get; set; }
        public bool RedirectStandardOutput { get; set; }
        public bool RedirectStandardError { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the process should be started in a detached manner.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Starts a new detached process with standard input, output, and error redirected to the null device
        /// (<c>NUL</c> on Windows, <c>/dev/null</c> on Unix) unless explicitly configured by the user with
        /// <see cref="RedirectStandardInput"/>, <see cref="RedirectStandardOutput"/>, <see cref="RedirectStandardError"/>,
        /// <see cref="StandardInputHandle"/>, <see cref="StandardOutputHandle"/>, or <see cref="StandardErrorHandle"/>.
        /// </para>
        /// <para>
        /// On Windows, the process is started with the
        /// <see href="https://learn.microsoft.com/windows/win32/procthread/process-creation-flags">DETACHED_PROCESS</see> flag.
        /// </para>
        /// <para>
        /// On Unix, the process is started as a leader of a new session.
        /// </para>
        /// <para>
        /// This property cannot be used together with <see cref="UseShellExecute"/> set to <see langword="true"/>.
        /// </para>
        /// </remarks>
        public bool StartDetached { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="SafeFileHandle"/> that will be used as the standard input of the child process.
        /// When set, the handle is passed directly to the child process and <see cref="RedirectStandardInput"/> must be <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The handle does not need to be inheritable; the runtime will duplicate it as inheritable if needed.
        /// </para>
        /// <para>
        /// Use <see cref="SafeFileHandle.CreateAnonymousPipe"/> to create a pair of connected pipe handles,
        /// <see cref="System.IO.File.OpenHandle"/> to open a file handle,
        /// <see cref="System.IO.File.OpenNullHandle"/> to provide an empty input,
        /// or <see cref="System.Console.OpenStandardInputHandle"/> to inherit the parent's standard input
        /// (the default behavior when this property is <see langword="null"/>).
        /// </para>
        /// <para>
        /// It's recommended to dispose the handle right after starting the process.
        /// </para>
        /// <para>
        /// This property cannot be used together with <see cref="RedirectStandardInput"/>
        /// and requires <see cref="UseShellExecute"/> to be <see langword="false"/>.
        /// </para>
        /// </remarks>
        /// <value>A <see cref="SafeFileHandle"/> to use as the standard input handle of the child process, or <see langword="null"/> to use the default behavior.</value>
        public SafeFileHandle? StandardInputHandle { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="SafeFileHandle"/> that will be used as the standard output of the child process.
        /// When set, the handle is passed directly to the child process and <see cref="RedirectStandardOutput"/> must be <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The handle does not need to be inheritable; the runtime will duplicate it as inheritable if needed.
        /// </para>
        /// <para>
        /// Use <see cref="SafeFileHandle.CreateAnonymousPipe"/> to create a pair of connected pipe handles,
        /// <see cref="System.IO.File.OpenHandle"/> to open a file handle,
        /// <see cref="System.IO.File.OpenNullHandle"/> to discard output,
        /// or <see cref="System.Console.OpenStandardOutputHandle"/> to inherit the parent's standard output
        /// (the default behavior when this property is <see langword="null"/>).
        /// </para>
        /// <para>
        /// It's recommended to dispose the handle right after starting the process.
        /// </para>
        /// <para>
        /// This property cannot be used together with <see cref="RedirectStandardOutput"/>
        /// and requires <see cref="UseShellExecute"/> to be <see langword="false"/>.
        /// </para>
        /// </remarks>
        /// <value>A <see cref="SafeFileHandle"/> to use as the standard output handle of the child process, or <see langword="null"/> to use the default behavior.</value>
        public SafeFileHandle? StandardOutputHandle { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="SafeFileHandle"/> that will be used as the standard error of the child process.
        /// When set, the handle is passed directly to the child process and <see cref="RedirectStandardError"/> must be <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The handle does not need to be inheritable; the runtime will duplicate it as inheritable if needed.
        /// </para>
        /// <para>
        /// Use <see cref="SafeFileHandle.CreateAnonymousPipe"/> to create a pair of connected pipe handles,
        /// <see cref="System.IO.File.OpenHandle"/> to open a file handle,
        /// <see cref="System.IO.File.OpenNullHandle"/> to discard error output,
        /// or <see cref="System.Console.OpenStandardErrorHandle"/> to inherit the parent's standard error
        /// (the default behavior when this property is <see langword="null"/>).
        /// </para>
        /// <para>
        /// It's recommended to dispose the handle right after starting the process.
        /// </para>
        /// <para>
        /// This property cannot be used together with <see cref="RedirectStandardError"/>
        /// and requires <see cref="UseShellExecute"/> to be <see langword="false"/>.
        /// </para>
        /// </remarks>
        /// <value>A <see cref="SafeFileHandle"/> to use as the standard error handle of the child process, or <see langword="null"/> to use the default behavior.</value>
        public SafeFileHandle? StandardErrorHandle { get; set; }

        /// <summary>
        /// Gets or sets a list of handles that will be inherited by the child process.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When this property is not <see langword="null"/>, handle inheritance is restricted to the standard handles
        /// and the handles from this list. If the list is empty, only the standard handles are inherited.
        /// </para>
        /// <para>
        /// Only <see cref="SafeFileHandle"/> and <see cref="SafePipeHandle"/> are supported in this list.
        /// </para>
        /// <para>
        /// Setting this property on Unix systems that do not have native support for controlling handle inheritance can severely degrade process start performance.
        /// </para>
        /// <para>
        /// Handles in this list should not have inheritance enabled beforehand.
        /// If they do, they could be unintentionally inherited by other processes started concurrently with different APIs,
        /// which may lead to security or resource management issues.
        /// </para>
        /// <para>
        /// Two concurrent process starts that pass same handle in <see cref="InheritedHandles"/>
        /// are not supported. The implementation temporarily modifies each handle's inheritance flags and this is not thread-safe across concurrent starts sharing the same handle.
        /// </para>
        /// <para>
        /// This API can't be used together with <see cref="UseShellExecute"/> set to <see langword="true"/> or when specifying a user name via the <see cref="UserName"/> property.
        /// </para>
        /// <para>
        /// On Windows, the implementation will temporarily enable inheritance on each handle in this list
        /// by modifying the handle's flags using <see href="https://learn.microsoft.com/windows/win32/api/handleapi/nf-handleapi-sethandleinformation">SetHandleInformation</see>.
        /// After the child process is created, inheritance will be unconditionally disabled on these handles to prevent them
        /// from being inherited by other processes started with different APIs.
        /// The handles themselves are not duplicated; they are made inheritable and passed to the child process.
        /// </para>
        /// <para>
        /// On Unix, the implementation will modify each file descriptor in the child process
        /// by removing the FD_CLOEXEC flag. This modification occurs after the fork and before the exec,
        /// so it does not affect the parent process.
        /// </para>
        /// </remarks>
        /// <value>
        /// A list of <see cref="SafeHandle"/> objects to be explicitly inherited by the child process,
        /// or <see langword="null"/> to use the default handle inheritance behavior.
        /// </value>
        public IList<SafeHandle>? InheritedHandles { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the child process should be terminated when the parent process exits.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When this property is set to <see langword="true"/>, the operating system will automatically terminate
        /// the child process when the parent process exits, regardless of whether the parent exits gracefully or crashes.
        /// </para>
        /// <para>
        /// This property cannot be used together with <see cref="UseShellExecute"/> set to <see langword="true"/>.
        /// </para>
        /// <para>
        /// On Windows, this is implemented using Job Objects with the
        /// <see href="https://learn.microsoft.com/windows/win32/api/winnt/ns-winnt-jobobject_basic_limit_information">JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE</see> flag.
        /// </para>
        /// </remarks>
        /// <value><see langword="true"/> to terminate the child process when the parent exits; otherwise, <see langword="false"/>. The default is <see langword="false"/>.</value>
        [SupportedOSPlatform("windows")]
        public bool KillOnParentExit { get; set; }

        public Encoding? StandardInputEncoding { get; set; }

        public Encoding? StandardErrorEncoding { get; set; }

        public Encoding? StandardOutputEncoding { get; set; }

        /// <devdoc>
        ///    <para>
        ///       Returns or sets the application, document, or URL that is to be launched.
        ///    </para>
        /// </devdoc>
        [Editor("System.Diagnostics.Design.StartFileNameEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [AllowNull]
        public string FileName
        {
            get => _fileName ?? string.Empty;
            set => _fileName = value;
        }

        /// <devdoc>
        ///     Returns or sets the initial directory for the process that is started.
        ///     Specify "" to if the default is desired.
        /// </devdoc>
        [Editor("System.Diagnostics.Design.WorkingDirectoryEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [AllowNull]
        public string WorkingDirectory
        {
            get => _directory ?? string.Empty;
            set => _directory = value;
        }

        public bool ErrorDialog { get; set; }
        public IntPtr ErrorDialogParentHandle { get; set; }

        public bool UseShellExecute
        {
            get;
            set
            {
                if (value)
                {
                    SafeProcessHandle.EnsureShellExecuteFunc();
                }
                field = value;
            }
        }

        [AllowNull]
        public string UserName
        {
            get => _userName ?? string.Empty;
            set => _userName = value;
        }

        [DefaultValue("")]
        [AllowNull]
        public string Verb
        {
            get => _verb ?? string.Empty;
            set => _verb = value;
        }

        [DefaultValueAttribute(System.Diagnostics.ProcessWindowStyle.Normal)]
        public ProcessWindowStyle WindowStyle
        {
            get => _windowStyle;
            set
            {
                if (!Enum.IsDefined(value))
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ProcessWindowStyle));
                }

                _windowStyle = value;
            }
        }

        internal string BuildArguments()
        {
            if (HasArgumentList)
            {
                var arguments = new ValueStringBuilder(stackalloc char[256]);
                AppendArgumentsTo(ref arguments);
                return arguments.ToString();
            }

            return Arguments;
        }

        internal void AppendArgumentsTo(ref ValueStringBuilder stringBuilder)
        {
            if (_argumentList != null && _argumentList.Count > 0)
            {
                foreach (string argument in _argumentList)
                {
                    PasteArguments.AppendArgument(ref stringBuilder, argument);
                }
            }
            else if (!string.IsNullOrEmpty(Arguments))
            {
                if (stringBuilder.Length > 0)
                {
                    stringBuilder.Append(' ');
                }

                stringBuilder.Append(Arguments);
            }
        }

        internal void ThrowIfInvalid(out bool anyRedirection, out SafeHandle[]? inheritedHandles)
        {
            if (FileName.Length == 0)
            {
                throw new InvalidOperationException(SR.FileNameMissing);
            }
            if (StandardInputEncoding != null && !RedirectStandardInput)
            {
                throw new InvalidOperationException(SR.StandardInputEncodingNotAllowed);
            }
            if (StandardOutputEncoding != null && !RedirectStandardOutput)
            {
                throw new InvalidOperationException(SR.StandardOutputEncodingNotAllowed);
            }
            if (StandardErrorEncoding != null && !RedirectStandardError)
            {
                throw new InvalidOperationException(SR.StandardErrorEncodingNotAllowed);
            }
            if (!string.IsNullOrEmpty(Arguments) && HasArgumentList)
            {
                throw new InvalidOperationException(SR.ArgumentAndArgumentListInitialized);
            }
            if (HasArgumentList)
            {
                int argumentCount = ArgumentList.Count;
                for (int i = 0; i < argumentCount; i++)
                {
                    if (ArgumentList[i] is null)
                    {
                        throw new ArgumentNullException($"ArgumentList[{i}]");
                    }
                }
            }

            anyRedirection = RedirectStandardInput || RedirectStandardOutput || RedirectStandardError;
            bool anyHandle = StandardInputHandle is not null || StandardOutputHandle is not null || StandardErrorHandle is not null;
            if (UseShellExecute && (anyRedirection || anyHandle))
            {
                throw new InvalidOperationException(SR.CantRedirectStreams);
            }

            if (StartDetached && UseShellExecute)
            {
                throw new InvalidOperationException(SR.StartDetachedNotCompatible);
            }

            if (InheritedHandles is not null && (UseShellExecute || !string.IsNullOrEmpty(UserName)))
            {
                throw new InvalidOperationException(SR.InheritedHandlesRequiresCreateProcess);
            }

#pragma warning disable CA1416 // KillOnParentExit getter works on all platforms; the attribute guards the actual effect
            if (KillOnParentExit && UseShellExecute)
#pragma warning restore CA1416
            {
                throw new InvalidOperationException(SR.KillOnParentExitCannotBeUsedWithUseShellExecute);
            }

            if (InheritedHandles is not null)
            {
                IList<SafeHandle> list = InheritedHandles;
                SafeHandle[] snapshot = new SafeHandle[list.Count];
                for (int i = 0; i < snapshot.Length; i++)
                {
                    SafeHandle? handle = list[i];
                    if (handle is null)
                    {
                        throw new ArgumentNullException(InheritedHandlesParamName(i));
                    }
                    if (handle.IsInvalid)
                    {
                        throw new ArgumentException(SR.Arg_InvalidHandle, InheritedHandlesParamName(i));
                    }
                    ObjectDisposedException.ThrowIf(handle.IsClosed, handle);

                    switch (handle)
                    {
                        case SafeFileHandle:
                        case SafePipeHandle:
                            break;
                        // As of today, we don't support other handle types because they would work
                        // only on Windows (e.g. Process/Wait handles).
                        default:
                            throw new ArgumentException(SR.InheritedHandles_OnlySelectedSafeHandlesAreSupported, InheritedHandlesParamName(i));
                    }

                    nint rawValue = handle.DangerousGetHandle();
                    for (int j = 0; j < i; j++)
                    {
                        if (snapshot[j].DangerousGetHandle() == rawValue)
                        {
                            throw new ArgumentException(SR.InheritedHandles_MustNotContainDuplicates, InheritedHandlesParamName(i));
                        }
                    }

                    snapshot[i] = handle;
                }

                inheritedHandles = snapshot;
            }
            else
            {
                inheritedHandles = null;
            }

            if (anyHandle)
            {
                if (StandardInputHandle is not null && RedirectStandardInput)
                {
                    throw new InvalidOperationException(SR.CantSetHandleAndRedirect);
                }
                if (StandardOutputHandle is not null && RedirectStandardOutput)
                {
                    throw new InvalidOperationException(SR.CantSetHandleAndRedirect);
                }
                if (StandardErrorHandle is not null && RedirectStandardError)
                {
                    throw new InvalidOperationException(SR.CantSetHandleAndRedirect);
                }

                ValidateHandle(StandardInputHandle, nameof(StandardInputHandle));
                ValidateHandle(StandardOutputHandle, nameof(StandardOutputHandle));
                ValidateHandle(StandardErrorHandle, nameof(StandardErrorHandle));
            }

            static void ValidateHandle(SafeFileHandle? handle, string paramName)
            {
                if (handle is not null)
                {
                    if (handle.IsInvalid)
                    {
                        throw new ArgumentException(SR.Arg_InvalidHandle, paramName);
                    }

                    ObjectDisposedException.ThrowIf(handle.IsClosed, handle);
                }
            }

            static string InheritedHandlesParamName(int i) => $"InheritedHandles[{i}]";
        }

        internal static void ValidateInheritedHandles(SafeFileHandle? stdinHandle, SafeFileHandle? stdoutHandle,
            SafeFileHandle? stderrHandle, SafeHandle[]? inheritedHandles = null)
        {
            if (inheritedHandles is null || inheritedHandles.Length == 0 || !ProcessUtils.PlatformSupportsConsole)
            {
                return;
            }

            Debug.Assert(stdinHandle is not null && stdoutHandle is not null && stderrHandle is not null);

            nint input = stdinHandle.DangerousGetHandle();
            nint output = stdoutHandle.DangerousGetHandle();
            nint error = stderrHandle.DangerousGetHandle();

            for (int i = 0; i < inheritedHandles.Length; i++)
            {
                nint handle = inheritedHandles[i].DangerousGetHandle();
                if (handle == input || handle == output || handle == error)
                {
                    // After process start, the Windows implementation unconditionally disables inheritance for all provided inheritable handles.
                    throw new ArgumentException(SR.InheritedHandles_MustNotContainStandardHandles, nameof(InheritedHandles));
                }
            }
        }
    }
}
