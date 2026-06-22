using Foundation;
using UIKit;

namespace AotTypeLoadRecovery.SourceOnly.iOS;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        StorePathProbe.RootWithoutRunning();
        return true;
    }
}
