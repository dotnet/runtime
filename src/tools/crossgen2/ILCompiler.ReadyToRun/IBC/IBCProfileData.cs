// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Internal.TypeSystem;

namespace ILCompiler.IBC
{

    public class IBCProfileData : ProfileData
    {
        public IBCProfileData(bool partialNGen, IEnumerable<MethodProfileData> methodData)
        {
            foreach (MethodProfileData data in methodData)
            {
                if (_methodData.ContainsKey(data.Method))
                    throw new Exception("Multiple copies of data for the same method"); // TODO, I think this is actually valid, but lets see

                _methodData.Add(data.Method, data);
            }
            _partialNGen = partialNGen;
        }

        private readonly Dictionary<MethodDesc, MethodProfileData> _methodData = new Dictionary<MethodDesc, MethodProfileData>();
        private readonly bool _partialNGen;

        public override bool PartialNGen { get { return _partialNGen; } }

        public override MethodProfileData GetMethodProfileData(MethodDesc m)
        {
            _methodData.TryGetValue(m, out MethodProfileData profileData);
            return profileData;
        }

        public override IEnumerable<MethodProfileData> GetAllMethodProfileData()
        {
            return _methodData.Values;
        }

        public override byte[] GetMethodBlockCount(MethodDesc m)
        {
            throw new NotImplementedException();
        }
    }
}
