// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class RemovedAttributeInAssembly : BaseInAssemblyAttribute
    {
        /// <summary>
        /// Asserts a CustomAttribute was kept on an assembly
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="attributeTypeName"></param>
        public RemovedAttributeInAssembly(string assemblyName, string attributeTypeName)
        {
        }

        /// <summary>
        /// Asserts a CustomAttribute was kept on an assembly
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="attributeType"></param>
        public RemovedAttributeInAssembly(string assemblyName, Type attributeType)
        {
        }

        /// <summary>
        /// Asserts a CustomAttribute was kept on a specific type
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="attributeTypeName"></param>
        /// <param name="onType"></param>
        public RemovedAttributeInAssembly(string assemblyName, string attributeTypeName, string onType)
        {
        }

        /// <summary>
        /// Asserts a CustomAttribute was kept on a specific type
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="attributeType"></param>
        /// <param name="onType"></param>
        public RemovedAttributeInAssembly(string assemblyName, Type attributeType, Type onType)
        {
        }

        /// <summary>
        /// Asserts a CustomAttribute was kept on a member in a specific type
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="attributeTypeName"></param>
        /// <param name="onType"></param>
        /// <param name="member"></param>
        public RemovedAttributeInAssembly(string assemblyName, string attributeTypeName, string onType, string member)
        {
        }

        /// <summary>
        /// Asserts a CustomAttribute was kept on a member in a specific type
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="attributeType"></param>
        /// <param name="onType"></param>
        /// <param name="member"></param>
        public RemovedAttributeInAssembly(string assemblyName, Type attributeType, Type onType, string member)
        {
        }
    }
}
