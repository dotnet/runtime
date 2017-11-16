// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    /// <summary>
    /// base class that converts dynamic properties / methods into ResourceManager.GetString(name) calls
    /// Dynamic properties are generated using the resource name as "assemblyResourceManager.ResourceName". Using the property once can get string 
    /// for this resource localized in the current thread's UI culture.
    /// Dynamic methods allow similar functionality as properties, except that they allow the user to specific different culture and/or format arguments (if resource is a format string).
    /// Methods are prefixes with "Get_". For example: Get_ResourceName(CultureInfo.Culture, arg1, arg2). CultureInfo must be the first argument, if present!
    /// 
    /// see SystemDataResouceManager implementation for usage.
    /// </summary>
    public class AssemblyResourceManager : DynamicObject
    {
        private System.Reflection.Assembly _resourceAssembly;

        public AssemblyResourceManager(Assembly assembly)
        {
            _resourceAssembly = assembly;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return new List<string>();
        }

        /// <summary>
        /// enables dynamic property: asmResourceManager.ResourceName
        /// </summary>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return TryGetResourceValue(binder.Name, null, out result);
        }

        /// <summary>
        /// enables dynamic property: asmResourceManager.ResourceName (params object[] args)
        /// This also support asmResourceManager.Get_ResourceName for old test as well
        /// </summary>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            var resourceName = binder.Name;
            if (resourceName.StartsWith("Get_"))
                resourceName = resourceName.Remove(0, 4);

            return TryGetResourceValue(resourceName, args, out result);
        }


        private bool TryGetResourceValue(string resourceName, object[] args, out object result)
        {
            if (PlatformDetection.IsNetNative)
            {
                result = string.Empty;
                return true;
            }
            else
            {
                var type = _resourceAssembly.GetType("System.SR");
                var info = type.GetProperty(resourceName, BindingFlags.NonPublic | BindingFlags.Static);

                result = null;
                if (info != null)
                {
                    result = info.GetValue(null);
                    if (args != null)
                    {
                        result = string.Format((string)result, args);
                    }
                }
                return result != null;
            }
        }
    }
}
