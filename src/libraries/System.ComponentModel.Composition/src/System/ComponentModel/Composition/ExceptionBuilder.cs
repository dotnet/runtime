// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Globalization;

namespace System.ComponentModel
{
    internal static class ExceptionBuilder
    {
        public static Exception CreateDiscoveryException(string messageFormat, params string[] arguments)
        {
            // DiscoveryError (Dev10:602872): This should go through the discovery error reporting when
            // we add a way to report discovery errors properly.
            return new InvalidOperationException(Format(messageFormat, arguments));
        }

        public static ArgumentException CreateContainsNullElement(string parameterName)
        {
            ArgumentNullException.ThrowIfNull(parameterName);

            string message = Format(SR.Argument_NullElement, parameterName);

            return new ArgumentException(message, parameterName);
        }

        public static ObjectDisposedException CreateObjectDisposed(object instance)
        {
            ArgumentNullException.ThrowIfNull(instance);

            return new ObjectDisposedException(instance.GetType().ToString());
        }

        public static NotImplementedException CreateNotOverriddenByDerived(string memberName)
        {
            ArgumentNullException.ThrowIfNull(memberName);

            if (memberName.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.ArgumentException_EmptyString, nameof(memberName)), nameof(memberName));
            }

            string message = Format(SR.NotImplemented_NotOverriddenByDerived, memberName);

            return new NotImplementedException(message);
        }

        public static ArgumentException CreateExportDefinitionNotOnThisComposablePart(string parameterName)
        {
            ArgumentNullException.ThrowIfNull(parameterName);

            if (parameterName.Length == 0)
            {
                throw new ArgumentException(SR.ArgumentException_EmptyString);
            }


            string message = Format(SR.ExportDefinitionNotOnThisComposablePart, parameterName);

            return new ArgumentException(message, parameterName);
        }

        public static ArgumentException CreateImportDefinitionNotOnThisComposablePart(string parameterName)
        {
            ArgumentNullException.ThrowIfNull(parameterName);

            if (parameterName.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.ArgumentException_EmptyString, nameof(parameterName)), nameof(parameterName));
            }

            string message = Format(SR.ImportDefinitionNotOnThisComposablePart, parameterName);

            return new ArgumentException(message, parameterName);
        }

        public static CompositionException CreateCannotGetExportedValue(ComposablePart part, ExportDefinition definition, Exception innerException)
        {
            ArgumentNullException.ThrowIfNull(part);
            ArgumentNullException.ThrowIfNull(definition);
            ArgumentNullException.ThrowIfNull(innerException);

            return new CompositionException(
                ErrorBuilder.CreateCannotGetExportedValue(part, definition, innerException));
        }

        public static ArgumentException CreateReflectionModelInvalidPartDefinition(string parameterName, Type partDefinitionType)
        {
            ArgumentNullException.ThrowIfNull(parameterName);

            if (parameterName.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.ArgumentException_EmptyString, nameof(parameterName)), nameof(parameterName));
            }

            if (partDefinitionType == null)
            {
                throw new ArgumentNullException(nameof(partDefinitionType));
            }

            return new ArgumentException(SR.Format(SR.ReflectionModel_InvalidPartDefinition, partDefinitionType), parameterName);
        }

        public static ArgumentException ExportFactory_TooManyGenericParameters(string typeName)
        {
            ArgumentNullException.ThrowIfNull(typeName);

            if (typeName.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.ArgumentException_EmptyString, nameof(typeName)), nameof(typeName));
            }

            string message = Format(SR.ExportFactory_TooManyGenericParameters, typeName);

            return new ArgumentException(message, typeName);
        }

        private static string Format(string format, params string[] arguments)
        {
            return string.Format(CultureInfo.CurrentCulture, format, arguments);
        }
    }
}
