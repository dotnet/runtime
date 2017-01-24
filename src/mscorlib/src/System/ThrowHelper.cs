// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System {
    // This file defines an internal class used to throw exceptions in BCL code.
    // The main purpose is to reduce code size. 
    // 
    // The old way to throw an exception generates quite a lot IL code and assembly code.
    // Following is an example:
    //     C# source
    //          throw new ArgumentNullException(nameof(key), Environment.GetResourceString("ArgumentNull_Key"));
    //     IL code:
    //          IL_0003:  ldstr      "key"
    //          IL_0008:  ldstr      "ArgumentNull_Key"
    //          IL_000d:  call       string System.Environment::GetResourceString(string)
    //          IL_0012:  newobj     instance void System.ArgumentNullException::.ctor(string,string)
    //          IL_0017:  throw
    //    which is 21bytes in IL.
    // 
    // So we want to get rid of the ldstr and call to Environment.GetResource in IL.
    // In order to do that, I created two enums: ExceptionResource, ExceptionArgument to represent the
    // argument name and resource name in a small integer. The source code will be changed to 
    //    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key, ExceptionResource.ArgumentNull_Key);
    //
    // The IL code will be 7 bytes.
    //    IL_0008:  ldc.i4.4
    //    IL_0009:  ldc.i4.4
    //    IL_000a:  call       void System.ThrowHelper::ThrowArgumentNullException(valuetype System.ExceptionArgument)
    //    IL_000f:  ldarg.0
    //
    // This will also reduce the Jitted code size a lot. 
    //
    // It is very important we do this for generic classes because we can easily generate the same code 
    // multiple times for different instantiation. 
    // 

    using Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;

    [Pure]
    internal static class ThrowHelper {    
        internal static void ThrowArrayTypeMismatchException() {
            throw new ArrayTypeMismatchException();
        }

        internal static void ThrowInvalidTypeWithPointersNotSupported(Type targetType) {
            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidTypeWithPointersNotSupported", targetType));
        }

        internal static void ThrowIndexOutOfRangeException() {
            throw new IndexOutOfRangeException();
        }

        internal static void ThrowArgumentOutOfRangeException() {
            throw new ArgumentOutOfRangeException();
        }

        internal static void ThrowArgumentException_DestinationTooShort() {
            throw new ArgumentException(Environment.GetResourceString("Argument_DestinationTooShort"));
        }

        internal static void ThrowNotSupportedException_CannotCallEqualsOnSpan() {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_CannotCallEqualsOnSpan"));
        }

        internal static void ThrowNotSupportedException_CannotCallGetHashCodeOnSpan() {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_CannotCallGetHashCodeOnSpan"));
        }

        internal static void ThrowArgumentOutOfRange_IndexException() {
            throw GetArgumentOutOfRangeException(ExceptionArgument.index, 
                                                    ExceptionResource.ArgumentOutOfRange_Index);
        }

        internal static void ThrowCountArgumentOutOfRange_NeedNonNegNumException() {
            throw GetArgumentOutOfRangeException(ExceptionArgument.count,
                                                    ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
        }

        internal static void ThrowIndexArgumentOutOfRange_NeedNonNegNumException() {
            throw GetArgumentOutOfRangeException(ExceptionArgument.index, 
                                                    ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
        }

        internal static void ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum() {
            throw GetArgumentOutOfRangeException(ExceptionArgument.length,
                                                    ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
        }

        internal static void ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_Index() {
            throw GetArgumentOutOfRangeException(ExceptionArgument.startIndex,
                                                    ExceptionResource.ArgumentOutOfRange_Index);
        }

        internal static void ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count() {
            throw GetArgumentOutOfRangeException(ExceptionArgument.count,
                                                    ExceptionResource.ArgumentOutOfRange_Count);
        }

        internal static void ThrowWrongKeyTypeArgumentException(object key, Type targetType) {
            throw GetWrongKeyTypeArgumentException(key, targetType);
        }

        internal static void ThrowWrongValueTypeArgumentException(object value, Type targetType) {
            throw GetWrongValueTypeArgumentException(value, targetType);
        }

        private static ArgumentException GetAddingDuplicateWithKeyArgumentException(object key) {
            return new ArgumentException(Environment.GetResourceString("Argument_AddingDuplicateWithKey", key));
        }

        internal static void ThrowAddingDuplicateWithKeyArgumentException(object key) {
            throw GetAddingDuplicateWithKeyArgumentException(key);
        }

        internal static void ThrowKeyNotFoundException() {
            throw new System.Collections.Generic.KeyNotFoundException();
        }
        
        internal static void ThrowArgumentException(ExceptionResource resource) {
            throw GetArgumentException(resource);
        }

        internal static void ThrowArgumentException(ExceptionResource resource, ExceptionArgument argument) {
            throw GetArgumentException(resource, argument);
        }

        internal static void ThrowArgumentNullException(ExceptionArgument argument) {
            throw new ArgumentNullException(GetArgumentName(argument));
        }

        internal static void ThrowArgumentNullException(ExceptionResource resource) {
            throw new ArgumentNullException(GetResourceString(resource));
        }

        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument) {
            throw new ArgumentOutOfRangeException(GetArgumentName(argument));
        }

        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument, ExceptionResource resource) {
            throw GetArgumentOutOfRangeException(argument, resource);
        }

        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument, int paramNumber, ExceptionResource resource) {
            throw GetArgumentOutOfRangeException(argument, paramNumber, resource);
        }

        internal static void ThrowInvalidOperationException(ExceptionResource resource) {
            throw GetInvalidOperationException(resource);
        }

        internal static void ThrowInvalidOperationException(ExceptionResource resource, Exception e) {
            throw new InvalidOperationException(GetResourceString(resource), e);
        }

        internal static void ThrowSerializationException(ExceptionResource resource) {
            throw new SerializationException(GetResourceString(resource));
        }

        internal static void  ThrowSecurityException(ExceptionResource resource) {
            throw new System.Security.SecurityException(GetResourceString(resource));
        }

        internal static void ThrowRankException(ExceptionResource resource) {
            throw new RankException(GetResourceString(resource));
        }

        internal static void ThrowNotSupportedException(ExceptionResource resource) {
            throw new NotSupportedException(GetResourceString(resource));
        }

        internal static void ThrowUnauthorizedAccessException(ExceptionResource resource) {
            throw new UnauthorizedAccessException(GetResourceString(resource));
        }

        internal static void ThrowObjectDisposedException(string objectName, ExceptionResource resource) {
            throw new ObjectDisposedException(objectName, GetResourceString(resource));
        }

        internal static void ThrowObjectDisposedException(ExceptionResource resource) {
            throw new ObjectDisposedException(null, GetResourceString(resource));
        }

        internal static void ThrowNotSupportedException() {
            throw new NotSupportedException();
        }

        internal static void ThrowAggregateException(List<Exception> exceptions) {
            throw new AggregateException(exceptions);
        }

        internal static void ThrowArgumentException_Argument_InvalidArrayType() {
            throw GetArgumentException(ExceptionResource.Argument_InvalidArrayType);
        }

        internal static void ThrowInvalidOperationException_InvalidOperation_EnumNotStarted() {
            throw GetInvalidOperationException(ExceptionResource.InvalidOperation_EnumNotStarted);
        }

        internal static void ThrowInvalidOperationException_InvalidOperation_EnumEnded() {
            throw GetInvalidOperationException(ExceptionResource.InvalidOperation_EnumEnded);
        }

        internal static void ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion() {
            throw GetInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
        }

        internal static ArgumentNullException GetArgumentNullException(ExceptionArgument argument, ExceptionResource resource) {
            throw new ArgumentNullException(GetArgumentName(argument), GetResourceString(resource));
        }

        internal static void ThrowArgumentNullException(ExceptionArgument argument, ExceptionResource resource) {
            throw GetArgumentNullException(argument, resource);
        }

        internal static void ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen() {
            throw GetInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
        }

        private static ArgumentException GetArgumentException(ExceptionResource resource) {
            return new ArgumentException(GetResourceString(resource));
        }

        private static InvalidOperationException GetInvalidOperationException(ExceptionResource resource) {
            return new InvalidOperationException(GetResourceString(resource));
        }

        private static ArgumentException GetWrongKeyTypeArgumentException(object key, Type targetType) {
            return new ArgumentException(Environment.GetResourceString("Arg_WrongType", key, targetType), nameof(key));
        }

        private static ArgumentException GetWrongValueTypeArgumentException(object value, Type targetType) {
            return new ArgumentException(Environment.GetResourceString("Arg_WrongType", value, targetType), nameof(value));
        }

        internal static ArgumentOutOfRangeException GetArgumentOutOfRangeException(ExceptionArgument argument, ExceptionResource resource) {
            return new ArgumentOutOfRangeException(GetArgumentName(argument), GetResourceString(resource));
        }

        private static ArgumentException GetArgumentException(ExceptionResource resource, ExceptionArgument argument) {
            return new ArgumentException(GetResourceString(resource), GetArgumentName(argument));
        }

        private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(ExceptionArgument argument, int paramNumber, ExceptionResource resource) {
            return new ArgumentOutOfRangeException(GetArgumentName(argument) + "[" + paramNumber.ToString() + "]", GetResourceString(resource));
        }

        // Allow nulls for reference types and Nullable<U>, but not for value types.
        // Aggressively inline so the jit evaluates the if in place and either drops the call altogether
        // Or just leaves null test and call to the Non-returning ThrowHelper.ThrowArgumentNullException
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void IfNullAndNullsAreIllegalThenThrow<T>(object value, ExceptionArgument argName) {
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>. 
            if (!(default(T) == null) && value == null)
                ThrowHelper.ThrowArgumentNullException(argName);
        }

        // This function will convert an ExceptionArgument enum value to the argument name string.
        private static string GetArgumentName(ExceptionArgument argument) {
            // This is indirected through a second NoInlining function it has a special meaning
            // in System.Private.CoreLib of indicatating it takes a StackMark which cause 
            // the caller to also be not inlined; so we can't mark it directly.
            // So is the effect of marking this function as non-inlining in a regular situation.
            return GetArgumentNameInner(argument);
        }

        // This function will convert an ExceptionArgument enum value to the argument name string.
        // Second function in chain so as to not propergate the non-inlining to outside caller
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetArgumentNameInner(ExceptionArgument argument) {
            Debug.Assert(Enum.IsDefined(typeof(ExceptionArgument), argument),
                "The enum value is not defined, please check the ExceptionArgument Enum.");

            return argument.ToString();
        }

        // This function will convert an ExceptionResource enum value to the resource string.
        private static string GetResourceString(ExceptionResource resource) {
            // This is indirected through a second NoInlining function it has a special meaning
            // in System.Private.CoreLib of indicatating it takes a StackMark which cause 
            // the caller to also be not inlined; so we can't mark it directly.
            // So is the effect of marking this function as non-inlining in a regular situation.
            return GetResourceStringInner(resource);
        }

        // This function will convert an ExceptionResource enum value to the resource string.
        // Second function in chain so as to not propergate the non-inlining to outside caller
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetResourceStringInner(ExceptionResource resource) {
            Debug.Assert(Enum.IsDefined(typeof(ExceptionResource), resource),
                "The enum value is not defined, please check the ExceptionResource Enum.");

            return Environment.GetResourceString(resource.ToString());
        }
    }

    //
    // The convention for this enum is using the argument name as the enum name
    // 
    internal enum ExceptionArgument {
        obj,
        dictionary,
        dictionaryCreationThreshold,
        array,
        info,
        key,
        collection,
        list,
        match,
        converter,
        queue,
        stack,
        capacity,
        index,
        startIndex,
        value,
        count,
        arrayIndex,
        name,
        mode,
        item,
        options,
        view,
        sourceBytesToCopy,
        action,
        comparison,
        offset,
        newSize,
        elementType,
        length,
        length1,
        length2,
        length3,
        lengths,
        len,
        lowerBounds,
        sourceArray,
        destinationArray,
        sourceIndex,
        destinationIndex,
        indices,
        index1,
        index2,
        index3,
        other,
        comparer,
        endIndex,
        keys,
        creationOptions,
        timeout,
        tasks,
        scheduler,
        continuationFunction,
        millisecondsTimeout,
        millisecondsDelay,
        function,
        exceptions,
        exception,
        cancellationToken,
        delay,
        asyncResult,
        endMethod,
        endFunction,
        beginMethod,
        continuationOptions,
        continuationAction,
        valueFactory,
        addValueFactory,
        updateValueFactory,
        concurrencyLevel,
        text,
        s,
        chars,
        bytes,
        byteIndex,
        charIndex,
        byteCount,
        charCount,

    }

    //
    // The convention for this enum is using the resource name as the enum name
    // 
    internal enum ExceptionResource {
        Argument_ImplementIComparable,
        Argument_InvalidType,     
        Argument_InvalidArgumentForComparison,
        Argument_InvalidRegistryKeyPermissionCheck,        
        ArgumentOutOfRange_NeedNonNegNum,
        
        Arg_ArrayPlusOffTooSmall,
        Arg_NonZeroLowerBound,        
        Arg_RankMultiDimNotSupported,        
        Arg_RegKeyDelHive,
        Arg_RegKeyStrLenBug,  
        Arg_RegSetStrArrNull,
        Arg_RegSetMismatchedKind,
        Arg_RegSubKeyAbsent,        
        Arg_RegSubKeyValueAbsent,
        
        Argument_AddingDuplicate,
        Serialization_InvalidOnDeser,
        Serialization_MissingKeys,
        Serialization_NullKey,
        Argument_InvalidArrayType,
        NotSupported_KeyCollectionSet,
        NotSupported_ValueCollectionSet,
        ArgumentOutOfRange_SmallCapacity,
        ArgumentOutOfRange_Index,
        Argument_InvalidOffLen,
        Argument_ItemNotExist,
        ArgumentOutOfRange_Count,
        ArgumentOutOfRange_InvalidThreshold,
        ArgumentOutOfRange_ListInsert,
        NotSupported_ReadOnlyCollection,
        InvalidOperation_CannotRemoveFromStackOrQueue,
        InvalidOperation_EmptyQueue,
        InvalidOperation_EnumOpCantHappen,
        InvalidOperation_EnumFailedVersion,
        InvalidOperation_EmptyStack,
        ArgumentOutOfRange_BiggerThanCollection,
        InvalidOperation_EnumNotStarted,
        InvalidOperation_EnumEnded,
        NotSupported_SortedListNestedWrite,
        InvalidOperation_NoValue,
        InvalidOperation_RegRemoveSubKey,
        Security_RegistryPermission,
        UnauthorizedAccess_RegistryNoWrite,
        ObjectDisposed_RegKeyClosed,
        NotSupported_InComparableType,
        Argument_InvalidRegistryOptionsCheck,
        Argument_InvalidRegistryViewCheck,
        InvalidOperation_NullArray,
        Arg_MustBeType,
        Arg_NeedAtLeast1Rank,
        ArgumentOutOfRange_HugeArrayNotSupported,
        Arg_RanksAndBounds,
        Arg_RankIndices,
        Arg_Need1DArray,
        Arg_Need2DArray,
        Arg_Need3DArray,
        NotSupported_FixedSizeCollection,
        ArgumentException_OtherNotArrayOfCorrectLength,
        Rank_MultiDimNotSupported,
        InvalidOperation_IComparerFailed,
        ArgumentOutOfRange_EndIndexStartIndex,
        Arg_LowerBoundsMustMatch,
        Arg_BogusIComparer,
        Task_WaitMulti_NullTask,
        Task_ThrowIfDisposed,
        Task_Start_TaskCompleted,
        Task_Start_Promise,
        Task_Start_ContinuationTask,
        Task_Start_AlreadyStarted,
        Task_RunSynchronously_TaskCompleted,
        Task_RunSynchronously_Continuation,
        Task_RunSynchronously_Promise,
        Task_RunSynchronously_AlreadyStarted,
        Task_MultiTaskContinuation_NullTask,
        Task_MultiTaskContinuation_EmptyTaskList,
        Task_Dispose_NotCompleted,
        Task_Delay_InvalidMillisecondsDelay,
        Task_Delay_InvalidDelay,
        Task_ctor_LRandSR,
        Task_ContinueWith_NotOnAnything,
        Task_ContinueWith_ESandLR,
        TaskT_TransitionToFinal_AlreadyCompleted,
        TaskT_ctor_SelfReplicating,
        TaskCompletionSourceT_TrySetException_NullException,
        TaskCompletionSourceT_TrySetException_NoExceptions,
        InvalidOperation_WrongAsyncResultOrEndCalledMultiple,
        ConcurrentDictionary_ConcurrencyLevelMustBePositive,
        ConcurrentDictionary_CapacityMustNotBeNegative,
        ConcurrentDictionary_TypeOfValueIncorrect,
        ConcurrentDictionary_TypeOfKeyIncorrect,
        ConcurrentDictionary_SourceContainsDuplicateKeys,
        ConcurrentDictionary_KeyAlreadyExisted,
        ConcurrentDictionary_ItemKeyIsNull,
        ConcurrentDictionary_IndexIsNegative,
        ConcurrentDictionary_ArrayNotLargeEnough,
        ConcurrentDictionary_ArrayIncorrectType,
        ConcurrentCollection_SyncRoot_NotSupported,
        ArgumentNull_Array,
        ArgumentOutOfRange_IndexCountBuffer,
        ArgumentNull_String,

    }
}

