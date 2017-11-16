// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace System.IO.IsolatedStorage
{
    public sealed partial class IsolatedStorageFile : IsolatedStorage, IDisposable
    {
        internal const string s_files = "Files";
        internal const string s_assemFiles = "AssemFiles";
        internal const string s_appFiles = "AppFiles";
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

        // Using this property to match NetFX for testing
        private string RootDirectory
        {
            get { return _rootDirectory; }
        }

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
                    return !Directory.Exists(RootDirectory);
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
            // final path is accessible and the directory already exists.  For example, consider trying
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
                return Directory.EnumerateFiles(RootDirectory, searchPattern).Select(f => Path.GetFileName(f)).ToArray();
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
                return Directory.EnumerateDirectories(RootDirectory, searchPattern).Select(m => m.Substring(Path.GetDirectoryName(m).Length + 1)).ToArray();
            }
            catch (UnauthorizedAccessException e)
            {
                throw GetIsolatedStorageException(SR.IsolatedStorage_Operation, e);
            }
        }

        // When constructing an IsolatedStorageFileStream we pass the partial path- it will call back for the full path

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
            return new IsolatedStorageFileEnumerator();
        }

        internal sealed class IsolatedStorageFileEnumerator : IEnumerator
        {
            public object Current
            {
                get
                {
                    // Getting current throws on NetFX if there is no current item.
                    throw new InvalidOperationException();
                }
            }

            public bool MoveNext()
            {
                // Nothing to return
                return false;
            }

            public void Reset()
            {
                // Do nothing
            }
        }

        public override long AvailableFreeSpace
        {
            get
            {
                return Quota - UsedSize;
            }
        }

        [CLSCompliant(false)]
        [Obsolete("IsolatedStorage.MaximumSize has been deprecated because it is not CLS Compliant.  To get the maximum size use IsolatedStorage.Quota")]
        public override ulong MaximumSize
        {
            get
            {
                return long.MaxValue;
            }
        }

        public override long Quota
        {
            get
            {
                return long.MaxValue;
            }
        }

        public override long UsedSize
        {
            get
            {
                return 0; // We do not have a mechanism for tracking usage.
            }
        }

        [CLSCompliant(false)]
        [Obsolete("IsolatedStorage.CurrentSize has been deprecated because it is not CLS Compliant.  To get the current size use IsolatedStorage.UsedSize")]
        public override ulong CurrentSize
        {
            get
            {
                return 0; // We do not have a mechanism for tracking usage.
            }
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
            // Scope MUST be Application
            return (applicationEvidenceType == null) ? GetStore(scope) : throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS); // https://github.com/dotnet/corefx/issues/10935
        }

        public static IsolatedStorageFile GetStore(IsolatedStorageScope scope, object applicationIdentity)
        {
            // Scope MUST be Application
            return (applicationIdentity == null) ? GetStore(scope) : throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS); // https://github.com/dotnet/corefx/issues/10935
        }

        public static IsolatedStorageFile GetStore(IsolatedStorageScope scope, Type domainEvidenceType, Type assemblyEvidenceType)
        {
            // Scope MUST NOT be Application (assembly is assumed otherwise)
            return (domainEvidenceType == null && assemblyEvidenceType == null) ? GetStore(scope) : throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS); // https://github.com/dotnet/corefx/issues/10935
        }

        public static IsolatedStorageFile GetStore(IsolatedStorageScope scope, object domainIdentity, object assemblyIdentity)
        {
            // Scope MUST NOT be Application (assembly is assumed otherwise)
            return (domainIdentity == null && assemblyIdentity == null) ? GetStore(scope) : throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS); // https://github.com/dotnet/corefx/issues/10935
        }

        // https://github.com/dotnet/corefx/issues/10935
        // Evidence isn't currently available
        // public static IsolatedStorageFile GetStore(IsolatedStorageScope scope, Evidence domainEvidence, Type domainEvidenceType, Evidence assemblyEvidence, Type assemblyEvidenceType) { return default(IsolatedStorageFile); }

        private void Initialize(IsolatedStorageScope scope)
        {
            // InitStore will set up the IdentityHash
            InitStore(scope, null, null);

            StringBuilder sb = new StringBuilder(Helper.GetRootDirectory(scope));
            sb.Append(SeparatorExternal);
            sb.Append(IdentityHash);
            sb.Append(SeparatorExternal);

            if (Helper.IsApplication(scope))
            {
                sb.Append(s_appFiles);
            }
            else if (Helper.IsDomain(scope))
            {
                sb.Append(s_files);
            }
            else
            {
                sb.Append(s_assemFiles);
            }
            sb.Append(SeparatorExternal);

            _rootDirectory = sb.ToString();
            Helper.CreateDirectory(_rootDirectory, scope);
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

            return Path.Combine(RootDirectory, partialPath);
        }

        internal void EnsureStoreIsValid()
        {
            // This validation is something we only did in Silverlight previously.
            if (Disposed)
                throw new ObjectDisposedException(null, SR.IsolatedStorage_StoreNotOpen);

            if (_closed || IsDeleted)
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
            // Deletes the current IsoFile's directory and the identity folder if possible.
            // (e.g. @"C:\Users\jerem\AppData\Local\IsolatedStorage\10v31ho4.bo2\eeolfu22.f2w\Url.qgeirsoc3cznuklvq5xlalurh1m0unxl\AssemFiles\")

            // This matches NetFX logic. We want to try and clean as well as possible without being more aggressive with the identity folders.
            // (e.g. Url.qgeirsoc3cznuklvq5xlalurh1m0unxl, etc.) We don't want to inadvertently yank folders for a different scope under the same
            // identity (at least no more so than NetFX).

            try
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
            catch
            {
                throw new IsolatedStorageException(SR.IsolatedStorage_DeleteDirectories);
            }

            Close();

            string parentDirectory = Path.GetDirectoryName(RootDirectory.TrimEnd(Path.DirectorySeparatorChar));

            if (ContainsUnknownFiles(parentDirectory))
                return;

            try
            {
                Directory.Delete(parentDirectory, recursive: true);
            }
            catch
            {
                return;
            }

            // Domain paths are doubly nested
            // @"C:\Users\jerem\AppData\Local\IsolatedStorage\10v31ho4.bo2\eeolfu22.f2w\Url.qgeirsoc3cznuklvq5xlalurh1m0unxl\Url.qgeirsoc3cznuklvq5xlalurh1m0unxl\Files\"
            if (Helper.IsDomain(Scope))
            {
                parentDirectory = Path.GetDirectoryName(parentDirectory);

                if (ContainsUnknownFiles(parentDirectory))
                    return;

                try
                {
                    Directory.Delete(parentDirectory, recursive: true);
                }
                catch
                {
                    return;
                }
            }
        }

        public static void Remove(IsolatedStorageScope scope)
        {
            // The static Remove() deletes ALL IsoStores for the given scope
            VerifyGlobalScope(scope);

            string root = Helper.GetRootDirectory(scope);

            try
            {
                Directory.Delete(root, recursive: true);
                Directory.CreateDirectory(root);
            }
            catch
            {
                throw new IsolatedStorageException(SR.IsolatedStorage_DeleteDirectories);
            }
        }

        public static bool IsEnabled
        {
            // Isolated storage is always available
            get { return true; }
        }

        private static void VerifyGlobalScope(IsolatedStorageScope scope)
        {
            if ((scope != IsolatedStorageScope.User) &&
                (scope != (IsolatedStorageScope.User |
                          IsolatedStorageScope.Roaming)) &&
                (scope != IsolatedStorageScope.Machine))
            {
                throw new ArgumentException(SR.IsolatedStorage_Scope_U_R_M);
            }
        }

        private bool ContainsUnknownFiles(string directory)
        {
            string[] dirs, files;

            try
            {
                files = Directory.GetFiles(directory);
                dirs = Directory.GetDirectories(directory);
            }
            catch
            {
                throw new IsolatedStorageException(SR.IsolatedStorage_DeleteDirectories);
            }

            if (dirs.Length > 1 || (dirs.Length > 0 && !IsMatchingScopeDirectory(dirs[0])))
            {
                // Unknown folder present
                return true;
            }

            if (files.Length == 0)
                return false;

            // Check if we have unknown files

            // Note that we don't generate these files in CoreFX, but we want to match
            // NetFX removal semantics as NetFX will generate these.

            if (Helper.IsRoaming(Scope))
                return ((files.Length > 1) || !IsIdFile(files[0]));

            return (files.Length > 2 ||
                (
                    (!IsIdFile(files[0]) && !IsInfoFile(files[0]))) ||
                    (files.Length == 2 && !IsIdFile(files[1]) && !IsInfoFile(files[1]))
                );
        }

        private bool IsMatchingScopeDirectory(string directory)
        {
            string directoryName = Path.GetFileName(directory);

            return
                (Helper.IsApplication(Scope) && string.Equals(directoryName, s_appFiles, StringComparison.Ordinal))
                || (Helper.IsAssembly(Scope) && string.Equals(directoryName, s_assemFiles, StringComparison.Ordinal))
                || (Helper.IsDomain(Scope) && string.Equals(directoryName, s_files, StringComparison.Ordinal));
        }

        private static bool IsIdFile(string file) => string.Equals(Path.GetFileName(file), "identity.dat");

        private static bool IsInfoFile(string file) => string.Equals(Path.GetFileName(file), "info.dat");
    }
}
