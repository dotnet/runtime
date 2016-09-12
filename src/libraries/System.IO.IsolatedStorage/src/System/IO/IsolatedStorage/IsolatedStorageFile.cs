// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Linq;

namespace System.IO.IsolatedStorage
{
    public sealed partial class IsolatedStorageFile : IsolatedStorage, IDisposable
    {
        private string _rootDirectoryForScope;
        private string _rootDirectory;

        private bool _disposed;
        private bool _closed;

        private readonly object _internalLock = new object();

        // Data file notes
        // ===============

        // "identity.dat" is the serialized identity object, such as StrongName or Url. It is used to
        // enumerate stores, which we currently do not support.
        //
        // private const string IDFile = "identity.dat";

        // "info.dat" is used to track disk space usage (against quota). The accounting file for Silverlight
        // stores is "appInfo.dat". CoreFX is always in full trust so we can safely ignore these.
        //
        // private const string InfoFile = "info.dat";
        // private const string AppInfoFile = "appInfo.dat";

        internal IsolatedStorageFile() { }

        internal bool Disposed
        {
            get
            {
                return _disposed;
            }
        }

        internal bool IsDeleted
        {
            get
            {
                try
                {
                    return !Directory.Exists(_rootDirectory);
                }
                catch (IOException)
                {
                    // It's better to assume the IsoStore is gone if we can't prove it is there.
                    return true;
                }
                catch (UnauthorizedAccessException)
                {
                    // It's better to assume the IsoStore is gone if we can't prove it is there.
                    return true;
                }
            }
        }

        public void Close()
        {
            if (Helper.IsRoaming(Scope))
                return;

            lock (_internalLock)
            {
                if (!_closed)
                {
                    _closed = true;
                    GC.SuppressFinalize(this);
                }
            }
        }

        public void DeleteFile(string file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            EnsureStoreIsValid();

            try
            {
                string fullPath = GetFullPath(file);
                File.Delete(fullPath);
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException(SR.IsolatedStorage_DeleteFile, e);
            }
        }

        public bool FileExists(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            EnsureStoreIsValid();

            return File.Exists(GetFullPath(path));
        }

        public bool DirectoryExists(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            EnsureStoreIsValid();

            return Directory.Exists(GetFullPath(path));
        }

        public void CreateDirectory(string dir)
        {
            if (dir == null)
                throw new ArgumentNullException(nameof(dir));

            EnsureStoreIsValid();

            string isPath = GetFullPath(dir); // Prepend IS root

            // We can save a bunch of work if the directory we want to create already exists.  This also
            // saves us in the case where sub paths are inaccessible (due to ERROR_ACCESS_DENIED) but the
            // final path is accessable and the directory already exists.  For example, consider trying
            // to create c:\Foo\Bar\Baz, where everything already exists but ACLS prevent access to c:\Foo
            // and c:\Foo\Bar.  In that case, this code will think it needs to create c:\Foo, and c:\Foo\Bar
            // and fail to due so, causing an exception to be thrown.  This is not what we want.
            if (Directory.Exists(isPath))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(isPath);
            }
            catch (Exception e)
            {
                // We have a slightly different behavior here in comparison to the traditional IsolatedStorage
                // which tries to remove any partial directories created in case of failure.
                // However providing that behavior required we could not reply on FileSystem APIs in general
                // and had to keep tabs on what all directories needed to be created and at what point we failed
                // and back-track from there. It is unclear how many apps would depend on this behavior and if required
                // we could add the behavior as a bug-fix later.
                throw GetIsolatedStorageException(SR.IsolatedStorage_CreateDirectory, e);
            }
        }

        public void DeleteDirectory(string dir)
        {
            if (dir == null)
                throw new ArgumentNullException(nameof(dir));

            EnsureStoreIsValid();

            try
            {
                string fullPath = GetFullPath(dir);
                Directory.Delete(fullPath, false);
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException(SR.IsolatedStorage_DeleteDirectory, e);
            }
        }

        public string[] GetFileNames()
        {
            return GetFileNames("*");
        }

        // foo\abc*.txt will give all abc*.txt files in foo directory
        public string[] GetFileNames(string searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));

            EnsureStoreIsValid();

            try
            {
                // FileSystem APIs return the complete path of the matching files however Iso store only provided the FileName
                // and hid the IsoStore root. Hence we find all the matching files from the fileSystem and simply return the fileNames.
                return Directory.EnumerateFiles(_rootDirectory, searchPattern).Select(f => Path.GetFileName(f)).ToArray();
            }
            catch (UnauthorizedAccessException e)
            {
                throw GetIsolatedStorageException(SR.IsolatedStorage_Operation, e);
            }
        }

        public string[] GetDirectoryNames()
        {
            return GetDirectoryNames("*");
        }

        // foo\data* will give all directory names in foo directory that starts with data
        public string[] GetDirectoryNames(string searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException(nameof(searchPattern));

            EnsureStoreIsValid();

            try
            {
                // FileSystem APIs return the complete path of the matching directories however Iso store only provided the directory name
                // and hid the IsoStore root. Hence we find all the matching directories from the fileSystem and simply return their names.
                return Directory.EnumerateDirectories(_rootDirectory, searchPattern).Select(m => m.Substring(Path.GetDirectoryName(m).Length + 1)).ToArray();
            }
            catch (UnauthorizedAccessException e)
            {
                throw GetIsolatedStorageException(SR.IsolatedStorage_Operation, e);
            }
        }

        public IsolatedStorageFileStream OpenFile(string path, FileMode mode)
        {
            EnsureStoreIsValid();
            return new IsolatedStorageFileStream(path, mode, this);
        }

        public IsolatedStorageFileStream OpenFile(string path, FileMode mode, FileAccess access)
        {
            EnsureStoreIsValid();
            return new IsolatedStorageFileStream(path, mode, access, this);
        }

        public IsolatedStorageFileStream OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
        {
            EnsureStoreIsValid();
            return new IsolatedStorageFileStream(path, mode, access, share, this);
        }

        public IsolatedStorageFileStream CreateFile(string path)
        {
            EnsureStoreIsValid();
            return new IsolatedStorageFileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, this);
        }

        public DateTimeOffset GetCreationTime(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (path == string.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            }

            EnsureStoreIsValid();

            try
            {
                return new DateTimeOffset(File.GetCreationTime(GetFullPath(path)));
            }
            catch (UnauthorizedAccessException)
            {
                return new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
            }
        }

        public DateTimeOffset GetLastAccessTime(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (path == string.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            }

            EnsureStoreIsValid();

            try
            {
                return new DateTimeOffset(File.GetLastAccessTime(GetFullPath(path)));
            }
            catch (UnauthorizedAccessException)
            {
                return new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
            }
        }

        public DateTimeOffset GetLastWriteTime(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (path == string.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            }

            EnsureStoreIsValid();

            try
            {
                return new DateTimeOffset(File.GetLastWriteTime(GetFullPath(path)));
            }
            catch (UnauthorizedAccessException)
            {
                return new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
            }
        }

        public void CopyFile(string sourceFileName, string destinationFileName)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName));

            if (destinationFileName == null)
                throw new ArgumentNullException(nameof(destinationFileName));

            if (sourceFileName == string.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(sourceFileName));
            }

            if (destinationFileName == string.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(destinationFileName));
            }

            CopyFile(sourceFileName, destinationFileName, false);
        }

        public void CopyFile(string sourceFileName, string destinationFileName, bool overwrite)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName));

            if (destinationFileName == null)
                throw new ArgumentNullException(nameof(destinationFileName));

            if (sourceFileName == string.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(sourceFileName));
            }

            if (destinationFileName == string.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(destinationFileName));
            }

            EnsureStoreIsValid();

            string sourceFileNameFullPath = GetFullPath(sourceFileName);
            string destinationFileNameFullPath = GetFullPath(destinationFileName);

            try
            {
                File.Copy(sourceFileNameFullPath, destinationFileNameFullPath, overwrite);
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException(string.Format(SR.PathNotFound_Path, sourceFileName));
            }
            catch (PathTooLongException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException(SR.IsolatedStorage_Operation, e);
            }
        }

        public void MoveFile(string sourceFileName, string destinationFileName)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName));

            if (destinationFileName == null)
                throw new ArgumentNullException(nameof(destinationFileName));

            if (sourceFileName == string.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(sourceFileName));
            }

            if (destinationFileName == string.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(destinationFileName));
            }

            EnsureStoreIsValid();

            string sourceFileNameFullPath = GetFullPath(sourceFileName);
            string destinationFileNameFullPath = GetFullPath(destinationFileName);

            try
            {
                File.Move(sourceFileNameFullPath, destinationFileNameFullPath);
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException(string.Format(SR.PathNotFound_Path, sourceFileName));
            }
            catch (PathTooLongException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException(SR.IsolatedStorage_Operation, e);
            }
        }

        public void MoveDirectory(string sourceDirectoryName, string destinationDirectoryName)
        {
            if (sourceDirectoryName == null)
                throw new ArgumentNullException(nameof(sourceDirectoryName));

            if (destinationDirectoryName == null)
                throw new ArgumentNullException(nameof(destinationDirectoryName));

            if (sourceDirectoryName == string.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(sourceDirectoryName));
            }

            if (destinationDirectoryName == string.Empty)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(destinationDirectoryName));
            }

            EnsureStoreIsValid();

            string sourceDirectoryNameFullPath = GetFullPath(sourceDirectoryName);
            string destinationDirectoryNameFullPath = GetFullPath(destinationDirectoryName);

            try
            {
                Directory.Move(sourceDirectoryNameFullPath, destinationDirectoryNameFullPath);
            }
            catch (DirectoryNotFoundException)
            {
                throw new DirectoryNotFoundException(string.Format(SR.PathNotFound_Path, sourceDirectoryName));
            }
            catch (PathTooLongException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException(SR.IsolatedStorage_Operation, e);
            }
        }

        public static IEnumerator GetEnumerator(IsolatedStorageScope scope)
        {
            // Not currently supported: https://github.com/dotnet/corefx/issues/10936

            // Implementing this would require serializing/deserializing identity objects which is particularly
            // complicated given the normal identity objects used by NetFX aren't available on CoreFX.
            //
            // Starting expectation is that a given store's location would be identical between implementations
            // (say, for a particular StrongName). You could iterate any store opened at least once by NetFX on
            // NetFX as it would create the needed identity file. You wouldn't be able to iterate if it was only
            // ever opened by CoreFX, as the needed file isn't there yet.
            throw new PlatformNotSupportedException();
        }

        public static IsolatedStorageFile GetUserStoreForApplication()
        {
            return GetStore(IsolatedStorageScope.Application | IsolatedStorageScope.User);
        }

        public static IsolatedStorageFile GetUserStoreForAssembly()
        {
            return GetStore(IsolatedStorageScope.Assembly | IsolatedStorageScope.User);
        }

        public static IsolatedStorageFile GetUserStoreForDomain()
        {
            return GetStore(IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain | IsolatedStorageScope.User);
        }

        public static IsolatedStorageFile GetUserStoreForSite()
        {
            // NetFX and Mono both throw for this method
            throw new NotSupportedException(SR.IsolatedStorage_NotValidOnDesktop);
        }

        public static IsolatedStorageFile GetMachineStoreForApplication()
        {
            return GetStore(IsolatedStorageScope.Application | IsolatedStorageScope.Machine);
        }

        public static IsolatedStorageFile GetMachineStoreForAssembly()
        {
            return GetStore(IsolatedStorageScope.Assembly | IsolatedStorageScope.Machine);
        }

        public static IsolatedStorageFile GetMachineStoreForDomain()
        {
            return GetStore(IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain | IsolatedStorageScope.Machine);
        }

        private static IsolatedStorageFile GetStore(IsolatedStorageScope scope)
        {
            IsolatedStorageFile isf = new IsolatedStorageFile();
            isf.Initialize(scope);
            return isf;
        }

        // Notes on the GetStore methods:
        //
        // The System.Security types that NetFX would be getting aren't available. We could potentially map the two
        // we implicitly support (StrongName and Url) to AssemblyName and Uri. We could also consider accepting those two
        // types.
        //
        // For the methods that take actual evidence objects we would have to do the same mapping and implement the
        // hashing required if it wasn't the two types we know. The hash is a two part hash of {typehash}.{instancehash}.
        // The hashing logic is basically this:
        //
        //  - if not a "known type" the type hash is from a BinaryFormatter serialized object.GetType()
        //  - if the identity object is INomalizeForIsolatedStorage, use .Normalize() result for hashing identity, otherwise the object itself
        //  - again, use BinaryFormatter to serialize the selected identity object for getting the instance hash
        // 
        // Hashing for the streams created is done in Helper.GetStrongHashSuitableForObjectName()
        //
        // "Known" types are Publisher, StrongName, Url, Site, and Zone.

        public static IsolatedStorageFile GetStore(IsolatedStorageScope scope, Type applicationEvidenceType)
        {
            // https://github.com/dotnet/corefx/issues/10935
            throw new PlatformNotSupportedException();
        }

        public static IsolatedStorageFile GetStore(IsolatedStorageScope scope, object applicationIdentity)
        {
            // https://github.com/dotnet/corefx/issues/10935
            throw new PlatformNotSupportedException();
        }

        public static IsolatedStorageFile GetStore(IsolatedStorageScope scope, Type domainEvidenceType, Type assemblyEvidenceType)
        {
            // https://github.com/dotnet/corefx/issues/10935
            throw new PlatformNotSupportedException();
        }

        public static IsolatedStorageFile GetStore(IsolatedStorageScope scope, object domainIdentity, object assemblyIdentity)
        {
            // https://github.com/dotnet/corefx/issues/10935
            throw new PlatformNotSupportedException();
        }

        // https://github.com/dotnet/corefx/issues/10935
        // Evidence isn't currently available
        // public static IsolatedStorageFile GetStore(IsolatedStorageScope scope, Evidence domainEvidence, Type domainEvidenceType, Evidence assemblyEvidence, Type assemblyEvidenceType) { return default(IsolatedStorageFile); }

        private void Initialize(IsolatedStorageScope scope)
        {
            InitStore(scope, null, null);
            _rootDirectoryForScope = Helper.GetRootDirectory(scope);
            _rootDirectory = Path.Combine(_rootDirectoryForScope, IdentityHash);
        }

        internal string GetFullPath(string partialPath)
        {
            Debug.Assert(partialPath != null, "partialPath should be non null");

            int i;

            // Chop off directory separator characters at the start of the string because they counfuse Path.Combine.
            for (i = 0; i < partialPath.Length; i++)
            {
                if (partialPath[i] != Path.DirectorySeparatorChar && partialPath[i] != Path.AltDirectorySeparatorChar)
                {
                    break;
                }
            }

            partialPath = partialPath.Substring(i);

            return Path.Combine(_rootDirectory, partialPath);
        }

        internal void EnsureStoreIsValid()
        {
            if (Disposed)
                throw new ObjectDisposedException(null, SR.IsolatedStorage_StoreNotOpen);

            if (IsDeleted)
            {
                throw new IsolatedStorageException(SR.IsolatedStorage_StoreNotOpen);
            }

            if (_closed)
                throw new InvalidOperationException(SR.IsolatedStorage_StoreNotOpen);
        }

        public void Dispose()
        {
            Close();
            _disposed = true;
        }

        internal static Exception GetIsolatedStorageException(string exceptionMsg, Exception rootCause)
        {
            IsolatedStorageException e = new IsolatedStorageException(exceptionMsg, rootCause);
            e._underlyingException = rootCause;
            return e;
        }

        public override bool IncreaseQuotaTo(long newQuotaSize)
        {
            // We don't support quotas, just call it ok
            return true;
        }

        public override void Remove()
        {
            // TODO: https://github.com/dotnet/corefx/issues/11125
            throw new NotImplementedException();
        }

        public static void Remove(IsolatedStorageScope scope)
        {
            // TODO: https://github.com/dotnet/corefx/issues/11125
            throw new NotImplementedException();
        }

        public static bool IsEnabled
        {
            // Isolated storage is always available
            get { return true; }
        }
    }
}
