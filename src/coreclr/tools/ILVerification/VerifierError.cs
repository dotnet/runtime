// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILVerify
{
    /// <summary>
    /// Identifies a problem that a <see cref="Verifier"/> found in IL or metadata.
    /// </summary>
    public enum VerifierError
    {
        /// <summary>No error code; <see cref="VerificationResult.Message"/> describes the failure.</summary>
        None = 0,
        /// <summary>The IL contains an opcode that is not defined.</summary>
        UnknownOpcode,         // Unknown opcode.
        /// <summary>Execution can run past the end of the method without returning.</summary>
        MethodFallthrough,              // Fall through end of the method without returning.
        /// <summary>
        /// An exception handling clause extends outside the IL of the method, or a filter does not
        /// start before its handler.
        /// </summary>
        EHClauseOutOfRange,             // Exception handling clause bounds outside code size.
        /// <summary>Execution falls through the end of a try block, filter or exception handler.</summary>
        FallthroughException,           // Fallthrough the end of an exception block.
        /// <summary>Execution falls through into an exception handler.</summary>
        FallthroughIntoHandler,         // Fallthrough into an exception handler.
        /// <summary>Execution falls through into an exception filter.</summary>
        FallthroughIntoFilter,          // Fallthrough into an exception filter.
        /// <summary>A leave instruction targets a location in a try block that it cannot enter.</summary>
        LeaveIntoTry,                   // Leave into try block.
        /// <summary>A leave instruction targets an exception handler from outside it.</summary>
        LeaveIntoHandler,               // Leave into exception handler block.
        /// <summary>A leave instruction targets an exception filter from outside it.</summary>
        LeaveIntoFilter,                // Leave into filter block.
        /// <summary>A leave instruction exits an exception filter.</summary>
        LeaveOutOfFilter,               // Leave out of filter block.
        /// <summary>A leave instruction exits a finally block.</summary>
        LeaveOutOfFinally,              // Leave out of finally block.
        /// <summary>A leave instruction exits a fault block.</summary>
        LeaveOutOfFault,                // Leave out of fault block.
        /// <summary>A rethrow instruction is outside a catch handler.</summary>
        Rethrow,                        // Rethrow from outside a catch handler.
        /// <summary>An endfinally instruction is outside a finally or fault block.</summary>
        Endfinally,                     // Endfinally from outside a finally handler.
        /// <summary>
        /// An endfilter instruction is outside an exception filter, or is not the last instruction of the filter.
        /// </summary>
        Endfilter,                      // Endfilter from outside an exception filter block.
        /// <summary>A branch instruction targets a location in a try block that it cannot enter.</summary>
        BranchIntoTry,                  // Branch into try block.
        /// <summary>A branch instruction enters an exception handler.</summary>
        BranchIntoHandler,              // Branch into exception handler block.
        /// <summary>A branch instruction enters an exception filter.</summary>
        BranchIntoFilter,               // Branch into exception filter block.
        /// <summary>A branch instruction exits a try block.</summary>
        BranchOutOfTry,                 // Branch out of try block.
        /// <summary>A branch instruction exits an exception handler that is not a finally block.</summary>
        BranchOutOfHandler,             // Branch out of exception handler block.
        /// <summary>A branch instruction exits an exception filter.</summary>
        BranchOutOfFilter,              // Branch out of exception filter block.
        /// <summary>A branch instruction exits a finally block.</summary>
        BranchOutOfFinally,             // Branch out of finally block.
        /// <summary>A ret instruction is inside a try block.</summary>
        ReturnFromTry,                  // Return out of try block.
        /// <summary>A ret instruction is inside an exception handler.</summary>
        ReturnFromHandler,              // Return out of exception handler block.
        /// <summary>A ret instruction is inside an exception filter.</summary>
        ReturnFromFilter,               // Return out of exception filter block.
        /// <summary>A branch or leave instruction targets the middle of an instruction.</summary>
        BadJumpTarget,                  // Branch / Leave into the middle of an instruction.
        /// <summary>The stack holds incompatible types at a point that is reached by more than one path.</summary>
        PathStackUnexpected,            // Non-compatible types on stack depending on path.
        /// <summary>The stack depth differs at a point that is reached by more than one path.</summary>
        PathStackDepth,                 // Stack depth differs depending on path.
        /// <summary>
        /// A constructor stores to <c>this</c>, or takes its address, before <c>this</c> is initialized.
        /// </summary>
        ThisUninitStore,                // Store into this when it is uninitialized.
        /// <summary>A constructor returns before <c>this</c> is initialized.</summary>
        ThisUninitReturn,               // Return from .ctor when this is uninitialized.
        /// <summary>An ldftn or ldvirtftn instruction refers to a constructor.</summary>
        LdftnCtor,                      // ldftn/ldvirtftn not allowed on .ctor.
        /// <summary>
        /// A value on the stack has a type that the instruction does not accept, or an async method returns a type
        /// other than Task, ValueTask or their generic forms.
        /// </summary>
        StackUnexpected,                // Unexpected type on the stack.
        /// <summary>The element type of the array on the stack does not match the type operand of ldelema.</summary>
        StackUnexpectedArrayType,       // Unexpected array type on the stack.
        /// <summary>An instruction pushes more values than the maximum stack size of the method allows.</summary>
        StackOverflow,                  // Stack overflow.
        /// <summary>An instruction pops a value from an empty stack.</summary>
        StackUnderflow,                 // Stack underflow.
        /// <summary>An instruction uses <c>this</c> before the constructor initializes it.</summary>
        UninitStack,                    // Uninitialized item on stack.
        /// <summary>An instruction expects an integer on the stack.</summary>
        ExpectedIntegerType,            // Expected I, I4, or I8 on the stack.
        /// <summary>An instruction expects a floating-point number on the stack.</summary>
        ExpectedFloatType,              // Expected R, R4, or R8 on the stack.
        /// <summary>An instruction expects a number on the stack.</summary>
        ExpectedNumericType,            // Expected numeric type on the stack.
        /// <summary>An instruction expects an object reference on the stack.</summary>
        StackObjRef,                    // Expected an ObjRef on the stack.
        /// <summary>An instruction expects a managed pointer (ByRef) on the stack.</summary>
        StackByRef,                     // Expected ByRef on the stack.
        /// <summary>A delegate constructor does not get a method pointer from ldftn or ldvirtftn.</summary>
        StackMethod,                    // Expected pointer to function on the stack.
        /// <summary>An instruction refers to a local variable that the method does not declare.</summary>
        UnrecognizedLocalNumber,        // Unrecognized local variable number.
        /// <summary>An instruction refers to an argument that the method does not have.</summary>
        UnrecognizedArgumentNumber,     // Unrecognized argument number.
        /// <summary>A token that should refer to a type does not.</summary>
        ExpectedTypeToken,              // Expected type token.
        /// <summary>A token or metadata reference cannot be resolved.</summary>
        TokenResolve,                   // Unable to resolve token.
        /// <summary>The token of an instruction does not refer to a method.</summary>
        ExpectedMethodToken,            // Expected memberRef, memberDef or methodSpec token.
        /// <summary>The token of an instruction does not refer to a field.</summary>
        ExpectedFieldToken,             // Expected field token.
        /// <summary>
        /// The method uses an instruction that is never verifiable, such as jmp, localloc, cpblk or initblk.
        /// </summary>
        Unverifiable,                   // Instruction can not be verified.
        /// <summary>The token of an ldstr instruction does not refer to a string.</summary>
        StringOperand,                  // Operand does not point to a valid string ref.
        /// <summary>A method returns a ByRef or ByRef-like value that might point to its own stack frame.</summary>
        ReturnPtrToStack,               // Return type is ByRef, TypedReference, ArgHandle, or ArgIterator.
        /// <summary>
        /// Values remain on the stack at ret in a method that returns void, or in an async method that
        /// returns Task or ValueTask.
        /// </summary>
        ReturnVoid,                     // Stack must be empty on return from a void function.
        /// <summary>A method that returns a value has an empty stack at ret.</summary>
        ReturnMissing,                  // Return value missing on the stack.
        /// <summary>The stack holds more than the return value at ret.</summary>
        ReturnEmpty,                    // Stack must contain only the return value.
        /// <summary>An instruction expects a single-dimensional, zero-based array on the stack.</summary>
        ExpectedArray,                  // Expected single-dimension zero-based array.
        /// <summary>The type operand of an unbox instruction is not a value type.</summary>
        ValueTypeExpected,              // Value type expected.
        /// <summary>The method refers to a type that it cannot access.</summary>
        TypeAccess,                     // Type is not visible.
        /// <summary>The method refers to a method that it cannot access.</summary>
        MethodAccess,                   // Method is not visible.
        /// <summary>The method refers to a field that it cannot access.</summary>
        FieldAccess,                    // Field is not visible.
        /// <summary>A static field instruction refers to an instance field.</summary>
        ExpectedStaticField,            // Expected static field.
        /// <summary>
        /// An initonly field is written by stsfld or addressed by ldsflda outside its type's static constructor.
        /// Alternatively, stfld writes to an initonly field outside a constructor or init accessor of its type, or
        /// through an object other than <c>this</c>.
        /// </summary>
        InitOnly,                       // Cannot change initonly field outside its .ctor.
        /// <summary>A callvirt instruction calls a method of a value type.</summary>
        CallVirtOnValueType,            // Callvirt on a value type method.
        /// <summary>A newobj instruction refers to a method that is not a constructor.</summary>
        CtorExpected,                   // .ctor expected.
        /// <summary>A newobj instruction refers to a static or abstract method.</summary>
        CtorSig,                        // newobj on static or abstract method.
        /// <summary>An array is created with a ByRef element type.</summary>
        ArrayByRef,                     // Array of ELEMENT_TYPE_BYREF or ELEMENT_TYPE_TYPEDBYREF.
        /// <summary>An instruction takes the address of a local variable or argument that is already a ByRef.</summary>
        ByrefOfByref,                   // ByRef of ByRef.
        /// <summary>The method body has no IL.</summary>
        CodeSizeZero,                   // Code size is zero.
        /// <summary>A tail. prefix is not followed by an instruction that accepts it.</summary>
        TailCall,                       // Missing call/callvirt/calli.
        /// <summary>A tail call passes a ByRef argument, or is a constrained call.</summary>
        TailByRef,                      // Cannot pass ByRef to a tail call.
        /// <summary>A tail call is not followed by ret.</summary>
        TailRet,                        // tail.call may only be followed by ret.
        /// <summary>The tail-called method returns void but its caller does not, or the other way around.</summary>
        TailRetVoid,                    // Void ret type expected for tail call.
        /// <summary>The return type of a tail call is not compatible with the return type of the caller.</summary>
        TailRetType,                    // Tail call return type not compatible.
        /// <summary>The stack holds more than the arguments of a tail call.</summary>
        TailStackEmpty,                 // Stack not empty after tail call.
        /// <summary>The IL of the method ends in the middle of an instruction.</summary>
        MethodEnd,                      // Method ends in the middle of an instruction.
        /// <summary>A branch targets a location outside the IL of the method.</summary>
        BadBranch,                      // Branch out of the method.
        /// <summary>A volatile. prefix is not followed by an instruction that accepts it.</summary>
        Volatile,                       // Missing ldsfld, stsfld, ldind, stind, ldfld, stfld, ldobj, stobj, initblk, or cpblk.
        /// <summary>An unaligned. prefix is not followed by an instruction that accepts it.</summary>
        Unaligned,                      // Missing ldind, stind, ldfld, stfld, ldobj, stobj, initblk, cpblk.
        /// <summary>A call or ldftn instruction refers to an abstract method.</summary>
        CallAbstract,                   // Call not allowed on abstract methods.
        /// <summary>The stack is not empty on entry to a try block.</summary>
        TryNonEmptyStack,               // Attempt to enter a try block with nonempty stack.
        /// <summary>The stack on entry to a filter or catch handler does not hold exactly one value.</summary>
        FilterOrCatchUnexpectedStack,   // Attempt to enter a filter or catch block with unexpected stack state.
        /// <summary>The stack is not empty on entry to a finally or fault block.</summary>
        FinOrFaultNonEmptyStack,        // Attempt to enter a finally or fault block with nonempty stack.
        /// <summary>
        /// A delegate constructor has the wrong number of parameters, or its target method and object do
        /// not match the delegate type.
        /// </summary>
        DelegateCtor,                   // Unrecognized arguments for delegate .ctor.
        /// <summary>
        /// The newobj instruction that creates a delegate is not immediately preceded in the same basic block by either
        /// ldftn or the sequence dup, ldvirtftn.
        /// </summary>
        DelegatePattern,                // Dup, ldvirtftn, newobj delegate::.ctor() pattern expected (in the same basic block).
        /// <summary>The type operand of a box instruction is a ByRef type.</summary>
        BoxByRef,                       // Cannot box byref.
        /// <summary>The stack holds more than the filter result at endfilter.</summary>
        EndfilterStack,                 // Stack not empty when leaving an exception filter.
        /// <summary>The method pointer parameter of a delegate constructor is not a native int.</summary>
        DelegateCtorSigI,               // Unrecognized delegate .ctor signature; expected Native Int.
        /// <summary>The target object passed to a delegate constructor is not an object reference.</summary>
        DelegateCtorSigO,               // Unrecognized delegate .ctor signature; expected Object.
        /// <summary>The type of a catch clause is a ByRef type.</summary>
        CatchByRef,                     // ByRef not allowed as catch type.
        /// <summary>
        /// A thrown or caught type is neither <see cref="object"/> nor assignable to <see cref="System.Exception"/>.
        /// Reported only when <see cref="VerifierOptions.SanityChecks"/> is <see langword="true"/>.
        /// </summary>
        ThrowOrCatchOnlyExceptionType,  // The type caught or thrown must be derived from System.Exception.
        /// <summary>An ldvirtftn instruction refers to a static method.</summary>
        LdvirtftnOnStatic,              // ldvirtftn on static.
        /// <summary>A callvirt instruction refers to a static method.</summary>
        CallVirtOnStatic,               // callvirt on static.
        /// <summary>A method that does not set localsinit loads a local variable or takes its address.</summary>
        InitLocals,                     // initlocals must be set for verifiable methods with one or more local variables.
        /// <summary>
        /// A constructor call does not match either permitted form: initializing <c>this</c> with a constructor of the
        /// same type or its base type, or calling a value type constructor through a managed pointer.
        /// </summary>
        CallCtor,                       // call to .ctor only allowed to initialize this pointer from within a .ctor. Try newobj.

        ////@GENERICSVER: new generics related error messages
        /// <summary>
        /// The type operand of a box instruction is not a value type, reference type or generic parameter.
        /// </summary>
        ExpectedValClassObjRefVariable, // Value type, ObjRef type or variable type expected.
        /// <summary>A readonly. prefix is not followed by an instruction that accepts it.</summary>
        ReadOnly,                       // Missing ldelema or call following readonly prefix.
        /// <summary>A constrained. prefix is not followed by an instruction that accepts it.</summary>
        Constrained,                    // Missing callvirt following constrained prefix.

        /// <summary>
        /// The type arguments of a referenced generic method do not satisfy the method's constraints.
        /// </summary>
        UnsatisfiedMethodInst,                // Method instantiation has unsatisfied method type parameter constraints.
        /// <summary>
        /// The type arguments of the type that declares a referenced method do not satisfy that type's constraints.
        /// </summary>
        UnsatisfiedMethodParentInst,          // Method parent instantiation has unsatisfied class type parameter constraints.
        /// <summary>
        /// The type arguments of the type that declares a referenced field do not satisfy that type's constraints.
        /// </summary>
        UnsatisfiedFieldParentInst,           // Field parent instantiation has unsatisfied class type parameter constraints.
        /// <summary>
        /// The type arguments of the type operand of a box instruction do not satisfy that type's constraints.
        /// </summary>
        UnsatisfiedBoxOperand,                // Type operand of box instruction has unsatisfied class type parameter constraints.
        /// <summary>The <c>this</c> argument of a constrained call is not a ByRef.</summary>
        ConstrainedCallWithNonByRefThis,      // The 'this' argument to a constrained call must have ByRef type.
        /// <summary>A readonly. prefix is applied to a call that is not to an array method returning a ByRef.</summary>
        ReadonlyUnexpectedCallee,             // The readonly prefix may only be applied to calls to array methods returning ByRefs.
        /// <summary>An instruction writes through a readonly ByRef.</summary>
        ReadOnlyIllegalWrite,                 // Illegal write to readonly ByRef.
        /// <summary>A tail. prefix is inside a try block, filter or exception handler.</summary>
        TailCallInsideER,                     // The tail.call (or calli or callvirt) instruction cannot be used to transfer control out of a try, filter, catch, or finally block.
        /// <summary>The stack is not empty at the start of a block that is reached only by backward branches.</summary>
        BackwardBranch,                       // Stack height at all points must be determinable in a single forward scan of IL.
        /// <summary>A newobj instruction creates an instance of an abstract class.</summary>
        NewobjAbstractClass,                  // Cannot construct an instance of abstract class.
        /// <summary>An instruction loads a local variable or argument of an unmanaged pointer type.</summary>
        UnmanagedPointer,                     // Unmanaged pointers are not a verifiable type.
        /// <summary>
        /// A delegate for a non-final virtual method of an unsealed type is created with ldftn on an object that is
        /// neither a boxed value type nor the unmodified <c>this</c> argument. For this check, assigning to <c>this</c>
        /// or taking its address anywhere in the method counts as modifying it.
        /// </summary>
        LdftnNonFinalVirtual,                 // Cannot LDFTN a non-final virtual method for delegate creation if target object is potentially not the same type as the method class.
        /// <summary>
        /// A non-virtual call to a non-final virtual method of an unsealed type passes an object that is neither a
        /// boxed value type nor the unmodified <c>this</c> argument. For this check, assigning to <c>this</c> or taking
        /// its address anywhere in the method counts as modifying it.
        /// </summary>
        ThisMismatch,                         // The 'this' parameter to the call must be the calling method's 'this' parameter.

        /// <summary>A type lists the same interface implementation more than once.</summary>
        InterfaceImplHasDuplicate,            // InterfaceImpl has a duplicate
        /// <summary>
        /// A non-abstract type is missing an implementation of a method required by one of its interfaces.
        /// </summary>
        InterfaceMethodNotImplemented,         // Class implements interface but not method
        /// <summary>The stack holds more than the size argument at localloc.</summary>
        LocallocStackNotEmpty, // localloc requires that stack must be empty, except for 'size' argument
        /// <summary>
        /// A type has no base type when it needs one, or its base type is a value type, an interface or not a class.
        /// </summary>
        InvalidBaseType, // Type has an invalid base type.
        /// <summary>A TypeSpec signature is invalid, or a TypeSpec is used as a custom modifier.</summary>
        BadTypeSpec, // Invalid TypeSpec metadata.
        /// <summary>
        /// The type operand of a constrained. prefix does not implement the interface that declares
        /// the static virtual method.
        /// </summary>
        ConstrainedTypeNoInterfaceImpl, // The type operand of the constrained prefix must implement the interface declaring the static virtual method.
        /// <summary>
        /// A method implementation is static and the method that it implements is an instance method, or vice versa.
        /// </summary>
        MethodImplStaticMismatch, // The MethodImpl body and declaration must both be static or both be instance methods.
        /// <summary>An instance method that implements another method is not virtual.</summary>
        MethodImplBodyNotVirtual, // An instance MethodImpl body must be virtual.
        /// <summary>A try block, filter or handler of an exception handling clause is empty.</summary>
        EHClauseEmpty, // Exception handling region is empty.
        /// <summary>An exception handling region does not start or end on an instruction boundary.</summary>
        EHClauseBoundary, // Exception handling region does not start or end on an instruction boundary.
        /// <summary>Exception handling regions overlap, or share a region in a way that is not allowed.</summary>
        EHClauseOverlap, // Exception handling regions overlap or are shared illegally.
        /// <summary>
        /// The try block, filter and handler of a clause are not in the same enclosing region, or are inside a filter.
        /// </summary>
        EHClauseNesting, // The parts of an exception handling clause have inconsistent nesting.
        /// <summary>
        /// An exception handling clause nested in a try block comes after the clause of that try block.
        /// </summary>
        EHClauseOrder, // An exception handling clause nested in a try must precede that try's clause.
        /// <summary>A filter does not end with endfilter, or contains more than one endfilter.</summary>
        EHClauseFilterEnd, // A filter must have exactly one endfilter as its last instruction.
        UnalignedAlignment, // Alignment specified for 'unaligned.' must be 1, 2, or 4.
    }
}
