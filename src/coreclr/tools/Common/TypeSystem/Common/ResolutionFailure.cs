// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public sealed class ResolutionFailure
    {
        private enum FailureType
        {
            TypeLoadException1,
            TypeLoadException2,
            TypeLoadException3,
            MissingMethodException1,
            MissingFieldException1,
            MissingAssemblyException1,
        }

        private ResolutionFailure() { }

        private FailureType _failureType;
        private string _namespace;
        private string _name;
        private string _moduleName;
        private ModuleDesc _module;
        private TypeDesc _owningType;
        private MethodSignature _methodSignature;


        public static ResolutionFailure GetTypeLoadResolutionFailure(string nestedTypeName, ModuleDesc module)
        {
            ResolutionFailure failure = new ResolutionFailure();
            failure._failureType = FailureType.TypeLoadException1;
            failure._name = nestedTypeName;
            failure._module = module;
            return failure;
        }

        public static ResolutionFailure GetTypeLoadResolutionFailure(string @namespace, string name, ModuleDesc module)
        {
            ResolutionFailure failure = new ResolutionFailure();
            failure._failureType = FailureType.TypeLoadException2;
            failure._namespace = @namespace;
            failure._name = name;
            failure._module = module;
            return failure;
        }

        public static ResolutionFailure GetTypeLoadResolutionFailure(string @namespace, string name, string moduleName)
        {
            ResolutionFailure failure = new ResolutionFailure();
            failure._failureType = FailureType.TypeLoadException3;
            failure._namespace = @namespace;
            failure._name = name;
            failure._moduleName = moduleName;
            return failure;
        }

        public static ResolutionFailure GetMissingMethodFailure(TypeDesc owningType, string methodName, MethodSignature signature)
        {
            ResolutionFailure failure = new ResolutionFailure();
            failure._failureType = FailureType.MissingMethodException1;
            failure._methodSignature = signature;
            failure._name = methodName;
            failure._owningType = owningType;
            return failure;
        }

        public static ResolutionFailure GetMissingFieldFailure(TypeDesc owningType, string fieldName)
        {
            ResolutionFailure failure = new ResolutionFailure();
            failure._failureType = FailureType.MissingMethodException1;
            failure._name = fieldName;
            failure._owningType = owningType;
            return failure;
        }

        public static ResolutionFailure GetAssemblyResolutionFailure(string simpleName)
        {
            ResolutionFailure failure = new ResolutionFailure();
            failure._failureType = FailureType.MissingAssemblyException1;
            failure._name = simpleName;
            return failure;
        }

        public void Throw()
        {
            switch (_failureType)
            {
                case FailureType.TypeLoadException1:
                    ThrowHelper.ThrowTypeLoadException(_name, _module);
                    break;
                case FailureType.TypeLoadException2:
                    ThrowHelper.ThrowTypeLoadException(_namespace, _name, _module);
                    break;
                case FailureType.TypeLoadException3:
                    ThrowHelper.ThrowTypeLoadException(_namespace, _name, _moduleName);
                    break;
                case FailureType.MissingMethodException1:
                    ThrowHelper.ThrowMissingMethodException(_owningType, _name, _methodSignature);
                    break;
                case FailureType.MissingFieldException1:
                    ThrowHelper.ThrowMissingFieldException(_owningType, _name);
                    break;
                case FailureType.MissingAssemblyException1:
                    ThrowHelper.ThrowFileNotFoundException(ExceptionStringID.FileLoadErrorGeneric, _name);
                    break;
            }
        }
    }
}
