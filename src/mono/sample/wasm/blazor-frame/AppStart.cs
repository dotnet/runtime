using System.Runtime.InteropServices.JavaScript;

namespace BrowserBench
{
        public partial class FrameApp
        {
            [JSImport("globalThis.frameApp.ReachedCallback")]
            public static partial Task ReachedCallback();

            [JSExport]
            public static void ReachedManaged()
            {
                ReachedCallback();
            }
        }
}
