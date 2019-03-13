// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Configuration;
using System.Configuration.Internal;
using System.Reflection;

public class TempConfigurationHost : DelegatingConfigHost 
{
    private static string s_assemblyName = PlatformDetection.IsFullFramework ? "System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" : "System.Configuration.ConfigurationManager";
    private static IInternalConfigConfigurationFactory s_configurationFactory;
    
    private ConfigurationFileMap _fileMap;

    public TempConfigurationHost() 
    {
        Type type = Type.GetType(InternalHostTypeName, true);
        Host = (IInternalConfigHost) Activator.CreateInstance(type, true);
    }

    public override void Init(IInternalConfigRoot configRoot, params object[] hostInitParams) 
    {
        Host.Init(configRoot, hostInitParams);
    }

    public override void InitForConfiguration(ref string locationSubPath, out string configPath, out string locationConfigPath,
                        IInternalConfigRoot configRoot, params object[] hostInitConfigurationParams) 
    {

        Host.Init(configRoot, hostInitConfigurationParams);

        _fileMap = hostInitConfigurationParams[1] as ConfigurationFileMap;
        
        locationSubPath = ConfigurationFactory.NormalizeLocationSubPath(locationSubPath, null);
        configPath = "MACHINE/EXE";
        locationConfigPath = locationSubPath;
    }

    public override bool IsTrustedConfigPath(string configPath) 
    {
        return true;
    }

    public override bool IsLocationApplicable(string configPath) 
    {
        return true;
    }

    public override bool IsFullTrustSectionWithoutAptcaAllowed(IInternalConfigRecord configRecord) 
    {
        return true;
    }

    public override bool PrefetchAll(string configPath, string streamName)
    {
        return false;
    }

    public override bool PrefetchSection(string sectionGroupName, string sectionName)
    {
        return false;
    }

    public override string GetStreamName(string configPath) 
    {
        return _fileMap.MachineConfigFilename;
    }

    static string InternalConfigConfigurationFactoryTypeName 
    {
        get { return "System.Configuration.Internal.InternalConfigConfigurationFactory, " + s_assemblyName; }
    }

    static string InternalHostTypeName 
    {
        get { return "System.Configuration.Internal.InternalConfigHost, " + s_assemblyName; }
    }

    static internal IInternalConfigConfigurationFactory ConfigurationFactory 
    {
        get 
        {    
            if (s_configurationFactory == null) 
            {
                Type type = Type.GetType(InternalConfigConfigurationFactoryTypeName, true);
                s_configurationFactory = (IInternalConfigConfigurationFactory) Activator.CreateInstance(type, true);
            }

            return s_configurationFactory;
        }
    }
}