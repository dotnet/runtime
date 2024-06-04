// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#import <UIKit/UIKit.h>

#if !USE_LIBRARY_MODE
#if !USE_NATIVE_AOT
#import "runtime.h"
#else
#import <os/log.h>
#import "util.h"
extern int __managed__Main(int argc, char* argv[]);
#endif // !USE_NATIVE_AOT
#else
#import "runtime-librarymode.h"
#endif // !USE_LIBRARY_MODE

@interface ViewController : UIViewController
@end

@interface AppDelegate : UIResponder <UIApplicationDelegate>
@property (strong, nonatomic) UIWindow *window;
@property (strong, nonatomic) ViewController *controller;
@end

@implementation AppDelegate
- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions {
    self.window = [[UIWindow alloc] initWithFrame:[[UIScreen mainScreen] bounds]];
    self.controller = [[ViewController alloc] initWithNibName:nil bundle:nil];
    self.window.rootViewController = self.controller;
    [self.window makeKeyAndVisible];
    return YES;
}
@end

UILabel *summaryLabel;
UITextView* logLabel;

@implementation ViewController

- (void)viewDidLoad {
    [super viewDidLoad];

#if TARGET_OS_MACCATALYST
    CGFloat summaryLabelHeight = 150.0;
#else
    CGFloat summaryLabelHeight = 50.0;
#endif
    CGRect applicationFrame = [[UIScreen mainScreen] bounds];
    logLabel = [[UITextView alloc] initWithFrame:
        CGRectMake(10.0, summaryLabelHeight, applicationFrame.size.width - 10.0, applicationFrame.size.height - summaryLabelHeight)];
    logLabel.font = [UIFont systemFontOfSize: 9.0];
    logLabel.backgroundColor = [UIColor blackColor];
    logLabel.textColor = [UIColor greenColor];
    logLabel.scrollEnabled = YES;
    logLabel.alwaysBounceVertical = YES;
#ifndef TARGET_OS_TV
    logLabel.editable = NO;
#endif
    logLabel.clipsToBounds = YES;

    summaryLabel = [[UILabel alloc] initWithFrame: CGRectMake(10.0, 0, applicationFrame.size.width - 10.0, summaryLabelHeight)];
    summaryLabel.font = [UIFont boldSystemFontOfSize: 12.0];
    summaryLabel.backgroundColor = [UIColor blackColor];
    summaryLabel.textColor = [UIColor whiteColor];
    summaryLabel.numberOfLines = 2;
    summaryLabel.textAlignment = NSTextAlignmentLeft;
#if !TARGET_OS_SIMULATOR || FORCE_AOT
    summaryLabel.text = @"Loading...";
#else
    summaryLabel.text = @"Jitting...";
#endif
    [self.view addSubview:logLabel];
    [self.view addSubview:summaryLabel];

    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
#if !USE_LIBRARY_MODE
#if !USE_NATIVE_AOT
        mono_ios_runtime_init ();
#else
#if INVARIANT_GLOBALIZATION
        setenv ("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1", TRUE);
#endif // INVARIANT_GLOBALIZATION
        char **managed_argv;
        int managed_argc = get_managed_args (&managed_argv);
        int ret_val = __managed__Main (managed_argc, managed_argv);
        free_managed_args (&managed_argv, managed_argc);
        os_log_info (OS_LOG_DEFAULT, EXIT_CODE_TAG ": %d", ret_val);
        exit (ret_val);
#endif // !USE_NATIVE_AOT
#else
        library_mode_init();
#endif //!USE_LIBRARY_MODE
    });
}

@end

// called from C#
void
invoke_external_native_api (void (*callback)(void))
{
    if (callback)
        callback();
}

// can be called from C# to update UI
void
mono_ios_set_summary (const char* value)
{
    NSString* nsstr = [NSString stringWithUTF8String:value];
    dispatch_async(dispatch_get_main_queue(), ^{
        summaryLabel.text = nsstr;
    });
}

// can be called from C# to update UI
void
mono_ios_append_output (const char* value)
{
    NSString* nsstr = [NSString stringWithUTF8String:value];
    dispatch_async(dispatch_get_main_queue(), ^{
        logLabel.text = [logLabel.text stringByAppendingString:nsstr];
        CGRect caretRect = [logLabel caretRectForPosition:logLabel.endOfDocument];
        [logLabel scrollRectToVisible:caretRect animated:NO];
        [logLabel setScrollEnabled:NO];
        [logLabel setScrollEnabled:YES];
    });
}

int main(int argc, char * argv[]) {
    @autoreleasepool {
        return UIApplicationMain(argc, argv, nil, NSStringFromClass([AppDelegate class]));
    }
}
