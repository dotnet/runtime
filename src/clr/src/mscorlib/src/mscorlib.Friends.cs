// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

// For now we are only moving to using this file over AssemblyAttributes.cspp in CoreSys, ideally we would move away from the centralized 
// AssemblyAttributes.cspp model for the other build types at a future point in time.

// Depends on things like SuppressUnmanagedCodeAttribute and WindowsRuntimeImportAttribute
[assembly: InternalsVisibleTo("System.Runtime.WindowsRuntime, PublicKey=00000000000000000400000000000000", AllInternalsVisible = false)]

// Depends on WindowsRuntimeImportAttribute
[assembly: InternalsVisibleTo("System.Runtime.WindowsRuntime.UI.Xaml, PublicKey=00000000000000000400000000000000", AllInternalsVisible = false)]

// Cross framework serialization needs access to internals
[assembly: InternalsVisibleTo("mscorlib, PublicKey=00000000000000000400000000000000", AllInternalsVisible=false)]
