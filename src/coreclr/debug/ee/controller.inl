// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

inline BOOL DebuggerControllerPatch::IsILPrimaryPatch()
{
    LIMITED_METHOD_CONTRACT;
    return (kind == PATCH_KIND_IL_PRIMARY);
}

inline BOOL DebuggerControllerPatch::IsILReplicaPatch()
{
    LIMITED_METHOD_CONTRACT;
    return (kind == PATCH_KIND_IL_REPLICA);
}

inline BOOL DebuggerControllerPatch::IsManagedPatch()
{
    return (IsILPrimaryPatch() || IsILReplicaPatch() || kind == PATCH_KIND_NATIVE_MANAGED);
}

inline BOOL DebuggerControllerPatch::IsNativePatch()
{
    return (kind == PATCH_KIND_NATIVE_MANAGED || kind == PATCH_KIND_NATIVE_UNMANAGED || (IsILReplicaPatch() && !offsetIsIL));
}

inline BOOL DebuggerControllerPatch::IsEnCRemapPatch()
{
    return (controller->GetDCType() == DEBUGGER_CONTROLLER_ENC);
}

#endif  // CONTROLLER_INL_
