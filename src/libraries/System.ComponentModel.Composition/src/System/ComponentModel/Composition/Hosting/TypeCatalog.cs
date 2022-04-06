// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.AttributedModel;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Internal;
using Microsoft.Internal.Collections;

namespace System.ComponentModel.Composition.Hosting
{
    /// <summary>
    ///     An immutable ComposablePartCatalog created from a type array or a list of managed types.  This class is threadsafe.
    ///     It is Disposable.
    /// </summary>
    [DebuggerTypeProxy(typeof(ComposablePartCatalogDebuggerProxy))]
    public class TypeCatalog : ComposablePartCatalog, ICompositionElement
    {
        private readonly object _thisLock = new object();
        private Type[]? _types;
        private volatile List<ComposablePartDefinition>? _parts;
        private volatile bool _isDisposed;
        private readonly ICompositionElement _definitionOrigin;
        private readonly Lazy<IDictionary<string, List<ComposablePartDefinition>>> _contractPartIndex;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TypeCatalog"/> class
        ///     with the specified types.
        /// </summary>
        /// <param name="types">
        ///     An <see cref="Array"/> of attributed <see cref="Type"/> objects to add to the
        ///     <see cref="TypeCatalog"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="types"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="types"/> contains an element that is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="types"/> contains an element that was loaded in the Reflection-only context.
        /// </exception>
        public TypeCatalog(params Type[] types) : this((IEnumerable<Type>)types)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TypeCatalog"/> class
        ///     with the specified types.
        /// </summary>
        /// <param name="types">
        ///     An <see cref="IEnumerable{T}"/> of attributed <see cref="Type"/> objects to add
        ///     to the <see cref="TypeCatalog"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="types"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="types"/> contains an element that is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="types"/> contains an element that was loaded in the reflection-only context.
        /// </exception>
        public TypeCatalog(IEnumerable<Type> types)
        {
            Requires.NotNull(types, nameof(types));

            InitializeTypeCatalog(types);

            _definitionOrigin = this;
            _contractPartIndex = new Lazy<IDictionary<string, List<ComposablePartDefinition>>>(CreateIndex, true);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TypeCatalog"/> class
        ///     with the specified types.
        /// </summary>
        /// <param name="types">
        ///     An <see cref="IEnumerable{T}"/> of attributed <see cref="Type"/> objects to add
        ///     to the <see cref="TypeCatalog"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="types"/> is <see langword="null"/>.
        /// </exception>
        /// <param name="definitionOrigin">
        ///     The <see cref="ICompositionElement"/> CompositionElement used by Diagnostics to identify the source for parts.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <paramref name="types"/> contains an element that is <see langword="null"/>.
        /// </exception>
        public TypeCatalog(IEnumerable<Type> types, ICompositionElement definitionOrigin)
        {
            Requires.NotNull(types, nameof(types));
            Requires.NotNull(definitionOrigin, nameof(definitionOrigin));

            InitializeTypeCatalog(types);

            _definitionOrigin = definitionOrigin;
            _contractPartIndex = new Lazy<IDictionary<string, List<ComposablePartDefinition>>>(CreateIndex, true);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TypeCatalog"/> class
        ///     with the specified types.
        /// </summary>
        /// <param name="types">
        ///     An <see cref="IEnumerable{T}"/> of attributed <see cref="Type"/> objects to add
        ///     to the <see cref="TypeCatalog"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="types"/> is <see langword="null"/>.
        /// </exception>
        /// <param name="reflectionContext">
        ///     The <see cref="ReflectionContext"/> a context used by the catalog when
        ///     interpreting the types to inject attributes into the type definition.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     <paramref name="types"/> contains an element that is <see langword="null"/>.
        /// </exception>
        public TypeCatalog(IEnumerable<Type> types, ReflectionContext reflectionContext)
        {
            Requires.NotNull(types, nameof(types));
            Requires.NotNull(reflectionContext, nameof(reflectionContext));

            InitializeTypeCatalog(types, reflectionContext);

            _definitionOrigin = this;
            _contractPartIndex = new Lazy<IDictionary<string, List<ComposablePartDefinition>>>(CreateIndex, true);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TypeCatalog"/> class
        ///     with the specified types.
        /// </summary>
        /// <param name="types">
        ///     An <see cref="IEnumerable{T}"/> of attributed <see cref="Type"/> objects to add
        ///     to the <see cref="TypeCatalog"/>.
        /// </param>
        /// <param name="reflectionContext">
        ///     The <see cref="ReflectionContext"/> a context used by the catalog when
        ///     interpreting the types to inject attributes into the type definition.
        /// </param>
        /// <param name="definitionOrigin">
        ///     The <see cref="ICompositionElement"/> CompositionElement used by Diagnostics to identify the source for parts.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="types"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="types"/> contains an element that is <see langword="null"/>.
        /// </exception>
        public TypeCatalog(IEnumerable<Type> types, ReflectionContext reflectionContext, ICompositionElement definitionOrigin)
        {
            Requires.NotNull(types, nameof(types));
            Requires.NotNull(reflectionContext, nameof(reflectionContext));
            Requires.NotNull(definitionOrigin, nameof(definitionOrigin));

            InitializeTypeCatalog(types, reflectionContext);

            _definitionOrigin = definitionOrigin;
            _contractPartIndex = new Lazy<IDictionary<string, List<ComposablePartDefinition>>>(CreateIndex, true);
        }

        private void InitializeTypeCatalog(IEnumerable<Type> types, ReflectionContext reflectionContext)
        {
            var typesList = new List<Type>();
            foreach (var type in types)
            {
                if (type == null)
                {
                    throw ExceptionBuilder.CreateContainsNullElement(nameof(types));
                }
                if (type.Assembly.ReflectionOnly)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_ElementReflectionOnlyType, nameof(types)), nameof(types));
                }
                var typeInfo = type.GetTypeInfo();
                var lclType = (reflectionContext != null) ? reflectionContext.MapType(typeInfo) : typeInfo;

                // It is valid for the reflectionContext to delete types by mapping them to null
                if (lclType != null)
                {
                    // The final mapped type may be activated so we check to see if it is in a reflect only assembly
                    if (lclType.Assembly.ReflectionOnly)
                    {
                        throw new ArgumentException(SR.Format(SR.Argument_ReflectionContextReturnsReflectionOnlyType, nameof(reflectionContext)), nameof(reflectionContext));
                    }
                    typesList.Add(lclType);
                }
            }
            _types = typesList.ToArray();
        }

        private void InitializeTypeCatalog(IEnumerable<Type> types)
        {
            Type[] arr = types.ToArray();
            foreach (Type type in arr)
            {
                if (type == null)
                {
                    throw ExceptionBuilder.CreateContainsNullElement(nameof(types));
                }

                if (type.Assembly.ReflectionOnly)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_ElementReflectionOnlyType, nameof(types)), nameof(types));
                }
            }

            _types = arr;
        }

        public override IEnumerator<ComposablePartDefinition> GetEnumerator()
        {
            ThrowIfDisposed();
            return PartsInternal.GetEnumerator();
        }

        /// <summary>
        ///     Gets the display name of the type catalog.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> containing a human-readable display name of the <see cref="TypeCatalog"/>.
        /// </value>
        string ICompositionElement.DisplayName
        {
            get { return GetDisplayName(); }
        }

        /// <summary>
        ///     Gets the composition element from which the type catalog originated.
        /// </summary>
        /// <value>
        ///     This property always returns <see langword="null"/>.
        /// </value>
        ICompositionElement? ICompositionElement.Origin
        {
            get { return null; }
        }

        private IEnumerable<ComposablePartDefinition> PartsInternal
        {
            get
            {
                if (_parts == null)
                {
                    lock (_thisLock)
                    {
                        if (_parts == null)
                        {
                            if (_types == null)
                            {
                                throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                            }

                            var collection = new List<ComposablePartDefinition>();
                            foreach (Type type in _types)
                            {
                                var definition = AttributedModelDiscovery.CreatePartDefinitionIfDiscoverable(type, _definitionOrigin);
                                if (definition != null)
                                {
                                    collection.Add(definition);
                                }
                            }
                            Thread.MemoryBarrier();

                            _types = null;
                            _parts = collection;
                        }
                    }
                }

                return _parts;
            }
        }

        internal override IEnumerable<ComposablePartDefinition>? GetCandidateParts(ImportDefinition definition!!)
        {
            string contractName = definition.ContractName;
            if (string.IsNullOrEmpty(contractName))
            {
                return PartsInternal;
            }

            string? genericContractName = definition.Metadata.GetValue<string>(CompositionConstants.GenericContractMetadataName);

            List<ComposablePartDefinition>? nonGenericMatches = GetCandidateParts(contractName);
            List<ComposablePartDefinition>? genericMatches = GetCandidateParts(genericContractName);

            return nonGenericMatches.ConcatAllowingNull(genericMatches);
        }

        private List<ComposablePartDefinition>? GetCandidateParts(string? contractName)
        {
            if (contractName == null)
            {
                return null;
            }

            _contractPartIndex.Value.TryGetValue(contractName, out List<ComposablePartDefinition>? contractCandidateParts);
            return contractCandidateParts;
        }

        private IDictionary<string, List<ComposablePartDefinition>> CreateIndex()
        {
            Dictionary<string, List<ComposablePartDefinition>> index = new Dictionary<string, List<ComposablePartDefinition>>(StringComparers.ContractName);

            foreach (var part in PartsInternal)
            {
                foreach (string contractName in part.ExportDefinitions.Select(export => export.ContractName).Distinct())
                {
                    List<ComposablePartDefinition>? contractParts = null;
                    if (!index.TryGetValue(contractName, out contractParts))
                    {
                        contractParts = new List<ComposablePartDefinition>();
                        index.Add(contractName, contractParts);
                    }
                    contractParts.Add(part);
                }
            }
            return index;
        }

        /// <summary>
        ///     Returns a string representation of the type catalog.
        /// </summary>
        /// <returns>
        ///     A <see cref="string"/> containing the string representation of the <see cref="TypeCatalog"/>.
        /// </returns>
        public override string ToString()
        {
            return GetDisplayName();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isDisposed = true;
            }

            base.Dispose(disposing);
        }

        private string GetDisplayName()
        {
            return SR.Format(
                                SR.TypeCatalog_DisplayNameFormat,
                                GetType().Name,
                                GetTypesDisplay());
        }

        private string GetTypesDisplay()
        {
            int count = PartsInternal.Count();
            if (count == 0)
            {
                return SR.TypeCatalog_Empty;
            }

            const int displayCount = 2;
            StringBuilder builder = new StringBuilder();
            foreach (ReflectionComposablePartDefinition definition in PartsInternal.Take(displayCount))
            {
                if (builder.Length > 0)
                {
                    builder.Append(CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                    builder.Append(' ');
                }

                builder.Append(definition.GetPartType().GetDisplayName());
            }

            if (count > displayCount)
            {   // Add an elipse to indicate that there
                // are more types than actually listed
                builder.Append(CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                builder.Append(" ...");
            }

            return builder.ToString();
        }

        [DebuggerStepThrough]
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw ExceptionBuilder.CreateObjectDisposed(this);
            }
        }
    }
}
