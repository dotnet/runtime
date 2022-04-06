// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.IO;

// We keep this class in Common to allow utilizing it without taking a dependency on System.CodeDom
#if CODEDOM
namespace System.CodeDom.Compiler
#else
namespace System.IO.Internal
#endif
{
    // Explicitly not [Serializable], so as to avoid accidentally deleting
    // files specified in a serialized payload.

#if CODEDOM
    public
#else
    internal
#endif
    class TempFileCollection : ICollection, IDisposable
    {
        private string _basePath;
        private readonly string _tempDir;
        private readonly Hashtable _files;

        public TempFileCollection() : this(null, false)
        {
        }

        public TempFileCollection(string tempDir) : this(tempDir, false)
        {
        }

        public TempFileCollection(string tempDir, bool keepFiles)
        {
            KeepFiles = keepFiles;
            _tempDir = tempDir;
            _files = new Hashtable(StringComparer.OrdinalIgnoreCase);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            SafeDelete();
        }

        ~TempFileCollection()
        {
            Dispose(false);
        }

        public string AddExtension(string fileExtension) => AddExtension(fileExtension, KeepFiles);

        public string AddExtension(string fileExtension, bool keepFile)
        {
            if (string.IsNullOrEmpty(fileExtension))
            {
                throw new ArgumentException(SR.Format(SR.InvalidNullEmptyArgument, nameof(fileExtension)), nameof(fileExtension));
            }

            string fileName = BasePath + "." + fileExtension;
            AddFile(fileName, keepFile);
            return fileName;
        }

        public void AddFile(string fileName, bool keepFile)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException(SR.Format(SR.InvalidNullEmptyArgument, nameof(fileName)), nameof(fileName));
            }

            if (_files[fileName] != null)
            {
                throw new ArgumentException(SR.Format(SR.DuplicateFileName, fileName), nameof(fileName));
            }

            _files.Add(fileName, keepFile);
        }

        public IEnumerator GetEnumerator() => _files.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _files.Keys.GetEnumerator();

        void ICollection.CopyTo(Array array, int start) => _files.Keys.CopyTo(array, start);

        public void CopyTo(string[] fileNames, int start) => _files.Keys.CopyTo(fileNames, start);

        public int Count => _files.Count;

        int ICollection.Count => _files.Count;

        object ICollection.SyncRoot => null;

        bool ICollection.IsSynchronized => false;

        public string TempDir => _tempDir ?? string.Empty;

        public string BasePath
        {
            get
            {
                EnsureTempNameCreated();
                return _basePath;
            }
        }

        private void EnsureTempNameCreated()
        {
            if (_basePath == null)
            {
                string tempFileName;
                bool uniqueFile;
                int retryCount = 5000;
                do
                {
                    _basePath = Path.Combine(
                        string.IsNullOrEmpty(TempDir) ? Path.GetTempPath() : TempDir,
                        Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
                    tempFileName = _basePath + ".tmp";

                    try
                    {
                        new FileStream(tempFileName, FileMode.CreateNew, FileAccess.Write).Dispose();
                        uniqueFile = true;
                    }
                    catch (IOException ex)
                    {
                        retryCount--;
                        if (retryCount == 0 || ex is DirectoryNotFoundException)
                        {
                            throw;
                        }
                        uniqueFile = false;
                    }
                } while (!uniqueFile);
                _files.Add(tempFileName, KeepFiles);
            }
        }

        public bool KeepFiles { get; set; }

        private bool KeepFile(string fileName)
        {
            object keep = _files[fileName];
            return keep != null ? (bool)keep : false;
        }

        public void Delete()
        {
            SafeDelete();
        }

        private static void Delete(string fileName)
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

        internal void SafeDelete()
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
    }
}
