// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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

        internal void ThrowIfInvalid(out bool anyRedirection)
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
                        throw new ArgumentNullException("item", SR.ArgumentListMayNotContainNull);
                    }
                }
            }

            anyRedirection = RedirectStandardInput || RedirectStandardOutput || RedirectStandardError;
            bool anyHandle = StandardInputHandle is not null || StandardOutputHandle is not null || StandardErrorHandle is not null;
            if (UseShellExecute && (anyRedirection || anyHandle))
            {
                throw new InvalidOperationException(SR.CantRedirectStreams);
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
        }
    }
}
