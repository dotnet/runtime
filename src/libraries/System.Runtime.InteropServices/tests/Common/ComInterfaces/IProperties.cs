// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
    [Guid(IID)]
    internal partial interface IProperties
    {
        public const string IID = "F8E80A83-53F7-4F90-8009-C64FD405F445";

        int IntProperty { get; set; }

        int ReadOnlyInt { get; }

        int WriteOnlyInt { set; }

        Guid GuidProperty { get; set; }

        string StringProperty { get; set; }

        IProperties? Self { get; set; }
    }

    [GeneratedComClass]
    internal partial class Properties : IProperties
    {
        private int _int;
        private int _writeOnlyIntSink;
        private Guid _guid;
        private string _string = string.Empty;
        private IProperties? _self;

        public int IntProperty
        {
            get => _int;
            set => _int = value;
        }

        public int ReadOnlyInt => 111;

        public int WriteOnlyInt
        {
            set => _writeOnlyIntSink = value;
        }

        public Guid GuidProperty
        {
            get => _guid;
            set => _guid = value;
        }

        public string StringProperty
        {
            get => _string;
            set => _string = value;
        }

        public IProperties? Self
        {
            get => _self;
            set => _self = value;
        }

        // Non-interface accessor so the WriteOnlyInt setter's side effect is observable from tests.
        internal int WriteOnlyIntSink => _writeOnlyIntSink;
    }
}
