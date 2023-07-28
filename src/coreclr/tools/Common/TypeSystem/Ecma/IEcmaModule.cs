// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

using Internal.TypeSystem.Ecma;

namespace Internal.IL
{
    // Marker interface implemented by EcmaMethodIL and EcmaMethodILScope
    // and other providers of IL that comes ultimately from Ecma IL.
    public interface IEcmaMethodIL
    {
        public IEcmaModule Module { get; }
    }

    public sealed partial class EcmaMethodIL : IEcmaMethodIL
    {
        IEcmaModule IEcmaMethodIL.Module
        {
            get
            {
                return _module;
            }
        }
    }

    public sealed partial class EcmaMethodILScope : IEcmaMethodIL
    {
        IEcmaModule IEcmaMethodIL.Module
        {
            get
            {
                return _module;
            }
        }
    }
}

namespace Internal.TypeSystem.Ecma
{
    public interface IEcmaModule
    {
        MetadataReader MetadataReader { get; }
        TypeDesc GetType(EntityHandle handle);
        object GetObject(EntityHandle handle, NotFoundBehavior notFoundBehavior = NotFoundBehavior.Throw);
        int CompareTo(IEcmaModule other);
        int ModuleTypeSort { get; }

        IAssemblyDesc Assembly { get; }
    }

    public partial class EcmaModule : IEcmaModule
    {
        public int CompareTo(IEcmaModule other)
        {
            if (this == other)
                return 0;

            if (other is EcmaModule emoduleOther)
            {
                return CompareTo(emoduleOther);
            }

            return ModuleTypeSort.CompareTo(other.ModuleTypeSort);
        }

        public int ModuleTypeSort => 0;
    }
}
