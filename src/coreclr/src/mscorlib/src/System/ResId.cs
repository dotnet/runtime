// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Contains identifiers for each of the resources
**          specified in resources.txt
**
**
===========================================================*/
namespace System {
    //This class contains only static members and does not need to be serializable.
    using System.Configuration.Assemblies;
    using System;
    internal static class ResId {
        // Only statics, does not need to be marked with the serializable attribute
        internal const String Arg_ArrayLengthsDiffer="Arg_ArrayLengthsDiffer";
        internal const String Argument_InvalidNumberOfMembers="Argument_InvalidNumberOfMembers";
        internal const String Argument_UnequalMembers="Argument_UnequalMembers";
        internal const String Argument_SpecifyValueSize="Argument_SpecifyValueSize";
        internal const String Argument_UnmatchingSymScope="Argument_UnmatchingSymScope";
        internal const String Argument_NotInExceptionBlock="Argument_NotInExceptionBlock";
        internal const String Argument_NotExceptionType="Argument_NotExceptionType";
        internal const String Argument_InvalidLabel="Argument_InvalidLabel";
        internal const String Argument_UnclosedExceptionBlock="Argument_UnclosedExceptionBlock";
        internal const String Argument_MissingDefaultConstructor="Argument_MissingDefaultConstructor";
        internal const String Argument_TooManyFinallyClause="Argument_TooManyFinallyClause";
        internal const String Argument_NotInTheSameModuleBuilder="Argument_NotInTheSameModuleBuilder";
        internal const String Argument_BadCurrentLocalVariable="Argument_BadCurrentLocalVariable";
        internal const String Argument_DuplicateModuleName="Argument_DuplicateModuleName";
        internal const String Argument_BadPersistableModuleInTransientAssembly="Argument_BadPersistableModuleInTransientAssembly";
        internal const String Argument_HasToBeArrayClass="Argument_HasToBeArrayClass";
        internal const String Argument_InvalidDirectory="Argument_InvalidDirectory";
        
        internal const String MissingType="MissingType";
        internal const String MissingModule="MissingModule";
    
        internal const String ArgumentOutOfRange_Index="ArgumentOutOfRange_Index";
        internal const String ArgumentOutOfRange_Range="ArgumentOutOfRange_Range";
     
        internal const String ExecutionEngine_YoureHosed="ExecutionEngine_YoureHosed";
    
        internal const String Format_NeedSingleChar="Format_NeedSingleChar";
        internal const String Format_StringZeroLength="Format_StringZeroLength";
    
        internal const String InvalidOperation_EnumEnded="InvalidOperation_EnumEnded";
        internal const String InvalidOperation_EnumFailedVersion="InvalidOperation_EnumFailedVersion";
        internal const String InvalidOperation_EnumNotStarted="InvalidOperation_EnumNotStarted";
        internal const String InvalidOperation_EnumOpCantHappen="InvalidOperation_EnumOpCantHappen";
        internal const String InvalidOperation_InternalState="InvalidOperation_InternalState";
        internal const String InvalidOperation_ModifyRONumFmtInfo="InvalidOperation_ModifyRONumFmtInfo";
        internal const String InvalidOperation_MethodBaked="InvalidOperation_MethodBaked";
        internal const String InvalidOperation_NotADebugModule="InvalidOperation_NotADebugModule";
        internal const String InvalidOperation_MethodHasBody="InvalidOperation_MethodHasBody";
        internal const String InvalidOperation_OpenLocalVariableScope="InvalidOperation_OpenLocalVariableScope";    
        internal const String InvalidOperation_TypeHasBeenCreated="InvalidOperation_TypeHasBeenCreated";
        internal const String InvalidOperation_RefedAssemblyNotSaved="InvalidOperation_RefedAssemblyNotSaved";
        internal const String InvalidOperation_AssemblyHasBeenSaved="InvalidOperation_AssemblyHasBeenSaved";
        internal const String InvalidOperation_ModuleHasBeenSaved="InvalidOperation_ModuleHasBeenSaved";
        internal const String InvalidOperation_CannotAlterAssembly="InvalidOperation_CannotAlterAssembly";
    
        internal const String NotSupported_CannotSaveModuleIndividually="NotSupported_CannotSaveModuleIndividually";
        internal const String NotSupported_Constructor="NotSupported_Constructor";
        internal const String NotSupported_Method="NotSupported_Method";
        internal const String NotSupported_NYI="NotSupported_NYI";
        internal const String NotSupported_DynamicModule="NotSupported_DynamicModule";
        internal const String NotSupported_NotDynamicModule="NotSupported_NotDynamicModule";
        internal const String NotSupported_NotAllTypesAreBaked="NotSupported_NotAllTypesAreBaked";
        internal const String NotSupported_SortedListNestedWrite="NotSupported_SortedListNestedWrite";
    
        
        internal const String Serialization_ArrayInvalidLength="Serialization_ArrayInvalidLength";
        internal const String Serialization_ArrayNoLength="Serialization_ArrayNoLength";
        internal const String Serialization_CannotGetType="Serialization_CannotGetType";
        internal const String Serialization_InsufficientState="Serialization_InsufficientState";
        internal const String Serialization_InvalidID="Serialization_InvalidID";
        internal const String Serialization_MalformedArray="Serialization_MalformedArray";
        internal const String Serialization_MultipleMembers="Serialization_MultipleMembers";
        internal const String Serialization_NoID="Serialization_NoID";
        internal const String Serialization_NoType="Serialization_NoType";
        internal const String Serialization_NoBaseType="Serialization_NoBaseType";
        internal const String Serialization_NullSignature="Serialization_NullSignature";
        internal const String Serialization_UnknownMember="Serialization_UnknownMember";
        internal const String Serialization_BadParameterInfo="Serialization_BadParameterInfo";
        internal const String Serialization_NoParameterInfo="Serialization_NoParameterInfo";

        internal const String WeakReference_NoLongerValid="WeakReference_NoLongerValid";
        internal const String Loader_InvalidPath="Loader_InvalidPath";
    }
}
