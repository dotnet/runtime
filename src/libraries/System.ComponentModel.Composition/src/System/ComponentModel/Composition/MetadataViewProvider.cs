// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Microsoft.Internal;
using System.Diagnostics;

namespace System.ComponentModel.Composition
{
    internal static class MetadataViewProvider
    {
        public static TMetadataView GetMetadataView<TMetadataView>(IDictionary<string, object?> metadata)
        {
            ArgumentNullException.ThrowIfNull(metadata);

            Type metadataViewType = typeof(TMetadataView);

            // If the Metadata dictionary is cast compatible with the passed in type
            if (metadataViewType.IsAssignableFrom(typeof(IDictionary<string, object?>)))
            {
                return (TMetadataView)metadata;
            }
            // otherwise is it a metadata view
            else
            {
                Type? proxyType = null;
                MetadataViewGenerator.MetadataViewFactory? metadataViewFactory = null;
                if (metadataViewType.IsInterface)
                {
                    if (!metadataViewType.IsAttributeDefined<MetadataViewImplementationAttribute>())
                    {
                        try
                        {
                            metadataViewFactory = MetadataViewGenerator.GetMetadataViewFactory(metadataViewType);
                        }
                        catch (TypeLoadException ex)
                        {
                            throw new NotSupportedException(SR.Format(SR.NotSupportedInterfaceMetadataView, metadataViewType.FullName), ex);
                        }
                    }
                    else
                    {
                        var implementationAttribute = metadataViewType.GetFirstAttribute<MetadataViewImplementationAttribute>();
                        Debug.Assert(implementationAttribute != null);
                        proxyType = implementationAttribute.ImplementationType;
                        if (proxyType == null)
                        {
                            throw new CompositionContractMismatchException(SR.Format(
                                SR.ContractMismatch_MetadataViewImplementationCanNotBeNull,
                                metadataViewType.FullName));
                        }
                        else
                        {
                            if (!metadataViewType.IsAssignableFrom(proxyType))
                            {
                                throw new CompositionContractMismatchException(SR.Format(
                                    SR.ContractMismatch_MetadataViewImplementationDoesNotImplementViewInterface,
                                    metadataViewType.FullName,
                                    proxyType.FullName));
                            }
                        }
                    }
                }
                else
                {
                    proxyType = metadataViewType;
                }

                // Now we have the type for the proxy create it
                try
                {
                    if (metadataViewFactory != null)
                    {
                        return MetadataViewGenerator.CreateMetadataView<TMetadataView>(metadataViewFactory, metadata);
                    }
                    else
                    {
                        if (proxyType == null)
                        {
                            throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                        }
                        return (TMetadataView)proxyType.SafeCreateInstance(metadata)!;
                    }
                }
                catch (MissingMethodException ex)
                {
                    // Unable to create an Instance of the Metadata view '{0}' because a constructor could not be selected.  Ensure that the type implements a constructor which takes an argument of type IDictionary<string, object>.
                    throw new CompositionContractMismatchException(SR.Format(
                        SR.CompositionException_MetadataViewInvalidConstructor,
                        proxyType!.AssemblyQualifiedName), ex);
                }
                catch (TargetInvocationException ex)
                {
                    //Unwrap known failures that we want to present as CompositionContractMismatchException
                    if (metadataViewType.IsInterface)
                    {
                        if (ex.InnerException!.GetType() == typeof(InvalidCastException))
                        {
                            // Unable to create an Instance of the Metadata view {0} because the exporter exported the metadata for the item {1} with the value {2} as type {3} but the view imports it as type {4}.
                            throw new CompositionContractMismatchException(SR.Format(
                                SR.ContractMismatch_InvalidCastOnMetadataField,
                                ex.InnerException.Data[MetadataViewGenerator.MetadataViewType],
                                ex.InnerException.Data[MetadataViewGenerator.MetadataItemKey],
                                ex.InnerException.Data[MetadataViewGenerator.MetadataItemValue],
                                ex.InnerException.Data[MetadataViewGenerator.MetadataItemSourceType],
                                ex.InnerException.Data[MetadataViewGenerator.MetadataItemTargetType]), ex);
                        }
                        else if (ex.InnerException.GetType() == typeof(NullReferenceException))
                        {
                            // Unable to create an Instance of the Metadata view {0} because the exporter exported the metadata for the item {1} with a null value and null is not a valid value for type {2}.
                            throw new CompositionContractMismatchException(SR.Format(
                                SR.ContractMismatch_NullReferenceOnMetadataField,
                                ex.InnerException.Data[MetadataViewGenerator.MetadataViewType],
                                ex.InnerException.Data[MetadataViewGenerator.MetadataItemKey],
                                ex.InnerException.Data[MetadataViewGenerator.MetadataItemTargetType]), ex);
                        }
                    }
                    throw;
                }
            }
        }

        public static bool IsViewTypeValid(Type metadataViewType)
        {
            ArgumentNullException.ThrowIfNull(metadataViewType);

            // If the Metadata dictionary is cast compatible with the passed in type
            if (ExportServices.IsDefaultMetadataViewType(metadataViewType) ||
                metadataViewType.IsInterface ||
                ExportServices.IsDictionaryConstructorViewType(metadataViewType))
            {
                return true;
            }

            return false;
        }
    }
}
