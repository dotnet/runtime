// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    public partial struct JSMarshalerArgument
    {
        /// <summary>
        /// Helps with marshaling of the Task result or Function arguments.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public delegate void ArgumentToManagedCallback<T>(ref JSMarshalerArgument arg, out T value);

        /// <summary>
        /// Helps with marshaling of the Task result or Function arguments.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public delegate void ArgumentToJSCallback<T>(ref JSMarshalerArgument arg, T value);

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToManaged(out Task value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToManaged<T>(out Task<T> value, ArgumentToManagedCallback<T> marshaler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public void ToJS(Task value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public void ToJS<T>(Task<T> value, ArgumentToJSCallback<T> marshaler)
        {
            throw new NotImplementedException();
        }
    }
}
