// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
    [Guid(IID)]
    internal partial interface IDerivedProperties : IProperties
    {
        public new const string IID = "28DBA1C7-AE33-47B9-AAE2-C7DF49F0042E";

        int DerivedIntProperty { get; set; }

        string DerivedStringProperty { get; set; }

        int DerivedReadOnlyInt { get; }
    }

    [GeneratedComClass]
    internal partial class DerivedProperties : Properties, IDerivedProperties
    {
        private int _derivedInt;
        private string _derivedString = string.Empty;

        public int DerivedIntProperty
        {
            get => _derivedInt;
            set => _derivedInt = value;
        }

        public string DerivedStringProperty
        {
            get => _derivedString;
            set => _derivedString = value;
        }

        public int DerivedReadOnlyInt => 2222;
    }
}
