// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.PortableExecutable;

using ILCompiler.IBC;
using Internal.TypeSystem;

namespace ILCompiler
{
    public class ProfileDataManager
    {
        private readonly Dictionary<MethodDesc, MethodProfileData> _mergedProfileData = new Dictionary<MethodDesc, MethodProfileData>();
        
        public ProfileDataManager(IEnumerable<string> mibcFiles,
                                  CompilerTypeSystemContext context)
        {
            List<ProfileData> _inputData = new List<ProfileData>();

            foreach (string file in mibcFiles)
            {
                using (PEReader peReader = MIbcProfileParser.OpenMibcAsPEReader(file))
                {
                    _inputData.Add(MIbcProfileParser.ParseMIbcFile(context, peReader, null, null));
                }
            }

            bool dummy = false;

            // Merge all data together
            foreach (ProfileData profileData in _inputData)
            {
                ProfileData.MergeProfileData(ref dummy, _mergedProfileData, profileData);
            }
        }

        public MethodProfileData this[MethodDesc method]
        {
            get
            {
                _mergedProfileData.TryGetValue(method, out var profileData);
                return profileData;
            }
        }
    }
}
