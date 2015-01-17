// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Internal;
using System.Reflection;

namespace Microsoft.Composition.Diagnostics
{
    internal static class CompositionTrace
    {
        internal static void Registration_ConstructorConventionOverridden(Type type)
        {
            Assumes.NotNull(type);

            if (CompositionTraceSource.CanWriteInformation)
            {
                CompositionTraceSource.WriteInformation(CompositionTraceId.Registration_ConstructorConventionOverridden,
                                                        Strings.Registration_ConstructorConventionOverridden,
                                                        type.FullName);
            }
        }

        internal static void Registration_TypeExportConventionOverridden(Type type)
        {
            Assumes.NotNull(type);

            if (CompositionTraceSource.CanWriteWarning)
            {
                CompositionTraceSource.WriteWarning(CompositionTraceId.Registration_TypeExportConventionOverridden,
                                                    Strings.Registration_TypeExportConventionOverridden,
                                                    type.FullName);
            }
        }

        internal static void Registration_MemberExportConventionOverridden(Type type, MemberInfo member)
        {
            Assumes.NotNull(type, member);

            if (CompositionTraceSource.CanWriteWarning)
            {
                CompositionTraceSource.WriteWarning(CompositionTraceId.Registration_MemberExportConventionOverridden,
                                                    Strings.Registration_MemberExportConventionOverridden,
                                                    member.Name, type.FullName);
            }
        }

        internal static void Registration_MemberImportConventionOverridden(Type type, MemberInfo member)
        {
            Assumes.NotNull(type, member);

            if (CompositionTraceSource.CanWriteWarning)
            {
                CompositionTraceSource.WriteWarning(CompositionTraceId.Registration_MemberImportConventionOverridden,
                                                    Strings.Registration_MemberImportConventionOverridden,
                                                    member.Name, type.FullName);
            }
        }

        internal static void Registration_OnSatisfiedImportNotificationOverridden(Type type, MemberInfo member)
        {
            Assumes.NotNull(type, member);

            if (CompositionTraceSource.CanWriteWarning)
            {
                CompositionTraceSource.WriteWarning(CompositionTraceId.Registration_OnSatisfiedImportNotificationOverridden,
                                                    Strings.Registration_OnSatisfiedImportNotificationOverridden,
                                                    member.Name, type.FullName);
            }
        }

        internal static void Registration_PartCreationConventionOverridden(Type type)
        {
            Assumes.NotNull(type);

            if (CompositionTraceSource.CanWriteWarning)
            {
                CompositionTraceSource.WriteWarning(CompositionTraceId.Registration_PartCreationConventionOverridden,
                                                    Strings.Registration_PartCreationConventionOverridden,
                                                    type.FullName);
            }
        }

        internal static void Registration_MemberImportConventionMatchedTwice(Type type, MemberInfo member)
        {
            Assumes.NotNull(type, member);

            if (CompositionTraceSource.CanWriteWarning)
            {
                CompositionTraceSource.WriteWarning(CompositionTraceId.Registration_MemberImportConventionMatchedTwice,
                                                    Strings.Registration_MemberImportConventionMatchedTwice,
                                                    member.Name, type.FullName);
            }
        }

        internal static void Registration_PartMetadataConventionOverridden(Type type)
        {
            Assumes.NotNull(type);

            if (CompositionTraceSource.CanWriteWarning)
            {
                CompositionTraceSource.WriteWarning(CompositionTraceId.Registration_PartMetadataConventionOverridden,
                                                    Strings.Registration_PartMetadataConventionOverridden,
                                                    type.FullName);
            }
        }

        internal static void Registration_ParameterImportConventionOverridden(ParameterInfo parameter, ConstructorInfo constructor)
        {
            Assumes.NotNull(parameter, constructor);

            if (CompositionTraceSource.CanWriteWarning)
            {
                CompositionTraceSource.WriteWarning(CompositionTraceId.Registration_ParameterImportConventionOverridden,
                                                    Strings.Registration_ParameterImportConventionOverridden,
                                                    parameter.Name, constructor.Name);
            }
        }
    }
}
