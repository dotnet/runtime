// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using Internal.TypeSystem;

namespace ILVerify
{
    public class VerificationResult
    {
        public VerifierError Code { get; internal set; }
        public ExceptionStringID? ExceptionID { get; internal set; }
        public TypeDefinitionHandle Type { get; internal set; }
        public MethodDefinitionHandle Method { get; internal set; }
        internal EntityHandle MetadataHandle { get; set; }
        public string Message { get; internal set; }
        public object[] Args { get; internal set; }
        public ErrorArgument[] ErrorArguments { get; set; } = Array.Empty<ErrorArgument>();

        public T GetArgumentValue<T>(string name)
        {
            TryGetArgumentValue<T>(name, out T argumentValue);
            return argumentValue;
        }

        public bool TryGetArgumentValue<T>(string name,out T argumentValue)
        {
            argumentValue = default;

            foreach (ErrorArgument argument in ErrorArguments)
            {
                if (argument.Name == name)
                {
                    argumentValue = (T)argument.Value;
                    return true;
                }
            }

            return false;
        }
    }

    public class ErrorArgument
    {
        public ErrorArgument() { }

        public ErrorArgument(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public object Value { get; }
    }
}
