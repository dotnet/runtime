// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class ApplicationCatalog : ComposablePartCatalog, ICompositionElement
    {
        private bool _isDisposed;
        private volatile AggregateCatalog? _innerCatalog;
        private readonly object _thisLock = new object();
        private readonly ICompositionElement? _definitionOrigin;
        private readonly ReflectionContext? _reflectionContext;

        public ApplicationCatalog() { }

        public ApplicationCatalog(ICompositionElement definitionOrigin)
        {
            Requires.NotNull(definitionOrigin, nameof(definitionOrigin));

            _definitionOrigin = definitionOrigin;
        }

        public ApplicationCatalog(ReflectionContext reflectionContext)
        {
            Requires.NotNull(reflectionContext, nameof(reflectionContext));

            _reflectionContext = reflectionContext;
        }

        public ApplicationCatalog(ReflectionContext reflectionContext, ICompositionElement definitionOrigin)
        {
            Requires.NotNull(reflectionContext, nameof(reflectionContext));
            Requires.NotNull(definitionOrigin, nameof(definitionOrigin));

            _reflectionContext = reflectionContext;
            _definitionOrigin = definitionOrigin;
        }

        internal ComposablePartCatalog CreateCatalog(string location, string pattern)
        {
            if (_reflectionContext != null)
            {
                return (_definitionOrigin != null)
                    ? new DirectoryCatalog(location, pattern, _reflectionContext, _definitionOrigin)
                    : new DirectoryCatalog(location, pattern, _reflectionContext);
            }
            return (_definitionOrigin != null)
                ? new DirectoryCatalog(location, pattern, _definitionOrigin)
                : new DirectoryCatalog(location, pattern);
        }

        //  Note:
        //      Creating a catalog does not cause change notifications to propagate, For some reason the DeploymentCatalog did, but that is a bug.
        //      InnerCatalog is delay evaluated, from data supplied at construction time and so does not propagate change notifications
        private AggregateCatalog InnerCatalog
        {
            get
            {
                if (_innerCatalog == null)
                {
                    lock (_thisLock)
                    {
                        if (_innerCatalog == null)
                        {
                            var location = AppDomain.CurrentDomain.BaseDirectory;
                            if (location == null)
                            {
                                throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                            }

                            var catalogs = new List<ComposablePartCatalog>();
                            catalogs.Add(CreateCatalog(location, "*.exe"));
                            catalogs.Add(CreateCatalog(location, "*.dll"));

                            string? relativeSearchPath = AppDomain.CurrentDomain.RelativeSearchPath;
                            if (!string.IsNullOrEmpty(relativeSearchPath))
                            {
                                string[] probingPaths = relativeSearchPath.Split(';', StringSplitOptions.RemoveEmptyEntries);
                                foreach (var probingPath in probingPaths)
                                {
                                    var path = Path.Combine(location, probingPath);
                                    if (Directory.Exists(path))
                                    {
                                        catalogs.Add(CreateCatalog(path, "*.dll"));
                                    }
                                }
                            }
                            var innerCatalog = new AggregateCatalog(catalogs);
                            _innerCatalog = innerCatalog;
                        }
                    }
                }

                return _innerCatalog;
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!_isDisposed)
                {
                    IDisposable? innerCatalog = null;
                    lock (_thisLock)
                    {
                        innerCatalog = _innerCatalog as IDisposable;
                        _innerCatalog = null;
                        _isDisposed = true;
                    }
                    innerCatalog?.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override IEnumerator<ComposablePartDefinition> GetEnumerator()
        {
            ThrowIfDisposed();

            return InnerCatalog.GetEnumerator();
        }

        /// <summary>
        ///     Returns the export definitions that match the constraint defined by the specified definition.
        /// </summary>
        /// <param name="definition">
        ///     The <see cref="ImportDefinition"/> that defines the conditions of the
        ///     <see cref="ExportDefinition"/> objects to return.
        /// </param>
        /// <returns>
        ///     An <see cref="IEnumerable{T}"/> of <see cref="Tuple{T1, T2}"/> containing the
        ///     <see cref="ExportDefinition"/> objects and their associated
        ///     <see cref="ComposablePartDefinition"/> for objects that match the constraint defined
        ///     by <paramref name="definition"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="definition"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="DirectoryCatalog"/> has been disposed of.
        /// </exception>
        public override IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> GetExports(ImportDefinition definition)
        {
            ThrowIfDisposed();

            Requires.NotNull(definition, nameof(definition));

            return InnerCatalog.GetExports(definition);
        }

        [DebuggerStepThrough]
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw ExceptionBuilder.CreateObjectDisposed(this);
            }
        }

        private string GetDisplayName() =>
            $"{GetType().Name} (Path=\"{AppDomain.CurrentDomain.BaseDirectory}\") (PrivateProbingPath=\"{AppDomain.CurrentDomain.RelativeSearchPath}\")";   // NOLOC

        /// <summary>
        ///     Returns a string representation of the directory catalog.
        /// </summary>
        /// <returns>
        ///     A <see cref="string"/> containing the string representation of the <see cref="DirectoryCatalog"/>.
        /// </returns>
        public override string ToString()
        {
            return GetDisplayName();
        }

        /// <summary>
        ///     Gets the display name of the ApplicationCatalog.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> containing a human-readable display name of the <see cref="ApplicationCatalog"/>.
        /// </value>
        string ICompositionElement.DisplayName
        {
            get { return GetDisplayName(); }
        }

        /// <summary>
        ///     Gets the composition element from which the ApplicationCatalog originated.
        /// </summary>
        /// <value>
        ///     This property always returns <see langword="null"/>.
        /// </value>
        ICompositionElement? ICompositionElement.Origin
        {
            get { return null; }
        }
    }
}
