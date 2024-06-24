// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#import <UIKit/UIKit.h>
#import <dlfcn.h>
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

void printMe(NSString* folderName, NSString* filePath)
{
    NSFileManager *fileManager = [NSFileManager defaultManager];
    NSString* exists = [fileManager fileExistsAtPath:filePath] ? @"Y" : @"N";
    NSString* writable = [fileManager isWritableFileAtPath:filePath] ? @"Y" : @"N";
    NSString* readable = [fileManager isReadableFileAtPath:filePath] ? @"Y" : @"N";
    NSLog(@"E: %@, W: %@, R: %@, %@               : %@", exists, writable, readable, [NSString stringWithFormat:@"%-25s", [folderName UTF8String]], filePath);
}

void oldBehaviour()
{
    NSArray *paths = NSSearchPathForDirectoriesInDomains(NSDocumentDirectory, NSUserDomainMask, YES);
    NSString *DocumentDir = [paths firstObject];
    NSString *ApplicationData = [DocumentDir stringByAppendingPathComponent:@".config"];
    NSString *CommonApplicationData = @"/usr/share";
    NSString *CommonTemplates = @"/usr/share/templates";
    paths = NSSearchPathForDirectoriesInDomains(NSDesktopDirectory, NSUserDomainMask, YES);
    NSString *Desktop = [DocumentDir stringByAppendingPathComponent:@"Desktop"];
    NSString *DesktopDirectory = Desktop;

    paths = NSSearchPathForDirectoriesInDomains(NSLibraryDirectory, NSUserDomainMask, YES);
    NSString *Library = [paths firstObject];
    NSString *Favorites = [Library stringByAppendingPathComponent:@"Favorites"];
    NSString *Fonts = [DocumentDir stringByAppendingPathComponent:@".fonts"];
    
    paths = NSSearchPathForDirectoriesInDomains(NSCachesDirectory, NSUserDomainMask, YES);
    NSString *InternetCache = [paths firstObject];

    NSString *LocalApplicationData = DocumentDir;
    NSString *MyDocuments = DocumentDir;
    NSString *MyMusic = [DocumentDir stringByAppendingPathComponent:@"Music"];
    NSString *MyPictures = [DocumentDir stringByAppendingPathComponent:@"Pictures"];
    NSString *MyVideos = [DocumentDir stringByAppendingPathComponent:@"Videos"];
    paths = NSSearchPathForDirectoriesInDomains(NSApplicationDirectory, NSUserDomainMask, YES);
    NSString *ProgramFiles = [paths firstObject];
    
    NSString *Resources = Library;
    NSString *System = NULL;
    NSString *Templates = [DocumentDir stringByAppendingPathComponent:@"Templates"];
    NSString *UserProfile = NSHomeDirectory();
    
    printMe(@"AdminTools", NULL);
    printMe(@"ApplicationData", ApplicationData);
    printMe(@"CDBurning", NULL);
    printMe(@"CommonAdminTools", NULL);
    printMe(@"CommonApplicationData", CommonApplicationData);
    printMe(@"CommonDesktopDirectory", NULL);
    printMe(@"CommonDocuments", NULL);
    printMe(@"CommonMusic", NULL);
    printMe(@"CommonOemLinks", NULL);
    printMe(@"CommonPictures", NULL);
    printMe(@"CommonProgramFiles", NULL);
    printMe(@"CommonProgramFilesX86", NULL);
    printMe(@"CommonPrograms", NULL);
    printMe(@"CommonStartMenu", NULL);
    printMe(@"CommonStartup", NULL);
    printMe(@"CommonTemplates", CommonTemplates);
    printMe(@"CommonVideos", NULL);
    printMe(@"Cookies", NULL);
    printMe(@"Desktop", Desktop);
    printMe(@"DesktopDirectory", DesktopDirectory);
    printMe(@"Favorites", Favorites);
    printMe(@"Fonts", Fonts);
    printMe(@"History", NULL);
    printMe(@"InternetCache", InternetCache);
    printMe(@"LocalApplicationData", LocalApplicationData);
    printMe(@"LocalizedResources", NULL);
    printMe(@"MyComputer", NULL);
    printMe(@"MyDocuments", MyDocuments);
    printMe(@"MyMusic", MyMusic);
    printMe(@"MyPictures", MyPictures);
    printMe(@"MyVideos", MyVideos);
    printMe(@"NetworkShortcuts", NULL);
    printMe(@"Personal", MyDocuments);
    printMe(@"PrinterShortcuts", NULL);
    printMe(@"ProgramFiles", ProgramFiles);
    printMe(@"ProgramFilesX86", NULL);
    printMe(@"Programs", NULL);
    printMe(@"Recent", NULL);
    printMe(@"Resources", Resources);
    printMe(@"SendTo", NULL);
    printMe(@"StartMenu", NULL);
    printMe(@"Startup", NULL);
    printMe(@"System", System);
    printMe(@"SystemX86", NULL);
    printMe(@"Templates", Templates);
    printMe(@"UserProfile", UserProfile);
    printMe(@"Windows", NULL);
}

void newBehaviour()
{
    NSArray *paths = NSSearchPathForDirectoriesInDomains(NSApplicationSupportDirectory, NSUserDomainMask, YES);
    NSString *ApplicationData = [paths firstObject];
    NSString *CommonApplicationData = @"/usr/share";
    NSString *CommonTemplates = @"/usr/share/templates";
    
    paths = NSSearchPathForDirectoriesInDomains(NSDesktopDirectory, NSUserDomainMask, YES);
    NSString *Desktop = [paths firstObject];
    NSString *DesktopDirectory = Desktop;

    paths = NSSearchPathForDirectoriesInDomains(NSLibraryDirectory, NSUserDomainMask, YES);
    NSString *Library = [paths firstObject];
    NSString *Favorites = [Library stringByAppendingPathComponent:@"Favorites"];
    NSString *Fonts = [Library stringByAppendingPathComponent:@"Fonts"];

    paths = NSSearchPathForDirectoriesInDomains(NSCachesDirectory, NSUserDomainMask, YES);
    NSString *InternetCache = [paths firstObject];

    NSString *LocalApplicationData = ApplicationData;

    paths = NSSearchPathForDirectoriesInDomains(NSDocumentDirectory, NSUserDomainMask, YES);
    NSString *MyDocuments = [paths firstObject];

    paths = NSSearchPathForDirectoriesInDomains(NSMusicDirectory, NSUserDomainMask, YES);
    NSString *MyMusic = [paths firstObject];

    paths = NSSearchPathForDirectoriesInDomains(NSPicturesDirectory, NSUserDomainMask, YES);
    NSString *MyPictures = [paths firstObject];

    paths = NSSearchPathForDirectoriesInDomains(NSMoviesDirectory, NSUserDomainMask, YES);
    NSString *MyVideos = [paths firstObject];

    NSString *ProgramFiles = @"/Applications";
    NSString *Resources = Library;
    NSString *System = @"/System";

    NSString *UserProfile = NSHomeDirectory();
    NSString *Templates = [UserProfile stringByAppendingPathComponent:@"Templates"];

    printMe(@"AdminTools", NULL);
    printMe(@"ApplicationData", ApplicationData);
    printMe(@"CDBurning", NULL);
    printMe(@"CommonAdminTools", NULL);
    printMe(@"CommonApplicationData", CommonApplicationData);
    printMe(@"CommonDesktopDirectory", NULL);
    printMe(@"CommonDocuments", NULL);
    printMe(@"CommonMusic", NULL);
    printMe(@"CommonOemLinks", NULL);
    printMe(@"CommonPictures", NULL);
    printMe(@"CommonProgramFiles", NULL);
    printMe(@"CommonProgramFilesX86", NULL);
    printMe(@"CommonPrograms", NULL);
    printMe(@"CommonStartMenu", NULL);
    printMe(@"CommonStartup", NULL);
    printMe(@"CommonTemplates", CommonTemplates);
    printMe(@"CommonVideos", NULL);
    printMe(@"Cookies", NULL);
    printMe(@"Desktop", Desktop);
    printMe(@"DesktopDirectory", DesktopDirectory);
    printMe(@"Favorites", Favorites);
    printMe(@"Fonts", Fonts);
    printMe(@"History", NULL);
    printMe(@"InternetCache", InternetCache);
    printMe(@"LocalApplicationData", LocalApplicationData);
    printMe(@"LocalizedResources", NULL);
    printMe(@"MyComputer", NULL);
    printMe(@"MyDocuments", MyDocuments);
    printMe(@"MyMusic", MyMusic);
    printMe(@"MyPictures", MyPictures);
    printMe(@"MyVideos", MyVideos);
    printMe(@"NetworkShortcuts", NULL);
    printMe(@"Personal", MyDocuments);
    printMe(@"PrinterShortcuts", NULL);
    printMe(@"ProgramFiles", ProgramFiles);
    printMe(@"ProgramFilesX86", NULL);
    printMe(@"Programs", NULL);
    printMe(@"Recent", NULL);
    printMe(@"Resources", Resources);
    printMe(@"SendTo", NULL);
    printMe(@"StartMenu", NULL);
    printMe(@"Startup", NULL);
    printMe(@"System", System);
    printMe(@"SystemX86", NULL);
    printMe(@"Templates", Templates);
    printMe(@"UserProfile", UserProfile);
    printMe(@"Windows", NULL);
}

@implementation ViewController

- (void)viewDidLoad {
    [super viewDidLoad];

    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
        NSLog(@"---------------------------");
        oldBehaviour();
        NSLog(@"---------------------------");
        newBehaviour();
        NSLog(@"---------------------------");
        mono_ios_runtime_init ();
    });
}

@end

int main(int argc, char * argv[]) {
    @autoreleasepool {
        return UIApplicationMain(argc, argv, nil, NSStringFromClass([AppDelegate class]));
    }
}
