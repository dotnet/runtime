// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Internal;

namespace System.ComponentModel.Composition
{
    /// <summary>
    ///     Enables the AssemblyCatalog to discover user provided ReflectionContexts.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false,Inherited = true)]
    public class CatalogReflectionContextAttribute : Attribute
    {
        Type _reflectionContextType;

        public CatalogReflectionContextAttribute(Type reflectionContextType)
        {
            Requires.NotNull(reflectionContextType, nameof(reflectionContextType));

            _reflectionContextType = reflectionContextType;
        }

        public ReflectionContext CreateReflectionContext()
        {
            if (_reflectionContextType == null)
            {
                throw new ArgumentNullException(nameof(_reflectionContextType));
            }

            ReflectionContext reflectionContext = null;
            try
            {
                reflectionContext = (ReflectionContext)Activator.CreateInstance(_reflectionContextType);
            }
            catch (InvalidCastException invalidCastException)
            {
                throw new InvalidOperationException(SR.ReflectionContext_Type_Required, invalidCastException);
            }
            catch (MissingMethodException missingMethodException)
            {
                throw new MissingMethodException(SR.ReflectionContext_Requires_DefaultConstructor, missingMethodException);
            }

            return reflectionContext;
        }
    }
}
