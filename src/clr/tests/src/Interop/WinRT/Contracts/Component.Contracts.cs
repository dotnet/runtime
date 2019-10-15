// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Component.Contracts
{
    [ComImport]
    [Guid("971AF13A-9793-4AF7-B2F2-72D829195592")]
    [WindowsRuntimeImport]
    public interface IBooleanTesting
    {
        bool And(bool left, bool right);
    }

    [ComImport]
    [Guid("C6F1F632-47B6-4A52-86D2-A89807ED2677")]
    [WindowsRuntimeImport]
    public interface IStringTesting
    {
        string ConcatStrings(string left, string right);
    }

    [ComImport]
    [Guid("939D4EE5-8D41-4C4B-8948-86017CEB9244")]
    [WindowsRuntimeImport]
    public interface INullableTesting
    {
        bool IsNull(int? value);
        int GetIntValue(int? value);
    }

    [ComImport]
    [Guid("BB545A14-9AE7-491A-874D-1C03D239FB70")]
    [WindowsRuntimeImport]
    public interface ITypeTesting
    {
        string GetTypeName(Type type);
    }

    [ComImport]
    [Guid("9162201d-b591-4f30-8f41-f0f79f6ecea3")]
    [WindowsRuntimeImport]
    public interface IExceptionTesting
    {
        void ThrowException(Exception ex);
        Exception GetException(int hResultToReturn);
    }

    [ComImport]
    [Guid("ccd10099-3a45-4382-970d-b76f52780bcd")]
    [WindowsRuntimeImport]
    public interface IKeyValuePairTesting
    {
        KeyValuePair<int, int> MakeSimplePair(int key, int value);
        KeyValuePair<string, string> MakeMarshaledPair(string key, string value);
        KeyValuePair<int, IEnumerable<int>> MakeProjectedPair(int key, int[] values);
    }

    [ComImport]
    [Guid("e0af24b3-e6c6-4e89-b8d1-a332979ef398")]
    [WindowsRuntimeImport]
    public interface IUriTesting
    {
        string GetFromUri(Uri uri);
        Uri CreateUriFromString(string uri);
    }

    [ComImport]
    [Guid("821B532D-CC5E-4218-90AB-A8361AC92794")]
    [WindowsRuntimeImport]
    public interface IArrayTesting
    {
        int Sum(int[] array);
        bool Xor(bool[] array);
    }

    [ComImport]
    [Guid("4bb923ae-986a-4aad-9bfb-13e0b5ecffa4")]
    [WindowsRuntimeImport]
    public interface IBindingViewModel
    {
        INotifyCollectionChanged Collection { get; }
        void AddElement(int i);
        string Name { get; set; }
    }

    [ComImport]
    [Guid("857e28e1-3e7f-4f6f-8554-efc73feba286")]
    [WindowsRuntimeImport]
    public interface IBindingProjectionsTesting
    {
        IBindingViewModel CreateViewModel();
        IDisposable InitializeXamlFrameworkForCurrentThread();
    }

    public enum TestEnum
    {
        A = 1,
        B = 2,
        C = 3
    }

    [ComImport]
    [Guid("d89d71b2-2671-444d-8576-536d206dea49")]
    [WindowsRuntimeImport]
    public interface IEnumTesting
    {
        TestEnum GetA();
        Boolean IsB(TestEnum val);
    }
}
