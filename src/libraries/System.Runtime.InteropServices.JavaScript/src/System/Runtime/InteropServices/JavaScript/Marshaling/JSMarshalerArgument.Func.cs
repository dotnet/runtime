// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.JavaScript
{
    public partial struct JSMarshalerArgument
    {
        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToManaged(out Action value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToManaged<T>(out Action<T> value, ArgumentToJSCallback<T> arg1Marshaler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToManaged<T1, T2>(out Action<T1, T2> value, ArgumentToJSCallback<T1> arg1Marshaler, ArgumentToJSCallback<T2> arg2Marshaler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToManaged<T1, T2, T3>(out Action<T1, T2, T3> value, ArgumentToJSCallback<T1> arg1Marshaler, ArgumentToJSCallback<T2> arg2Marshaler, ArgumentToJSCallback<T3> arg3Marshaler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToManaged<TResult>(out Func<TResult> value, ArgumentToManagedCallback<TResult> resMarshaler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToManaged<T, TResult>(out Func<T, TResult> value, ArgumentToJSCallback<T> arg1Marshaler, ArgumentToManagedCallback<TResult> resMarshaler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToManaged<T1, T2, TResult>(out Func<T1, T2, TResult> value, ArgumentToJSCallback<T1> arg1Marshaler, ArgumentToJSCallback<T2> arg2Marshaler, ArgumentToManagedCallback<TResult> resMarshaler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToManaged<T1, T2, T3, TResult>(out Func<T1, T2, T3, TResult> value, ArgumentToJSCallback<T1> arg1Marshaler, ArgumentToJSCallback<T2> arg2Marshaler, ArgumentToJSCallback<T3> arg3Marshaler, ArgumentToManagedCallback<TResult> resMarshaler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToJS(Action value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToJS<T>(Action<T> value, ArgumentToManagedCallback<T> arg1Marshaler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToJS<T1, T2>(Action<T1, T2> value, ArgumentToManagedCallback<T1> arg1Marshaler, ArgumentToManagedCallback<T2> arg2Marshaler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToJS<T1, T2, T3>(Action<T1, T2, T3> value, ArgumentToManagedCallback<T1> arg1Marshaler, ArgumentToManagedCallback<T2> arg2Marshaler, ArgumentToManagedCallback<T3> arg3Marshaler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToJS<TResult>(Func<TResult> value, ArgumentToJSCallback<TResult> resMarshaler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToJS<T, TResult>(Func<T, TResult> value, ArgumentToManagedCallback<T> arg1Marshaler, ArgumentToJSCallback<TResult> resMarshaler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToJS<T1, T2, TResult>(Func<T1, T2, TResult> value, ArgumentToManagedCallback<T1> arg1Marshaler, ArgumentToManagedCallback<T2> arg2Marshaler, ArgumentToJSCallback<TResult> resMarshaler)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToJS<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> value, ArgumentToManagedCallback<T1> arg1Marshaler, ArgumentToManagedCallback<T2> arg2Marshaler, ArgumentToManagedCallback<T3> arg3Marshaler, ArgumentToJSCallback<TResult> resMarshaler)
        {
            throw new NotImplementedException();
        }
    }
}
