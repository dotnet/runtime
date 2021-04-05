// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>

#import <UIKit/UIKit.h>
#import "runtime.h"

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
void (*clickHandlerApplyUpdatePtr)(void);

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
    [button setFrame:CGRectMake(50, 250, 200, 50)];
    [button setTitle:@"Click me (wire me up)" forState:UIControlStateNormal];
    [button setExclusiveTouch:YES];
    [self.view addSubview:button];

    UIButton *apply_button = [UIButton buttonWithType:UIButtonTypeInfoDark];
    [apply_button addTarget:self action:@selector(applyUpdateButtonClicked:) forControlEvents:UIControlEventTouchUpInside];
    [apply_button setFrame:CGRectMake(50, 300, 200, 50)];
    [apply_button setTitle:@"ApplyUpdate" forState:UIControlStateNormal];
    [apply_button setExclusiveTouch:YES];
    [self.view addSubview:apply_button];

    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
        mono_ios_runtime_init ();
    });
}
-(void) buttonClicked:(UIButton*)sender
{
    if (clickHandlerPtr)
        clickHandlerPtr();
}

-(void) applyUpdateButtonClicked:(UIButton*)sender
{
    if (clickHandlerApplyUpdatePtr)
        clickHandlerApplyUpdatePtr();
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
ios_register_applyupdate_click (void* ptr)
{
    clickHandlerApplyUpdatePtr = ptr;
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
        setenv("DOTNET_MODIFIABLE_ASSEMBLIES", "Debug", 1);
        return UIApplicationMain(argc, argv, nil, NSStringFromClass([AppDelegate class]));
    }
}

