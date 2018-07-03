// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.ComponentModel.Composition.Diagnostics
{
    internal static class CompositionTrace
    {
        internal static void Registration_ConstructorConventionOverridden(Type type)
        {
            if (type == null)
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);

            if (CompositionTraceSource.CanWriteInformation)
            {
                CompositionTraceSource.WriteInformation(CompositionTraceId.Registration_ConstructorConventionOverridden,
                    SR.Registration_ConstructorConventionOverridden,
                    type.FullName);
            }
        }

        internal static void Registration_TypeExportConventionOverridden(Type type)
        {
            if (type == null)
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);

            if (CompositionTraceSource.CanWriteWarning)
            {
                CompositionTraceSource.WriteWarning(CompositionTraceId.Registration_TypeExportConventionOverridden,
                    SR.Registration_TypeExportConventionOverridden,
                    type.FullName);
            }
        }

        internal static void Registration_MemberExportConventionOverridden(Type type, MemberInfo member)
        {
            if (type == null || member == null)
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);

            if (CompositionTraceSource.CanWriteWarning)
            {
                CompositionTraceSource.WriteWarning(CompositionTraceId.Registration_MemberExportConventionOverridden,
                    SR.Registration_MemberExportConventionOverridden,
                    member.Name, type.FullName);
            }
        }

        internal static void Registration_MemberImportConventionOverridden(Type type, MemberInfo member)
        {
            if (type == null || member == null)
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);

            if (CompositionTraceSource.CanWriteWarning)
            {
                CompositionTraceSource.WriteWarning(CompositionTraceId.Registration_MemberImportConventionOverridden,
                    SR.Registration_MemberImportConventionOverridden,
                    member.Name, type.FullName);
            }
        }

        internal static void Registration_PartCreationConventionOverridden(Type type)
        {
            if (type == null)
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);

            if (CompositionTraceSource.CanWriteWarning)
            {
                CompositionTraceSource.WriteWarning(CompositionTraceId.Registration_PartCreationConventionOverridden,
                    SR.Registration_PartCreationConventionOverridden,
                    type.FullName);
            }
        }

        internal static void Registration_MemberImportConventionMatchedTwice(Type type, MemberInfo member)
        {
            if (type == null || member == null)
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);

            if (CompositionTraceSource.CanWriteWarning)
            {
                CompositionTraceSource.WriteWarning(CompositionTraceId.Registration_MemberImportConventionMatchedTwice,
                    SR.Registration_MemberImportConventionMatchedTwice,
                    member.Name, type.FullName);
            }
        }

        internal static void Registration_PartMetadataConventionOverridden(Type type)
        {
            if (type == null)
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);

            if (CompositionTraceSource.CanWriteWarning)
            {
                CompositionTraceSource.WriteWarning(CompositionTraceId.Registration_PartMetadataConventionOverridden,
                    SR.Registration_PartMetadataConventionOverridden,
                    type.FullName);
            }
        }

        internal static void Registration_ParameterImportConventionOverridden(ParameterInfo parameter, ConstructorInfo constructor)
        {
            if (parameter == null || constructor == null)
                throw new Exception(SR.Diagnostic_InternalExceptionMessage);

            if (CompositionTraceSource.CanWriteWarning)
            {
                CompositionTraceSource.WriteWarning(CompositionTraceId.Registration_ParameterImportConventionOverridden,
                    SR.Registration_ParameterImportConventionOverridden,
                    parameter.Name, constructor.Name);
            }
        }
    }
}
