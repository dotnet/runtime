// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.TypeSystem;

using AssemblyName = System.Reflection.AssemblyName;
using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs.StartupCode
{
    /// <summary>
    /// Startup code that does initialization, Main invocation
    /// and shutdown of the runtime.
    /// </summary>
    public sealed partial class StartupCodeMainMethod : ILStubMethod
    {
        private TypeDesc _owningType;
        private MainMethodWrapper _mainMethod;
        private MethodSignature _signature;
        private IReadOnlyCollection<MethodDesc> _libraryInitializers;

        public StartupCodeMainMethod(TypeDesc owningType, MethodDesc mainMethod, IReadOnlyCollection<MethodDesc> libraryInitializers)
        {
            _owningType = owningType;
            _mainMethod = new MainMethodWrapper(owningType, mainMethod);
            _libraryInitializers = libraryInitializers;
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _owningType.Context;
            }
        }

        public override TypeDesc OwningType
        {
            get
            {
                return _owningType;
            }
        }

        public override string Name
        {
            get
            {
                return "StartupCodeMain";
            }
        }

        public override string DiagnosticName
        {
            get
            {
                return "StartupCodeMain";
            }
        }

        public override MethodIL EmitIL()
        {
            ILEmitter emitter = new ILEmitter();
            ILCodeStream codeStream = emitter.NewCodeStream();

            if (Context.Target.IsWindows)
                codeStream.MarkDebuggerStepThroughPoint();

            // Allow the class library to run explicitly ordered class constructors first thing in start-up.
            if (_libraryInitializers != null)
            {
                foreach (MethodDesc method in _libraryInitializers)
                {
                    codeStream.Emit(ILOpcode.call, emitter.NewToken(method));
                }
            }
            
            MetadataType startup = Context.GetOptionalHelperType("StartupCodeHelpers");

            // Initialize command line args if the class library supports this
            string initArgsName = (Context.Target.OperatingSystem == TargetOS.Windows)
                                ? "InitializeCommandLineArgsW"
                                : "InitializeCommandLineArgs";
            MethodDesc initArgs = startup?.GetMethod(initArgsName, null);
            if (initArgs != null)
            {
                codeStream.Emit(ILOpcode.ldarg_0); // argc
                codeStream.Emit(ILOpcode.ldarg_1); // argv
                codeStream.Emit(ILOpcode.call, emitter.NewToken(initArgs));
            }

            // Initialize the entrypoint assembly if the class library supports this
            MethodDesc initEntryAssembly = startup?.GetMethod("InitializeEntryAssembly", null);
            if (initEntryAssembly != null)
            {
                ModuleDesc entrypointModule = ((MetadataType)_mainMethod.WrappedMethod.OwningType).Module;
                codeStream.Emit(ILOpcode.ldtoken, emitter.NewToken(entrypointModule.GetGlobalModuleType()));
                codeStream.Emit(ILOpcode.call, emitter.NewToken(initEntryAssembly));
            }

            // Initialize COM apartment
            MethodDesc initApartmentState = startup?.GetMethod("InitializeApartmentState", null);
            if (initApartmentState != null)
            {
                if (_mainMethod.WrappedMethod.HasCustomAttribute("System", "STAThreadAttribute"))
                {
                    codeStream.EmitLdc((int)System.Threading.ApartmentState.STA);
                    codeStream.Emit(ILOpcode.call, emitter.NewToken(initApartmentState));
                }
                if (_mainMethod.WrappedMethod.HasCustomAttribute("System", "MTAThreadAttribute"))
                {
                    codeStream.EmitLdc((int)System.Threading.ApartmentState.MTA);
                    codeStream.Emit(ILOpcode.call, emitter.NewToken(initApartmentState));
                }
            }

            // Run module initializers
            MethodDesc runModuleInitializers = startup?.GetMethod("RunModuleInitializers", null);
            if (runModuleInitializers != null)
            {
                codeStream.Emit(ILOpcode.call, emitter.NewToken(runModuleInitializers));
            }

            // Call program Main
            if (_mainMethod.Signature.Length > 0)
            {
                // TODO: better exception
                if (initArgs == null)
                    throw new Exception("Main() has parameters, but the class library doesn't support them");

                codeStream.Emit(ILOpcode.call, emitter.NewToken(startup.GetKnownMethod("GetMainMethodArguments", null)));
            }

            if (Context.Target.IsWindows)
                codeStream.MarkDebuggerStepInPoint();
            codeStream.Emit(ILOpcode.call, emitter.NewToken(_mainMethod));

            MethodDesc setLatchedExitCode = startup?.GetMethod("SetLatchedExitCode", null);
            MethodDesc shutdown = startup?.GetMethod("Shutdown", null);

            // The class library either supports "advanced shutdown", or doesn't. No half-implementations allowed.
            Debug.Assert((setLatchedExitCode != null) == (shutdown != null));

            if (setLatchedExitCode != null)
            {
                // If the main method has a return value, save it
                if (!_mainMethod.Signature.ReturnType.IsVoid)
                {
                    codeStream.Emit(ILOpcode.call, emitter.NewToken(setLatchedExitCode));
                }

                // Ask the class library to shut down and return exit code.
                codeStream.Emit(ILOpcode.call, emitter.NewToken(shutdown));
            }
            else
            {
                // This is a class library that doesn't have SetLatchedExitCode/Shutdown.
                // If the main method returns void, we simply use 0 exit code.
                if (_mainMethod.Signature.ReturnType.IsVoid)
                {
                    codeStream.EmitLdc(0);
                }
            }

            codeStream.Emit(ILOpcode.ret);

            return emitter.Link(this);
        }

        public override MethodSignature Signature
        {
            get
            {
                if (_signature == null)
                {
                    _signature = new MethodSignature(MethodSignatureFlags.Static | MethodSignatureFlags.UnmanagedCallingConvention, 0,
                            Context.GetWellKnownType(WellKnownType.Int32),
                            new TypeDesc[2] {
                                Context.GetWellKnownType(WellKnownType.Int32),
                                Context.GetWellKnownType(WellKnownType.IntPtr) });
                }

                return _signature;
            }
        }

        public override bool IsUnmanagedCallersOnly
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Wraps the main method in a layer of indirection. This is necessary to protect the startup code
        /// infrastructure from situations when the owning type of the main method cannot be loaded, and codegen
        /// is instructed to generate a throwing body. Without wrapping, this behavior would result in
        /// replacing the entire startup code sequence with a throwing body, causing us to enter the "rich" managed
        /// environment without it being fully initialized. (In particular, the unhandled exception experience
        /// won't be initialized, making this difficult to diagnose.)
        /// </summary>
        private partial class MainMethodWrapper : ILStubMethod
        {
            public MainMethodWrapper(TypeDesc owningType, MethodDesc mainMethod)
            {
                WrappedMethod = mainMethod;
                OwningType = owningType;
            }

            public MethodDesc WrappedMethod
            {
                get;
            }

            public override TypeSystemContext Context
            {
                get
                {
                    return OwningType.Context;
                }
            }

            public override TypeDesc OwningType
            {
                get;
            }

            public override string Name
            {
                get
                {
                    return "MainMethodWrapper";
                }
            }

            public override string DiagnosticName
            {
                get
                {
                    return "MainMethodWrapper";
                }
            }

            public override MethodSignature Signature
            {
                get
                {
                    return WrappedMethod.Signature;
                }
            }

            public override MethodIL EmitIL()
            {
                ILEmitter emit = new ILEmitter();
                ILCodeStream codeStream = emit.NewCodeStream();

                if (Context.Target.IsWindows)
                    codeStream.MarkDebuggerStepThroughPoint();

                for (int i = 0; i < Signature.Length; i++)
                    codeStream.EmitLdArg(i);

                if (Context.Target.IsWindows)
                    codeStream.MarkDebuggerStepInPoint();

                codeStream.Emit(ILOpcode.call, emit.NewToken(WrappedMethod));

                codeStream.Emit(ILOpcode.ret);

                return emit.Link(this);
            }
        }
    }
}
