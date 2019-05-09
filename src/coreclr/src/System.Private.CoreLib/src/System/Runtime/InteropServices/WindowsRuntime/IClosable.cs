// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // Local definition of Windows.Foundation.IClosable
    [ComImport]
    [Guid("30d5a829-7fa4-4026-83bb-d75bae4ea99e")]
    [WindowsRuntimeImport]
    internal interface IClosable
    {
        void Close();
    }

    // Adapter class - converts IClosable.Close calls to Disposable.Dispose
    internal sealed class IDisposableToIClosableAdapter
    {
        private IDisposableToIClosableAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        public void Close()
        {
            IDisposable _this = Unsafe.As<IDisposable>(this);
            _this.Dispose();
        }
    }

    // Adapter class which converts IDisposable.Dispose calls into IClosable.Close
    internal sealed class IClosableToIDisposableAdapter
    {
        private IClosableToIDisposableAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        private void Dispose()
        {
            IClosable _this = Unsafe.As<IClosable>(this);
            _this.Close();
        }
    }
}
