// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace Activator
{
    internal sealed class ComActivationContextShim
    {
        private static readonly Type _comActivationContextType = typeof(object).Assembly.GetType("Internal.Runtime.InteropServices.ComActivationContext", throwOnError: true);
        private static readonly FieldInfo _classIdField = _comActivationContextType.GetField("ClassId");
        private static readonly FieldInfo _interfaceIdField = _comActivationContextType.GetField("InterfaceId");
        private static readonly FieldInfo _assemblyPathField = _comActivationContextType.GetField("AssemblyPath");
        private static readonly FieldInfo _assemblyNameField = _comActivationContextType.GetField("AssemblyName");
        private static readonly FieldInfo _typeNameField = _comActivationContextType.GetField("TypeName");

        public object UnderlyingContext { get; } = System.Activator.CreateInstance(_comActivationContextType);

        public Guid ClassId
        {
            get => (Guid)_classIdField.GetValue(UnderlyingContext);
            set => _classIdField.SetValue(UnderlyingContext, value);
        }
        public Guid InterfaceId
        {
            get => (Guid)_interfaceIdField.GetValue(UnderlyingContext);
            set => _interfaceIdField.SetValue(UnderlyingContext, value);
        }
        public string AssemblyPath
        {
            get => (string)_assemblyPathField.GetValue(UnderlyingContext);
            set => _assemblyPathField.SetValue(UnderlyingContext, value);
        }
        public string AssemblyName
        {
            get => (string)_assemblyNameField.GetValue(UnderlyingContext);
            set => _assemblyNameField.SetValue(UnderlyingContext, value);
        }
        public string TypeName
        {
            get => (string)_typeNameField.GetValue(UnderlyingContext);
            set => _typeNameField.SetValue(UnderlyingContext, value);
        }
    }
}
