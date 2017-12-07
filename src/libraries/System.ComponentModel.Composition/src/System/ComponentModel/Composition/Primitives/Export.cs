// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Threading;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Primitives
{
    /// <summary>
    ///     Represents an export. That is, a type that is made up of a delay-created exported value 
    ///     and metadata that describes that object.
    /// </summary>
    public class Export
    {
        private readonly ExportDefinition _definition;
        private readonly Func<object> _exportedValueGetter;
        private static readonly object _EmptyValue = new object();
        private volatile object _exportedValue = Export._EmptyValue;
        
        /// <summary>
        ///     Initializes a new instance of the <see cref="Export"/> class.
        /// </summary>
        /// <remarks>
        ///     <note type="inheritinfo">
        ///         Derived types calling this constructor must override <see cref="Definition"/>
        ///         and <see cref="GetExportedValueCore"/>.
        ///     </note>
        /// </remarks>
        protected Export()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Export"/> class 
        ///     with the specified contract name and exported value getter.
        /// </summary>
        /// <param name="contractName">
        ///     A <see cref="String"/> containing the contract name of the 
        ///     <see cref="Export"/>.
        /// </param>
        /// <param name="exportedValueGetter">
        ///     A <see cref="Func{T}"/> that is called to create the exported value of the 
        ///     <see cref="Export"/>. This allows the creation of the object to be delayed
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="contractName"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="exportedValueGetter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="contractName"/> is an empty string ("").
        /// </exception>
        public Export(string contractName, Func<object> exportedValueGetter)
            : this(new ExportDefinition(contractName, (IDictionary<string, object>)null), exportedValueGetter)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Export"/> class 
        ///     with the specified contract name, metadata and exported value getter.
        /// </summary>
        /// <param name="contractName">
        ///     A <see cref="String"/> containing the contract name of the 
        ///     <see cref="Export"/>.
        /// </param>
        /// <param name="metadata">
        ///     An <see cref="IDictionary{TKey, TValue}"/> containing the metadata of the 
        ///     <see cref="Export"/>; or <see langword="null"/> to set the 
        ///     <see cref="Metadata"/> property to an empty, read-only 
        ///     <see cref="IDictionary{TKey, TValue}"/>.
        /// </param>
        /// <param name="exportedValueGetter">
        ///     A <see cref="Func{T}"/> that is called to create the exported value of the 
        ///     <see cref="Export"/>. This allows the creation of the object to be delayed.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="contractName"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="exportedValueGetter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="contractName"/> is an empty string ("").
        /// </exception>
        public Export(string contractName, IDictionary<string, object> metadata, Func<object> exportedValueGetter) 
            : this(new ExportDefinition(contractName, metadata), exportedValueGetter)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Export"/> class 
        ///     with the specified export definition and exported value getter.
        /// </summary>
        /// <param name="definition">
        ///     An <see cref="ExportDefinition"/> that describes the contract that the 
        ///     <see cref="Export"/> satisfies.
        /// </param>
        /// <param name="exportedValueGetter">
        ///     A <see cref="Func{T}"/> that is called to create the exported value of the 
        ///     <see cref="Export"/>. This allows the creation of the object to be delayed. 
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="definition"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="exportedValueGetter"/> is <see langword="null"/>.
        /// </exception>
        public Export(ExportDefinition definition, Func<object> exportedValueGetter)
        {
            Requires.NotNull(definition, nameof(definition));
            Requires.NotNull(exportedValueGetter, nameof(exportedValueGetter));

            _definition = definition;
            _exportedValueGetter = exportedValueGetter;
        }

        /// <summary>
        ///     Gets the definition that describes the contract that the export satisfies.
        /// </summary>
        /// <value>
        ///     An <see cref="ExportDefinition"/> that describes the contract that 
        ///     the <see cref="Export"/> satisfies.
        /// </value>
        /// <exception cref="NotImplementedException">
        ///     This property was not overridden by a derived class.
        /// </exception>
        /// <remarks>
        ///     <note type="inheritinfo">
        ///         Overriders of this property should never return
        ///         <see langword="null"/>.
        ///     </note>
        /// </remarks>
        public virtual ExportDefinition Definition
        {
            get 
            {
                Contract.Ensures(Contract.Result<ExportDefinition>() != null);
                
                if (_definition != null)
                {
                    return _definition;
                }

                throw ExceptionBuilder.CreateNotOverriddenByDerived("Definition");
            }
        }

        /// <summary>
        ///     Gets the metadata of the export.
        /// </summary>
        /// <value>
        ///     An <see cref="IDictionary{TKey, TValue}"/> containing the metadata of the 
        ///     <see cref="Export"/>.
        /// </value>
        /// <exception cref="NotImplementedException">
        ///     The <see cref="Definition"/> property was not overridden by a derived class.
        /// </exception>
        /// <remarks>
        ///     <para>
        ///         This property returns the value of <see cref="ExportDefinition.Metadata"/>
        ///         of the <see cref="Definition"/> property.
        ///     </para>
        /// </remarks>
        public IDictionary<string, object> Metadata
        {
            get 
            {
                Contract.Ensures(Contract.Result<IDictionary<string, object>>() != null);

                return Definition.Metadata; 
            }        
        }

        /// <summary>
        ///     Returns the exported value of the export.
        /// </summary>
        /// <returns>
        ///     The exported <see cref="Object"/> of the <see cref="Export"/>.
        /// </returns>
        /// <exception cref="CompositionException">
        ///     An error occurred during composition. <see cref="CompositionException.Errors"/> will 
        ///     contain a collection of errors that occurred.
        /// </exception>
        /// <exception cref="CompositionContractMismatchException">
        ///     The current instance is an instance of <see cref="Lazy{T}"/> and the underlying 
        ///     exported value cannot be cast to <c>T</c>.
        /// </exception>
        /// <exception cref="NotImplementedException">
        ///     The <see cref="GetExportedValueCore"/> method was not overridden by a derived class.
        /// </exception>
        public object Value
        {
            get
            {
                // NOTE : the logic below guarantees that the value will be set exactly once. It DOES NOT, however, guarantee that GetExportedValueCore() will be executed
                // more than once, as locking would be required for that. The said locking is problematic, as we can't reliable call 3rd party code under a lock.
                if (_exportedValue == Export._EmptyValue)
                {
                    object exportedValue = GetExportedValueCore();

                    // NOTE : According to http://msdn.microsoft.com/en-us/library/4bw5ewxy.aspx, the warning is bogus when used with Interlocked API.
#pragma warning disable 420
                    Interlocked.CompareExchange(ref _exportedValue, exportedValue, Export._EmptyValue);
#pragma warning restore 420
                }

                return _exportedValue;
            }
        }

        /// <summary>
        ///     Returns the exported value of the export.
        /// </summary>
        /// <returns>
        ///     The exported <see cref="Object"/> of the <see cref="Export"/>.
        /// </returns>
        /// <exception cref="CompositionException">
        ///     An error occurred during composition. <see cref="CompositionException.Errors"/> will 
        ///     contain a collection of errors that occurred.
        /// </exception>
        /// <exception cref="CompositionContractMismatchException">
        ///     The current instance is an instance of <see cref="Lazy{T}"/> and the underlying 
        ///     exported value cannot be cast to <c>T</c>.
        /// </exception>
        /// <exception cref="NotImplementedException">
        ///     The method was not overridden by a derived class.
        /// </exception>
        /// <remarks>
        ///     <note type="inheritinfo">
        ///         Overriders of this method should never return
        ///         <see langword="null"/>.
        ///     </note>
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        protected virtual object GetExportedValueCore()
        {
            if (_exportedValueGetter != null)
            {
                return _exportedValueGetter.Invoke();
            }

            throw ExceptionBuilder.CreateNotOverriddenByDerived("GetExportedValueCore");
        }
    }   
}
