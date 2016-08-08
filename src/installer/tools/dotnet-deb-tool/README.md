# Debian Package Tool

This is a tool which simplifies the creation process of a debian package. 
Use of this tool requires creating a json configuration, and appropriate
directory structure with your desired files to be included.


## Usage

```
Usage: package_tool [-i <INPUT_DIR>] [-o <OUTPUT_DIRECTORY>] 
[-n <PACKAGE_NAME>] [-v <PACKAGE_VERSION>] [-h]

REQUIRED:
    -i <INPUT_DIR>: Input directory conforming to package_tool conventions and debian_config.json
    -o <OUTPUT_DIR>: Output directory for debian package and other artifacts

OPTIONAL:
    -n <PACKAGE_NAME>: name of created package, will override value in debian_config.json
    -v <PACKAGE_VERSION>: version of created package, will override value in debian_config.json
    -h: Show this message

NOTES:
    See Below for more information on package_tool conventions and debian_config.json format
```

## Input Directory Spec

```
package/
    $/                      (Contents in this directory will be placed absolutely according to their relative path)
        usr/lib/somelib.so  (ex. This file gets placed at /usr/lib/somelib.so at install)
    package_root/           (Contents placed in install root)
    samples/                (Contents here will be installed as samples)
    docs/                   (Contents will be installed as manpages)
    debian_config.json      (See example below)
    docs.json               (For manpage generation)
    (ex. dotnet-commands-test.sh)
```


Note: The default install root is `/usr/share/{package_name}` where package_name is replaced with the name of the created package

## full example debian_config.json
Note: Use the commentless version [here](example_config.json).

```javascript
{
    "maintainer_name":"Microsoft",                              // [required]
    "maintainer_email": "optimus@service.microsoft.com",        // [required]

    "package_name": "Packagify_Test",                           // [required]

    "short_description": "This is a test package",              // [required] Max. 60 chars
    "long_description": "This is a longer description of the test package", // [required]
    "homepage": "http://testpackage.com",                       // (optional no default)

    "release":{
        "package_version":"0.1",                                // [required]
        "package_revision":"1",                                 // [required]
        "urgency" : "low",                                      // (optional default="low") https://www.debian.org/doc/debian-policy/ch-controlfields.html#s-f-Urgency
        "changelog_message" : "some stuff here"                 // [required]
    },

    "control": {                                                // (optional)
        "priority":"standard",                                  // (optional default="standard") https://www.debian.org/doc/debian-policy/ch-archive.html#s-priorities
        "section":"devel",                                      // (optional default="misc") https://www.debian.org/doc/debian-policy/ch-archive.html#s-subsections
        "architecture":"all"                                    // (optional default="all" ) 
    },

    "copyright": "2015 Microsoft",                              // [required]
    "license": {                                                // [required]
        "type": "some_license",                                 // [required]
        "full_text": "full license text here"                   // [required]
    },

    "debian_dependencies" : {                                   // (optional no default)
        "package_name": {
            "package_version" : "1.0.0"                         // (optional within package_name no default)
        }
    }, 

    "symlinks": {                                               // (optional no defaults)
        "path_relative_to_package_root/test_exe.sh" : "usr/bin/test_exe.sh" 
    }
}
```
