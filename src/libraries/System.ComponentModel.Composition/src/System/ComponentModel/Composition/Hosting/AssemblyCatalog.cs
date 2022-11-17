// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Hosting
{
    /// <summary>
    ///     An immutable ComposablePartCatalog created from a managed code assembly.
    /// </summary>
    /// <remarks>
    ///     This type is thread safe.
    /// </remarks>
    [DebuggerTypeProxy(typeof(AssemblyCatalogDebuggerProxy))]
    public class AssemblyCatalog : ComposablePartCatalog, ICompositionElement
    {
        private readonly object _thisLock = new object();
        private readonly ICompositionElement _definitionOrigin;
        private volatile Assembly _assembly;
        private volatile ComposablePartCatalog? _innerCatalog;
        private int _isDisposed;

        private readonly ReflectionContext? _reflectionContext;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AssemblyCatalog"/> class
        ///     with the specified code base.
        /// </summary>
        /// <param name="codeBase">
        ///     A <see cref="string"/> containing the code base of the assembly containing the
        ///     attributed <see cref="Type"/> objects to add to the <see cref="AssemblyCatalog"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="codeBase"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="codeBase"/> is a zero-length string, contains only white space,
        ///     or contains one or more invalid characters. />.
        /// </exception>
        /// <exception cref="PathTooLongException">
        ///     The specified path, file name, or both exceed the system-defined maximum length.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        ///     The caller does not have path discovery permission.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        ///     <paramref name="codeBase"/> is not found.
        /// </exception>
        /// <exception cref="FileLoadException ">
        ///     <paramref name="codeBase"/> could not be loaded.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="codeBase"/> specified a directory.
        /// </exception>
        /// <exception cref="BadImageFormatException">
        ///     <paramref name="codeBase"/> is not a valid assembly
        ///     -or-
        ///     Version 2.0 or later of the common language runtime is currently loaded
        ///     and <paramref name="codeBase"/> was compiled with a later version.
        /// </exception>
        /// <remarks>
        ///     The assembly referenced by <paramref langword="codeBase"/> is loaded into the Load context.
        /// </remarks>
        public AssemblyCatalog(string codeBase)
        {
            Requires.NotNullOrEmpty(codeBase, nameof(codeBase));

            InitializeAssemblyCatalog(LoadAssembly(codeBase));
            _definitionOrigin = this;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AssemblyCatalog"/> class
        ///     with the specified code base.
        /// </summary>
        /// <param name="codeBase">
        ///     A <see cref="string"/> containing the code base of the assembly containing the
        ///     attributed <see cref="Type"/> objects to add to the <see cref="AssemblyCatalog"/>.
        /// </param>
        /// <param name="reflectionContext">
        ///     The <see cref="ReflectionContext"/> a context used by the catalog when
        ///     interpreting the types to inject attributes into the type definition<see cref="AssemblyCatalog"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="codeBase"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="reflectionContext"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="codeBase"/> is a zero-length string, contains only white space,
        ///     or contains one or more invalid characters. />.
        /// </exception>
        /// <exception cref="PathTooLongException">
        ///     The specified path, file name, or both exceed the system-defined maximum length.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        ///     The caller does not have path discovery permission.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        ///     <paramref name="codeBase"/> is not found.
        /// </exception>
        /// <exception cref="FileLoadException ">
        ///     <paramref name="codeBase"/> could not be loaded.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="codeBase"/> specified a directory.
        /// </exception>
        /// <exception cref="BadImageFormatException">
        ///     <paramref name="codeBase"/> is not a valid assembly
        ///     -or-
        ///     Version 2.0 or later of the common language runtime is currently loaded
        ///     and <paramref name="codeBase"/> was compiled with a later version.
        /// </exception>
        /// <remarks>
        ///     The assembly referenced by <paramref langword="codeBase"/> is loaded into the Load context.
        /// </remarks>
        public AssemblyCatalog(string codeBase, ReflectionContext reflectionContext)
        {
            Requires.NotNullOrEmpty(codeBase, nameof(codeBase));
            Requires.NotNull(reflectionContext, nameof(reflectionContext));

            InitializeAssemblyCatalog(LoadAssembly(codeBase));
            _reflectionContext = reflectionContext;
            _definitionOrigin = this;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AssemblyCatalog"/> class
        ///     with the specified code base.
        /// </summary>
        /// <param name="codeBase">
        ///     A <see cref="string"/> containing the code base of the assembly containing the
        ///     attributed <see cref="Type"/> objects to add to the <see cref="AssemblyCatalog"/>.
        /// </param>
        /// <param name="definitionOrigin">
        ///     The <see cref="ICompositionElement"/> CompositionElement used by Diagnostics to identify the source for parts.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="codeBase"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="definitionOrigin"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="codeBase"/> is a zero-length string, contains only white space,
        ///     or contains one or more invalid characters. />.
        /// </exception>
        /// <exception cref="PathTooLongException">
        ///     The specified path, file name, or both exceed the system-defined maximum length.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        ///     The caller does not have path discovery permission.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        ///     <paramref name="codeBase"/> is not found.
        /// </exception>
        /// <exception cref="FileLoadException ">
        ///     <paramref name="codeBase"/> could not be loaded.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="codeBase"/> specified a directory.
        /// </exception>
        /// <exception cref="BadImageFormatException">
        ///     <paramref name="codeBase"/> is not a valid assembly
        ///     -or-
        ///     Version 2.0 or later of the common language runtime is currently loaded
        ///     and <paramref name="codeBase"/> was compiled with a later version.
        /// </exception>
        /// <remarks>
        ///     The assembly referenced by <paramref langword="codeBase"/> is loaded into the Load context.
        /// </remarks>
        public AssemblyCatalog(string codeBase, ICompositionElement definitionOrigin)
        {
            Requires.NotNullOrEmpty(codeBase, nameof(codeBase));
            Requires.NotNull(definitionOrigin, nameof(definitionOrigin));

            InitializeAssemblyCatalog(LoadAssembly(codeBase));
            _definitionOrigin = definitionOrigin;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AssemblyCatalog"/> class
        ///     with the specified code base.
        /// </summary>
        /// <param name="codeBase">
        ///     A <see cref="string"/> containing the code base of the assembly containing the
        ///     attributed <see cref="Type"/> objects to add to the <see cref="AssemblyCatalog"/>.
        /// </param>
        /// <param name="reflectionContext">
        ///     The <see cref="ReflectionContext"/> a context used by the catalog when
        ///     interpreting the types to inject attributes into the type definition<see cref="AssemblyCatalog"/>.
        /// </param>
        /// <param name="definitionOrigin">
        ///     The <see cref="ICompositionElement"/> CompositionElement used by Diagnostics to identify the source for parts.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="codeBase"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="reflectionContext"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="definitionOrigin"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="codeBase"/> is a zero-length string, contains only white space,
        ///     or contains one or more invalid characters. />.
        /// </exception>
        /// <exception cref="PathTooLongException">
        ///     The specified path, file name, or both exceed the system-defined maximum length.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        ///     The caller does not have path discovery permission.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        ///     <paramref name="codeBase"/> is not found.
        /// </exception>
        /// <exception cref="FileLoadException ">
        ///     <paramref name="codeBase"/> could not be loaded.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="codeBase"/> specified a directory.
        /// </exception>
        /// <exception cref="BadImageFormatException">
        ///     <paramref name="codeBase"/> is not a valid assembly
        ///     -or-
        ///     Version 2.0 or later of the common language runtime is currently loaded
        ///     and <paramref name="codeBase"/> was compiled with a later version.
        /// </exception>
        /// <remarks>
        ///     The assembly referenced by <paramref langword="codeBase"/> is loaded into the Load context.
        /// </remarks>
        public AssemblyCatalog(string codeBase, ReflectionContext reflectionContext, ICompositionElement definitionOrigin)
        {
            Requires.NotNullOrEmpty(codeBase, nameof(codeBase));
            Requires.NotNull(reflectionContext, nameof(reflectionContext));
            Requires.NotNull(definitionOrigin, nameof(definitionOrigin));

            InitializeAssemblyCatalog(LoadAssembly(codeBase));
            _reflectionContext = reflectionContext;
            _definitionOrigin = definitionOrigin;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AssemblyCatalog"/> class
        ///     with the specified assembly and reflection context.
        /// </summary>
        /// <param name="assembly">
        ///     The <see cref="Assembly"/> containing the attributed <see cref="Type"/> objects to
        ///     add to the <see cref="AssemblyCatalog"/>.
        /// </param>
        /// <param name="reflectionContext">
        ///     The <see cref="ReflectionContext"/> a context used by the catalog when
        ///     interpreting the types to inject attributes into the type definition.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <paramref name="assembly"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="assembly"/> was loaded in the reflection-only context.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="reflectionContext"/> is <see langword="null"/>.
        /// </exception>
        public AssemblyCatalog(Assembly assembly, ReflectionContext reflectionContext)
        {
            Requires.NotNull(assembly, nameof(assembly));
            Requires.NotNull(reflectionContext, nameof(reflectionContext));

            InitializeAssemblyCatalog(assembly);
            _reflectionContext = reflectionContext;
            _definitionOrigin = this;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AssemblyCatalog"/> class
        ///     with the specified assembly, reflectionContext and definitionOrigin.
        /// </summary>
        /// <param name="assembly">
        ///     The <see cref="Assembly"/> containing the attributed <see cref="Type"/> objects to
        ///     add to the <see cref="AssemblyCatalog"/>.
        /// </param>
        /// <param name="reflectionContext">
        ///     The <see cref="ReflectionContext"/> a context used by the catalog when
        ///     interpreting the types to inject attributes into the type definition.
        /// </param>
        /// <param name="definitionOrigin">
        ///     The <see cref="ICompositionElement"/> CompositionElement used by Diagnostics to identify the source for parts.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <paramref name="assembly"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="assembly"/> was loaded in the reflection-only context.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="reflectionContext"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="definitionOrigin"/> is <see langword="null"/>.
        /// </exception>
        public AssemblyCatalog(Assembly assembly, ReflectionContext reflectionContext, ICompositionElement definitionOrigin)
        {
            Requires.NotNull(assembly, nameof(assembly));
            Requires.NotNull(reflectionContext, nameof(reflectionContext));
            Requires.NotNull(definitionOrigin, nameof(definitionOrigin));

            InitializeAssemblyCatalog(assembly);
            _reflectionContext = reflectionContext;
            _definitionOrigin = definitionOrigin;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AssemblyCatalog"/> class
        ///     with the specified assembly.
        /// </summary>
        /// <param name="assembly">
        ///     The <see cref="Assembly"/> containing the attributed <see cref="Type"/> objects to
        ///     add to the <see cref="AssemblyCatalog"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <paramref name="assembly"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="assembly"/> was loaded in the reflection-only context.
        /// </exception>
        public AssemblyCatalog(Assembly assembly)
        {
            Requires.NotNull(assembly, nameof(assembly));

            InitializeAssemblyCatalog(assembly);
            _definitionOrigin = this;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AssemblyCatalog"/> class
        ///     with the specified assembly.
        /// </summary>
        /// <param name="assembly">
        ///     The <see cref="Assembly"/> containing the attributed <see cref="Type"/> objects to
        ///     add to the <see cref="AssemblyCatalog"/>.
        /// </param>
        /// <param name="definitionOrigin">
        ///     The <see cref="ICompositionElement"/> CompositionElement used by Diagnostics to identify the source for parts.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <paramref name="assembly"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="assembly"/> was loaded in the reflection-only context.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="definitionOrigin"/> is <see langword="null"/>.
        /// </exception>
        public AssemblyCatalog(Assembly assembly, ICompositionElement definitionOrigin)
        {
            Requires.NotNull(assembly, nameof(assembly));
            Requires.NotNull(definitionOrigin, nameof(definitionOrigin));

            InitializeAssemblyCatalog(assembly);
            _definitionOrigin = definitionOrigin;
        }

        [MemberNotNull(nameof(_assembly))]
        private void InitializeAssemblyCatalog(Assembly assembly)
        {
            if (assembly.ReflectionOnly)
            {
                throw new ArgumentException(SR.Format(SR.Argument_AssemblyReflectionOnly, nameof(assembly)), nameof(assembly));
            }
            _assembly = assembly;
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
        ///     The <see cref="ComposablePartCatalog"/> has been disposed of.
        /// </exception>
        /// <remarks>
        ///     <note type="inheritinfo">
        ///         Overriders of this property should never return <see langword="null"/>, if no
        ///         <see cref="ExportDefinition"/> match the conditions defined by
        ///         <paramref name="definition"/>, return an empty <see cref="IEnumerable{T}"/>.
        ///     </note>
        /// </remarks>
        public override IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> GetExports(ImportDefinition definition)
        {
            return InnerCatalog.GetExports(definition);
        }

        private ComposablePartCatalog InnerCatalog
        {
            get
            {
                ThrowIfDisposed();

                if (_innerCatalog == null)
                {
                    var catalogReflectionContextAttribute = _assembly.GetFirstAttribute<CatalogReflectionContextAttribute>();
                    var assembly = (catalogReflectionContextAttribute != null)
                        ? catalogReflectionContextAttribute.CreateReflectionContext().MapAssembly(_assembly)
                        : _assembly;
                    lock (_thisLock)
                    {
                        if (_innerCatalog == null)
                        {
                            var catalog = (_reflectionContext != null)
                                ? new TypeCatalog(assembly.GetTypes(), _reflectionContext, _definitionOrigin)
                                : new TypeCatalog(assembly.GetTypes(), _definitionOrigin);
                            Thread.MemoryBarrier();
                            _innerCatalog = catalog;
                        }
                    }
                }
                return _innerCatalog;
            }
        }

        /// <summary>
        ///     Gets the assembly containing the attributed types contained within the assembly
        ///     catalog.
        /// </summary>
        /// <value>
        ///     The <see cref="Assembly"/> containing the attributed <see cref="Type"/> objects
        ///     contained within the <see cref="AssemblyCatalog"/>.
        /// </value>
        public Assembly Assembly
        {
            get
            {
                Debug.Assert(_assembly != null);

                return _assembly;
            }
        }

        /// <summary>
        ///     Gets the display name of the assembly catalog.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> containing a human-readable display name of the <see cref="AssemblyCatalog"/>.
        /// </value>
        string ICompositionElement.DisplayName
        {
            get { return GetDisplayName(); }
        }

        /// <summary>
        ///     Gets the composition element from which the assembly catalog originated.
        /// </summary>
        /// <value>
        ///     This property always returns <see langword="null"/>.
        /// </value>
        ICompositionElement? ICompositionElement.Origin
        {
            get { return null; }
        }

        /// <summary>
        ///     Returns a string representation of the assembly catalog.
        /// </summary>
        /// <returns>
        ///     A <see cref="string"/> containing the string representation of the <see cref="AssemblyCatalog"/>.
        /// </returns>
        public override string ToString()
        {
            return GetDisplayName();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
                {
                    if (disposing)
                    {
                        _innerCatalog?.Dispose();
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override IEnumerator<ComposablePartDefinition> GetEnumerator()
        {
            return InnerCatalog.GetEnumerator();
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed == 1)
            {
                throw ExceptionBuilder.CreateObjectDisposed(this);
            }
        }

        private string GetDisplayName() =>
            $"{GetType().Name} (Assembly=\"{Assembly.FullName}\")";   // NOLOC

        [UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file",
            Justification = "Setting a CodeBase is single file compatible")]
        private static Assembly LoadAssembly(string codeBase)
        {
            Requires.NotNullOrEmpty(codeBase, nameof(codeBase));

            AssemblyName assemblyName;

            try
            {
                assemblyName = AssemblyName.GetAssemblyName(codeBase);
            }
            catch (ArgumentException)
            {
                assemblyName = new AssemblyName();
#pragma warning disable SYSLIB0044 // AssemblyName.CodeBase and AssemblyName.EscapedCodeBase are obsolete. Using them for loading an assembly is not supported.
                assemblyName.CodeBase = codeBase;
#pragma warning restore SYSLIB0044
            }

            try
            {
                return Assembly.Load(assemblyName);
            }
            //fallback attempt issue https://github.com/dotnet/runtime/issues/25177
            catch (FileNotFoundException)
            {
                return Assembly.LoadFrom(codeBase);
            }
        }
    }
}
