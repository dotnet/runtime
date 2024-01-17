// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using AssemblyName = System.Reflection.AssemblyName;

namespace ILCompiler
{
    /// <summary>
    /// Compilation root provider that provides roots based on the RD.XML file format.
    /// Only supports a subset of the Runtime Directives configuration file format.
    /// </summary>
    /// <remarks>https://msdn.microsoft.com/en-us/library/dn600639(v=vs.110).aspx</remarks>
    internal sealed class RdXmlRootProvider : ICompilationRootProvider
    {
        private XElement _documentRoot;
        private TypeSystemContext _context;

        public RdXmlRootProvider(TypeSystemContext context, string rdXmlFileName)
        {
            _context = context;
            _documentRoot = XElement.Load(rdXmlFileName);
        }

        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            var libraryOrApplication = _documentRoot.Elements().Single();

            if (libraryOrApplication.Name.LocalName != "Library" && libraryOrApplication.Name.LocalName != "Application")
                throw new NotSupportedException($"{libraryOrApplication.Name.LocalName} is not a supported top level Runtime Directive. Supported top level Runtime Directives are \"Library\" and \"Application\".");

            if (libraryOrApplication.Attributes().Any())
                throw new NotSupportedException($"The {libraryOrApplication.Name.LocalName} Runtime Directive does not support any attributes");

            foreach (var element in libraryOrApplication.Elements())
            {
                switch (element.Name.LocalName)
                {
                    case "Assembly":
                        ProcessAssemblyDirective(rootProvider, element);
                        break;

                    default:
                        throw new NotSupportedException($"\"{element.Name.LocalName}\" is not a supported Runtime Directive.");
                }
            }
        }

        private void ProcessAssemblyDirective(IRootingServiceProvider rootProvider, XElement assemblyElement)
        {
            var assemblyNameAttribute = assemblyElement.Attribute("Name");
            if (assemblyNameAttribute == null)
                throw new Exception("The \"Name\" attribute is required on the \"Assembly\" Runtime Directive.");

            ModuleDesc assembly = _context.ResolveAssembly(new AssemblyName(assemblyNameAttribute.Value));

            rootProvider.RootModuleMetadata(assembly, "RD.XML root");

            var dynamicDegreeAttribute = assemblyElement.Attribute("Dynamic");
            if (dynamicDegreeAttribute != null)
            {
                if (dynamicDegreeAttribute.Value != "Required All")
                    throw new NotSupportedException($"\"{dynamicDegreeAttribute.Value}\" is not a supported value for the \"Dynamic\" attribute of the \"Assembly\" Runtime Directive. Supported values are \"Required All\".");

                foreach (TypeDesc type in ((EcmaModule)assembly).GetAllTypes())
                {
                    RootingHelpers.TryRootType(rootProvider, type, rootBaseTypes: true, "RD.XML root");
                }
            }

            foreach (var element in assemblyElement.Elements())
            {
                switch (element.Name.LocalName)
                {
                    case "Type":
                        ProcessTypeDirective(rootProvider, assembly, element);
                        break;
                    default:
                        throw new NotSupportedException($"\"{element.Name.LocalName}\" is not a supported Runtime Directive.");
                }
            }
        }

        private static void ProcessTypeDirective(IRootingServiceProvider rootProvider, ModuleDesc containingModule, XElement typeElement)
        {
            var typeNameAttribute = typeElement.Attribute("Name");
            if (typeNameAttribute == null)
                throw new Exception("The \"Name\" attribute is required on the \"Type\" Runtime Directive.");

            string typeName = typeNameAttribute.Value;
            TypeDesc type = containingModule.GetTypeByCustomAttributeTypeName(typeName);

            var dynamicDegreeAttribute = typeElement.Attribute("Dynamic");
            if (dynamicDegreeAttribute != null)
            {
                if (dynamicDegreeAttribute.Value != "Required All")
                    throw new NotSupportedException($"\"{dynamicDegreeAttribute.Value}\" is not a supported value for the \"Dynamic\" attribute of the \"Type\" Runtime Directive. Supported values are \"Required All\".");

                RootingHelpers.RootType(rootProvider, type, rootBaseTypes: true, "RD.XML root");
            }

            var marshalStructureDegreeAttribute = typeElement.Attribute("MarshalStructure");
            if (marshalStructureDegreeAttribute != null && type is DefType defType)
            {
                if (marshalStructureDegreeAttribute.Value != "Required All")
                    throw new NotSupportedException($"\"{marshalStructureDegreeAttribute.Value}\" is not a supported value for the \"MarshalStructure\" attribute of the \"Type\" Runtime Directive. Supported values are \"Required All\".");

                rootProvider.RootStructMarshallingData(defType, "RD.XML root");
            }

            var marshalDelegateDegreeAttribute = typeElement.Attribute("MarshalDelegate");
            if (marshalDelegateDegreeAttribute != null && type.IsDelegate)
            {
                if (marshalDelegateDegreeAttribute.Value != "Required All")
                    throw new NotSupportedException($"\"{marshalDelegateDegreeAttribute.Value}\" is not a supported value for the \"MarshalDelegate\" attribute of the \"Type\" Runtime Directive. Supported values are \"Required All\".");

                rootProvider.RootDelegateMarshallingData((DefType)type, "RD.XML root");
            }

            foreach (var element in typeElement.Elements())
            {
                switch (element.Name.LocalName)
                {
                    case "Method":
                        ProcessMethodDirective(rootProvider, containingModule, type, element);
                        break;
                    default:
                        throw new NotSupportedException($"\"{element.Name.LocalName}\" is not a supported Runtime Directive.");
                }
            }
        }

        private static void ProcessMethodDirective(IRootingServiceProvider rootProvider, ModuleDesc containingModule, TypeDesc containingType, XElement methodElement)
        {
            var methodNameAttribute = methodElement.Attribute("Name");
            if (methodNameAttribute == null)
                throw new Exception("The \"Name\" attribute is required on the \"Method\" Runtime Directive.");

            var parameter = new List<TypeDesc>();
            var instArgs = new List<TypeDesc>();
            foreach (var element in methodElement.Elements())
            {
                switch (element.Name.LocalName)
                {
                    case "Parameter":
                        string paramName = element.Attribute("Name").Value;
                        parameter.Add(containingModule.GetTypeByCustomAttributeTypeName(paramName));
                        break;
                    case "GenericArgument":
                        string instArgName = element.Attribute("Name").Value;
                        instArgs.Add(containingModule.GetTypeByCustomAttributeTypeName(instArgName));
                        break;
                    default:
                        throw new NotSupportedException($"\"{element.Name.LocalName}\" is not a supported Runtime Directive.");
                }
            }

            static bool SignatureMatches(MethodDesc method, List<TypeDesc> parameter)
            {
                if (parameter.Count != method.Signature.Length)
                    return false;

                for (int i = 0; i < method.Signature.Length; i++)
                {
                    if (method.Signature[i] != parameter[i])
                        return false;
                }

                return true;
            }

            bool rootedAnyMethod = false;
            string methodName = methodNameAttribute.Value;
            foreach (var m in containingType.GetMethods())
            {
                var method = m;
                if (method.Name != methodName)
                    continue;

                if (instArgs.Count != method.Instantiation.Length)
                    continue;

                if (instArgs.Count > 0)
                {
                    var methodInst = new Instantiation(instArgs.ToArray());
                    method = method.MakeInstantiatedMethod(methodInst);
                }

                if (parameter.Count > 0 && !SignatureMatches(method, parameter))
                    continue;

                RootingHelpers.RootMethod(rootProvider, method, "RD.XML root");
                rootedAnyMethod = true;
            }

            if (!rootedAnyMethod)
            {
                string parameterString = parameter.Count > 0 ? "(" + string.Join(", ", parameter) + ")" : null;
                string instantiationString = instArgs.Count > 0 ? "<" + string.Join(", ", instArgs) + ">" : null;
                throw new Exception($"Could not find Method(s) {containingType}.{methodName}{instantiationString}{parameterString} specified by a Runtime Directive.");
            }
        }
    }
}
