// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILVerify
{
    public enum VerifierError
    {
        None = 0,
        //E_HRESULT           "[HRESULT 0x%08X]"
        //E_OFFSET            "[offset 0x%08X]"
        //E_OPCODE            "[opcode %s]"
        //E_OPERAND           "[operand 0x%08X]"
        //E_TOKEN             "[token  0x%08X]"
        //E_EXCEPT            "[exception #0x%08X]"
        //E_STACK_SLOT        "[stack slot 0x%08X]"
        //E_LOC               "[local variable #0x%08X]"
        //E_LOC_BYNAME        "[local variable '%s']"
        //E_ARG               "[argument #0x%08x]"
        //E_FOUND             "[found %s]"
        //E_EXPECTED          "[expected %s]"

        UnknownOpcode,         // Unknown opcode.
        //E_SIG_CALLCONV      "Unknown calling convention [0x%08X]."
        //E_SIG_ELEMTYPE      "Unknown ELEMENT_TYPE [0x%08x]."

        //E_RET_SIG           "[return sig]"
        //E_FIELD_SIG         "[field sig]"

        //E_INTERNAL           "Internal error."
        //E_STACK_TOO_LARGE    "Stack is too large."
        //E_ARRAY_NAME_LONG    "Array name is too long."

        MethodFallthrough,              // Fall through end of the method without returning.
        //E_TRY_GTEQ_END                "try start >= try end."
        //E_TRYEND_GT_CS                "try end > code size."
        //E_HND_GTEQ_END                "handler start >= handler end."
        //E_HNDEND_GT_CS                "handler end > code size."
        //E_TRY_START                   "Try starts in the middle of an instruction."
        //E_HND_START                   "Handler starts in the middle of an instruction."
        //E_TRY_OVERLAP                 "Try block overlap with another block."
        //E_TRY_EQ_HND_FIL              "Try and filter/handler blocks are equivalent."
        //E_TRY_SHARE_FIN_FAL           "Shared try has finally or fault handler."
        //E_HND_EQ                      "Handler block is the same as another block."
        //E_FIL_CONT_TRY                "Filter contains try."
        //E_FIL_CONT_HND                "Filter contains handler."
        //E_FIL_CONT_FIL                "Nested filters."
        //E_FIL_GTEQ_CS                 "filter >= code size."
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
        Endfinally,                     // Endfinally from outside a finally handler.
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
        //E_THIS_UNINIT_EXCEP           "Uninitialized this on entering a try block."
        ThisUninitStore,                // Store into this when it is uninitialized.
        ThisUninitReturn,               // Return from .ctor when this is uninitialized.
        LdftnCtor,                      // ldftn/ldvirtftn not allowed on .ctor.
        //StackNotEq,                   // Non-compatible types on the stack.
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
        //E_TOKEN_TYPE                  "Unable to resolve type of the token."
        ExpectedMethodToken,            // Expected memberRef, memberDef or methodSpec token.
        ExpectedFieldToken,             // Expected field token.
        Unverifiable,                   // Instruction can not be verified.
        StringOperand,                  // Operand does not point to a valid string ref.
        ReturnPtrToStack,               // Return type is ByRef, TypedReference, ArgHandle, or ArgIterator.
        ReturnVoid,                     // Stack must be empty on return from a void function.
        ReturnMissing,                  // Return value missing on the stack.
        ReturnEmpty,                    // Stack must contain only the return value.
        ExpectedArray,                  // Expected single-dimension zero-based array.
        //E_ARRAY_SD_PTR                "Expected single dimension array of pointer types."
        //E_ARGLIST                     "Allowed only in vararg methods."
        ValueTypeExpected,              // Value type expected.
        //E_OPEN_DLGT_PROT_ACC          "Protected method access through an open instance delegate is not verifiable."
        TypeAccess,                     // Type is not visible.
        MethodAccess,                   // Method is not visible.
        FieldAccess,                    // Field is not visible.
        ExpectedStaticField,            // Expected static field.
        InitOnly,                       // Cannot change initonly field outside its .ctor.
        //E_WRITE_RVA_STATIC            "Cannot modify an imaged based (RVA) static"
        CallVirtOnValueType,            // Callvirt on a value type method.
        CtorExpected,                   // .ctor expected.
        CtorSig,                        // newobj on static or abstract method.
        //E_SIG_ARRAY                   "Cannot resolve Array type."
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
        //E_LEXICAL_NESTING             "Lexical nesting."
        Volatile,                       // Missing ldsfld, stsfld, ldind, stind, ldfld, stfld, ldobj, stobj, initblk, or cpblk.
        Unaligned,                      // Missing ldind, stind, ldfld, stfld, ldobj, stobj, initblk, cpblk.
        //E_INNERMOST_FIRST             "Innermost exception blocks should be declared first."
        CallAbstract,                   // Call not allowed on abstract methods.
        TryNonEmptyStack,               // Attempt to enter a try block with nonempty stack.
        FilterOrCatchUnexpectedStack,   // Attempt to enter a filter or catch block with unexpected stack state.
        FinOrFaultNonEmptyStack,        // Attempt to enter a finally or fault block with nonempty stack.
        DelegateCtor,                   // Unrecognized arguments for delegate .ctor.
        DelegatePattern,                // Dup, ldvirtftn, newobj delegate::.ctor() pattern expected (in the same basic block).
        //E_SIG_C_VC                    "ELEMENT_TYPE_CLASS ValueClass in signature."
        //E_SIG_VC_C                    "ELEMENT_TYPE_VALUETYPE non-ValueClass in signature."
        //E_BOX_PTR_TO_STACK            "Box operation on TypedReference, ArgHandle, or ArgIterator."
        BoxByRef,                       // Cannot box byref.
        //E_SIG_BYREF_TB_AH             "ByRef of TypedReference, ArgHandle, or ArgIterator."
        EndfilterStack,                 // Stack not empty when leaving an exception filter.
        DelegateCtorSigI,               // Unrecognized delegate .ctor signature; expected Native Int.
        DelegateCtorSigO,               // Unrecognized delegate .ctor signature; expected Object.
        //E_RA_PTR_TO_STACK             "Mkrefany on TypedReference, ArgHandle, or ArgIterator."
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

        //E_CIRCULAR_VAR_CONSTRAINTS    "Method parent has circular class type parameter constraints."
        //E_CIRCULAR_MVAR_CONSTRAINTS   "Method has circular method type parameter constraints."

        UnsatisfiedMethodInst,                // Method instantiation has unsatisfied method type parameter constraints.
        UnsatisfiedMethodParentInst,          // Method parent instantiation has unsatisfied class type parameter constraints.
        UnsatisfiedFieldParentInst,           // Field parent instantiation has unsatisfied class type parameter constraints.
        UnsatisfiedBoxOperand,                // Type operand of box instruction has unsatisfied class type parameter constraints.
        ConstrainedCallWithNonByRefThis,      // The 'this' argument to a constrained call must have ByRef type.
        //E_CONSTRAINED_OF_NON_VARIABLE_TYPE "The operand to a constrained prefix instruction must be a type parameter."
        ReadonlyUnexpectedCallee,             // The readonly prefix may only be applied to calls to array methods returning ByRefs.
        ReadOnlyIllegalWrite,                 // Illegal write to readonly ByRef.
        //E_READONLY_IN_MKREFANY              "A readonly ByRef cannot be used with mkrefany."
        //E_UNALIGNED_ALIGNMENT               "Alignment specified for 'unaligned' prefix must be 1, 2, or 4."
        TailCallInsideER,                     // The tail.call (or calli or callvirt) instruction cannot be used to transfer control out of a try, filter, catch, or finally block.
        BackwardBranch,                       // Stack height at all points must be determinable in a single forward scan of IL.
        //E_CALL_TO_VTYPE_BASE                "Call to base type of valuetype."
        NewobjAbstractClass,                  // Cannot construct an instance of abstract class.
        UnmanagedPointer,                     // Unmanaged pointers are not a verifiable type.
        LdftnNonFinalVirtual,                 // Cannot LDFTN a non-final virtual method for delegate creation if target object is potentially not the same type as the method class.
        //E_FIELD_OVERLAP                     "Accessing type with overlapping fields."
        ThisMismatch,                         // The 'this' parameter to the call must be the calling method's 'this' parameter.

        //E_BAD_PE             "Unverifiable PE Header/native stub."
        //E_BAD_MD             "Unrecognized metadata, unable to verify IL."
        //E_BAD_APPDOMAIN      "Unrecognized appdomain pointer."

        //E_TYPELOAD           "Type load failed."
        //E_PE_LOAD            "Module load failed."

        //IDS_E_FORMATTING     "Error formatting message."
        //IDS_E_ILERROR        "[IL]: Error: "
        //IDS_E_GLOBAL         "<GlobalFunction>"
        //IDS_E_MDTOKEN        "[mdToken=0x%x]"
        InterfaceImplHasDuplicate,            // InterfaceImpl has a duplicate
        InterfaceMethodNotImplemented,         // Class implements interface but not method
        LocallocStackNotEmpty, // localloc requires that stack must be empty, except for 'size' argument
    }
}
