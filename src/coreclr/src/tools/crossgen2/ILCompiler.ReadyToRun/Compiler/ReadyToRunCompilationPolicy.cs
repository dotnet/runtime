// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;

using Internal.TypeSystem;

[Flags]
public enum ReadyToRunCompilationPolicyFlags
{
    NoMethods = 0x1,
    OnlyProfileSpecifiedMethods = 0x2,
    IgnoreProfileData = 0x4,
    RootNonGenericMethods = 0x08,
    RootGenericCanonInstantiations = 0x10,
    AllowGenericCanonInstantiations = 0x20,
    AllowLocalGenericInstantiations = 0x40,
    AllowPrimitiveGenericInstantiations = 0x80,
    AllowAllGenericInstantiations = 0x100,
    CompileVirtualMethodsOnReferencedTypes = 0x200,
    CompileNonVirtualMethodsOnReferencedTypes = 0x400,
}

public class ReadyToRunCompilationSpecificPolicy
{
    public ReadyToRunCompilationSpecificPolicy Clone()
    {
        ReadyToRunCompilationSpecificPolicy clone = new ReadyToRunCompilationSpecificPolicy();
        clone.Flags = Flags;
        clone.MaxGenericDepth = MaxGenericDepth;
        clone.MaxGenericDepthReferencedTypeExpansion = MaxGenericDepthReferencedTypeExpansion;
        return clone;
    }

    public ReadyToRunCompilationPolicyFlags Flags;
    public int MaxGenericDepth = 10;
    public int MaxGenericDepthReferencedTypeExpansion = 10;
}

public class ReadyToRunCompilationPolicy
{
    public ReadyToRunCompilationPolicy(TypeSystemContext context, string policySpec)
    {
        string[] topLevelGroups = policySpec.Split(";");
        foreach (string topLevelUntrimmed in topLevelGroups)
        {
            string topLevel = topLevelUntrimmed.Trim();
            if (String.IsNullOrEmpty(topLevel))
                continue;

            ReadyToRunCompilationSpecificPolicy policyBeingAdjusted = Global;
            int equalsIndex = topLevel.IndexOf('=');
            if (equalsIndex != -1)
            {
                string moduleName = topLevel.Substring(0, equalsIndex);
                topLevel = topLevel.Substring(equalsIndex + 1);
                var module = context.ResolveAssembly(new AssemblyName(moduleName));
                if (!_policies.TryGetValue(module, out policyBeingAdjusted))
                {
                    policyBeingAdjusted = Global.Clone(); 
                    _policies.Add(module, policyBeingAdjusted);
                }
            }

            foreach (string policyUntrimmed in topLevel.Split(','))
            {
                string policy = policyUntrimmed.Trim();
                if (String.IsNullOrEmpty(policy))
                    continue;
                
                switch (policy)
                {
                    case "+NoMethods":
                    case "NoMethods":
                        policyBeingAdjusted.Flags |= ReadyToRunCompilationPolicyFlags.NoMethods;
                        break;
                    case "-NoMethods":
                        policyBeingAdjusted.Flags &= ~ReadyToRunCompilationPolicyFlags.NoMethods;
                        break;

                    case "+OnlyProfileSpecifiedMethods":
                    case "OnlyProfileSpecifiedMethods":
                        policyBeingAdjusted.Flags |= ReadyToRunCompilationPolicyFlags.OnlyProfileSpecifiedMethods;
                        break;
                    case "-OnlyProfileSpecifiedMethods":
                        policyBeingAdjusted.Flags &= ~ReadyToRunCompilationPolicyFlags.OnlyProfileSpecifiedMethods;
                        break;

                    case "+IgnoreProfileData":
                    case "IgnoreProfileData":
                        policyBeingAdjusted.Flags |= ReadyToRunCompilationPolicyFlags.IgnoreProfileData;
                        break;
                    case "-IgnoreProfileData":
                        policyBeingAdjusted.Flags &= ~ReadyToRunCompilationPolicyFlags.IgnoreProfileData;
                        break;

                    case "+RootNonGenericMethods":
                    case "RootNonGenericMethods":
                        policyBeingAdjusted.Flags |= ReadyToRunCompilationPolicyFlags.RootNonGenericMethods;
                        break;
                    case "-RootNonGenericMethods":
                        policyBeingAdjusted.Flags &= ~ReadyToRunCompilationPolicyFlags.RootNonGenericMethods;
                        break;

                    case "+RootGenericCanonInstantiations":
                    case "RootGenericCanonInstantiations":
                        policyBeingAdjusted.Flags |= ReadyToRunCompilationPolicyFlags.RootGenericCanonInstantiations;
                        break;
                    case "-RootGenericCanonInstantiations":
                        policyBeingAdjusted.Flags &= ~ReadyToRunCompilationPolicyFlags.RootGenericCanonInstantiations;
                        break;

                    case "+AllowGenericCanonInstantiations":
                    case "AllowGenericCanonInstantiations":
                        policyBeingAdjusted.Flags |= ReadyToRunCompilationPolicyFlags.AllowGenericCanonInstantiations;
                        break;
                    case "-AllowGenericCanonInstantiations":
                        policyBeingAdjusted.Flags &= ~ReadyToRunCompilationPolicyFlags.AllowGenericCanonInstantiations;
                        break;

                    case "+AllowLocalGenericInstantiations":
                    case "AllowLocalGenericInstantiations":
                        policyBeingAdjusted.Flags |= ReadyToRunCompilationPolicyFlags.AllowLocalGenericInstantiations;
                        break;
                    case "-AllowLocalGenericInstantiations":
                        policyBeingAdjusted.Flags &= ~ReadyToRunCompilationPolicyFlags.AllowLocalGenericInstantiations;
                        break;

                    case "+AllowPrimitiveGenericInstantiations":
                    case "AllowPrimitiveGenericInstantiations":
                        policyBeingAdjusted.Flags |= ReadyToRunCompilationPolicyFlags.AllowPrimitiveGenericInstantiations;
                        break;
                    case "-AllowPrimitiveGenericInstantiations":
                        policyBeingAdjusted.Flags &= ~ReadyToRunCompilationPolicyFlags.AllowPrimitiveGenericInstantiations;
                        break;

                    case "+AllowAllGenericInstantiations":
                    case "AllowAllGenericInstantiations":
                        policyBeingAdjusted.Flags |= ReadyToRunCompilationPolicyFlags.AllowAllGenericInstantiations;
                        break;
                    case "-AllowAllGenericInstantiations":
                        policyBeingAdjusted.Flags &= ~ReadyToRunCompilationPolicyFlags.AllowAllGenericInstantiations;
                        break;

                    case "+CompileVirtualMethodsOnReferencedTypes":
                    case "CompileVirtualMethodsOnReferencedTypes":
                        policyBeingAdjusted.Flags |= ReadyToRunCompilationPolicyFlags.CompileVirtualMethodsOnReferencedTypes;
                        break;
                    case "-CompileVirtualMethodsOnReferencedTypes":
                        policyBeingAdjusted.Flags &= ~ReadyToRunCompilationPolicyFlags.CompileVirtualMethodsOnReferencedTypes;
                        break;

                    case "+CompileNonVirtualMethodsOnReferencedTypes":
                    case "CompileNonVirtualMethodsOnReferencedTypes":
                        policyBeingAdjusted.Flags |= ReadyToRunCompilationPolicyFlags.CompileNonVirtualMethodsOnReferencedTypes;
                        break;
                    case "-CompileNonVirtualMethodsOnReferencedTypes":
                        policyBeingAdjusted.Flags &= ~ReadyToRunCompilationPolicyFlags.CompileNonVirtualMethodsOnReferencedTypes;
                        break;

                    default:
                    {
                        var indexOfColon = policy.IndexOf(':');
                        bool foundPolicy = false;
                        if (indexOfColon != -1)
                        {
                            string numericPolicy = policy.Substring(0,indexOfColon);
                            if (Int32.TryParse(policy.Substring(indexOfColon + 1).Trim(), out int numericValue))
                            {
                                foundPolicy = true;
                                switch (numericPolicy)
                                {
                                    case "MaxGenericDepth":
                                        policyBeingAdjusted.MaxGenericDepth = numericValue;
                                        break;
                                    case "MaxGenericDepthReferencedTypeExpansion":
                                        policyBeingAdjusted.MaxGenericDepthReferencedTypeExpansion = numericValue;
                                        break;

                                    default:
                                        foundPolicy = false;
                                        break;
                                }
                            }
                        }

                        if (!foundPolicy)
                        {
                            throw new ArgumentException(policy);
                        }
                        break;
                    }
                }
            }
        }
    }

    private Dictionary<ModuleDesc, ReadyToRunCompilationSpecificPolicy> _policies = new Dictionary<ModuleDesc, ReadyToRunCompilationSpecificPolicy>();

    public readonly ReadyToRunCompilationSpecificPolicy Global = new ReadyToRunCompilationSpecificPolicy();

    public ReadyToRunCompilationSpecificPolicy For(MethodDesc method)
    {
        return For(method.OwningType);
    }

    public ReadyToRunCompilationSpecificPolicy For(TypeDesc type)
    {
        if (type is MetadataType typeWithModule)
        {
            return For(typeWithModule.Module);
        }
        else
        {
            return Global;
        }
    }

    public ReadyToRunCompilationSpecificPolicy For(ModuleDesc module)
    {
        if (_policies.TryGetValue(module, out var policy))
        {
            return policy;
        }
        return Global;
    }
}
