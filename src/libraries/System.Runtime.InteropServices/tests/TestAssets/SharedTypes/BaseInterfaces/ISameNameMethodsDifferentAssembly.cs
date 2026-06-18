// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid(IID_A)]
    public partial interface IExternalSameNameA
    {
        public const string IID_A = "2d3b434a-9119-45c6-9f40-1d35bb38d494";

        double MyMethod();
    }

    [GeneratedComInterface]
    [Guid(IID_B)]
    public partial interface IExternalSameNameB
    {
        public const string IID_B = "5ef8dd8a-6c27-4724-8645-4406a4e45a8d";

        int MyMethod();
    }
}
