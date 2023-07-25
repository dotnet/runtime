// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("F4964CBF-89AF-460B-8495-107782187708")]
    internal partial interface IArrayOfStatefulElements
    {
        void Method([MarshalUsing(CountElementName = nameof(size))] StatefulType[] param, int size);
        void MethodIn([MarshalUsing(CountElementName = nameof(size))] in StatefulType[] param, int size);
        void MethodOut([MarshalUsing(CountElementName = nameof(size))] out StatefulType[] param, int size);
        void MethodRef([MarshalUsing(CountElementName = nameof(size))] ref StatefulType[] param, int size);
        void MethodContentsIn([MarshalUsing(CountElementName = nameof(size))][In] StatefulType[] param, int size);
        void MethodContentsOut([MarshalUsing(CountElementName = nameof(size))][Out] StatefulType[] param, int size);
        void MethodContentsInOut([MarshalUsing(CountElementName = nameof(size))][In, Out] StatefulType[] param, int size);
    }
}
