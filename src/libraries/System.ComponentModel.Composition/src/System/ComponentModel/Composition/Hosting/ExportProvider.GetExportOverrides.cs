// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Hosting
{
    public abstract partial class ExportProvider
    {
        /// <summary>
        ///     Returns the export with the contract name derived from the specified type parameter, 
        ///     throwing an exception if there is not exactly one matching export.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the <see cref="Lazy{T}"/> object to return. The contract name is also 
        ///     derived from this type parameter.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="Lazy{T}"/> object with the contract name derived from 
        ///     <typeparamref name="T"/>.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The returned <see cref="Lazy{T}"/> object is an instance of 
        ///         <see cref="Lazy{T, TMetadataView}"/> underneath, where 
        ///         <c>TMetadataView</c>
        ///         is <see cref="IDictionary{TKey, TValue}"/> and where <c>TKey</c> 
        ///         is <see cref="String"/> and <c>TValue</c> is <see cref="Object"/>.
        ///     </para>
        ///     <para>
        ///         The contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on <typeparamref name="T"/>.
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        /// <exception cref="ImportCardinalityMismatchException">
        ///     <para>
        ///         There are zero <see cref="Lazy{T}"/> objects with the contract name derived 
        ///         from <typeparamref name="T"/> in the <see cref="CompositionContainer"/>.
        ///     </para>
        ///     -or-
        ///     <para>
        ///         There are more than one <see cref="Lazy{T}"/> objects with the contract name 
        ///         derived from <typeparamref name="T"/> in the <see cref="CompositionContainer"/>.
        ///     </para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="CompositionContainer"/> has been disposed of.
        /// </exception>
        public Lazy<T> GetExport<T>()
        {
            return GetExport<T>((string)null);
        }

        /// <summary>
        ///     Returns the export with the specified contract name, throwing an exception if there 
        ///     is not exactly one matching export.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the <see cref="Lazy{T}"/> object to return.
        /// </typeparam>
        /// <param name="contractName">
        ///     A <see cref="String"/> containing the contract name of the <see cref="Lazy{T}"/> 
        ///     object to return; or <see langword="null"/> or an empty string ("") to use the 
        ///     default contract name.
        /// </param>
        /// <returns>
        ///     The <see cref="Lazy{T}"/> object with the specified contract name.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The returned <see cref="Lazy{T}"/> object is an instance of 
        ///         <see cref="Lazy{T, TMetadataView}"/> underneath, where 
        ///         <c>TMetadataView</c>
        ///         is <see cref="IDictionary{TKey, TValue}"/> and where <c>TKey</c> 
        ///         is <see cref="String"/> and <c>TValue</c> is <see cref="Object"/>.
        ///     </para>
        ///     <para>
        ///         The contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on <typeparamref name="T"/>.
        ///     </para>
        ///     <para>
        ///         The default contract name is compared using a case-sensitive, non-linguistic 
        ///         comparison using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        /// <exception cref="ImportCardinalityMismatchException">
        ///     <para>
        ///         There are zero <see cref="Lazy{T}"/> objects with the specified contract name 
        ///         in the <see cref="CompositionContainer"/>.
        ///     </para>
        ///     -or-
        ///     <para>
        ///         There are more than one <see cref="Lazy{T}"/> objects with the specified contract
        ///         name in the <see cref="CompositionContainer"/>.
        ///     </para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="CompositionContainer"/> has been disposed of.
        /// </exception>
        public Lazy<T> GetExport<T>(string contractName)
        {
            return GetExportCore<T>(contractName);
        }

        /// <summary>
        ///     Returns the export with the contract name derived from the specified type parameter, 
        ///     throwing an exception if there is not exactly one matching export.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the <see cref="Lazy{T, TMetadataView}"/> object to return. The 
        ///     contract name is also derived from this type parameter.
        /// </typeparam>
        /// <typeparam name="TMetadataView">
        ///     The type of the metadata view of the <see cref="Lazy{T, TMetadataView}"/> object
        ///     to return.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="Lazy{T, TMetadataView}"/> object with the contract name derived 
        ///     from <typeparamref name="T"/>.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on <typeparamref name="T"/>.
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        /// <exception cref="ImportCardinalityMismatchException">
        ///     <para>
        ///         There are zero <see cref="Lazy{T, TMetadataView}"/> objects with the contract 
        ///         name derived from <typeparamref name="T"/> in the 
        ///         <see cref="CompositionContainer"/>.
        ///     </para>
        ///     -or-
        ///     <para>
        ///         There are more than one <see cref="Lazy{T, TMetadataView}"/> objects with the 
        ///         contract name derived from <typeparamref name="T"/> in the 
        ///         <see cref="CompositionContainer"/>.
        ///     </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <typeparamref name="TMetadataView"/> is not a valid metadata view type.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="CompositionContainer"/> has been disposed of.
        /// </exception>
        public Lazy<T, TMetadataView> GetExport<T, TMetadataView>()
        {
            return GetExport<T, TMetadataView>((string)null);
        }

        /// <summary>
        ///     Returns the export with the specified contract name, throwing an exception if there 
        ///     is not exactly one matching export.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the <see cref="Lazy{T, TMetadataView}"/> object to return.
        /// </typeparam>
        /// <typeparam name="TMetadataView">
        ///     The type of the metadata view of the <see cref="Lazy{T, TMetadataView}"/> object
        ///     to return.
        /// </typeparam>
        /// <param name="contractName">
        ///     A <see cref="String"/> containing the contract name of the 
        ///     <see cref="Lazy{T, TMetadataView}"/> object to return; or <see langword="null"/> 
        ///     or an empty string ("") to use the default contract name.
        /// </param>
        /// <returns>
        ///     The <see cref="Lazy{T, TMetadataView}"/> object with the specified contract name.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The default contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on <typeparamref name="T"/>.
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        /// <exception cref="ImportCardinalityMismatchException">
        ///     <para>
        ///         There are zero <see cref="Lazy{T, TMetadataView}"/> objects with the 
        ///         specified contract name in the <see cref="CompositionContainer"/>.
        ///     </para>
        ///     -or-
        ///     <para>
        ///         There are more than one <see cref="Lazy{T, TMetadataView}"/> objects with the 
        ///         specified contract name in the <see cref="CompositionContainer"/>.
        ///     </para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <typeparamref name="TMetadataView"/> is not a valid metadata view type.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="CompositionContainer"/> has been disposed of.
        /// </exception>
        public Lazy<T, TMetadataView> GetExport<T, TMetadataView>(string contractName)
        {
            return GetExportCore<T, TMetadataView>(contractName);
        }

        /// <summary>
        ///     Returns the exports with the specified contract name.
        /// </summary>
        /// <param name="type">
        ///     The <see cref="Type"/> of the <see cref="Export"/> objects to return.
        /// </param>
        /// <param name="metadataViewType">
        ///     The <see cref="Type"/> of the metadata view of the <see cref="Export"/> objects to
        ///     return.
        /// </param>
        /// <param name="contractName">
        ///     A <see cref="String"/> containing the contract name of the 
        ///     <see cref="Export"/> object to return; or <see langword="null"/> 
        ///     or an empty string ("") to use the default contract name.
        /// </param>
        /// <returns>
        ///     An <see cref="IEnumerable{T}"/> containing the <see cref="Lazy{Object, Object}"/> objects 
        ///     with the specified contract name, if found; otherwise, an empty 
        ///     <see cref="IEnumerable{T}"/>.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The returned <see cref="Export"/> objects are instances of 
        ///         <see cref="Lazy{T, TMetadataView}"/> underneath, where <c>T</c>
        ///         is <paramref name="type"/> and <c>TMetadataView</c> is 
        ///         <paramref name="metadataViewType"/>.
        ///     </para>
        ///     <para>
        ///         The default contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on <paramref name="type"/>.
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="type"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="metadataViewType"/> is not a valid metadata view type.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="CompositionContainer"/> has been disposed of.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1006")]
        public IEnumerable<Lazy<object, object>> GetExports(Type type, Type metadataViewType, string contractName)
        {
            IEnumerable<Export> exports = GetExportsCore(type, metadataViewType, contractName, ImportCardinality.ZeroOrMore);
            Collection<Lazy<object, object>> result = new Collection<Lazy<object, object>>();

            Func<Export, Lazy<object, object>> typedExportFactory = ExportServices.CreateSemiStronglyTypedLazyFactory(type, metadataViewType);
            foreach (Export export in exports)
            {
                result.Add(typedExportFactory.Invoke(export));
            }

            return result;
        }

        /// <summary>
        ///     Returns the exports with the contract name derived from the specified type parameter.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the <see cref="Lazy{T}"/> objects to return. The contract name is also 
        ///     derived from this type parameter.
        /// </typeparam>
        /// <returns>
        ///     An <see cref="IEnumerable{T}"/> containing the <see cref="Lazy{T}"/> objects
        ///     with the contract name derived from <typeparamref name="T"/>, if found; otherwise,
        ///     an empty <see cref="IEnumerable{T}"/>.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The returned <see cref="Lazy{T}"/> objects are instances of 
        ///         <see cref="Lazy{T, TMetadataView}"/> underneath, where 
        ///         <c>TMetadataView</c>
        ///         is <see cref="IDictionary{TKey, TValue}"/> and where <c>TKey</c> 
        ///         is <see cref="String"/> and <c>TValue</c> is <see cref="Object"/>.
        ///     </para>
        ///     <para>
        ///         The contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on <typeparamref name="T"/>.
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="CompositionContainer"/> has been disposed of.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1006")]
        public IEnumerable<Lazy<T>> GetExports<T>()
        {
            return GetExports<T>((string)null);
        }

        /// <summary>
        ///     Returns the exports with the specified contract name.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the <see cref="Lazy{T}"/> objects to return.
        /// </typeparam>
        /// <param name="contractName">
        ///     A <see cref="String"/> containing the contract name of the <see cref="Lazy{T}"/> 
        ///     objects to return; or <see langword="null"/> or an empty string ("") to use the 
        ///     default contract name.
        /// </param>
        /// <returns>
        ///     An <see cref="IEnumerable{T}"/> containing the <see cref="Lazy{T}"/> objects
        ///     with the specified contract name, if found; otherwise, an empty 
        ///     <see cref="IEnumerable{T}"/>.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The returned <see cref="Lazy{T}"/> objects are instances of 
        ///         <see cref="Lazy{T, TMetadataView}"/> underneath, where 
        ///         <c>TMetadataView</c>
        ///         is <see cref="IDictionary{TKey, TValue}"/> and where <c>TKey</c> 
        ///         is <see cref="String"/> and <c>TValue</c> is <see cref="Object"/>.
        ///     </para>
        ///     <para>
        ///         The default contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on <typeparamref name="T"/>.
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="CompositionContainer"/> has been disposed of.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1006")]
        public IEnumerable<Lazy<T>> GetExports<T>(string contractName)
        {
            return GetExportsCore<T>(contractName);
        }

        /// <summary>
        ///     Returns the exports with the contract name derived from the specified type parameter.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the <see cref="Lazy{T, TMetadataView}"/> objects to return. The 
        ///     contract name is also derived from this type parameter.
        /// </typeparam>
        /// <typeparam name="TMetadataView">
        ///     The type of the metadata view of the <see cref="Lazy{T, TMetadataView}"/> objects
        ///     to return.
        /// </typeparam>
        /// <returns>
        ///     An <see cref="IEnumerable{T}"/> containing the 
        ///     <see cref="Lazy{T, TMetadataView}"/> objects with the contract name derived from 
        ///     <typeparamref name="T"/>, if found; otherwise, an empty 
        ///     <see cref="IEnumerable{T}"/>.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on <typeparamref name="T"/>.
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     <typeparamref name="TMetadataView"/> is not a valid metadata view type.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="CompositionContainer"/> has been disposed of.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1006")]
        public IEnumerable<Lazy<T, TMetadataView>> GetExports<T, TMetadataView>()
        {
            return GetExports<T, TMetadataView>((string)null);
        }

        /// <summary>
        ///     Returns the exports with the specified contract name.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the <see cref="Lazy{T, TMetadataView}"/> objects to return. The 
        ///     contract name is also derived from this type parameter.
        /// </typeparam>
        /// <typeparam name="TMetadataView">
        ///     The type of the metadata view of the <see cref="Lazy{T, TMetadataView}"/> objects
        ///     to return.
        /// </typeparam>
        /// <param name="contractName">
        ///     A <see cref="String"/> containing the contract name of the 
        ///     <see cref="Lazy{T, TMetadataView}"/> objects to return; or <see langword="null"/> 
        ///     or an empty string ("") to use the default contract name.
        /// </param>
        /// <returns>
        ///     An <see cref="IEnumerable{T}"/> containing the 
        ///     <see cref="Lazy{T, TMetadataView}"/> objects with the specified contract name if 
        ///     found; otherwise, an empty <see cref="IEnumerable{T}"/>.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The default contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on <typeparamref name="T"/>.
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     <typeparamref name="TMetadataView"/> is not a valid metadata view type.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="CompositionContainer"/> has been disposed of.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1006")]
        public IEnumerable<Lazy<T, TMetadataView>> GetExports<T, TMetadataView>(string contractName)
        {
            return GetExportsCore<T, TMetadataView>(contractName);
        }

        /// <summary>
        ///     Returns the exported value with the contract name derived from the specified type 
        ///     parameter, throwing an exception if there is not exactly one matching exported value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the exported value to return. The contract name is also 
        ///     derived from this type parameter.
        /// </typeparam>
        /// <returns>
        ///     The exported <see cref="Object"/> with the contract name derived from 
        ///     <typeparamref name="T"/>.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on <typeparamref name="T"/>.
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        /// <exception cref="CompositionContractMismatchException">
        ///     The underlying exported value cannot be cast to <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="ImportCardinalityMismatchException">
        ///     <para>
        ///         There are zero exported values with the contract name derived from 
        ///         <typeparamref name="T"/> in the <see cref="CompositionContainer"/>.
        ///     </para>
        ///     -or-
        ///     <para>
        ///         There are more than one exported values with the contract name derived from
        ///         <typeparamref name="T"/> in the <see cref="CompositionContainer"/>.
        ///     </para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="CompositionContainer"/> has been disposed of.
        /// </exception>
        /// <exception cref="CompositionException">
        ///     An error occurred during composition. <see cref="CompositionException.Errors"/> will 
        ///     contain a collection of errors that occurred.
        /// </exception>
        public T GetExportedValue<T>()
        {
            return GetExportedValue<T>((string)null);
        }

        /// <summary>
        ///     Returns the exported value with the specified contract name, throwing an exception 
        ///     if there is not exactly one matching exported value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the exported value to return.
        /// </typeparam>
        /// <param name="contractName">
        ///     A <see cref="String"/> containing the contract name of the exported value to return,
        ///     or <see langword="null"/> or an empty string ("") to use the default contract name.
        /// </param>
        /// <returns>
        ///     The exported <see cref="Object"/> with the specified contract name.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The default contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on <typeparamref name="T"/>.
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        /// <exception cref="CompositionContractMismatchException">
        ///     The underlying exported value cannot be cast to <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="ImportCardinalityMismatchException">
        ///     <para>
        ///         There are zero exported values with the specified contract name in the 
        ///         <see cref="CompositionContainer"/>.
        ///     </para>
        ///     -or-
        ///     <para>
        ///         There are more than one exported values with the specified contract name in the
        ///         <see cref="CompositionContainer"/>.
        ///     </para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="CompositionContainer"/> has been disposed of.
        /// </exception>
        /// <exception cref="CompositionException">
        ///     An error occurred during composition. <see cref="CompositionException.Errors"/> will 
        ///     contain a collection of errors that occurred.
        /// </exception>
        public T GetExportedValue<T>(string contractName)
        {
            return GetExportedValueCore<T>(contractName, ImportCardinality.ExactlyOne);
        }

        /// <summary>
        ///     Returns the exported value with the contract name derived from the specified type 
        ///     parameter, throwing an exception if there is more than one matching exported value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the exported value to return. The contract name is also 
        ///     derived from this type parameter.
        /// </typeparam>
        /// <returns>
        ///     The exported <see cref="Object"/> with the contract name derived from 
        ///     <typeparamref name="T"/>, if found; otherwise, the default value for
        ///     <typeparamref name="T"/>.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         If the exported value is not found, then this method returns the appropriate 
        ///         default value for <typeparamref name="T"/>; for example, 0 (zero) for integer 
        ///         types, <see langword="false"/> for Boolean types, and <see langword="null"/> 
        ///         for reference types.
        ///     </para>
        ///     <para>
        ///         The contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on <typeparamref name="T"/>.
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        /// <exception cref="CompositionContractMismatchException">
        ///     The underlying exported value cannot be cast to <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="ImportCardinalityMismatchException">
        ///     <para>
        ///         There are more than one exported values with the contract name derived from
        ///         <typeparamref name="T"/> in the <see cref="CompositionContainer"/>.
        ///     </para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="CompositionContainer"/> has been disposed of.
        /// </exception>
        /// <exception cref="CompositionException">
        ///     An error occurred during composition. <see cref="CompositionException.Errors"/> will 
        ///     contain a collection of errors that occurred.
        /// </exception>
        public T GetExportedValueOrDefault<T>()
        {
            return GetExportedValueOrDefault<T>((string)null);
        }

        /// <summary>
        ///     Returns the exported value with the specified contract name, throwing an exception 
        ///     if there is more than one matching exported value.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the exported value to return.
        /// </typeparam>
        /// <param name="contractName">
        ///     A <see cref="String"/> containing the contract name of the exported value to return,
        ///     or <see langword="null"/> or an empty string ("") to use the default contract name.
        /// </param>
        /// <returns>
        ///     The exported <see cref="Object"/> with the specified contract name, if found; 
        ///     otherwise, the default value for <typeparamref name="T"/>.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         If the exported value is not found, then this method returns the appropriate 
        ///         default value for <typeparamref name="T"/>; for example, 0 (zero) for integer 
        ///         types, <see langword="false"/> for Boolean types, and <see langword="null"/> 
        ///         for reference types.
        ///     </para>
        ///     <para>
        ///         The default contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on <typeparamref name="T"/>.
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        /// <exception cref="CompositionContractMismatchException">
        ///     The underlying exported value cannot be cast to <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="ImportCardinalityMismatchException">
        ///     There are more than one exported values with the specified contract name in the
        ///     <see cref="CompositionContainer"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="CompositionContainer"/> has been disposed of.
        /// </exception>
        /// <exception cref="CompositionException">
        ///     An error occurred during composition. <see cref="CompositionException.Errors"/> will 
        ///     contain a collection of errors that occurred.
        /// </exception>
        public T GetExportedValueOrDefault<T>(string contractName)
        {
            return GetExportedValueCore<T>(contractName, ImportCardinality.ZeroOrOne);
        }

        /// <summary>
        ///     Returns the exported values with the contract name derived from the specified type 
        ///     parameter.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the exported value to return. The contract name is also 
        ///     derived from this type parameter.
        /// </typeparam>
        /// <returns>
        ///     An <see cref="Collection{T}"/> containing the exported values with the contract name 
        ///     derived from the specified type parameter, if found; otherwise, an empty 
        ///     <see cref="Collection{T}"/>.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on <typeparamref name="T"/>.
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        /// <exception cref="CompositionContractMismatchException">
        ///     One or more of the underlying exported values cannot be cast to 
        ///     <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="CompositionContainer"/> has been disposed of.
        /// </exception>
        /// <exception cref="CompositionException">
        ///     An error occurred during composition. <see cref="CompositionException.Errors"/> will 
        ///     contain a collection of errors that occurred.
        /// </exception>
        public IEnumerable<T> GetExportedValues<T>()
        {
            return GetExportedValues<T>((string)null);
        }

        /// <summary>
        ///     Returns the exported values with the specified contract name.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the exported value to return.
        /// </typeparam>
        /// <param name="contractName">
        ///     A <see cref="String"/> containing the contract name of the exported values to 
        ///     return; or <see langword="null"/> or an empty string ("") to use the default 
        ///     contract name.
        /// </param>
        /// <returns>
        ///     An <see cref="Collection{T}"/> containing the exported values with the specified 
        ///     contract name, if found; otherwise, an empty <see cref="Collection{T}"/>.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The default contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on <typeparamref name="T"/>.
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        /// <exception cref="CompositionContractMismatchException">
        ///     One or more of the underlying exported values cannot be cast to 
        ///     <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="CompositionContainer"/> has been disposed of.
        /// </exception>
        /// <exception cref="CompositionException">
        ///     An error occurred during composition. <see cref="CompositionException.Errors"/> will 
        ///     contain a collection of errors that occurred.
        /// </exception>
        public IEnumerable<T> GetExportedValues<T>(string contractName)
        {
            return GetExportedValuesCore<T>(contractName);
        }

        private IEnumerable<T> GetExportedValuesCore<T>(string contractName)
        {
            IEnumerable<Export> exports = GetExportsCore(typeof(T), (Type)null, contractName, ImportCardinality.ZeroOrMore);

            Collection<T> result = new Collection<T>();
            foreach (Export export in exports)
            {
                result.Add(ExportServices.GetCastedExportedValue<T>(export));
            }
            return result;
        }

        private T GetExportedValueCore<T>(string contractName, ImportCardinality cardinality)
        {
            if (!cardinality.IsAtMostOne())
            {
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);
            }

            Export export = GetExportsCore(typeof(T), (Type)null, contractName, cardinality).SingleOrDefault();

            return (export != null) ? ExportServices.GetCastedExportedValue<T>(export) : default(T);
        }

        private IEnumerable<Lazy<T>> GetExportsCore<T>(string contractName)
        {
            IEnumerable<Export> exports = GetExportsCore(typeof(T), (Type)null, contractName, ImportCardinality.ZeroOrMore);

            Collection<Lazy<T>> result = new Collection<Lazy<T>>();
            foreach (Export export in exports)
            {
                result.Add(ExportServices.CreateStronglyTypedLazyOfT<T>(export));
            }
            return result;
        }

        private IEnumerable<Lazy<T, TMetadataView>> GetExportsCore<T, TMetadataView>(string contractName)
        {
            IEnumerable<Export> exports = GetExportsCore(typeof(T), typeof(TMetadataView), contractName, ImportCardinality.ZeroOrMore);

            Collection<Lazy<T, TMetadataView>> result = new Collection<Lazy<T, TMetadataView>>();
            foreach (Export export in exports)
            {
                result.Add(ExportServices.CreateStronglyTypedLazyOfTM<T, TMetadataView>(export));
            }
            return result;
        }

        private Lazy<T, TMetadataView> GetExportCore<T, TMetadataView>(string contractName)
        {
            Export export = GetExportsCore(typeof(T), typeof(TMetadataView), contractName, ImportCardinality.ExactlyOne).SingleOrDefault();

            return (export != null) ? ExportServices.CreateStronglyTypedLazyOfTM<T, TMetadataView>(export) : null;
        }

        private Lazy<T> GetExportCore<T>(string contractName)
        {
            Export export = GetExportsCore(typeof(T), null, contractName, ImportCardinality.ExactlyOne).SingleOrDefault();

            return (export != null) ? ExportServices.CreateStronglyTypedLazyOfT<T>(export) : null;
        }

        private IEnumerable<Export> GetExportsCore(Type type, Type metadataViewType, string contractName, ImportCardinality cardinality)
        {
            // Only 'type' cannot be null - the other parameters have sensible defaults.
            Requires.NotNull(type, nameof(type));

            if (string.IsNullOrEmpty(contractName))
            {
                contractName = AttributedModelServices.GetContractName(type);
            }

            if (metadataViewType == null)
            {
                metadataViewType = ExportServices.DefaultMetadataViewType;
            }

            if (!MetadataViewProvider.IsViewTypeValid(metadataViewType))
            {
                throw new InvalidOperationException(SR.Format(SR.InvalidMetadataView, metadataViewType.Name));
            }

            ImportDefinition importDefinition = BuildImportDefinition(type, metadataViewType, contractName, cardinality);
            return GetExports(importDefinition, null);
        }

        private static ImportDefinition BuildImportDefinition(Type type, Type metadataViewType, string contractName, ImportCardinality cardinality)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (metadataViewType == null)
            {
                throw new ArgumentNullException(nameof(metadataViewType));
            }

            if (contractName == null)
            {
                throw new ArgumentNullException(nameof(contractName));
            }

            IEnumerable<KeyValuePair<string, Type>> requiredMetadata = CompositionServices.GetRequiredMetadata(metadataViewType);
            IDictionary<string, object> metadata = CompositionServices.GetImportMetadata(type, null);

            string requiredTypeIdentity = null;
            if (type != typeof(object))
            {
                requiredTypeIdentity = AttributedModelServices.GetTypeIdentity(type);
            }

            return new ContractBasedImportDefinition(contractName, requiredTypeIdentity, requiredMetadata, cardinality, false, true, CreationPolicy.Any, metadata);
        }
    }
}
