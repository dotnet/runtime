// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Resources;
using System.Reflection;

// This partial file is designed to allow the runtime variant of the type system to not
// need to support accessing these strings via the ResourceManager
namespace Internal.TypeSystem
{
    public partial class TypeSystemException : Exception
    {
        private static Lazy<ResourceManager> s_stringResourceManager =
            new Lazy<ResourceManager>(() => new ResourceManager("Internal.TypeSystem.Strings", typeof(TypeSystemException).GetTypeInfo().Assembly));

        public static string GetFormatString(ExceptionStringID id)
        {
            return s_stringResourceManager.Value.GetString(id.ToString(), CultureInfo.InvariantCulture);
        }
    }
}
