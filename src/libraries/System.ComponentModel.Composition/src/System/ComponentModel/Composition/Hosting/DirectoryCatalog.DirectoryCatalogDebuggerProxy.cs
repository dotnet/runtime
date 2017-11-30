// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using Microsoft.Internal;
using Microsoft.Internal.Collections;

namespace System.ComponentModel.Composition.Hosting
{
    partial class DirectoryCatalog
    {
        internal class DirectoryCatalogDebuggerProxy
        {
            private readonly DirectoryCatalog _catalog;

            public DirectoryCatalogDebuggerProxy(DirectoryCatalog catalog)
            {
                Requires.NotNull(catalog, nameof(catalog));

                _catalog = catalog;
            }

            public ReadOnlyCollection<Assembly> Assemblies
            {
                get
                {
                    return _catalog._assemblyCatalogs.Values.Select(catalog => catalog.Assembly)
                                                                 .ToReadOnlyCollection();
                }
            }

            public ReflectionContext ReflectionContext
            {
                get
                {
                    return _catalog._reflectionContext;
                }
            }

            public string SearchPattern
            {
                get { return _catalog.SearchPattern; }
            }

            public string Path
            {
                get { return _catalog._path; }
            }

            public string FullPath
            {
                get { return _catalog._fullPath; }
            }

            public ReadOnlyCollection<string> LoadedFiles
            {
                get { return _catalog._loadedFiles; }
            }

            public ReadOnlyCollection<ComposablePartDefinition> Parts
            {
                // NOTE: This shouldn't be cached, so that on every query of
                // the current value of the underlying catalog is respected.
                // We use ReadOnlyCollection as arrays do not have the 
                // appropriate debugger display attributes applied to them.
                get { return _catalog.Parts.ToReadOnlyCollection(); }
            }
        }
    }
}
