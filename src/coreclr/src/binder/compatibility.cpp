// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// Compatibility.cpp
//


//
// Implements the V2 Compatibility class
//
// ============================================================

#include "compatibility.hpp"
#include "assemblyname.hpp"
#include "utils.hpp"
#include "ndpversion.h"

#define ECMAKeyToken W("B77A5C561934E089")       // The ECMA key used by some framework assemblies: mscorlib, system, etc.
#define FXKeyToken W("b03f5f7f11d50a3a")         // The FX key used by other framework assemblies: System.Web, System.Drawing, etc.
#define CoreClrKeyToken W("7CEC85D7BEA7798E")    // The silverlight platform key used by CoreClr framework assemblies: mscorlib, system, etc
#define SilverlightKeyToken W("31bf3856ad364e35")

#define NETCF_PUBLIC_KEY_TOKEN_3        W("969db8053d3322ac")

#ifdef FEATURE_LEGACYNETCF
extern BOOL RuntimeIsLegacyNetCF(DWORD adid);
#endif

namespace BINDER_SPACE
{
    typedef struct
    {
        LPCWSTR pwzSimpleName;
        LPCWSTR pwzPublicKeyToken;
        LPCWSTR pwzVersion;
        // Newline
        LPCWSTR pwzNewSimpleName;
        LPCWSTR pwzNewPublicKeyToken;
        LPCWSTR pwzNewVersion;
#ifdef FEATURE_LEGACYNETCF
        BOOL    fMangoOnly;
#endif
    } RetargetConfig;

    namespace
    {
        // Hard-coded retargeting table from legacy Fusion

        static RetargetConfig arRetargetConfig[] = 
        {
            // Example entry
            // {W("System.Data.SqlServerCe"), SQL_MOBILE_PUBLIC_KEY_TOKEN, W("3.0.3600.0"),
            //  NULL, SQL_PUBLIC_KEY_TOKEN, VER_SQL_ASSEMBLYVERSION_STR_L}
            {W("Microsoft.CSharp"), SilverlightKeyToken, W("1.0.0.0-99.0.0.0"),
             NULL, FXKeyToken, VER_ASSEMBLYVERSION_STR_L
#ifdef FEATURE_LEGACYNETCF
                , FALSE
#endif
            },
            {W("System.Xml"), NETCF_PUBLIC_KEY_TOKEN_3, W("1.0.0.0-99.0.0.0"),
             NULL, CoreClrKeyToken, VER_ASSEMBLYVERSION_STR_L
#ifdef FEATURE_LEGACYNETCF
                ,TRUE
#endif
            },
            {W("System"), NETCF_PUBLIC_KEY_TOKEN_3, W("1.0.0.0-99.0.0.0"),
             NULL, CoreClrKeyToken, VER_ASSEMBLYVERSION_STR_L
#ifdef FEATURE_LEGACYNETCF
                , TRUE
#endif
            },
            {W("Microsoft.VisualBasic"), NETCF_PUBLIC_KEY_TOKEN_3, W("1.0.0.0-99.0.0.0"),
             NULL, CoreClrKeyToken, VER_ASSEMBLYVERSION_STR_L
#ifdef FEATURE_LEGACYNETCF
                , TRUE
#endif
            },
            {W("System.Core"), NETCF_PUBLIC_KEY_TOKEN_3, W("1.0.0.0-99.0.0.0"),
             NULL, CoreClrKeyToken, VER_ASSEMBLYVERSION_STR_L
#ifdef FEATURE_LEGACYNETCF
                , TRUE
#endif
            },
            {W("System.Runtime.Serialization"), NETCF_PUBLIC_KEY_TOKEN_3, W("1.0.0.0-99.0.0.0"),
             NULL, CoreClrKeyToken, VER_ASSEMBLYVERSION_STR_L
#ifdef FEATURE_LEGACYNETCF
                , TRUE
#endif
            },
            {W("System.ServiceModel"), NETCF_PUBLIC_KEY_TOKEN_3, W("1.0.0.0-99.0.0.0"),
             NULL, CoreClrKeyToken, VER_ASSEMBLYVERSION_STR_L
#ifdef FEATURE_LEGACYNETCF
                , TRUE
#endif
            },
            {W("System.ServiceModel.Web"), NETCF_PUBLIC_KEY_TOKEN_3, W("1.0.0.0-99.0.0.0"),
             NULL, CoreClrKeyToken, VER_ASSEMBLYVERSION_STR_L
#ifdef FEATURE_LEGACYNETCF
                , TRUE
#endif
            }
        };


        BOOL IsMatchingString(/* in */ SString &sValue,
                              /* in */ LPCWSTR  pwzValue)
        {
            SString value(SString::Literal, pwzValue);

            return EqualsCaseInsensitive(sValue, value);
        }

        BOOL IsMatchingVersion(/* in */ AssemblyVersion *pAssemblyVersion,
                               /* in */ LPCWSTR          pwzAssemblyVersion)
        {
            SmallStackSString assemblyVersionStr(pwzAssemblyVersion);
            assemblyVersionStr.Normalize();
            SString::CIterator pos = assemblyVersionStr.Begin();

            if (assemblyVersionStr.Find(pos, W('-')))
            {
                SmallStackSString beginVersionStr(assemblyVersionStr,
                                                  assemblyVersionStr.Begin(),
                                                  pos++);
                SmallStackSString endVersionStr(assemblyVersionStr, pos, assemblyVersionStr.End());

                BINDER_LOG_STRING(W("begin"), beginVersionStr);
                BINDER_LOG_STRING(W("end"), endVersionStr);

                AssemblyVersion beginVersion;
                AssemblyVersion endVersion;
                BOOL fIsValidBeginVersion = beginVersion.SetVersion(beginVersionStr.GetUnicode());
                BOOL fIsValidEndVersion = endVersion.SetVersion(endVersionStr.GetUnicode());
                _ASSERTE(fIsValidBeginVersion && fIsValidEndVersion);

                return (pAssemblyVersion->IsLargerOrEqual(&beginVersion) &&
                        pAssemblyVersion->IsSmallerOrEqual(&endVersion));
            }
            else
            {
                AssemblyVersion assemblyVersion;
                BOOL fIsValidVersion = assemblyVersion.SetVersion(pwzAssemblyVersion);
                _ASSERTE(fIsValidVersion);

                return pAssemblyVersion->Equals(&assemblyVersion);
            }
        }
    };


    /* static */
    HRESULT Compatibility::Retarget(AssemblyName  *pAssemblyName,
                                    AssemblyName **ppRetargetedAssemblyName,
                                    BOOL          *pfIsRetargeted)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(W("Compatibility::Retarget"));

        IF_FALSE_GO(pAssemblyName != NULL);
        IF_FALSE_GO(ppRetargetedAssemblyName != NULL);

        BINDER_LOG_ASSEMBLY_NAME(W("source"), pAssemblyName);

        if (pfIsRetargeted)
        {
            *pfIsRetargeted = FALSE;
        }
#ifdef FEATURE_CORESYSTEM
        // Apply retargeting only for strong-named culture neutral assemblies
        if (pAssemblyName->IsStronglyNamed() &&
            pAssemblyName->GetDeNormalizedCulture().IsEmpty())
        {
            ReleaseHolder<AssemblyName> pRetargetedAssemblyName;
            SString &simpleName = pAssemblyName->GetSimpleName();
            AssemblyVersion *pAssemblyVersion = pAssemblyName->GetVersion();
            SString publicKeyToken;

            TextualIdentityParser::BlobToHex(pAssemblyName->GetPublicKeyTokenBLOB(),
                                                 publicKeyToken);

            // Perform linear search for matching assembly. Legacy Fusion also does that
            for (unsigned int i = 0; i < LENGTH_OF(arRetargetConfig); i++)
            {
#ifdef FEATURE_LEGACYNETCF
                if (!RuntimeIsLegacyNetCF(0) && arRetargetConfig[i].fMangoOnly == TRUE)
                    continue;
#endif
                if (IsMatchingString(simpleName, arRetargetConfig[i].pwzSimpleName) &&
                    IsMatchingVersion(pAssemblyVersion, arRetargetConfig[i].pwzVersion) &&
                    IsMatchingString(publicKeyToken, arRetargetConfig[i].pwzPublicKeyToken))
                {
                    AssemblyVersion newAssemblyVersion;
                    IF_FALSE_GO(newAssemblyVersion.SetVersion(arRetargetConfig[i].pwzNewVersion));

                    SAFE_NEW(pRetargetedAssemblyName, AssemblyName);

                    if (arRetargetConfig[i].pwzNewSimpleName != NULL)
                    {
                        pRetargetedAssemblyName->
                            GetSimpleName().Set(arRetargetConfig[i].pwzNewSimpleName);
                    }
                    else
                    {
                        pRetargetedAssemblyName->GetSimpleName().Set(simpleName);
                    }
                    pRetargetedAssemblyName->SetVersion(&newAssemblyVersion);
                    
                    SBuffer newPublicKeyTokenBlob;
                    SmallStackSString newPublicKeyToken(arRetargetConfig[i].pwzNewPublicKeyToken);
                    TextualIdentityParser::HexToBlob(newPublicKeyToken,
                                                          FALSE /* fValidateHex */,
                                                          TRUE /* fIsToken */,
                                                          newPublicKeyTokenBlob);

                    pRetargetedAssemblyName->GetPublicKeyTokenBLOB().Set(newPublicKeyTokenBlob);

                    BINDER_LOG_ASSEMBLY_NAME(W("retargeted"), pRetargetedAssemblyName);

                    *ppRetargetedAssemblyName = pRetargetedAssemblyName.Extract();

                    if (pfIsRetargeted)
                    {
                        *pfIsRetargeted = TRUE;
                    }

                    GO_WITH_HRESULT(S_OK);
                }
            }

            // Create a clone without retargetable flag
            if (pAssemblyName->GetIsRetargetable())
            {
                IF_FAIL_GO(pAssemblyName->Clone(&pRetargetedAssemblyName));
                pRetargetedAssemblyName->SetIsRetargetable(FALSE);
                *ppRetargetedAssemblyName = pRetargetedAssemblyName.Extract();
            } else
            {
                pAssemblyName->AddRef();
                *ppRetargetedAssemblyName = pAssemblyName;
            }
        }
        else
#endif // FEATURE_CORESYSTEM
        {
            pAssemblyName->AddRef();
            *ppRetargetedAssemblyName = pAssemblyName;
        }

    Exit:
        BINDER_LOG_LEAVE_HR(W("Compatibility::Retarget"), hr);
        return hr;
    }
};
