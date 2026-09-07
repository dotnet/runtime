// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Configuration
{
    internal abstract class FileProviderHandle
    {
        public static FileProviderHandle CreateBorrowed(IFileProvider fileProvider)
            => new BorrowedFileProviderHandle(fileProvider);

        public static FileProviderHandle CreateOwned(PhysicalFileProvider fileProvider)
            => new OwnedFileProviderHandle(fileProvider);

        public abstract IFileProvider? Current { get; }

        public abstract IFileProvider GetOrCreate();

        public virtual void Acquire() { }

        public virtual void Release() { }

        public virtual void DiscardIfIdle() { }

        private sealed class BorrowedFileProviderHandle : FileProviderHandle
        {
            private readonly IFileProvider _fileProvider;

            public BorrowedFileProviderHandle(IFileProvider fileProvider)
            {
                _fileProvider = fileProvider;
            }

            public override IFileProvider Current => _fileProvider;

            public override IFileProvider GetOrCreate() => _fileProvider;
        }

        private sealed class OwnedFileProviderHandle : FileProviderHandle
        {
            private readonly string _root;
            private readonly object _syncObj = new object();
            private PhysicalFileProvider? _fileProvider;
            private int _referenceCount;
            private PollingSettings _settings;

            public OwnedFileProviderHandle(PhysicalFileProvider fileProvider)
            {
                _fileProvider = fileProvider;
                _root = fileProvider.Root;
            }

            public override IFileProvider? Current
            {
                get
                {
                    lock (_syncObj)
                    {
                        return _fileProvider;
                    }
                }
            }

            public override IFileProvider GetOrCreate()
            {
                lock (_syncObj)
                {
                    return GetOrCreateCore();
                }
            }

            public override void Acquire()
            {
                lock (_syncObj)
                {
                    PhysicalFileProvider fileProvider = GetOrCreateCore();
                    if (_referenceCount == 0)
                    {
                        _settings = PollingSettings.Capture(fileProvider);
                    }

                    _referenceCount++;
                }
            }

            public override void Release()
            {
                PhysicalFileProvider? fileProvider = null;

                lock (_syncObj)
                {
                    Debug.Assert(_referenceCount > 0);

                    if (--_referenceCount == 0)
                    {
                        fileProvider = _fileProvider;
                        _fileProvider = null;
                    }
                }

                Dispose(fileProvider);
            }

            public override void DiscardIfIdle()
            {
                PhysicalFileProvider? fileProvider = null;

                lock (_syncObj)
                {
                    if (_referenceCount == 0 && _fileProvider is not null)
                    {
                        _settings = PollingSettings.Capture(_fileProvider);
                        fileProvider = _fileProvider;
                        _fileProvider = null;
                    }
                }

                Dispose(fileProvider);
            }

            private PhysicalFileProvider GetOrCreateCore()
            {
                return _fileProvider ??= new PhysicalFileProvider(_root)
                {
                    UsePollingFileWatcher = _settings.UsePollingFileWatcher,
                    UseActivePolling = _settings.UseActivePolling
                };
            }

            private static void Dispose(PhysicalFileProvider? fileProvider)
            {
                // PhysicalFileProvider.Dispose can call into file-system watcher implementations.
                fileProvider?.Dispose();
            }
        }

        private readonly struct PollingSettings
        {
            private PollingSettings(bool usePollingFileWatcher, bool useActivePolling)
            {
                UsePollingFileWatcher = usePollingFileWatcher;
                UseActivePolling = useActivePolling;
            }

            public bool UsePollingFileWatcher { get; }
            public bool UseActivePolling { get; }

            public static PollingSettings Capture(PhysicalFileProvider fileProvider) =>
                new PollingSettings(fileProvider.UsePollingFileWatcher, fileProvider.UseActivePolling);
        }
    }
}
