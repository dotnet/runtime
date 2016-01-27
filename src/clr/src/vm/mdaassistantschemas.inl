// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


    //
    // Assistants
    //
    
//    ************************************************
//        PLEASE KEEP MDAS IN ALPHABETICAL ORDER (starting after AsynchronousThreadAbort)
//    ************************************************


    // Framework
    MDA_DEFINE_ASSISTANT(Framework, NULL)
        // Input
        MDA_DEFINE_INPUT(Framework)
#ifdef _DEBUG
        MDA_XSD_OPTIONAL()
            MDA_XSD_ELEMENT(Diagnostics)
                MDA_XSD_ATTRIBUTE_DEFAULT(DumpAssistantMsgSchema, BOOL, W("false"))
                MDA_XSD_ATTRIBUTE_DEFAULT(DumpAssistantSchema, BOOL, W("false"))
                MDA_XSD_ATTRIBUTE_DEFAULT(DumpSchemaSchema, BOOL, W("false"))
            MDA_XSD_ELEMENT_END(Diagnostics)           
        MDA_XSD_OPTIONAL_END()
        MDA_XSD_ATTRIBUTE_DEFAULT(DisableAsserts, BOOL, FALSE)
#endif 
        MDA_DEFINE_INPUT_END(Framework)
        // Output
        MDA_DEFINE_OUTPUT(Framework)
        MDA_DEFINE_OUTPUT_END(Framework)         
    MDA_DEFINE_ASSISTANT_END(Framework)   
        
    // AsynchronousThreadAbort
    MDA_DEFINE_ASSISTANT(AsynchronousThreadAbort, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(AsynchronousThreadAbort)
        // Output
        MDA_DEFINE_OUTPUT(AsynchronousThreadAbort)
            MDA_XSD_ONCE()
                MDA_XSD_ELEMENT_REFTYPE(CallingThread, ThreadType)
                MDA_XSD_ELEMENT_REFTYPE(AbortedThread, ThreadType)
            MDA_XSD_ONCE_END()        
        MDA_DEFINE_OUTPUT_END(AsynchronousThreadAbort)         
    MDA_DEFINE_ASSISTANT_END(AsynchronousThreadAbort)

    // BindingFailure
    MDA_DEFINE_ASSISTANT(BindingFailure, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(BindingFailure)
        // Output
        MDA_DEFINE_OUTPUT(BindingFailure)
            MDA_XSD_ONCE()
                MDA_XSD_ELEMENT(AssemblyInfo)
                    MDA_XSD_ATTRIBUTE_REQ(AppDomainId, INT32)
                    MDA_XSD_ATTRIBUTE_REQ(DisplayName, SString)
                    MDA_XSD_ATTRIBUTE_REQ(CodeBase, SString)
                    MDA_XSD_ATTRIBUTE_REQ(HResult, INT32)
                    MDA_XSD_ATTRIBUTE_REQ(BindingContextId, INT32)
                MDA_XSD_ELEMENT_END(AssemblyInfo)
            MDA_XSD_ONCE_END()
        MDA_DEFINE_OUTPUT_END(BindingFailure)         
    MDA_DEFINE_ASSISTANT_END(BindingFailure)

    // CallbackOnCollectedDelegate
    MDA_DEFINE_ASSISTANT(CallbackOnCollectedDelegate, NULL)
        // Input
        MDA_DEFINE_INPUT(CallbackOnCollectedDelegate)
            MDA_XSD_ATTRIBUTE_DEFAULT(ListSize, INT32, W("1000"))
        MDA_DEFINE_INPUT_END(CallbackOnCollectedDelegate)
        // Output
        MDA_DEFINE_OUTPUT(CallbackOnCollectedDelegate)
            MDA_XSD_ONCE()
                MDA_XSD_ELEMENT_REFTYPE(Delegate, MethodType)
            MDA_XSD_ONCE_END()
        MDA_DEFINE_OUTPUT_END(CallbackOnCollectedDelegate)
    MDA_DEFINE_ASSISTANT_END(CallbackOnCollectedDelegate)

    // ContextSwitchDeadlock
    MDA_DEFINE_ASSISTANT(ContextSwitchDeadlock, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(ContextSwitchDeadlock)
        // Output
        MDA_DEFINE_OUTPUT(ContextSwitchDeadlock)
        MDA_DEFINE_OUTPUT_END(ContextSwitchDeadlock)      
    MDA_DEFINE_ASSISTANT_END(ContextSwitchDeadlock)

    // DangerousThreadingAPI
    MDA_DEFINE_ASSISTANT(DangerousThreadingAPI, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(DangerousThreadingAPI)
        // Output
        MDA_DEFINE_OUTPUT(DangerousThreadingAPI)
        MDA_DEFINE_OUTPUT_END(DangerousThreadingAPI)         
    MDA_DEFINE_ASSISTANT_END(DangerousThreadingAPI)
    
    // DateTimeInvalidLocalFormat
    MDA_DEFINE_ASSISTANT(DateTimeInvalidLocalFormat, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(DateTimeInvalidLocalFormat)
        // Output
        MDA_DEFINE_OUTPUT(DateTimeInvalidLocalFormat)
        MDA_DEFINE_OUTPUT_END(DateTimeInvalidLocalFormat)
    MDA_DEFINE_ASSISTANT_END(DateTimeInvalidLocalFormat)   

    // DirtyCastAndCallOnInterface
    MDA_DEFINE_ASSISTANT(DirtyCastAndCallOnInterface, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(DirtyCastAndCallOnInterface)
        // Output
        MDA_DEFINE_OUTPUT(DirtyCastAndCallOnInterface)
        MDA_DEFINE_OUTPUT_END(DirtyCastAndCallOnInterface)      
    MDA_DEFINE_ASSISTANT_END(DirtyCastAndCallOnInterface)

    // DisconnectedContext
    MDA_DEFINE_ASSISTANT(DisconnectedContext, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(DisconnectedContext)
        // Output
        MDA_DEFINE_OUTPUT(DisconnectedContext)
        MDA_DEFINE_OUTPUT_END(DisconnectedContext)      
    MDA_DEFINE_ASSISTANT_END(DisconnectedContext)

    // DllMainReturnsFalse
    MDA_DEFINE_ASSISTANT(DllMainReturnsFalse, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(DllMainReturnsFalse)
        // Output
        MDA_DEFINE_OUTPUT(DllMainReturnsFalse)
        MDA_DEFINE_OUTPUT_END(DllMainReturnsFalse)
    MDA_DEFINE_ASSISTANT_END(DllMainReturnsFalse)   

    // ExceptionSwallowedOnCallFromCom
    MDA_DEFINE_ASSISTANT(ExceptionSwallowedOnCallFromCom, NULL)       
        // Input
        MDA_DEFINE_INPUT(ExceptionSwallowedOnCallFromCom)
        MDA_DEFINE_INPUT_END(ExceptionSwallowedOnCallFromCom)
        // Output
        MDA_DEFINE_OUTPUT(ExceptionSwallowedOnCallFromCom)
            MDA_XSD_ONCE()
                MDA_XSD_ELEMENT__REFTYPE(Method, MethodType)
                MDA_XSD_ELEMENT__REFTYPE(Type, TypeType)
                MDA_XSD_ELEMENT_REFTYPE(Exception, ExceptionType)
            MDA_XSD_ONCE_END()
        MDA_DEFINE_OUTPUT_END(ExceptionSwallowedOnCallFromCom)
    MDA_DEFINE_ASSISTANT_END(ExceptionSwallowedOnCallFromCom)

    // FailedQI
    MDA_DEFINE_ASSISTANT(FailedQI, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(FailedQI)
        // Output
        MDA_DEFINE_OUTPUT(FailedQI)
            MDA_XSD_ONCE()
                MDA_XSD_ELEMENT__REFTYPE(Type, TypeType)
            MDA_XSD_ONCE_END()       
        MDA_DEFINE_OUTPUT_END(FailedQI)      
    MDA_DEFINE_ASSISTANT_END(FailedQI)
    
    // FatalExecutionEngineError
    MDA_DEFINE_ASSISTANT(FatalExecutionEngineError, NULL)   
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(FatalExecutionEngineError)
        // Output
        MDA_DEFINE_OUTPUT(FatalExecutionEngineError)
        MDA_DEFINE_OUTPUT_END(FatalExecutionEngineError)         
    MDA_DEFINE_ASSISTANT_END(FatalExecutionEngineError)      

    // GcManagedToUnmanaged
    MDA_DEFINE_ASSISTANT(GcManagedToUnmanaged, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(GcManagedToUnmanaged)
    MDA_DEFINE_ASSISTANT_END(GcManagedToUnmanaged)

    // GcUnmanagedToManaged
    MDA_DEFINE_ASSISTANT(GcUnmanagedToManaged, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(GcUnmanagedToManaged)
    MDA_DEFINE_ASSISTANT_END(GcUnmanagedToManaged)

    // IllegalPrepareConstrainedRegion
    MDA_DEFINE_ASSISTANT(IllegalPrepareConstrainedRegion, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(IllegalPrepareConstrainedRegion)
        // Output
        MDA_DEFINE_OUTPUT(IllegalPrepareConstrainedRegion)
            MDA_XSD_ONCE()
                MDA_XSD_ELEMENT__REFTYPE(Callsite, MethodAndOffsetType)
            MDA_XSD_ONCE_END()
        MDA_DEFINE_OUTPUT_END(IllegalPrepareConstrainedRegion)         
    MDA_DEFINE_ASSISTANT_END(IllegalPrepareConstrainedRegion)

    // InvalidApartmentStateChange
    MDA_DEFINE_ASSISTANT(InvalidApartmentStateChange, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(InvalidApartmentStateChange)
        // Output
        MDA_DEFINE_OUTPUT(InvalidApartmentStateChange)
            MDA_XSD_ONCE()
                MDA_XSD_ELEMENT_REFTYPE(Thread, ThreadType)
            MDA_XSD_ONCE_END()
        MDA_DEFINE_OUTPUT_END(InvalidApartmentStateChange)
    MDA_DEFINE_ASSISTANT_END(InvalidApartmentStateChange)

    // InvalidCERCall
    MDA_DEFINE_ASSISTANT(InvalidCERCall, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(InvalidCERCall)
        // Output
        MDA_DEFINE_OUTPUT(InvalidCERCall)
            MDA_XSD_ONCE()
                MDA_XSD_ELEMENT__REFTYPE(Method, MethodType)
                MDA_XSD_ELEMENT_REFTYPE(Callsite, MethodAndOffsetType)
            MDA_XSD_ONCE_END()
        MDA_DEFINE_OUTPUT_END(InvalidCERCall)         
    MDA_DEFINE_ASSISTANT_END(InvalidCERCall)

    // InvalidFunctionPointerInDelegate
    MDA_DEFINE_ASSISTANT(InvalidFunctionPointerInDelegate, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(InvalidFunctionPointerInDelegate)
        // Output
        MDA_DEFINE_OUTPUT(InvalidFunctionPointerInDelegate)
        MDA_DEFINE_OUTPUT_END(InvalidFunctionPointerInDelegate)      
    MDA_DEFINE_ASSISTANT_END(InvalidFunctionPointerInDelegate)

    // InvalidGCHandleCookie
    MDA_DEFINE_ASSISTANT(InvalidGCHandleCookie, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(InvalidGCHandleCookie)
        // Output
        MDA_DEFINE_OUTPUT(InvalidGCHandleCookie)
        MDA_DEFINE_OUTPUT_END(InvalidGCHandleCookie)         
    MDA_DEFINE_ASSISTANT_END(InvalidGCHandleCookie)

    // InvalidIUnknown
    MDA_DEFINE_ASSISTANT(InvalidIUnknown, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(InvalidIUnknown)
        // Output
        MDA_DEFINE_OUTPUT(InvalidIUnknown)
        MDA_DEFINE_OUTPUT_END(InvalidIUnknown)      
    MDA_DEFINE_ASSISTANT_END(InvalidIUnknown)

    // InvalidMemberDeclaration
    MDA_DEFINE_ASSISTANT(InvalidMemberDeclaration, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(InvalidMemberDeclaration)
        // Output
        MDA_DEFINE_OUTPUT(InvalidMemberDeclaration)
            MDA_XSD_ONCE()
                MDA_XSD_CHOICE()
                    MDA_XSD_ELEMENT__REFTYPE(Method, MethodType)
                    MDA_XSD_ELEMENT_REFTYPE(Field, FieldType)
                MDA_XSD_CHOICE_END()            
                MDA_XSD_ELEMENT_REFTYPE(Type, TypeType)
                MDA_XSD_ELEMENT__REFTYPE(Exception, ExceptionType)
            MDA_XSD_ONCE_END()       
        MDA_DEFINE_OUTPUT_END(InvalidMemberDeclaration)
    MDA_DEFINE_ASSISTANT_END(InvalidMemberDeclaration)

    // InvalidOverlappedToPinvoke
    MDA_DEFINE_ASSISTANT(InvalidOverlappedToPinvoke, NULL)
        // Input
        MDA_DEFINE_INPUT(InvalidOverlappedToPinvoke)
            MDA_XSD_ATTRIBUTE__DEFAULT(JustMyCode, BOOL, W("true"))
        MDA_DEFINE_INPUT_END(InvalidOverlappedToPinvoke)
        // Output
        MDA_DEFINE_OUTPUT(InvalidOverlappedToPinvoke)
        MDA_DEFINE_OUTPUT_END(InvalidOverlappedToPinvoke)
    MDA_DEFINE_ASSISTANT_END(InvalidOverlappedToPinvoke) 

    // InvalidVariant
    MDA_DEFINE_ASSISTANT(InvalidVariant, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(InvalidVariant)
        // Output
        MDA_DEFINE_OUTPUT(InvalidVariant)
        MDA_DEFINE_OUTPUT_END(InvalidVariant)      
    MDA_DEFINE_ASSISTANT_END(InvalidVariant)

    // JitCompilationStart
    MDA_DEFINE_ASSISTANT(JitCompilationStart, NULL)   
        // Input
        MDA_DEFINE_INPUT(JitCompilationStart)
            MDA_XSD_OPTIONAL()
                MDA_XSD_ELEMENT_REFTYPE(Methods, MemberFilterType)                
            MDA_XSD_OPTIONAL_END()
            MDA_XSD_ATTRIBUTE_DEFAULT(Break, BOOL, W("true"))
        MDA_DEFINE_INPUT_END(JitCompilationStart)
        // Output
        MDA_DEFINE_OUTPUT(JitCompilationStart)
            MDA_XSD_ONCE()
                MDA_XSD_ELEMENT_REFTYPE(Method, MethodType)
            MDA_XSD_ONCE_END()
        MDA_DEFINE_OUTPUT_END(JitCompilationStart)         
    MDA_DEFINE_ASSISTANT_END(JitCompilationStart)   

    // LoaderLock
    MDA_DEFINE_ASSISTANT(LoaderLock, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(LoaderLock)
        // Output
        MDA_DEFINE_OUTPUT(LoaderLock)
        MDA_DEFINE_OUTPUT_END(LoaderLock)         
    MDA_DEFINE_ASSISTANT_END(LoaderLock)

    // LoadFromContext
    MDA_DEFINE_ASSISTANT(LoadFromContext, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(LoadFromContext)
        // Output
        MDA_DEFINE_OUTPUT(LoadFromContext)
            MDA_XSD_ONCE()
                MDA_XSD__ELEMENT(AssemblyInfo)
                    MDA_XSD_ATTRIBUTE__REQ(DisplayName, SString)
                    MDA_XSD_ATTRIBUTE__REQ(CodeBase, SString)
                MDA_XSD_ELEMENT_END(AssemblyInfo)
            MDA_XSD_ONCE_END()
        MDA_DEFINE_OUTPUT_END(LoadFromContext)         
    MDA_DEFINE_ASSISTANT_END(LoadFromContext)

    // MarshalCleanupError
    MDA_DEFINE_ASSISTANT(MarshalCleanupError, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(MarshalCleanupError)
        // Output
        MDA_DEFINE_OUTPUT(MarshalCleanupError)
        MDA_DEFINE_OUTPUT_END(MarshalCleanupError)      
    MDA_DEFINE_ASSISTANT_END(MarshalCleanupError)

    // Marshaling
    MDA_DEFINE_ASSISTANT(Marshaling, NULL)
        // Input
        MDA_DEFINE_INPUT(Marshaling)
            MDA_XSD_ONCE()
                MDA_XSD_OPTIONAL()
                    MDA_XSD_ELEMENT_REFTYPE(MethodFilter, MemberFilterType)
                MDA_XSD_OPTIONAL_END()
                MDA_XSD_OPTIONAL()
                    MDA_XSD_ELEMENT_REFTYPE(FieldFilter, MemberFilterType)
                MDA_XSD_OPTIONAL_END()
            MDA_XSD_ONCE_END()
        MDA_DEFINE_INPUT_END(Marshaling)
        // Output
        MDA_DEFINE_OUTPUT(Marshaling)
            MDA_XSD_CHOICE()
                MDA_XSD_ELEMENT_REFTYPE(MarshalingParameter, ParameterType)
                MDA_XSD_ELEMENT_REFTYPE(MarshalingField, FieldType)
            MDA_XSD_CHOICE_END()
        MDA_DEFINE_OUTPUT_END(Marshaling)
    MDA_DEFINE_ASSISTANT_END(Marshaling)

    // MemberInfoCacheCreation
    MDA_DEFINE_ASSISTANT(MemberInfoCacheCreation, NULL)   
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(MemberInfoCacheCreation)
        // Output
        MDA_DEFINE_OUTPUT(MemberInfoCacheCreation)
        MDA_DEFINE_OUTPUT_END(MemberInfoCacheCreation)         
    MDA_DEFINE_ASSISTANT_END(MemberInfoCacheCreation)   

    // ModuloObjectHashcode
    MDA_DEFINE_ASSISTANT(ModuloObjectHashcode, W("moh"))
        // Input
        MDA_DEFINE_INPUT(ModuloObjectHashcode)
            MDA_XSD_ATTRIBUTE_DEFAULT(Modulus, INT32, W("1"))
        MDA_DEFINE_INPUT_END(ModuloObjectHashcode)
    MDA_DEFINE_ASSISTANT_END(ModuloObjectHashcode)

    // NonComVisibleBaseClass
    MDA_DEFINE_ASSISTANT(NonComVisibleBaseClass, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(NonComVisibleBaseClass)
        // Output
        MDA_DEFINE_OUTPUT(NonComVisibleBaseClass)
            MDA_XSD_ONCE()
                MDA_XSD_ELEMENT_REFTYPE(DerivedType, TypeType)
                MDA_XSD_ELEMENT_REFTYPE(BaseType, TypeType)
            MDA_XSD_ONCE_END()
        MDA_DEFINE_OUTPUT_END(NonComVisibleBaseClass)         
    MDA_DEFINE_ASSISTANT_END(NonComVisibleBaseClass)

    // NotMarshalable
    MDA_DEFINE_ASSISTANT(NotMarshalable, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(NotMarshalable)
        // Output
        MDA_DEFINE_OUTPUT(NotMarshalable)
        MDA_DEFINE_OUTPUT_END(NotMarshalable)      
    MDA_DEFINE_ASSISTANT_END(NotMarshalable)

    // OpenGenericCERCall
    MDA_DEFINE_ASSISTANT(OpenGenericCERCall, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(OpenGenericCERCall)
        // Output
        MDA_DEFINE_OUTPUT(OpenGenericCERCall)
            MDA_XSD_ONCE()
                MDA_XSD_ELEMENT__REFTYPE(Method, MethodType)
            MDA_XSD_ONCE_END()
        MDA_DEFINE_OUTPUT_END(OpenGenericCERCall)         
    MDA_DEFINE_ASSISTANT_END(OpenGenericCERCall)

    // OverlappedFreeError
    MDA_DEFINE_ASSISTANT(OverlappedFreeError, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(OverlappedFreeError)
        // Output
        MDA_DEFINE_OUTPUT(OverlappedFreeError)
        MDA_DEFINE_OUTPUT_END(OverlappedFreeError)
    MDA_DEFINE_ASSISTANT_END(OverlappedFreeError) 

    // PInvokeLog
    MDA_DEFINE_ASSISTANT(PInvokeLog, NULL)
        // Input
        MDA_DEFINE_INPUT(PInvokeLog)
            MDA_XSD_OPTIONAL()
                MDA_XSD_ELEMENT(Filter)
                    MDA_XSD_PERIODIC()
                        MDA_XSD__ELEMENT(Match)
                            MDA_XSD_ATTRIBUTE__REQ(DllName, SString)                    
                        MDA_XSD_ELEMENT_END(Match)
                    MDA_XSD_PERIODIC_END()
                MDA_XSD_ELEMENT_END(Filter)
            MDA_XSD_OPTIONAL_END()
        MDA_DEFINE_INPUT_END(PInvokeLog)
        // Output
        MDA_DEFINE_OUTPUT(PInvokeLog)
            MDA_XSD_GROUP_REF(PInvokeGrpType)
        MDA_DEFINE_OUTPUT_END(PInvokeLog)
    MDA_DEFINE_ASSISTANT_END(PInvokeLog) 

#ifdef _TARGET_X86_ 
    // PInvokeStackImbalance
    MDA_DEFINE_ASSISTANT(PInvokeStackImbalance, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(PInvokeStackImbalance)
        // Output
        MDA_DEFINE_OUTPUT(PInvokeStackImbalance)
            MDA_XSD_ONCE()
                MDA_XSD_ELEMENT__REFTYPE(Method, MethodType)
            MDA_XSD_ONCE_END()
        MDA_DEFINE_OUTPUT_END(PInvokeStackImbalance)
    MDA_DEFINE_ASSISTANT_END(PInvokeStackImbalance) 
#endif    

    // RaceOnRCWCleanup
    MDA_DEFINE_ASSISTANT(RaceOnRCWCleanup, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(RaceOnRCWCleanup)
        // Output
        MDA_DEFINE_OUTPUT(RaceOnRCWCleanup)
        MDA_DEFINE_OUTPUT_END(RaceOnRCWCleanup)      
    MDA_DEFINE_ASSISTANT_END(RaceOnRCWCleanup)

    // Reentrancy
    MDA_DEFINE_ASSISTANT(Reentrancy, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(Reentrancy)
        // Output
        MDA_DEFINE_OUTPUT(Reentrancy)
        MDA_DEFINE_OUTPUT_END(Reentrancy)         
    MDA_DEFINE_ASSISTANT_END(Reentrancy)

    // ReleaseHandleFailed
    MDA_DEFINE_ASSISTANT(ReleaseHandleFailed, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(ReleaseHandleFailed)
        // Output
        MDA_DEFINE_OUTPUT(ReleaseHandleFailed)
            MDA_XSD_ONCE()
                MDA_XSD_ELEMENT__REFTYPE(Type, TypeType)
                MDA_XSD_ELEMENT(Handle)
                    MDA_XSD_ATTRIBUTE_REQ(Value, SString)
                MDA_XSD_ELEMENT_END(Handle)
            MDA_XSD_ONCE_END()
        MDA_DEFINE_OUTPUT_END(ReleaseHandleFailed)         
    MDA_DEFINE_ASSISTANT_END(ReleaseHandleFailed)

    // ReportAvOnComRelease
    MDA_DEFINE_ASSISTANT(ReportAvOnComRelease, NULL)
        // Input
        MDA_DEFINE_INPUT(ReportAvOnComRelease)
            MDA_XSD_ATTRIBUTE_DEFAULT(AllowAv, BOOL, W("false"))
        MDA_DEFINE_INPUT_END(ReportAvOnComRelease)
        // Output
        MDA_DEFINE_OUTPUT(ReportAvOnComRelease)
        MDA_DEFINE_OUTPUT_END(ReportAvOnComRelease)
    MDA_DEFINE_ASSISTANT_END(ReportAvOnComRelease)   

    // StreamWriterBufferedDataLost
    MDA_DEFINE_ASSISTANT(StreamWriterBufferedDataLost, NULL)
        // Input
        MDA_DEFINE_INPUT(StreamWriterBufferedDataLost)
            MDA_XSD_ATTRIBUTE_DEFAULT(CaptureAllocatedCallStack, BOOL, W("false"))
        MDA_DEFINE_INPUT_END(StreamWriterBufferedDataLost)
        //Output
        MDA_DEFINE_OUTPUT(StreamWriterBufferedDataLost)
        MDA_DEFINE_OUTPUT_END(StreamWriterBufferedDataLost)        
    MDA_DEFINE_ASSISTANT_END(StreamWriterBufferedDataLost)
    
    // VirtualCERCall
    MDA_DEFINE_ASSISTANT(VirtualCERCall, NULL)
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(VirtualCERCall)
        // Output
        MDA_DEFINE_OUTPUT(VirtualCERCall)
            MDA_XSD_ONCE()
                MDA_XSD_ELEMENT__REFTYPE(Method, MethodType)
                MDA_XSD_ELEMENT__REFTYPE(Callsite, MethodAndOffsetType)
            MDA_XSD_ONCE_END()
        MDA_DEFINE_OUTPUT_END(VirtualCERCall)         
    MDA_DEFINE_ASSISTANT_END(VirtualCERCall)

    //
    // Framework helper assistants
    //
#if _DEBUG
    // XmlValidationError
    MDA_DEFINE_ASSISTANT(XmlValidationError, NULL)   
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(XmlValidationError)
        // Output       
        MDA_DEFINE_OUTPUT(XmlValidationError)
            MDA_XSD_ONCE()
                MDA_XSD_ELEMENT(ViolatingXml)
                MDA_XSD_ELEMENT_END(ViolatingXml)
                MDA_XSD_ELEMENT(ViolatedXsd)
                MDA_XSD_ELEMENT_END(ViolatedXsd)
            MDA_XSD_ONCE_END()
        MDA_DEFINE_OUTPUT_END(XmlValidationError)        
    MDA_DEFINE_ASSISTANT_END(XmlValidationError)   
#endif

    // InvalidConfigFile
    MDA_DEFINE_ASSISTANT(InvalidConfigFile, NULL)   
        // Input
        MDA_DEFINE_INPUT_AS_SWITCH(InvalidConfigFile)
        // Output
        MDA_DEFINE_OUTPUT(InvalidConfigFile)
            MDA_XSD_ATTRIBUTE_REQ(ConfigFile, SString)
        MDA_DEFINE_OUTPUT_END(InvalidConfigFile)        
    MDA_DEFINE_ASSISTANT_END(InvalidConfigFile)   


    //
    // Framework Type and Element definitions
    // 
    MDA_XSD_OUTPUT_ONLY()
    
        // Module
        MDA_XSD_DEFINE_TYPE(ModuleType)
            MDA_XSD_ATTRIBUTE__OPT(Name, SString)
        MDA_XSD_DEFINE_TYPE_END(ModuleType)
    
        // Type
        MDA_XSD_DEFINE_TYPE(TypeType)
            MDA_XSD_ATTRIBUTE__REQ(Name, SString)
        MDA_XSD_DEFINE_TYPE_END(TypeType)

        // Parameter
        MDA_XSD_DEFINE_TYPE(ParameterType)
            MDA_XSD_ONCE()
                MDA_XSD_ELEMENT_REFTYPE(DeclaringMethod, MethodType)      
            MDA_XSD_ONCE_END()
            
            MDA_XSD_ATTRIBUTE_OPT(Index, INT32)
            MDA_XSD_ATTRIBUTE__OPT(Name, SString)
        MDA_XSD_DEFINE_TYPE_END(ParameterType)

        // Method
        MDA_XSD_DEFINE_TYPE(MethodType)
            MDA_XSD_ATTRIBUTE__REQ(Name, SString)
        MDA_XSD_DEFINE_TYPE_END(MethodType)
    
        // Field
        MDA_XSD_DEFINE_TYPE(FieldType)
            MDA_XSD_ATTRIBUTE_REQ(Name, SString)
        MDA_XSD_DEFINE_TYPE_END(FieldType)

        // Thread
        MDA_XSD_DEFINE_TYPE(ThreadType)
            MDA_XSD_ATTRIBUTE_REQ(OsId, INT32)
            MDA_XSD_ATTRIBUTE_OPT(ManagedId, INT32)
        MDA_XSD_DEFINE_TYPE_END(ThreadType)

        // Exception
        MDA_XSD_DEFINE_TYPE(ExceptionType)
            MDA_XSD_ONCE()
                MDA_XSD_ELEMENT__REFTYPE(Type, TypeType)
            MDA_XSD_ONCE_END()        
            MDA_XSD_ATTRIBUTE_REQ(Message, SString)
        MDA_XSD_DEFINE_TYPE_END(ExceptionType)

        // MethodAndOffset
        MDA_XSD_DEFINE_TYPE(MethodAndOffsetType)        
            MDA_XSD_ATTRIBUTE__REQ(Name, SString)
            MDA_XSD_ATTRIBUTE_OPT(Offset, SString)
        MDA_XSD_DEFINE_TYPE_END(MethodAndOffsetType)

        // PInvoke
        MDA_XSD_GROUP(PInvokeGrpType)
            MDA_XSD_ONCE()
                MDA_XSD_ELEMENT__REFTYPE(Method, MethodType)
                MDA_XSD_ELEMENT(DllImport)
                    MDA_XSD_ATTRIBUTE_REQ(EntryPoint, SString)                    
                    MDA_XSD_ATTRIBUTE_REQ(DllName, SString)
                MDA_XSD_ELEMENT_END(DllImport)
            MDA_XSD_ONCE_END()
        MDA_XSD_GROUP_END(PInvokeGrpType)
        
    MDA_XSD_OUTPUT_ONLY_END()


    
    MDA_XSD_INPUT_ONLY()

        // MemberFilter
        MDA_XSD_DEFINE_TYPE(MemberFilterType)
            MDA_XSD_PERIODIC()
                MDA_XSD_ELEMENT(Match)
                    MDA_XSD_ATTRIBUTE_DEFAULT(Module, SString, NULL)
                    MDA_XSD_ATTRIBUTE__REQ(Name, SString)
                    MDA_XSD_ATTRIBUTE__OPT(JustMyCode, BOOL)
                MDA_XSD_ELEMENT_END(Match)
            MDA_XSD_PERIODIC_END()

            MDA_XSD_ATTRIBUTE_DEFAULT(JustMyCode, BOOL, W("true"))
        MDA_XSD_DEFINE_TYPE_END(MemberFilterType)   


    MDA_XSD_INPUT_ONLY_END()


    


