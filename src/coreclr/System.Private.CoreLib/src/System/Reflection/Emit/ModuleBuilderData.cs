// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    // This is a package private class. This class hold all of the managed
    // data member for ModuleBuilder. Note that what ever data members added to
    // this class cannot be accessed from the EE.
    internal sealed class ModuleBuilderData
    {
        public const string MultiByteValueClass = "$ArrayType$";

        public readonly TypeBuilder _globalTypeBuilder;
        public readonly string _moduleName;
        public bool _hasGlobalBeenCreated;

        internal ModuleBuilderData(ModuleBuilder module, string moduleName)
        {
            _globalTypeBuilder = new TypeBuilder(module);
            _moduleName = moduleName;
        }
    }
}
