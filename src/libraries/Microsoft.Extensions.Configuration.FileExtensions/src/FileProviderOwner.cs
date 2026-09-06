// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Configuration
{
    // A source can produce several simultaneously live providers, so its PhysicalFileProvider can be
    // disposed only after the last provider releases it. A later build recreates it from the same root.
    internal sealed class FileProviderOwner
    {
        private readonly string _root;
        private readonly object _syncObj = new object();
        private PhysicalFileProvider? _fileProvider;
        private int _referenceCount;

        public FileProviderOwner(PhysicalFileProvider fileProvider)
        {
            // FileConfigurationSource only owns providers it created with the default filters, so the root
            // contains everything needed to create an equivalent replacement after the final release.
            _fileProvider = fileProvider;
            _root = fileProvider.Root;
        }

        public IFileProvider Acquire()
        {
            lock (_syncObj)
            {
                PhysicalFileProvider fileProvider = _fileProvider ??= new PhysicalFileProvider(_root);
                _referenceCount++;
                return fileProvider;
            }
        }

        public void Release()
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

            // PhysicalFileProvider.Dispose can call into file-system watcher implementations.
            fileProvider?.Dispose();
        }

        public void Retire()
        {
            PhysicalFileProvider? fileProvider = null;

            lock (_syncObj)
            {
                if (_referenceCount == 0)
                {
                    fileProvider = _fileProvider;
                    _fileProvider = null;
                }
            }

            // PhysicalFileProvider.Dispose can call into file-system watcher implementations.
            fileProvider?.Dispose();
        }
    }
}
