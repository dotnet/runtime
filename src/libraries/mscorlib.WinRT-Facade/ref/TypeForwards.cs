// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

// The following types are can be referenced by windows.winmd and are spec'ed to come from mscorlib
[assembly: TypeForwardedTo(typeof(System.Attribute))]
[assembly: TypeForwardedTo(typeof(System.Boolean))]
[assembly: TypeForwardedTo(typeof(System.Byte))]
[assembly: TypeForwardedTo(typeof(System.Char))]
[assembly: TypeForwardedTo(typeof(System.Double))]
[assembly: TypeForwardedTo(typeof(System.Enum))]
[assembly: TypeForwardedTo(typeof(System.FlagsAttribute))]
[assembly: TypeForwardedTo(typeof(System.Guid))]
[assembly: TypeForwardedTo(typeof(System.Int16))]
[assembly: TypeForwardedTo(typeof(System.Int32))]
[assembly: TypeForwardedTo(typeof(System.Int64))]
[assembly: TypeForwardedTo(typeof(System.IntPtr))]
[assembly: TypeForwardedTo(typeof(System.MulticastDelegate))]
[assembly: TypeForwardedTo(typeof(System.Object))]
[assembly: TypeForwardedTo(typeof(System.Runtime.CompilerServices.IsConst))]
[assembly: TypeForwardedTo(typeof(System.Single))]
[assembly: TypeForwardedTo(typeof(System.String))]
[assembly: TypeForwardedTo(typeof(System.Type))]
[assembly: TypeForwardedTo(typeof(System.UInt16))]
[assembly: TypeForwardedTo(typeof(System.UInt32))]
[assembly: TypeForwardedTo(typeof(System.UInt64))]
[assembly: TypeForwardedTo(typeof(System.ValueType))]
[assembly: TypeForwardedTo(typeof(void))] // System.Void

// XAML compiler is checking for the following
[assembly: TypeForwardedTo(typeof(System.Array))]