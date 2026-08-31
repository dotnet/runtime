// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition.Primitives;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Hosting
{
    /// <summary>
    ///     A mutable collection of <see cref="ComposablePartCatalog"/>s.
    /// </summary>
    /// <remarks>
    ///     This type is thread safe.
    /// </remarks>
    public class CompositionService : ICompositionService, IDisposable
    {
        private readonly CompositionContainer? _compositionContainer;
        private readonly INotifyComposablePartCatalogChanged? _notifyCatalog;

        internal CompositionService(ComposablePartCatalog composablePartCatalog)
        {
            ArgumentNullException.ThrowIfNull(composablePartCatalog);

            _notifyCatalog = composablePartCatalog as INotifyComposablePartCatalogChanged;
            try
            {
                if (_notifyCatalog != null)
                {
                    _notifyCatalog.Changing += OnCatalogChanging;
                }

                var compositionOptions = CompositionOptions.DisableSilentRejection | CompositionOptions.IsThreadSafe | CompositionOptions.ExportCompositionService;
                var compositionContainer = new CompositionContainer(composablePartCatalog, compositionOptions);

                _compositionContainer = compositionContainer;
            }
            catch
            {
                if (_notifyCatalog != null)
                {
                    _notifyCatalog.Changing -= OnCatalogChanging;
                }
                throw;
            }
        }

        public void SatisfyImportsOnce(ComposablePart part)
        {
            Requires.NotNull(part, nameof(part));
            if (_compositionContainer == null)
            {
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);
            }
            _compositionContainer.SatisfyImportsOnce(part);
        }

        public void Dispose()
        {
            if (_compositionContainer == null)
            {
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);
            }

            // Delegates are cool there is no concern if you try to remove an item from them and they don't exist
            if (_notifyCatalog != null)
            {
                _notifyCatalog.Changing -= OnCatalogChanging;
            }
            _compositionContainer.Dispose();
        }

        private void OnCatalogChanging(object? sender, ComposablePartCatalogChangeEventArgs e)
        {
            throw new ChangeRejectedException(SR.NotSupportedCatalogChanges);
        }
    }
}
