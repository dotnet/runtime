// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using Microsoft.NET.HostModel.MachO;

#nullable enable

namespace Microsoft.NET.HostModel.AppHost
{
    public abstract class FileManager
    {
        private MemoryMappedFile? _memoryMappedFile;
        private MemoryMappedViewAccessor? _memoryMappedViewAccessor;
        private FileStream _fileStream;
        private MemoryMappedViewStream? _memoryMappedViewStream;

        public FileManager(FileStream fileStream)
        {
            _fileStream = fileStream;
        }

        public virtual MemoryMappedFile GetMemoryMappedFile()
        {
            return _memoryMappedFile ??= MemoryMappedFile.CreateFromFile(_fileStream, null, _fileStream.Length, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, leaveOpen: true);
        }

        public virtual MemoryMappedViewAccessor GetMemoryMappedViewAccessor(long? length = null)
        {
            if (_fileStream.Length < length)
            {
                _memoryMappedFile.Cl
            }

        }

        public virtual Stream GetStream()
        {
            if (_memoryMappedFile != null)
            {
                return _memoryMappedViewStream ??= _memoryMappedFile.CreateViewStream();
            }
            return _fileStream;
        }

        public void SetLength()
        {
            _memoryMappedViewStream?.Dispose();
            _memoryMappedViewAccessor?.Dispose();
            _memoryMappedFile?.Dispose();
            _fileStream.SetLength(_fileStream.Length);
        }
    }

    public abstract class MemoryMappedHost
    {
        /// <summary>
        /// hash value embedded in default apphost executable in a place where the path to the app binary should be stored.
        /// </summary>
        protected const string AppBinaryPathPlaceholder = "c3ab8ff13720e8ad9047dd39466b3c8974e592c2fa383d4a3960714caef0c4f2";
        protected static readonly byte[] AppBinaryPathPlaceholderSearchValue = Encoding.UTF8.GetBytes(AppBinaryPathPlaceholder);

        // See placeholder array in corehost.cpp
        protected const int MaxAppBinaryPathSizeInBytes = 1024;

        /// <summary>
        /// Value embedded in default apphost executable for configuration of how it will search for the .NET install
        /// </summary>
        protected const string DotNetSearchPlaceholder = "\0\019ff3e9c3602ae8e841925bb461a0adb064a1f1903667a5e0d87e8f608f425ac";
        protected static readonly byte[] DotNetSearchPlaceholderSearchValue = Encoding.UTF8.GetBytes(DotNetSearchPlaceholder);

        // See placeholder array in hostfxr_resolver.cpp
        protected const int MaxDotNetSearchSizeInBytes = 512;
        protected const int MaxAppRelativeDotNetSizeInBytes = MaxDotNetSearchSizeInBytes - 3; // -2 for search location + null, -1 for null terminator
        protected string _appHostSourcePath;
        protected string _modifiedAppHostPath;
        protected string _destinationPath;
        protected bool _macosCodesign;

        public MemoryMappedHost(string appHostSourcePath, string modifiedAppHostPath, string destinationPath, bool macosCodesign)
        {
            _appHostSourcePath = appHostSourcePath;
            _modifiedAppHostPath = modifiedAppHostPath;
            _destinationPath = destinationPath;
            _macosCodesign = macosCodesign;
        }

        public void Execute()
        {
            try
            {
                bool isMachOImage;
                using (FileStream appHostDestinationStream = new FileStream(_modifiedAppHostPath, FileMode.CreateNew, FileAccess.ReadWrite))
                {
                    using (FileStream appHostSourceStream = new(_appHostSourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1))
                    {
                        isMachOImage = MachObjectFile.IsMachOImage(appHostSourceStream);
                        if (!isMachOImage && _macosCodesign)
                        {
                            throw new InvalidDataException("Cannot sign a non-Mach-O file.");
                        }
                        appHostSourceStream.CopyTo(appHostDestinationStream);
                    }
                    // Get the size of the source app host to ensure that we don't write extra data to the destination.
                    // On Windows, the size of the view accessor is rounded up to the next page boundary.
                    long appHostLength = appHostDestinationStream.Length;
                    string destinationFileName = Path.GetFileName(_modifiedAppHostPath);
                    // On Mac, we need to extend the file size to accommodate the signature.
                    long appHostTmpCapacity = _macosCodesign ?
                        appHostLength + MachObjectFile.GetSignatureSizeEstimate((uint)appHostLength, destinationFileName)
                        : appHostLength;

                    using (MemoryMappedFile memoryMappedFile = MemoryMappedFile.CreateFromFile(appHostDestinationStream, null, appHostTmpCapacity, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true))
                    using (MemoryMappedViewAccessor memoryMappedViewAccessor = memoryMappedFile.CreateViewAccessor(0, appHostTmpCapacity, MemoryMappedFileAccess.ReadWrite))
                    {
                        Modify(memoryMappedFile, memoryMappedViewAccessor);
                        appHostLength = Sign();
                    }
                    appHostDestinationStream.SetLength(appHostLength);
                    long? newSize = Modify(appHostDestinationStream);
                    if (newSize.HasValue)
                    {
                        appHostDestinationStream.SetLength(newSize.Value);
                    }
                }

                if (_destinationPath != _modifiedAppHostPath)
                    File.Copy(_modifiedAppHostPath, _destinationPath, overwrite: true);
            }
            catch (Exception)
            {
                File.Delete(_modifiedAppHostPath);
                File.Delete(_destinationPath);
                throw;
            }
            finally
            {
                if (_destinationPath != _modifiedAppHostPath)
                    File.Delete(_modifiedAppHostPath);
            }
        }

        private long Sign() => throw new NotImplementedException();

        protected virtual void Modify(MemoryMappedFile file, MemoryMappedViewAccessor action)
        { }

        protected virtual long? Modify(Stream file)
        { return null; }


        public void AppHost()
        {
            bool isMachOImage;
            // MacOS requires a new inode to be created when updating a signed file, so we'll delete the file and create a new one.
            if (File.Exists(_modifiedAppHostPath))
                File.Delete(_modifiedAppHostPath);

            using (FileStream appHostDestinationStream = new FileStream(_modifiedAppHostPath, FileMode.CreateNew, FileAccess.ReadWrite))
            {
                using (FileStream appHostSourceStream = new(_appHostSourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1))
                {
                    isMachOImage = MachObjectFile.IsMachOImage(appHostSourceStream);
                    if (!isMachOImage && _macosCodesign)
                    {
                        throw new InvalidDataException("Cannot sign a non-Mach-O file.");
                    }
                    appHostSourceStream.CopyTo(appHostDestinationStream);
                }
                // Get the size of the source app host to ensure that we don't write extra data to the destination.
                // On Windows, the size of the view accessor is rounded up to the next page boundary.
                long appHostLength = appHostDestinationStream.Length;
                string destinationFileName = Path.GetFileName(_modifiedAppHostPath);
                // On Mac, we need to extend the file size to accommodate the signature.
                long appHostTmpCapacity = _macosCodesign ?
                    appHostLength + MachObjectFile.GetSignatureSizeEstimate((uint)appHostLength, destinationFileName)
                    : appHostLength;

                using (MemoryMappedFile memoryMappedFile = MemoryMappedFile.CreateFromFile(appHostDestinationStream, null, appHostTmpCapacity, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true))
                using (MemoryMappedViewAccessor memoryMappedViewAccessor = memoryMappedFile.CreateViewAccessor(0, appHostTmpCapacity, MemoryMappedFileAccess.ReadWrite))
                {
                    // Transform the host file in-memory.
                    RewriteAppHost(memoryMappedFile, memoryMappedViewAccessor);
                    if (isMachOImage)
                    {
                        if (_macosCodesign)
                        {
                            MachObjectFile machObjectFile = MachObjectFile.Create(memoryMappedViewAccessor);
                            appHostLength = machObjectFile.CreateAdHocSignature(memoryMappedViewAccessor, destinationFileName);
                        }
                        else if (MachObjectFile.RemoveCodeSignatureIfPresent(memoryMappedViewAccessor, out long? length))
                        {
                            appHostLength = length.Value;
                        }
                    }
                }
                appHostDestinationStream.SetLength(appHostLength);

                if (assemblyToCopyResourcesFrom != null && appHostIsPEImage)
                {
                    using var updater = new ResourceUpdater(appHostDestinationStream, true);
                    updater.AddResourcesFromPEImage(assemblyToCopyResourcesFrom);
                    updater.Update();
                }
            }
        }

        public void SingleFileHost()
        {
            string tmpFile = null;
            try
            {
                tmpFile = Path.GetTempFileName();
                using (FileStream newBundleStream = new FileStream(tmpFile, FileMode.Create, FileAccess.ReadWrite))
                {
                    using (FileStream oldBundleStream = new FileStream(_appHostSourcePath, FileMode.Open, FileAccess.Read))
                    {
                        oldBundleStream.CopyTo(newBundleStream);
                    }

                    long bundleSize = newBundleStream.Length;
                    long mmapFileSize = _macosCodesign
                        ? bundleSize + MachObjectFile.GetSignatureSizeEstimate((uint)bundleSize, Path.GetFileName(_appHostSourcePath))
                        : bundleSize;
                    using (MemoryMappedFile memoryMappedFile = MemoryMappedFile.CreateFromFile(newBundleStream, null, mmapFileSize, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, leaveOpen: true))
                    using (MemoryMappedViewAccessor accessor = memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite))
                    {
                        BinaryUtils.SearchAndReplace(accessor,
                                                    bundleHeaderPlaceholder,
                                                    BitConverter.GetBytes(bundleHeaderOffset),
                                                    pad0s: false);

                        if (MachObjectFile.IsMachOImage(accessor))
                        {
                            var machObjectFile = MachObjectFile.Create(accessor);
                            if (machObjectFile.HasSignature)
                                throw new AppHostMachOFormatException(MachOFormatError.SignNotRemoved);

                            bool wasBundled = machObjectFile.TryAdjustHeadersForBundle((ulong)bundleSize, accessor);
                            if (!wasBundled)
                                throw new InvalidOperationException("The single-file bundle was unable to be created. This is likely because the bundled content is too large.");

                            if (_macosCodesign)
                                bundleSize = machObjectFile.CreateAdHocSignature(accessor, Path.GetFileName(_appHostSourcePath));
                        }
                    }
                    newBundleStream.SetLength(bundleSize);
                }
                File.Copy(tmpFile, appHostPath, overwrite: true);
                Chmod755(appHostPath);
            }
            finally
            {
                if (tmpFile is not null)
                    File.Delete(tmpFile);
            }
        }
    }

    public class FrameworkDependentAppHost : MemoryMappedHost
    {
        private bool _appHostIsPEImage;
        private bool _disableCetCompat;
        private bool _windowsGraphicalUserInterface;
        private byte[] _searchOptionsBytes;
        private byte[] _appPathBytes;

        public FrameworkDependentAppHost(
            string appHostSourcePath,
            string modifiedAppHostPath,
            string destinationPath,
            bool macosCodesign,
            string appPath,
            bool appHostIsPEImage,
            bool disableCetCompat,
            bool windowsGraphicalUserInterface,
            byte[] searchOptionsBytes)
            : base(appHostSourcePath, modifiedAppHostPath, destinationPath, macosCodesign)
        {
            _appHostIsPEImage = appHostIsPEImage;
            _disableCetCompat = disableCetCompat;
            _windowsGraphicalUserInterface = windowsGraphicalUserInterface;
            _searchOptionsBytes = searchOptionsBytes;
            _appPathBytes = Encoding.UTF8.GetBytes(appPath);
        }


        protected override ulong? Modify(MemoryMappedFile file, MemoryMappedViewAccessor accessor)
        {
            RewriteAppHost(file, accessor);
        }

        protected override ulong? Modify(Stream file)
        {
        }

        private void RewriteAppHost(MemoryMappedFile mappedFile, MemoryMappedViewAccessor accessor)
        {
            // Re-write the destination apphost with the proper contents.
            BinaryUtils.SearchAndReplace(accessor, AppBinaryPathPlaceholderSearchValue, _appPathBytes);

            // Update the .NET search configuration
            if (_searchOptionsBytes != null)
            {
                BinaryUtils.SearchAndReplace(accessor, DotNetSearchPlaceholderSearchValue, _searchOptionsBytes);
            }

            _appHostIsPEImage = PEUtils.IsPEImage(accessor);

            if (_windowsGraphicalUserInterface)
            {
                if (!_appHostIsPEImage)
                {
                    throw new AppHostNotPEFileException("PE file signature not found.");
                }

                PEUtils.SetWindowsGraphicalUserInterfaceBit(accessor);
            }

            if (_disableCetCompat && _appHostIsPEImage)
            {
                PEUtils.RemoveCetCompatBit(mappedFile, accessor);
            }
        }

    }
}
