// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.Context.Delegation;

namespace System.Reflection.Context.Projection
{
    // Recursively 'projects' any assemblies, modules, types and members returned by a given assembly
    internal class ProjectingAssembly : DelegatingAssembly, IProjectable
    {
        public ProjectingAssembly(Assembly assembly, Projector projector)
            : base(assembly)
        {
            Debug.Assert(null != projector);

            Projector = projector;
        }

        public Projector Projector { get; }

        public override Module ManifestModule
        {
            get { return Projector.ProjectModule(base.ManifestModule); }
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            attributeType = Projector.Unproject(attributeType);

            return base.GetCustomAttributes(attributeType, inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return Projector.Project(base.GetCustomAttributesData(), Projector.ProjectCustomAttributeData);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            attributeType = Projector.Unproject(attributeType);

            return base.IsDefined(attributeType, inherit);
        }

        public override MethodInfo EntryPoint
        {
            get { return Projector.ProjectMethod(base.EntryPoint); }
        }

        public override Type[] GetExportedTypes()
        {
            return Projector.Project(base.GetExportedTypes(), Projector.ProjectType);
        }

        public override Module[] GetLoadedModules(bool getResourceModules)
        {
            return Projector.Project(base.GetLoadedModules(getResourceModules), Projector.ProjectModule);
        }

        public override ManifestResourceInfo GetManifestResourceInfo(string resourceName)
        {
            return Projector.ProjectManifestResource(base.GetManifestResourceInfo(resourceName));
        }

        public override Module GetModule(string name)
        {
            return Projector.ProjectModule(base.GetModule(name));
        }

        public override Module[] GetModules(bool getResourceModules)
        {
            return Projector.Project(base.GetModules(getResourceModules), Projector.ProjectModule);
        }

        public override Assembly GetSatelliteAssembly(CultureInfo culture)
        {
            return Projector.ProjectAssembly(base.GetSatelliteAssembly(culture));
        }

        public override Assembly GetSatelliteAssembly(CultureInfo culture, Version version)
        {
            return Projector.ProjectAssembly(base.GetSatelliteAssembly(culture, version));
        }

        public override Type GetType(string name, bool throwOnError, bool ignoreCase)
        {
            return Projector.ProjectType(base.GetType(name, throwOnError, ignoreCase));
        }

        public override Type[] GetTypes()
        {
            return Projector.Project(base.GetTypes(), Projector.ProjectType);
        }

        public override Module LoadModule(string moduleName, byte[] rawModule, byte[] rawSymbolStore)
        {
            return Projector.ProjectModule(base.LoadModule(moduleName, rawModule, rawSymbolStore));
        }

        public override bool Equals(object o)
        {
            var other = o as ProjectingAssembly;

            return other != null &&
                   Projector == other.Projector &&
                   UnderlyingAssembly == other.UnderlyingAssembly;
        }

        public override int GetHashCode()
        {
            return Projector.GetHashCode() ^ UnderlyingAssembly.GetHashCode();
        }
    }
}
