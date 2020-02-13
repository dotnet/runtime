// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Dynamic;
using System.Linq.Expressions;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    internal sealed class ComTypeLibInfo : IDynamicMetaObjectProvider
    {
        private readonly ComTypeLibDesc _typeLibDesc;

        internal ComTypeLibInfo(ComTypeLibDesc typeLibDesc)
        {
            _typeLibDesc = typeLibDesc;
        }

        public string Name => _typeLibDesc.Name;

        public Guid Guid => _typeLibDesc.Guid;

        public short VersionMajor => _typeLibDesc.VersionMajor;

        public short VersionMinor => _typeLibDesc.VersionMinor;

        public ComTypeLibDesc TypeLibDesc => _typeLibDesc;

        // TODO: internal
        public string[] GetMemberNames()
        {
            return new string[] { Name, "Guid", "Name", "VersionMajor", "VersionMinor" };
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new TypeLibInfoMetaObject(parameter, this);
        }
    }
}
