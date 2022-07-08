// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    // Methods in this file are marshaling System.Object signature to Any JS signature dynamicaly.
    // In order to do that, we are refering to all well know marshaled types
    // therefore they could not be linked out during AOT, when user uses System.Object signature in his [JSImport] or [JSExport]
    // it is pay for play

    public partial struct JSMarshalerArgument
    {
        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToManaged(out object? value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToJS(object? value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToManaged(out object?[]? value)
        {
            throw new NotImplementedException();

        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ToJS(object?[] value)
        {
            throw new NotImplementedException();
        }
    }
}
