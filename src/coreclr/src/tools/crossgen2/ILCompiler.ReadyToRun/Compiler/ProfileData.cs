// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

using ILCompiler.IBC;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    [Flags]
    public enum MethodProfilingDataFlags
    {
        // Important: update toolbox\ibcmerge\ibcmerge.cs if you change these
        ReadMethodCode = 0,  // 0x00001  // Also means the method was executed
        ReadMethodDesc = 1,  // 0x00002
        RunOnceMethod = 2,  // 0x00004
        RunNeverMethod = 3,  // 0x00008
                             //  MethodStoredDataAccess        = 4,  // 0x00010  // obsolete
        WriteMethodDesc = 5,  // 0x00020
                              //  ReadFCallHash                 = 6,  // 0x00040  // obsolete
        ReadGCInfo = 7,  // 0x00080
        CommonReadGCInfo = 8,  // 0x00100
                               //  ReadMethodDefRidMap           = 9,  // 0x00200  // obsolete
        ReadCerMethodList = 10, // 0x00400
        ReadMethodPrecode = 11, // 0x00800
        WriteMethodPrecode = 12, // 0x01000
        ExcludeHotMethodCode = 13, // 0x02000  // Hot method should be excluded from the ReadyToRun image
        ExcludeColdMethodCode = 14, // 0x04000  // Cold method should be excluded from the ReadyToRun image
        DisableInlining = 15, // 0x08000  // Disable inlining of this method in optimized AOT native code
    }

    public class MethodProfileData
    {
        public MethodProfileData(MethodDesc method, MethodProfilingDataFlags flags, uint scenarioMask)
        {
            Method = method;
            Flags = flags;
            ScenarioMask = scenarioMask;
        }
        public readonly MethodDesc Method;
        public readonly MethodProfilingDataFlags Flags;
        public readonly uint ScenarioMask;
    }

    public abstract class ProfileData
    {
        public abstract bool PartialNGen { get; }
        public abstract MethodProfileData GetMethodProfileData(MethodDesc m);
        public abstract IEnumerable<MethodProfileData> GetAllMethodProfileData();
        public abstract byte[] GetMethodBlockCount(MethodDesc m);
    }

    public class EmptyProfileData : ProfileData
    {
        private static readonly EmptyProfileData s_singleton = new EmptyProfileData();

        private EmptyProfileData()
        {
        }

        public override bool PartialNGen => false;

        public static EmptyProfileData Singleton => s_singleton;

        public override MethodProfileData GetMethodProfileData(MethodDesc m)
        {
            return null;
        }

        public override IEnumerable<MethodProfileData> GetAllMethodProfileData()
        {
            return Array.Empty<MethodProfileData>();
        }

        public override byte[] GetMethodBlockCount(MethodDesc m)
        {
            return null;
        }
    }


    public class ProfileDataManager
    {
        private readonly IBCProfileParser _ibcParser;

        public ProfileDataManager(Logger logger, IEnumerable<ModuleDesc> possibleReferenceModules)
        {
            _ibcParser = new IBCProfileParser(logger, possibleReferenceModules);
        }

        private readonly Dictionary<ModuleDesc, ProfileData> _profileData = new Dictionary<ModuleDesc, ProfileData>();

        public ProfileData GetDataForModuleDesc(ModuleDesc moduleDesc)
        {
            lock (_profileData)
            {
                if (_profileData.TryGetValue(moduleDesc, out ProfileData precomputedProfileData))
                    return precomputedProfileData;
            }

            ProfileData computedProfileData = ComputeDataForModuleDesc(moduleDesc);

            lock (_profileData)
            {
                if (_profileData.TryGetValue(moduleDesc, out ProfileData precomputedProfileData))
                    return precomputedProfileData;

                _profileData.Add(moduleDesc, computedProfileData);
                return computedProfileData;
            }
        }

        private ProfileData ComputeDataForModuleDesc(ModuleDesc moduleDesc)
        {
            if (!(moduleDesc is EcmaModule ecmaModule))
                return EmptyProfileData.Singleton;

            ProfileData profileData = _ibcParser.ParseIBCDataFromModule(ecmaModule);
            if (profileData == null)
                profileData = EmptyProfileData.Singleton;

            return profileData;
        }
    }
}
