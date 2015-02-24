// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Security;
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

        private bool _createNoWindow = false;
        private WeakReference _weakParentProcess;
        internal Dictionary<string, string> _environmentVariables;

        /// <devdoc>
        ///     Default constructor.  At least the <see cref='System.Diagnostics.ProcessStartInfo.FileName'/>
        ///     property must be set before starting the process.
        /// </devdoc>
        public ProcessStartInfo()
        {
        }

        internal ProcessStartInfo(Process parent)
        {
            _weakParentProcess = new WeakReference(parent);
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

        public IDictionary<string, string> Environment
        {
            get
            {
                if (_environmentVariables == null)
                {
                    _environmentVariables = new Dictionary<string, string>();

                    // if not in design mode, initialize the child environment block with all the parent variables
                    if (!(_weakParentProcess != null &&
                          _weakParentProcess.IsAlive))
                    {
                        foreach (DictionaryEntry entry in System.Environment.GetEnvironmentVariables())
                            _environmentVariables.Add((string)entry.Key, (string)entry.Value);
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

        // CoreCLR can't correctly support UseShellExecute=true for the following reasons
        // 1. ShellExecuteEx is not supported on onecore.
        // 2. ShellExecuteEx needs to run as STA but managed code runs as MTA by default and Thread.SetApartmentState() is not supported on all platforms.
        //
        // Irrespective of the limited functionality of the property we still support it in the contract for the below reason.
        // The default value of UseShellExecute is true on desktop and scenarios like redirection mandates the value to be false.
        // So in order to provide maximum code portability we expose UseShellExecute in the contract 
        // and throw PlatformNotSupportedException in portable library in case it is set to true.
        public bool UseShellExecute
        {
            get { return false; }
            set { if (value == true) throw new PlatformNotSupportedException(SR.UseShellExecute); }
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
    }
}
