// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ComInterfaceGenerator.Unit.Tests
{
    internal class GeneratedComInterfaceAttributeProvider : IComInterfaceAttributeProvider
    {
        private ComInterfaceOptions? _options;

        public GeneratedComInterfaceAttributeProvider()
        {
        }

        public GeneratedComInterfaceAttributeProvider(ComInterfaceOptions options) => _options = options;

        public string VirtualMethodIndex(
            int index,
            bool? ImplicitThisParameter = null,
            MarshalDirection? Direction = null,
            StringMarshalling? StringMarshalling = null,
            Type? StringMarshallingCustomType = null,
            bool? SetLastError = null,
            ExceptionMarshalling? ExceptionMarshalling = null,
            Type? ExceptionMarshallingType = null)
                => "";

        public string UnmanagedObjectUnwrapper(Type t) => "";

        public string GeneratedComInterface(StringMarshalling? stringMarshalling = null, Type? stringMarshallingCustomType = null)
        {
            List<string> arguments = new();
            if (stringMarshalling is not null)
            {
                arguments.Add($"StringMarshalling = {typeof(StringMarshalling).FullName}.{stringMarshalling!.Value}");
            }
            if (stringMarshallingCustomType is not null)
            {
                arguments.Add($"StringMarshallingCustomType = typeof({stringMarshallingCustomType!.FullName})");
            }
            if (_options is not null)
            {
                arguments.Add($"Options = (ComInterfaceOptions){_options.Value:D}");
            }
            return @$"[global::System.Runtime.InteropServices.Marshalling.GeneratedComInterface("
                + string.Join(", ", arguments)
                + @$"), global::System.Runtime.InteropServices.Guid(""0A52B77C-E08B-4274-A1F4-1A2BF2C07E60"")]";
        }

        public string AdditionalUserRequiredInterfaces(string userDefinedInterfaceName) => "";
    }
}
