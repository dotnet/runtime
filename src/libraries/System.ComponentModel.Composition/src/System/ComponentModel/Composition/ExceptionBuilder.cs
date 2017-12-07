// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Globalization;
using Microsoft.Internal;

namespace System.ComponentModel
{
    internal static class ExceptionBuilder // UNDONE combine with other one
    {
        public static Exception CreateDiscoveryException(string messageFormat, params string[] arguments)
        {
            // DiscoveryError (Dev10:602872): This should go through the discovery error reporting when 
            // we add a way to report discovery errors properly.
            return new InvalidOperationException(Format(messageFormat, arguments));
        }

        public static ArgumentException CreateContainsNullElement(string parameterName)
        {
            Assumes.NotNull(parameterName);

            string message = Format(SR.Argument_NullElement, parameterName);

            return new ArgumentException(message, parameterName);
        }

        public static ObjectDisposedException CreateObjectDisposed(object instance)
        {
            Assumes.NotNull(instance);

            return new ObjectDisposedException(instance.GetType().ToString());
        }

        public static NotImplementedException CreateNotOverriddenByDerived(string memberName)
        {
            Assumes.NotNullOrEmpty(memberName);

            string message = Format(SR.NotImplemented_NotOverriddenByDerived, memberName);

            return new NotImplementedException(message);
        }

        public static ArgumentException CreateExportDefinitionNotOnThisComposablePart(string parameterName)
        {
            Assumes.NotNullOrEmpty(parameterName);

            string message = Format(SR.ExportDefinitionNotOnThisComposablePart, parameterName);

            return new ArgumentException(message, parameterName);
        }

        public static ArgumentException CreateImportDefinitionNotOnThisComposablePart(string parameterName)
        {
            Assumes.NotNullOrEmpty(parameterName);

            string message = Format(SR.ImportDefinitionNotOnThisComposablePart, parameterName);

            return new ArgumentException(message, parameterName);
        }

        public static CompositionException CreateCannotGetExportedValue(ComposablePart part, ExportDefinition definition, Exception innerException)
        {
            Assumes.NotNull(part, definition, innerException);

            return new CompositionException(
                ErrorBuilder.CreateCannotGetExportedValue(part, definition, innerException));
        }

        public static ArgumentException CreateReflectionModelInvalidPartDefinition(string parameterName, Type partDefinitionType)
        {
            Assumes.NotNullOrEmpty(parameterName);
            Assumes.NotNull(partDefinitionType);

            return new ArgumentException(string.Format(CultureInfo.CurrentCulture, SR.ReflectionModel_InvalidPartDefinition, partDefinitionType), parameterName);
        }

        public static ArgumentException ExportFactory_TooManyGenericParameters(string typeName)
        {
            Assumes.NotNullOrEmpty(typeName);

            string message = Format(SR.ExportFactory_TooManyGenericParameters, typeName);

            return new ArgumentException(message, typeName);
        }

        private static string Format(string format, params string[] arguments)
        {
            return String.Format(CultureInfo.CurrentCulture, format, arguments);
        }
    }
}
