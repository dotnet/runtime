// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Internal.TypeSystem;
using System.Linq;

namespace ILCompiler.IBC
{

    public class IBCProfileData : ProfileData
    {
        public IBCProfileData(bool partialNGen, IEnumerable<MethodProfileData> methodData)
        {
            MethodProfileData[] dataArray = methodData.ToArray();
            foreach (MethodProfileData data in dataArray)
            {
                if (!_methodData.ContainsKey(data.Method))
                {
                    _methodData.Add(data.Method, data);
                }
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
