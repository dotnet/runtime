// This is auto generated file. Please don’t modify manually.
// The file is generated as part of the build through the ResourceGenerator tool 
// which takes the project resx resource file and generated this source code file.
// By default the tool will use Resources\Strings.resx but projects can customize
// that by overriding the StringResourcesPath property group.
namespace System
{
    internal static partial class SR
    {
#pragma warning disable 0414
        private const string s_resourcesName = "System.Threading.Tasks.Parallel.resources"; // assembly Name + .resources
#pragma warning restore 0414

#if !DEBUGRESOURCES
        internal static string Parallel_Invoke_ActionNull {
              get { return SR.GetResourceString("Parallel_Invoke_ActionNull", null); }
        }
        internal static string Parallel_ForEach_OrderedPartitionerKeysNotNormalized {
              get { return SR.GetResourceString("Parallel_ForEach_OrderedPartitionerKeysNotNormalized", null); }
        }
        internal static string Parallel_ForEach_PartitionerNotDynamic {
              get { return SR.GetResourceString("Parallel_ForEach_PartitionerNotDynamic", null); }
        }
        internal static string Parallel_ForEach_PartitionerReturnedNull {
              get { return SR.GetResourceString("Parallel_ForEach_PartitionerReturnedNull", null); }
        }
        internal static string Parallel_ForEach_NullEnumerator {
              get { return SR.GetResourceString("Parallel_ForEach_NullEnumerator", null); }
        }
        internal static string ParallelState_Break_InvalidOperationException_BreakAfterStop {
              get { return SR.GetResourceString("ParallelState_Break_InvalidOperationException_BreakAfterStop", null); }
        }
        internal static string ParallelState_Stop_InvalidOperationException_StopAfterBreak {
              get { return SR.GetResourceString("ParallelState_Stop_InvalidOperationException_StopAfterBreak", null); }
        }
        internal static string ParallelState_NotSupportedException_UnsupportedMethod {
              get { return SR.GetResourceString("ParallelState_NotSupportedException_UnsupportedMethod", null); }
        }
#else
        internal static string Parallel_Invoke_ActionNull {
              get { return SR.GetResourceString("Parallel_Invoke_ActionNull", @"One of the actions was null."); }
        }
        internal static string Parallel_ForEach_OrderedPartitionerKeysNotNormalized {
              get { return SR.GetResourceString("Parallel_ForEach_OrderedPartitionerKeysNotNormalized", @"This method requires the use of an OrderedPartitioner with the KeysNormalized property set to true."); }
        }
        internal static string Parallel_ForEach_PartitionerNotDynamic {
              get { return SR.GetResourceString("Parallel_ForEach_PartitionerNotDynamic", @"The Partitioner used here must support dynamic partitioning."); }
        }
        internal static string Parallel_ForEach_PartitionerReturnedNull {
              get { return SR.GetResourceString("Parallel_ForEach_PartitionerReturnedNull", @"The Partitioner used here returned a null partitioner source."); }
        }
        internal static string Parallel_ForEach_NullEnumerator {
              get { return SR.GetResourceString("Parallel_ForEach_NullEnumerator", @"The Partitioner source returned a null enumerator."); }
        }
        internal static string ParallelState_Break_InvalidOperationException_BreakAfterStop {
              get { return SR.GetResourceString("ParallelState_Break_InvalidOperationException_BreakAfterStop", @"Break was called after Stop was called."); }
        }
        internal static string ParallelState_Stop_InvalidOperationException_StopAfterBreak {
              get { return SR.GetResourceString("ParallelState_Stop_InvalidOperationException_StopAfterBreak", @"Stop was called after Break was called."); }
        }
        internal static string ParallelState_NotSupportedException_UnsupportedMethod {
              get { return SR.GetResourceString("ParallelState_NotSupportedException_UnsupportedMethod", @"This method is not supported."); }
        }

#endif
    }
}
