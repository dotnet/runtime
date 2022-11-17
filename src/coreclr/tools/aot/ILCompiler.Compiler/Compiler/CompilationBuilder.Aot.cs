// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILCompiler
{
    public partial class CompilationBuilder
    {
        private PreinitializationManager _preinitializationManager;

        // These need to provide reasonable defaults so that the user can optionally skip
        // calling the Use/Configure methods and still get something reasonable back.
        protected MetadataManager _metadataManager;
        protected InteropStubManager _interopStubManager = new EmptyInteropStubManager();
        protected VTableSliceProvider _vtableSliceProvider = new LazyVTableSliceProvider();
        protected DictionaryLayoutProvider _dictionaryLayoutProvider = new LazyDictionaryLayoutProvider();
        protected DebugInformationProvider _debugInformationProvider = new DebugInformationProvider();
        protected DevirtualizationManager _devirtualizationManager = new DevirtualizationManager();
        protected MethodImportationErrorProvider _methodImportationErrorProvider = new MethodImportationErrorProvider();
        protected IInliningPolicy _inliningPolicy;
        protected bool _methodBodyFolding;
        protected InstructionSetSupport _instructionSetSupport;
        protected SecurityMitigationOptions _mitigationOptions;
        protected bool _useDwarf5;

        partial void InitializePartial()
        {
            _metadataManager = new AnalysisBasedMetadataManager(_context);
            _instructionSetSupport = new InstructionSetSupport(default, default, _context.Target.Architecture);
        }

        public CompilationBuilder UseInstructionSetSupport(InstructionSetSupport support)
        {
            _instructionSetSupport = support;
            return this;
        }

        public CompilationBuilder UseInliningPolicy(IInliningPolicy policy)
        {
            _inliningPolicy = policy;
            return this;
        }

        public CompilationBuilder UseMetadataManager(MetadataManager metadataManager)
        {
            _metadataManager = metadataManager;
            return this;
        }

        public CompilationBuilder UseInteropStubManager(InteropStubManager interopStubManager)
        {
            _interopStubManager = interopStubManager;
            return this;
        }

        public CompilationBuilder UseVTableSliceProvider(VTableSliceProvider provider)
        {
            _vtableSliceProvider = provider;
            return this;
        }

        public CompilationBuilder UseGenericDictionaryLayoutProvider(DictionaryLayoutProvider provider)
        {
            _dictionaryLayoutProvider = provider;
            return this;
        }

        public CompilationBuilder UseDevirtualizationManager(DevirtualizationManager manager)
        {
            _devirtualizationManager = manager;
            return this;
        }

        public CompilationBuilder UseDebugInfoProvider(DebugInformationProvider provider)
        {
            _debugInformationProvider = provider;
            return this;
        }

        public CompilationBuilder UseSecurityMitigationOptions(SecurityMitigationOptions options)
        {
            _mitigationOptions = options;
            return this;
        }

        public CompilationBuilder UseMethodBodyFolding(bool enable)
        {
            _methodBodyFolding = enable;
            return this;
        }

        public CompilationBuilder UsePreinitializationManager(PreinitializationManager manager)
        {
            _preinitializationManager = manager;
            return this;
        }

        public CompilationBuilder UseMethodImportationErrorProvider(MethodImportationErrorProvider errorProvider)
        {
            _methodImportationErrorProvider = errorProvider;
            return this;
        }

        public CompilationBuilder UseDwarf5(bool value)
        {
            _useDwarf5 = value;
            return this;
        }

        protected PreinitializationManager GetPreinitializationManager()
        {
            if (_preinitializationManager == null)
                return new PreinitializationManager(_context, _compilationGroup, GetILProvider(), enableInterpreter: false);
            return _preinitializationManager;
        }

        public ILScannerBuilder GetILScannerBuilder(CompilationModuleGroup compilationGroup = null)
        {
            return new ILScannerBuilder(_context, compilationGroup ?? _compilationGroup, _nameMangler, GetILProvider(), GetPreinitializationManager());
        }
    }

    [Flags]
    public enum SecurityMitigationOptions
    {
        ControlFlowGuardAnnotations = 0x0001,
    }
}
