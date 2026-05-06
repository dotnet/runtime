
using Internal.IL.Stubs;
using Internal.JitInterface;
using Internal.TypeSystem;

namespace ILCompiler.ReadyToRun.TypeSystem
{
    internal static class MethodDescExtensions
    {
        /// <summary>
        /// For methods that have multiple MethodDescs representing the same logical method (e.g. async methods or unboxing stubs),
        /// gets the primary MethodDesc the metadata encodes for.
        /// </summary>
        public static MethodDesc GetPrimaryMethodDesc(this MethodDesc method)
        {
            if (method.IsAsyncVariant())
            {
                return method.GetTargetOfAsyncVariant();
            }
            if (method.IsUnboxingThunk())
            {
                return method.GetUnboxedMethod().GetPrimaryMethodDesc();
            }
            return method switch
            {
                PInvokeTargetNativeMethod pinvokeTarget => pinvokeTarget.Target,
                AsyncResumptionStub resumptionStub => resumptionStub.TargetMethod.GetPrimaryMethodDesc(),
                _ => method,
            };
        }

        public static bool IsPrimaryMethodDesc(this MethodDesc method)
        {
            return method == method.GetPrimaryMethodDesc();
        }
    }
}
