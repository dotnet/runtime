// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILVerify
{
    public enum VerifierError
    {
        None = 0,
        UnknownOpcode,         // Unknown opcode.
        MethodFallthrough,              // Fall through end of the method without returning.
        EHClauseOutOfRange,             // Exception handling clause bounds outside code size.
        FallthroughException,           // Fallthrough the end of an exception block.
        FallthroughIntoHandler,         // Fallthrough into an exception handler.
        FallthroughIntoFilter,          // Fallthrough into an exception filter.
        LeaveIntoTry,                   // Leave into try block.
        LeaveIntoHandler,               // Leave into exception handler block.
        LeaveIntoFilter,                // Leave into filter block.
        LeaveOutOfFilter,               // Leave out of filter block.
        LeaveOutOfFinally,              // Leave out of finally block.
        LeaveOutOfFault,                // Leave out of fault block.
        Rethrow,                        // Rethrow from outside a catch handler.
        Endfinally,                     // Endfinally/endfault from outside a finally or fault handler.
        Endfilter,                      // Endfilter from outside an exception filter block.
        BranchIntoTry,                  // Branch into try block.
        BranchIntoHandler,              // Branch into exception handler block.
        BranchIntoFilter,               // Branch into exception filter block.
        BranchOutOfTry,                 // Branch out of try block.
        BranchOutOfHandler,             // Branch out of exception handler block.
        BranchOutOfFilter,              // Branch out of exception filter block.
        BranchOutOfFinally,             // Branch out of finally block.
        ReturnFromTry,                  // Return out of try block.
        ReturnFromHandler,              // Return out of exception handler block.
        ReturnFromFilter,               // Return out of exception filter block.
        BadJumpTarget,                  // Branch / Leave into the middle of an instruction.
        PathStackUnexpected,            // Non-compatible types on stack depending on path.
        PathStackDepth,                 // Stack depth differs depending on path.
        ThisUninitStore,                // Store into this when it is uninitialized.
        ThisUninitReturn,               // Return from .ctor when this is uninitialized.
        LdftnCtor,                      // ldftn/ldvirtftn not allowed on .ctor.
        StackUnexpected,                // Unexpected type on the stack.
        StackUnexpectedArrayType,       // Unexpected array type on the stack.
        StackOverflow,                  // Stack overflow.
        StackUnderflow,                 // Stack underflow.
        UninitStack,                    // Uninitialized item on stack.
        ExpectedIntegerType,            // Expected I, I4, or I8 on the stack.
        ExpectedFloatType,              // Expected R, R4, or R8 on the stack.
        ExpectedNumericType,            // Expected numeric type on the stack.
        StackObjRef,                    // Expected an ObjRef on the stack.
        StackByRef,                     // Expected ByRef on the stack.
        StackMethod,                    // Expected pointer to function on the stack.
        UnrecognizedLocalNumber,        // Unrecognized local variable number.
        UnrecognizedArgumentNumber,     // Unrecognized argument number.
        ExpectedTypeToken,              // Expected type token.
        TokenResolve,                   // Unable to resolve token.
        ExpectedMethodToken,            // Expected memberRef, memberDef or methodSpec token.
        ExpectedFieldToken,             // Expected field token.
        Unverifiable,                   // Instruction can not be verified.
        StringOperand,                  // Operand does not point to a valid string ref.
        ReturnPtrToStack,               // Return type is ByRef, TypedReference, ArgHandle, or ArgIterator.
        ReturnVoid,                     // Stack must be empty on return from a void function.
        ReturnMissing,                  // Return value missing on the stack.
        ReturnEmpty,                    // Stack must contain only the return value.
        ExpectedArray,                  // Expected single-dimension zero-based array.
        ValueTypeExpected,              // Value type expected.
        TypeAccess,                     // Type is not visible.
        MethodAccess,                   // Method is not visible.
        FieldAccess,                    // Field is not visible.
        ExpectedStaticField,            // Expected static field.
        InitOnly,                       // Cannot change initonly field outside its .ctor.
        CallVirtOnValueType,            // Callvirt on a value type method.
        CtorExpected,                   // .ctor expected.
        CtorSig,                        // newobj on static or abstract method.
        ArrayByRef,                     // Array of ELEMENT_TYPE_BYREF or ELEMENT_TYPE_TYPEDBYREF.
        ByrefOfByref,                   // ByRef of ByRef.
        CodeSizeZero,                   // Code size is zero.
        TailCall,                       // Missing call/callvirt/calli.
        TailByRef,                      // Cannot pass ByRef to a tail call.
        TailRet,                        // tail.call may only be followed by ret.
        TailRetVoid,                    // Void ret type expected for tail call.
        TailRetType,                    // Tail call return type not compatible.
        TailStackEmpty,                 // Stack not empty after tail call.
        MethodEnd,                      // Method ends in the middle of an instruction.
        BadBranch,                      // Branch out of the method.
        Volatile,                       // Missing ldsfld, stsfld, ldind, stind, ldfld, stfld, ldobj, stobj, initblk, or cpblk.
        Unaligned,                      // Missing ldind, stind, ldfld, stfld, ldobj, stobj, initblk, cpblk.
        CallAbstract,                   // Call not allowed on abstract methods.
        TryNonEmptyStack,               // Attempt to enter a try block with nonempty stack.
        FilterOrCatchUnexpectedStack,   // Attempt to enter a filter or catch block with unexpected stack state.
        FinOrFaultNonEmptyStack,        // Attempt to enter a finally or fault block with nonempty stack.
        DelegateCtor,                   // Unrecognized arguments for delegate .ctor.
        DelegatePattern,                // Dup, ldvirtftn, newobj delegate::.ctor() pattern expected (in the same basic block).
        BoxByRef,                       // Cannot box byref.
        EndfilterStack,                 // Stack not empty when leaving an exception filter.
        DelegateCtorSigI,               // Unrecognized delegate .ctor signature; expected Native Int.
        DelegateCtorSigO,               // Unrecognized delegate .ctor signature; expected Object.
        CatchByRef,                     // ByRef not allowed as catch type.
        ThrowOrCatchOnlyExceptionType,  // The type caught or thrown must be derived from System.Exception.
        LdvirtftnOnStatic,              // ldvirtftn on static.
        CallVirtOnStatic,               // callvirt on static.
        InitLocals,                     // initlocals must be set for verifiable methods with one or more local variables.
        CallCtor,                       // call to .ctor only allowed to initialize this pointer from within a .ctor. Try newobj.

        ////@GENERICSVER: new generics related error messages
        ExpectedValClassObjRefVariable, // Value type, ObjRef type or variable type expected.
        ReadOnly,                       // Missing ldelema or call following readonly prefix.
        Constrained,                    // Missing callvirt following constrained prefix.

        UnsatisfiedMethodInst,                // Method instantiation has unsatisfied method type parameter constraints.
        UnsatisfiedMethodParentInst,          // Method parent instantiation has unsatisfied class type parameter constraints.
        UnsatisfiedFieldParentInst,           // Field parent instantiation has unsatisfied class type parameter constraints.
        UnsatisfiedBoxOperand,                // Type operand of box instruction has unsatisfied class type parameter constraints.
        ConstrainedCallWithNonByRefThis,      // The 'this' argument to a constrained call must have ByRef type.
        ReadonlyUnexpectedCallee,             // The readonly prefix may only be applied to calls to array methods returning ByRefs.
        ReadOnlyIllegalWrite,                 // Illegal write to readonly ByRef.
        TailCallInsideER,                     // The tail.call (or calli or callvirt) instruction cannot be used to transfer control out of a try, filter, catch, or finally block.
        BackwardBranch,                       // Stack height at all points must be determinable in a single forward scan of IL.
        NewobjAbstractClass,                  // Cannot construct an instance of abstract class.
        UnmanagedPointer,                     // Unmanaged pointers are not a verifiable type.
        LdftnNonFinalVirtual,                 // Cannot LDFTN a non-final virtual method for delegate creation if target object is potentially not the same type as the method class.
        ThisMismatch,                         // The 'this' parameter to the call must be the calling method's 'this' parameter.

        InterfaceImplHasDuplicate,            // InterfaceImpl has a duplicate
        InterfaceMethodNotImplemented,         // Class implements interface but not method
        LocallocStackNotEmpty, // localloc requires that stack must be empty, except for 'size' argument
        InvalidBaseType, // Type has an invalid base type.
        BadTypeSpec, // Invalid TypeSpec metadata.
        ConstrainedTypeNoInterfaceImpl, // The type operand of the constrained prefix must implement the interface declaring the static virtual method.
        MethodImplStaticMismatch, // The MethodImpl body and declaration must both be static or both be instance methods.
        MethodImplBodyNotVirtual, // An instance MethodImpl body must be virtual.
        EHClauseEmpty, // Exception handling region is empty.
        EHClauseBoundary, // Exception handling region does not start or end on an instruction boundary.
        EHClauseOverlap, // Exception handling regions overlap or are shared illegally.
        EHClauseNesting, // The parts of an exception handling clause have inconsistent nesting.
        EHClauseOrder, // An exception handling clause nested in a try must precede that try's clause.
        EHClauseFilterEnd, // A filter must have exactly one endfilter as its last instruction.
        UnalignedAlignment, // Alignment specified for 'unaligned.' must be 1, 2, or 4.
    }
}
