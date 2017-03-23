// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

namespace System
{
    internal static class MemberSerializationStringGenerator
    {
        //
        // Generate the "Signature2" binary serialization string for PropertyInfos
        //
        // Because the string is effectively a file format for serialized Reflection objects, it must be exactly correct. If missing
        // metadata prevents generating the string, this method throws a MissingMetadata exception.
        // 
        public static string SerializationToString(this PropertyInfo property) => ((RuntimePropertyInfo)property).SerializationToString();

        //
        // Generate the "Signature2" binary serialization string for ConstructorInfos
        //
        // Because the string is effectively a file format for serialized Reflection objects, it must be exactly correct. If missing
        // metadata prevents generating the string, this method throws a MissingMetadata exception.
        // 
        public static string SerializationToString(this ConstructorInfo constructor) => ((RuntimeConstructorInfo)constructor).SerializationToString();

        //
        // Generate the "Signature2" binary serialization string for MethodInfos
        //
        // Because the string is effectively a file format for serialized Reflection objects, it must be exactly correct. If missing
        // metadata prevents generating the string, this method throws a MissingMetadata exception.
        // 
        public static string SerializationToString(this MethodInfo method) => ((RuntimeMethodInfo)method).SerializationToString();
    }
}
