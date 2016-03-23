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
        internal static void ThrowArgumentOutOfRangeException() {        
            ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);            
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

        internal static void ThrowInvalidOperationException(ExceptionResource resource) {
            throw new InvalidOperationException(Environment.GetResourceString(GetResourceName(resource)));
        }

        internal static void ThrowSerializationException(ExceptionResource resource) {
            throw new SerializationException(Environment.GetResourceString(GetResourceName(resource)));
        }

        internal static void  ThrowSecurityException(ExceptionResource resource) {
            throw new System.Security.SecurityException(Environment.GetResourceString(GetResourceName(resource)));
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

        // Allow nulls for reference types and Nullable<U>, but not for value types.
        internal static void IfNullAndNullsAreIllegalThenThrow<T>(object value, ExceptionArgument argName) {
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>. 
            if (value == null && !(default(T) == null))
                ThrowHelper.ThrowArgumentNullException(argName);
        }

        //
        // This function will convert an ExceptionArgument enum value to the argument name string.
        //
        internal static string GetArgumentName(ExceptionArgument argument) {
            string argumentName = null;

            switch (argument) {
                case ExceptionArgument.action:
                    argumentName = "action";
                    break;

                case ExceptionArgument.array:
                    argumentName = "array";
                    break;

                case ExceptionArgument.arrayIndex:
                    argumentName = "arrayIndex";
                    break;

                case ExceptionArgument.capacity:
                    argumentName = "capacity";
                    break;

                case ExceptionArgument.collection:
                    argumentName = "collection";
                    break;

                case ExceptionArgument.comparison:
                    argumentName = "comparison";
                    break;

                case ExceptionArgument.list:
                    argumentName = "list";
                    break;

                case ExceptionArgument.converter:
                    argumentName = "converter";
                    break;

                case ExceptionArgument.count:
                    argumentName = "count";
                    break;

                case ExceptionArgument.dictionary:
                    argumentName = "dictionary";
                    break;

                case ExceptionArgument.dictionaryCreationThreshold:
                    argumentName = "dictionaryCreationThreshold";
                    break;

                case ExceptionArgument.index:
                    argumentName = "index";
                    break;

                case ExceptionArgument.info:
                    argumentName = "info";
                    break;

                case ExceptionArgument.key:
                    argumentName = "key";
                    break;

                case ExceptionArgument.match:
                    argumentName = "match";
                    break;

                case ExceptionArgument.obj:
                    argumentName = "obj";
                    break;

                case ExceptionArgument.queue:
                    argumentName = "queue";
                    break;

                case ExceptionArgument.stack:
                    argumentName = "stack";
                    break;

                case ExceptionArgument.startIndex:
                    argumentName = "startIndex";
                    break;

                case ExceptionArgument.value:
                    argumentName = "value";
                    break;

                case ExceptionArgument.name:
                    argumentName = "name";
                    break;

                case ExceptionArgument.mode:
                    argumentName = "mode";
                    break;

                case ExceptionArgument.item:
                    argumentName = "item";
                    break;

                case ExceptionArgument.options:
                    argumentName = "options";
                    break;

                case ExceptionArgument.view:
                    argumentName = "view";
                    break;

               case ExceptionArgument.sourceBytesToCopy:
                    argumentName = "sourceBytesToCopy";
                    break;

                default:
                    Contract.Assert(false, "The enum value is not defined, please checked ExceptionArgumentName Enum.");
                    return string.Empty;
            }

            return argumentName;
        }

        //
        // This function will convert an ExceptionResource enum value to the resource string.
        //
        internal static string GetResourceName(ExceptionResource resource) {
            string resourceName = null;

            switch (resource) {
                case ExceptionResource.Argument_ImplementIComparable:
                    resourceName = "Argument_ImplementIComparable";
                    break;

                case ExceptionResource.Argument_AddingDuplicate:
                    resourceName = "Argument_AddingDuplicate";
                    break;

                case ExceptionResource.ArgumentOutOfRange_BiggerThanCollection:
                    resourceName = "ArgumentOutOfRange_BiggerThanCollection";
                    break;

                case ExceptionResource.ArgumentOutOfRange_Count:
                    resourceName = "ArgumentOutOfRange_Count";
                    break;

                case ExceptionResource.ArgumentOutOfRange_Index:
                    resourceName = "ArgumentOutOfRange_Index";
                    break;

                case ExceptionResource.ArgumentOutOfRange_InvalidThreshold:
                    resourceName = "ArgumentOutOfRange_InvalidThreshold";
                    break;

                case ExceptionResource.ArgumentOutOfRange_ListInsert:
                    resourceName = "ArgumentOutOfRange_ListInsert";
                    break;

                case ExceptionResource.ArgumentOutOfRange_NeedNonNegNum:
                    resourceName = "ArgumentOutOfRange_NeedNonNegNum";
                    break;

                case ExceptionResource.ArgumentOutOfRange_SmallCapacity:
                    resourceName = "ArgumentOutOfRange_SmallCapacity";
                    break;

                case ExceptionResource.Arg_ArrayPlusOffTooSmall:
                    resourceName = "Arg_ArrayPlusOffTooSmall";
                    break;

                case ExceptionResource.Arg_RankMultiDimNotSupported:
                    resourceName = "Arg_RankMultiDimNotSupported";
                    break;

                case ExceptionResource.Arg_NonZeroLowerBound:
                    resourceName = "Arg_NonZeroLowerBound";
                    break;

                case ExceptionResource.Argument_InvalidArrayType:
                    resourceName = "Argument_InvalidArrayType";
                    break;

                case ExceptionResource.Argument_InvalidOffLen:
                    resourceName = "Argument_InvalidOffLen";
                    break;

                case ExceptionResource.Argument_ItemNotExist:
                    resourceName = "Argument_ItemNotExist";
                    break;                    

                case ExceptionResource.InvalidOperation_CannotRemoveFromStackOrQueue:
                    resourceName = "InvalidOperation_CannotRemoveFromStackOrQueue";
                    break;

                case ExceptionResource.InvalidOperation_EmptyQueue:
                    resourceName = "InvalidOperation_EmptyQueue";
                    break;

                case ExceptionResource.InvalidOperation_EnumOpCantHappen:
                    resourceName = "InvalidOperation_EnumOpCantHappen";
                    break;

                case ExceptionResource.InvalidOperation_EnumFailedVersion:
                    resourceName = "InvalidOperation_EnumFailedVersion";
                    break;

                case ExceptionResource.InvalidOperation_EmptyStack:
                    resourceName = "InvalidOperation_EmptyStack";
                    break;

                case ExceptionResource.InvalidOperation_EnumNotStarted:
                    resourceName = "InvalidOperation_EnumNotStarted";
                    break;

                case ExceptionResource.InvalidOperation_EnumEnded:
                    resourceName = "InvalidOperation_EnumEnded";
                    break;

                case ExceptionResource.NotSupported_KeyCollectionSet:
                    resourceName = "NotSupported_KeyCollectionSet";
                    break;

                case ExceptionResource.NotSupported_ReadOnlyCollection:
                    resourceName = "NotSupported_ReadOnlyCollection";
                    break;

                case ExceptionResource.NotSupported_ValueCollectionSet:
                    resourceName = "NotSupported_ValueCollectionSet";
                    break;


                case ExceptionResource.NotSupported_SortedListNestedWrite:
                    resourceName = "NotSupported_SortedListNestedWrite";
                    break;


                case ExceptionResource.Serialization_InvalidOnDeser:
                    resourceName = "Serialization_InvalidOnDeser";
                    break;

                case ExceptionResource.Serialization_MissingKeys:
                    resourceName = "Serialization_MissingKeys";
                    break;

                case ExceptionResource.Serialization_NullKey:
                    resourceName = "Serialization_NullKey";
                    break;

                case ExceptionResource.Argument_InvalidType:
                    resourceName = "Argument_InvalidType";
                    break;

                case ExceptionResource.Argument_InvalidArgumentForComparison:
                    resourceName = "Argument_InvalidArgumentForComparison";                    
                    break;

                case ExceptionResource.InvalidOperation_NoValue:
                    resourceName = "InvalidOperation_NoValue";                    
                    break;

                case ExceptionResource.InvalidOperation_RegRemoveSubKey:
                    resourceName = "InvalidOperation_RegRemoveSubKey";                    
                    break;

                case ExceptionResource.Arg_RegSubKeyAbsent:
                    resourceName = "Arg_RegSubKeyAbsent";                    
                    break;

                case ExceptionResource.Arg_RegSubKeyValueAbsent:
                    resourceName = "Arg_RegSubKeyValueAbsent";                    
                    break;
                    
                case ExceptionResource.Arg_RegKeyDelHive:
                    resourceName = "Arg_RegKeyDelHive";                    
                    break;

                case ExceptionResource.Security_RegistryPermission:
                    resourceName = "Security_RegistryPermission";                    
                    break;

                case ExceptionResource.Arg_RegSetStrArrNull:
                    resourceName = "Arg_RegSetStrArrNull";                    
                    break;

                case ExceptionResource.Arg_RegSetMismatchedKind:
                    resourceName = "Arg_RegSetMismatchedKind";                    
                    break;

                case ExceptionResource.UnauthorizedAccess_RegistryNoWrite:
                    resourceName = "UnauthorizedAccess_RegistryNoWrite";
                    break;

                case ExceptionResource.ObjectDisposed_RegKeyClosed:
                    resourceName = "ObjectDisposed_RegKeyClosed";
                    break;

                case ExceptionResource.Arg_RegKeyStrLenBug:
                    resourceName = "Arg_RegKeyStrLenBug";
                    break;

                case ExceptionResource.Argument_InvalidRegistryKeyPermissionCheck:
                    resourceName = "Argument_InvalidRegistryKeyPermissionCheck";
                    break;

                case ExceptionResource.NotSupported_InComparableType:
                    resourceName = "NotSupported_InComparableType";
                    break;

                case ExceptionResource.Argument_InvalidRegistryOptionsCheck:
                    resourceName = "Argument_InvalidRegistryOptionsCheck";
                    break;

                case ExceptionResource.Argument_InvalidRegistryViewCheck:
                    resourceName = "Argument_InvalidRegistryViewCheck";
                    break;

                default:
                    Contract.Assert( false, "The enum value is not defined, please checked ExceptionArgumentName Enum.");
                    return string.Empty;
            }

            return resourceName;
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
        comparison
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
        Argument_InvalidRegistryViewCheck
    }
}

