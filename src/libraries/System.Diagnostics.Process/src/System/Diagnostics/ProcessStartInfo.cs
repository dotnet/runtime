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
        private string _verb;
        private ProcessWindowStyle _windowStyle;

        internal DictionaryWrapper _environmentVariables;

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
            get => _arguments ?? string.Empty;
            set => _arguments = value;
        }

        public bool CreateNoWindow { get; set; }

        public StringDictionary EnvironmentVariables => new StringDictionaryWrapper(Environment as DictionaryWrapper);

        public IDictionary<string, string> Environment
        {
            get
            {
                if (_environmentVariables == null)
                {
                    IDictionary envVars = System.Environment.GetEnvironmentVariables();

#pragma warning disable 0429 // CaseSensitiveEnvironmentVaribles is constant but varies depending on if we build for Unix or Windows
                    _environmentVariables = new DictionaryWrapper(new Dictionary<string, string>(
                        envVars.Count,
                        CaseSensitiveEnvironmentVariables ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase));
#pragma warning restore 0429

                    // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
                    IDictionaryEnumerator e = envVars.GetEnumerator();
                    Debug.Assert(!(e is IDisposable), "Environment.GetEnvironmentVariables should not be IDisposable.");
                    while (e.MoveNext())
                    {
                        DictionaryEntry entry = e.Entry;
                        _environmentVariables.Add((string)entry.Key, (string)entry.Value);
                    }
                }
                return _environmentVariables;
            }
        }

        public bool RedirectStandardInput { get; set; }
        public bool RedirectStandardOutput { get; set; }
        public bool RedirectStandardError { get; set; }

        public Encoding StandardInputEncoding { get; set; }

        public Encoding StandardErrorEncoding { get; set; }
        
        public Encoding StandardOutputEncoding { get; set; }

        /// <devdoc>
        ///    <para>
        ///       Returns or sets the application, document, or URL that is to be launched.
        ///    </para>
        /// </devdoc>
        public string FileName
        {
            get => _fileName ?? string.Empty;
            set => _fileName = value;
        }

        /// <devdoc>
        ///     Returns or sets the initial directory for the process that is started.
        ///     Specify "" to if the default is desired.
        /// </devdoc>
        public string WorkingDirectory
        {
            get => _directory ?? string.Empty;
            set => _directory = value;
        }

        public bool ErrorDialog { get; set; }
        public IntPtr ErrorDialogParentHandle { get; set; }

        [DefaultValue("")]
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
                if (!Enum.IsDefined(typeof(ProcessWindowStyle), value))
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ProcessWindowStyle));
                } 
                    
                _windowStyle = value;
            }
        }
    }
}
