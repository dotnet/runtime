// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Text.Json.SourceGeneration
{
    public sealed partial class JsonSourceGenerator
    {
        private sealed partial class Emitter
        {
            /// <summary>
            /// Unlike sourcegen warnings, exception messages should not be localized so we keep them in source.
            /// </summary>
            private static class ExceptionMessages
            {
                public const string InaccessibleJsonIncludePropertiesNotSupported =
                    "The member '{0}.{1}' has been annotated with the JsonIncludeAttribute but is not visible to the source generator.";

                public const string IncompatibleConverterType =
                    "The converter '{0}' is not compatible with the type '{1}'.";

                public const string InitOnlyPropertySetterNotSupported =
                    "Setting init-only properties is not supported in source generation mode.";

                public const string InvalidJsonConverterFactoryOutput =
                    "The converter '{0}' cannot return null or a JsonConverterFactory instance.";

                public const string InvalidSerializablePropertyConfiguration =
                    "Invalid serializable-property configuration specified for type '{0}'. For more information, see 'JsonSourceGenerationMode.Serialization'.";

                public const string PropertyGetterDisallowNull =
                    "The property or field '{0}' on type '{1}' doesn't allow getting null values. Consider updating its nullability annotation.";
            };
        }
    }
}
