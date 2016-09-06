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
    //          throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
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

    using System.Runtime.CompilerServices;        
    using System.Runtime.Serialization;
    using System.Diagnostics.Contracts;

    [Pure]
    internal static class ThrowHelper {    
        internal static void ThrowArgumentOutOfRange_IndexException() {
            throw new ArgumentOutOfRangeException(GetArgumentName(ExceptionArgument.index),
                                                    Environment.GetResourceString(GetResourceName(ExceptionResource.ArgumentOutOfRange_Index)));
        }

        internal static void ThrowIndexArgumentOutOfRange_NeedNonNegNumException() {
            throw new ArgumentOutOfRangeException(
                        GetArgumentName(ExceptionArgument.index),
                        Environment.GetResourceString(GetResourceName(ExceptionResource.ArgumentOutOfRange_NeedNonNegNum)));
        }

        internal static void ThrowWrongKeyTypeArgumentException(object key, Type targetType) {
            throw new ArgumentException(Environment.GetResourceString("Arg_WrongType", key, targetType), "key");
        }

        internal static void ThrowWrongValueTypeArgumentException(object value, Type targetType) {
            throw new ArgumentException(Environment.GetResourceString("Arg_WrongType", value, targetType), "value");
        }

#if FEATURE_CORECLR
        internal static void ThrowAddingDuplicateWithKeyArgumentException(object key) {
            throw new ArgumentException(Environment.GetResourceString("Argument_AddingDuplicateWithKey", key));
        }
#endif

        internal static void ThrowKeyNotFoundException() {
            throw new System.Collections.Generic.KeyNotFoundException();
        }
        
        internal static void ThrowArgumentException(ExceptionResource resource) {
            throw new ArgumentException(Environment.GetResourceString(GetResourceName(resource)));
        }

        internal static void ThrowArgumentException(ExceptionResource resource, ExceptionArgument argument) {
            throw new ArgumentException(Environment.GetResourceString(GetResourceName(resource)), GetArgumentName(argument));
        }

        internal static void ThrowArgumentNullException(ExceptionArgument argument) {
            throw new ArgumentNullException(GetArgumentName(argument));
        }

        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument) {
            throw new ArgumentOutOfRangeException(GetArgumentName(argument));
        }

        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument, ExceptionResource resource) {
            throw new ArgumentOutOfRangeException(GetArgumentName(argument),
                                                    Environment.GetResourceString(GetResourceName(resource)));
        }

        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument, int paramNumber, ExceptionResource resource) {
            throw new ArgumentOutOfRangeException(GetArgumentName(argument) + "[" + paramNumber.ToString() + "]",
                                                    Environment.GetResourceString(GetResourceName(resource)));
        }

        internal static void ThrowInvalidOperationException(ExceptionResource resource) {
            throw new InvalidOperationException(Environment.GetResourceString(GetResourceName(resource)));
        }

        internal static void ThrowInvalidOperationException(ExceptionResource resource, Exception e) {
            throw new InvalidOperationException(Environment.GetResourceString(GetResourceName(resource)), e);
        }

        internal static void ThrowSerializationException(ExceptionResource resource) {
            throw new SerializationException(Environment.GetResourceString(GetResourceName(resource)));
        }

        internal static void  ThrowSecurityException(ExceptionResource resource) {
            throw new System.Security.SecurityException(Environment.GetResourceString(GetResourceName(resource)));
        }

        internal static void ThrowRankException(ExceptionResource resource) {
            throw new RankException(Environment.GetResourceString(GetResourceName(resource)));
        }

        internal static void ThrowNotSupportedException(ExceptionResource resource) {
            throw new NotSupportedException(Environment.GetResourceString(GetResourceName(resource)));
        }

        internal static void ThrowUnauthorizedAccessException(ExceptionResource resource) {
            throw new UnauthorizedAccessException(Environment.GetResourceString(GetResourceName(resource)));
        }

        internal static void ThrowObjectDisposedException(string objectName, ExceptionResource resource) {
            throw new ObjectDisposedException(objectName, Environment.GetResourceString(GetResourceName(resource)));
        }

        internal static void ThrowNotSupportedException()
        {
            throw new NotSupportedException();
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

        //
        // This function will convert an ExceptionArgument enum value to the argument name string.
        //
        internal static string GetArgumentName(ExceptionArgument argument) {
            Contract.Assert(Enum.IsDefined(typeof(ExceptionArgument), argument),
                "The enum value is not defined, please check the ExceptionArgument Enum.");

            return argument.ToString();
        }

        //
        // This function will convert an ExceptionResource enum value to the resource string.
        //
        internal static string GetResourceName(ExceptionResource resource) {
            Contract.Assert(Enum.IsDefined(typeof(ExceptionResource), resource),
                "The enum value is not defined, please check the ExceptionResource Enum.");

            return resource.ToString();
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

    }
}

