// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#import <UIKit/UIKit.h>
#if !USE_NATIVE_AOT
#import "runtime.h"
#else
#import <os/log.h>
#import "util.h"
extern int __managed__Main(int argc, char* argv[]);
#endif

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

UILabel *label;
void (*clickHandlerPtr)(void);

@implementation ViewController

- (void)viewDidLoad {
    [super viewDidLoad];
    
    label = [[UILabel alloc] initWithFrame:[[UIScreen mainScreen] bounds]];
    label.textColor = [UIColor greenColor];
    label.font = [UIFont boldSystemFontOfSize: 30];
    label.numberOfLines = 2;
    label.textAlignment = NSTextAlignmentCenter;
    label.text = @"Hello, wire me up!\n(dllimport ios_set_text)";
    [self.view addSubview:label];
    
    UIButton *button = [UIButton buttonWithType:UIButtonTypeInfoDark];
    [button addTarget:self action:@selector(buttonClicked:) forControlEvents:UIControlEventTouchUpInside];
    [button setFrame:CGRectMake(50, 300, 200, 50)];
    [button setTitle:@"Click me (wire me up)" forState:UIControlStateNormal];
    [button setExclusiveTouch:YES];
    [self.view addSubview:button];

    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
#if !USE_NATIVE_AOT
        mono_ios_runtime_init ();
#else
#if INVARIANT_GLOBALIZATION
        setenv ("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1", TRUE);
#endif
        char **managed_argv;
        int managed_argc = get_managed_args (&managed_argv);
        int ret_val = __managed__Main (managed_argc, managed_argv);
        free_managed_args (&managed_argv, managed_argc);
        os_log_info (OS_LOG_DEFAULT, EXIT_CODE_TAG ": %d", ret_val);
        exit (ret_val);
#endif
    });
}
-(void) buttonClicked:(UIButton*)sender
{
    if (clickHandlerPtr)
        clickHandlerPtr();
}

@end

// called from C# sample
void
ios_register_button_click (void* ptr)
{
    clickHandlerPtr = ptr;
}

// called from C# sample
void
ios_set_text (const char* value)
{
    NSString* nsstr = [NSString stringWithUTF8String:strdup(value)];
    dispatch_async(dispatch_get_main_queue(), ^{
        label.text = nsstr;
    });
}

int main(int argc, char * argv[]) {
    @autoreleasepool {
        return UIApplicationMain(argc, argv, nil, NSStringFromClass([AppDelegate class]));
    }
}
