// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NETSTANDARD2_0
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Index))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Range))]

#if !NETSTANDARD2_1
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Text.Rune))]
#endif // !NETSTANDARD2_1

#endif // !NETSTANDARD2_0
