// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    // Runtime exception messages; not localized so we keep them in source.
    internal static class ExceptionMessages
    {
        public const string CannotBindToConstructorParameter = "Cannot create instance of type '{0}' because one or more parameters cannot be bound to. Constructor parameters cannot be declared as in, out, or ref. Invalid parameters are: '{1}'";
        public const string CannotSpecifyBindNonPublicProperties = "The configuration binding source generator does not support 'BinderOptions.BindNonPublicProperties'.";
        public const string ConstructorParametersDoNotMatchProperties = "Cannot create instance of type '{0}' because one or more parameters cannot be bound to. Constructor parameters must have corresponding properties. Fields are not supported. Missing properties are: '{1}'";
        public const string FailedBinding = "Failed to convert configuration value at '{0}' to type '{1}'.";
        public const string MissingConfig = "'{0}' was set on the provided {1}, but the following properties were not found on the instance of {2}: {3}";
        public const string MissingPublicInstanceConstructor = "Cannot create instance of type '{0}' because it is missing a public instance constructor.";
        public const string MultipleParameterizedConstructors = "Cannot create instance of type '{0}' because it has multiple public parameterized constructors.";
        public const string ParameterBeingBoundToIsUnnamed = "Cannot create instance of type '{0}' because one or more parameters are unnamed.";
        public const string ParameterHasNoMatchingConfig = "Cannot create instance of type '{0}' because parameter '{1}' has no matching config. Each parameter in the constructor that does not have a default value must have a corresponding config entry.";
        public const string TypeNotDetectedAsInput = "Unable to bind to type '{0}': generator did not detect the type as input.";
        public const string TypeNotSupportedAsInput = "Unable to bind to type '{0}': generator does not support this type as input to this method.";
    }
}
