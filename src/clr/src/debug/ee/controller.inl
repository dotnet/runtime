//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// File: controller.inl
// 

//
// Inline definitions for the Left-Side of the CLR debugging services
// This is logically part of the header file. 
//
//*****************************************************************************

#ifndef CONTROLLER_INL_
#define CONTROLLER_INL_

inline BOOL DebuggerControllerPatch::IsBreakpointPatch()
{
    return (controller->GetDCType() == DEBUGGER_CONTROLLER_BREAKPOINT);
}

inline BOOL DebuggerControllerPatch::IsStepperPatch() 
{ 
    return (controller->IsStepperDCType()); 
}

inline DebuggerPatchKind DebuggerControllerPatch::GetKind()
{
    return kind;
}
inline BOOL DebuggerControllerPatch::IsILMasterPatch()
{
    LIMITED_METHOD_CONTRACT;

    return (kind == PATCH_KIND_IL_MASTER);
}

inline BOOL DebuggerControllerPatch::IsILSlavePatch()
{
    LIMITED_METHOD_CONTRACT;

    return (kind == PATCH_KIND_IL_SLAVE);
}

inline BOOL DebuggerControllerPatch::IsManagedPatch()
{
    return (IsILMasterPatch() || IsILSlavePatch() || kind == PATCH_KIND_NATIVE_MANAGED);

}
inline BOOL DebuggerControllerPatch::IsNativePatch()
{
    return (kind == PATCH_KIND_NATIVE_MANAGED || kind == PATCH_KIND_NATIVE_UNMANAGED || (IsILSlavePatch() && !offsetIsIL));

}

#endif  // CONTROLLER_INL_
