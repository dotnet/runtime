// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;

namespace System.Diagnostics
{
    /// <devdoc>
    ///     A set of values used to specify a process to start.  This is
    ///     used in conjunction with the <see cref='System.Diagnostics.Process'/>
    ///     component.
    /// </devdoc>
    public sealed partial class ProcessStartInfo
    {
        private string _fileName;
        private string _arguments;
        private string _directory;
        private bool _redirectStandardInput = false;
        private bool _redirectStandardOutput = false;
        private bool _redirectStandardError = false;
        private Encoding _standardOutputEncoding;
        private Encoding _standardErrorEncoding;
        private bool _errorDialog;
        private IntPtr _errorDialogParentHandle;
        private string _verb;
        private ProcessWindowStyle _windowStyle;

        private bool _createNoWindow = false;
        internal Dictionary<string, string> _environmentVariables;

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
        public ProcessStartInfo(string fileName, string arguments)
        {
            _fileName = fileName;
            _arguments = arguments;
        }

        /// <devdoc>
        ///     Specifies the set of command line arguments to use when starting the application.
        /// </devdoc>
        public string Arguments
        {
            get
            {
                if (_arguments == null) return string.Empty;
                return _arguments;
            }
            set
            {
                _arguments = value;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool CreateNoWindow
        {
            get { return _createNoWindow; }
            set { _createNoWindow = value; }
        }

        public StringDictionary EnvironmentVariables => new StringDictionaryWrapper(Environment as Dictionary<string,string>);

        public IDictionary<string, string> Environment
        {
            get
            {
                if (_environmentVariables == null)
                {
                    IDictionary envVars = System.Environment.GetEnvironmentVariables();

#pragma warning disable 0429 // CaseSensitiveEnvironmentVaribles is constant but varies depending on if we build for Unix or Windows
                    _environmentVariables = new Dictionary<string, string>(
                        envVars.Count,
                        CaseSensitiveEnvironmentVariables ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
#pragma warning restore 0429

                    // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
                    IDictionaryEnumerator e = envVars.GetEnumerator();
                    try
                    {
                        while (e.MoveNext())
                        {
                            DictionaryEntry entry = e.Entry;
                            _environmentVariables.Add((string)entry.Key, (string)entry.Value);
                        }
                    }
                    finally
                    {
                        (e as IDisposable)?.Dispose();
                    }
                }
                return _environmentVariables;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool RedirectStandardInput
        {
            get { return _redirectStandardInput; }
            set { _redirectStandardInput = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool RedirectStandardOutput
        {
            get { return _redirectStandardOutput; }
            set { _redirectStandardOutput = value; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool RedirectStandardError
        {
            get { return _redirectStandardError; }
            set { _redirectStandardError = value; }
        }


        public Encoding StandardErrorEncoding
        {
            get { return _standardErrorEncoding; }
            set { _standardErrorEncoding = value; }
        }

        public Encoding StandardOutputEncoding
        {
            get { return _standardOutputEncoding; }
            set { _standardOutputEncoding = value; }
        }

        /// <devdoc>
        ///    <para>
        ///       Returns or sets the application, document, or URL that is to be launched.
        ///    </para>
        /// </devdoc>
        public string FileName
        {
            get { return _fileName ?? string.Empty; }
            set { _fileName = value; }
        }

        /// <devdoc>
        ///     Returns or sets the initial directory for the process that is started.
        ///     Specify "" to if the default is desired.
        /// </devdoc>
        public string WorkingDirectory
        {
            get { return _directory ?? string.Empty; }
            set { _directory = value; }
        }

        public bool ErrorDialog
        {
            get { return _errorDialog; }
            set { _errorDialog = value; }
        }

        public IntPtr ErrorDialogParentHandle 
        {
            get { return _errorDialogParentHandle; }
            set { _errorDialogParentHandle = value; }
        }

        [DefaultValueAttribute("")]
        public string Verb 
        {
            get { return _verb ?? string.Empty; }
            set { _verb = value; }
        }

        [DefaultValueAttribute(System.Diagnostics.ProcessWindowStyle.Normal)]
        public ProcessWindowStyle WindowStyle
        {
            get 
            { 
                return _windowStyle; 
            }
            set 
            {
                if (!Enum.IsDefined(typeof(ProcessWindowStyle), value))
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ProcessWindowStyle));
                } 
                    
                _windowStyle = value;
            }
        }
    }
}
