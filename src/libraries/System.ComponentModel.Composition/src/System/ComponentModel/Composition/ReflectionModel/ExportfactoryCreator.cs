// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition.Primitives;
using System.Reflection;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal sealed partial class ExportFactoryCreator
    {
        private static readonly MethodInfo _createStronglyTypedExportFactoryOfT = typeof(ExportFactoryCreator).GetMethod("CreateStronglyTypedExportFactoryOfT", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly MethodInfo _createStronglyTypedExportFactoryOfTM = typeof(ExportFactoryCreator).GetMethod("CreateStronglyTypedExportFactoryOfTM", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;

        private readonly Type _exportFactoryType;

        public ExportFactoryCreator(Type exportFactoryType)
        {
            ArgumentNullException.ThrowIfNull(exportFactoryType);

            _exportFactoryType = exportFactoryType;
        }

        public Func<Export, object> CreateStronglyTypedExportFactoryFactory(Type exportType, Type? metadataViewType)
        {
            MethodInfo genericMethod;
            if (metadataViewType == null)
            {
                genericMethod = _createStronglyTypedExportFactoryOfT.MakeGenericMethod(exportType);
            }
            else
            {
                genericMethod = _createStronglyTypedExportFactoryOfTM.MakeGenericMethod(exportType, metadataViewType);
            }

            if (genericMethod == null)
            {
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);
            }

            Func<Export, object> exportFactoryFactory = (Func<Export, object>)Delegate.CreateDelegate(typeof(Func<Export, object>), this, genericMethod);
            return exportFactoryFactory.Invoke;
        }

        private object CreateStronglyTypedExportFactoryOfT<T>(Export export)
        {
            Type[] typeArgs = { typeof(T) };
            Type constructed = _exportFactoryType.MakeGenericType(typeArgs);

            Func<Tuple<T, Action>> exportLifetimeContextCreator = () => LifetimeContext.GetExportLifetimeContextFromExport<T>(export);
            object[] args = { exportLifetimeContextCreator };

            var instance = Activator.CreateInstance(constructed, args);

            return instance!;
        }

        private object CreateStronglyTypedExportFactoryOfTM<T, M>(Export export)
        {
            Type[] typeArgs = { typeof(T), typeof(M) };
            Type constructed = _exportFactoryType.MakeGenericType(typeArgs);

            Func<Tuple<T, Action>> exportLifetimeContextCreator = () => LifetimeContext.GetExportLifetimeContextFromExport<T>(export);
            var metadataView = AttributedModelServices.GetMetadataView<M>(export.Metadata);
            object?[] args = { exportLifetimeContextCreator, metadataView };

            var instance = Activator.CreateInstance(constructed, args);

            return instance!;
        }

    }
}
