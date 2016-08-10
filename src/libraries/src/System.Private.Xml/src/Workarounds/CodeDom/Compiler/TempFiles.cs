// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Compiler
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Win32;
    using System.Security;
    using System.ComponentModel;
    using System.Globalization;
    using System.Runtime.Versioning;

    /// <devdoc>
    ///    <para>Represents a collection of temporary file names that are all based on a
    ///       single base filename located in a temporary directory.</para>
    /// </devdoc>
    internal class TempFileCollection : ICollection, IDisposable
    {
        private string _tempDir;
        private bool _keepFiles;
        private Hashtable _files;

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public TempFileCollection() : this(null, false)
        {
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public TempFileCollection(string tempDir) : this(tempDir, false)
        {
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public TempFileCollection(string tempDir, bool keepFiles)
        {
            _keepFiles = keepFiles;
            _tempDir = tempDir;
#if !FEATURE_CASE_SENSITIVE_FILESYSTEM            
            _files = new Hashtable(StringComparer.OrdinalIgnoreCase);
#else
            files = new Hashtable();
#endif
        }

        /// <internalonly/>
        /// <devdoc>
        /// <para> To allow it's stuff to be cleaned up</para>
        /// </devdoc>
        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            // It is safe to call Delete from here even if Dispose is called from Finalizer
            // because the graph of objects is guaranteed to be there and
            // neither Hashtable nor String have a finalizer of their own that could 
            // be called before TempFileCollection Finalizer
            Delete();
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ~TempFileCollection()
        {
            Dispose(false);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void AddFile(string fileName, bool keepFile)
        {
            if (fileName == null || fileName.Length == 0)
                throw new ArgumentException(string.Format(SR.InvalidNullEmptyArgument, "fileName"), "fileName");  // fileName not specified

            if (_files[fileName] != null)
                throw new ArgumentException(string.Format(SR.DuplicateFileName, fileName), "fileName");  // duplicate fileName
            _files.Add(fileName, (object)keepFile);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public IEnumerator GetEnumerator()
        {
            return _files.Keys.GetEnumerator();
        }

        /// <internalonly/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _files.Keys.GetEnumerator();
        }

        /// <internalonly/>
        void ICollection.CopyTo(Array array, int start)
        {
            _files.Keys.CopyTo(array, start);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(string[] fileNames, int start)
        {
            _files.Keys.CopyTo(fileNames, start);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Count
        {
            get
            {
                return _files.Count;
            }
        }

        /// <internalonly/>
        int ICollection.Count
        {
            get { return _files.Count; }
        }

        /// <internalonly/>
        object ICollection.SyncRoot
        {
            get { return null; }
        }

        /// <internalonly/>
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string TempDir
        {
            get { return _tempDir == null ? string.Empty : _tempDir; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool KeepFiles
        {
            get { return _keepFiles; }
            set { _keepFiles = value; }
        }

        private bool KeepFile(string fileName)
        {
            object keep = _files[fileName];
            if (keep == null) return false;
            return (bool)keep;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Delete()
        {
            if (_files != null && _files.Count > 0)
            {
                string[] fileNames = new string[_files.Count];
                _files.Keys.CopyTo(fileNames, 0);
                foreach (string fileName in fileNames)
                {
                    if (!KeepFile(fileName))
                    {
                        Delete(fileName);
                        _files.Remove(fileName);
                    }
                }
            }
        }

        private void Delete(string fileName)
        {
            try
            {
                File.Delete(fileName);
            }
            catch
            {
                // Ignore all exceptions
            }
        }
    }
}
