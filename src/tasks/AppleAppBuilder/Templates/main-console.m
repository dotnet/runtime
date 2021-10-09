// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#import <UIKit/UIKit.h>
#import "runtime.h"
#include <TargetConditionals.h>

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

    CGRect applicationFrame = [[UIScreen mainScreen] bounds];
    logLabel = [[UITextView alloc] initWithFrame:
        CGRectMake(2.0, 50.0, applicationFrame.size.width - 2.0, applicationFrame.size.height - 50.0)];
    logLabel.font = [UIFont systemFontOfSize:9.0];
    logLabel.backgroundColor = [UIColor blackColor];
    logLabel.textColor = [UIColor greenColor];
    logLabel.scrollEnabled = YES;
    logLabel.alwaysBounceVertical = YES;
#ifndef TARGET_OS_TV
    logLabel.editable = NO;
#endif
    logLabel.clipsToBounds = YES;

    summaryLabel = [[UILabel alloc] initWithFrame: CGRectMake(10.0, 0.0, applicationFrame.size.width - 10.0, 50)];
    summaryLabel.textColor = [UIColor whiteColor];
    summaryLabel.font = [UIFont boldSystemFontOfSize: 12];
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
        mono_ios_runtime_init ();
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
