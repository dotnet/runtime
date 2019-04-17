// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace System.Runtime.Versioning
{
    [Flags]
    internal enum SxSRequirements
    {
        None = 0,
        AppDomainID = 0x1,
        ProcessID = 0x2,
        CLRInstanceID = 0x4, // for multiple CLR's within the process
        AssemblyName = 0x8,
        TypeName = 0x10
    }

    public static partial class VersioningHelper
    {
        // These depend on the exact values given to members of the ResourceScope enum.
        private const ResourceScope ResTypeMask = ResourceScope.Machine | ResourceScope.Process | ResourceScope.AppDomain | ResourceScope.Library;
        private const ResourceScope VisibilityMask = ResourceScope.Private | ResourceScope.Assembly;

        public static string MakeVersionSafeName(string? name, ResourceScope from, ResourceScope to)
        {
            return MakeVersionSafeName(name, from, to, type: null);
        }

        public static string MakeVersionSafeName(string? name, ResourceScope from, ResourceScope to, Type? type)
        {
            ResourceScope fromResType = from & ResTypeMask;
            ResourceScope toResType = to & ResTypeMask;
            if (fromResType > toResType)
                throw new ArgumentException(SR.Format(SR.Argument_ResourceScopeWrongDirection, fromResType, toResType), nameof(from));

            SxSRequirements requires = GetRequirements(to, from);

            if ((requires & (SxSRequirements.AssemblyName | SxSRequirements.TypeName)) != 0 && type == null)
                throw new ArgumentNullException(nameof(type), SR.ArgumentNull_TypeRequiredByResourceScope);

            // Add in process ID, CLR base address, and appdomain ID's.  Also, use a character identifier
            // to ensure that these can never accidentally overlap (ie, you create enough appdomains and your
            // appdomain ID might equal your process ID).
            StringBuilder safeName = new StringBuilder(name);
            char separator = '_';
            if ((requires & SxSRequirements.ProcessID) != 0)
            {
                safeName.Append(separator);
                safeName.Append('p');
                safeName.Append(GetCurrentProcessId());
            }
            if ((requires & SxSRequirements.CLRInstanceID) != 0)
            {
                string clrID = GetCLRInstanceString();
                safeName.Append(separator);
                safeName.Append('r');
                safeName.Append(clrID);
            }
            if ((requires & SxSRequirements.AppDomainID) != 0) {
                safeName.Append(separator);
                safeName.Append("ad");
                safeName.Append(AppDomain.CurrentDomain.Id);
            }
            if ((requires & SxSRequirements.TypeName) != 0)
            {
                safeName.Append(separator);
                safeName.Append(type!.Name);
            }
            if ((requires & SxSRequirements.AssemblyName) != 0)
            {
                safeName.Append(separator);
                safeName.Append(type!.Assembly.FullName);
            }
            return safeName.ToString();
        }

        private static string GetCLRInstanceString()
        {
            // We are going to hardcode the value here to 3 (a random number) so that we don't have to 
            // actually call GetRuntimeId() which is an ecall method and cannot be 
            // directly called from outside of the corelib.
            // In CoreCLR, GetRuntimeId() gets the TLS index for the thread and adds 3 to that number.
            int id = 3;
            return id.ToString(CultureInfo.InvariantCulture);
        }

        private static SxSRequirements GetRequirements(ResourceScope consumeAsScope, ResourceScope calleeScope)
        {
            SxSRequirements requires = SxSRequirements.None;

            switch (calleeScope & ResTypeMask)
            {
                case ResourceScope.Machine:
                    switch (consumeAsScope & ResTypeMask)
                    {
                        case ResourceScope.Machine:
                            // No work
                            break;

                        case ResourceScope.Process:
                            requires |= SxSRequirements.ProcessID;
                            break;

                        case ResourceScope.AppDomain:
                            requires |= SxSRequirements.AppDomainID | SxSRequirements.CLRInstanceID | SxSRequirements.ProcessID;
                            break;

                        default:
                            throw new ArgumentException(SR.Format(SR.Argument_BadResourceScopeTypeBits, consumeAsScope), nameof(consumeAsScope));
                    }
                    break;

                case ResourceScope.Process:
                    if ((consumeAsScope & ResourceScope.AppDomain) != 0)
                        requires |= SxSRequirements.AppDomainID | SxSRequirements.CLRInstanceID;
                    break;

                case ResourceScope.AppDomain:
                    // No work
                    break;

                default:
                    throw new ArgumentException(SR.Format(SR.Argument_BadResourceScopeTypeBits, calleeScope), nameof(calleeScope));
            }

            switch (calleeScope & VisibilityMask)
            {
                case ResourceScope.None:  // Public - implied
                    switch (consumeAsScope & VisibilityMask)
                    {
                        case ResourceScope.None:  // Public - implied
                            // No work
                            break;

                        case ResourceScope.Assembly:
                            requires |= SxSRequirements.AssemblyName;
                            break;

                        case ResourceScope.Private:
                            requires |= SxSRequirements.TypeName | SxSRequirements.AssemblyName;
                            break;

                        default:
                            throw new ArgumentException(SR.Format(SR.Argument_BadResourceScopeVisibilityBits, consumeAsScope), nameof(consumeAsScope));
                    }
                    break;

                case ResourceScope.Assembly:
                    if ((consumeAsScope & ResourceScope.Private) != 0)
                        requires |= SxSRequirements.TypeName;
                    break;

                case ResourceScope.Private:
                    // No work
                    break;

                default:
                    throw new ArgumentException(SR.Format(SR.Argument_BadResourceScopeVisibilityBits, calleeScope), nameof(calleeScope));
            }

            if (consumeAsScope == calleeScope)
            {
                Debug.Assert(requires == SxSRequirements.None, "Computed a strange set of required resource scoping.  It's probably wrong.");
            }

            return requires;
        }
    }
}
