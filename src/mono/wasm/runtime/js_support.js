// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Loads the config file located in the root of the project
// Not meant to be used outside of this class (TODO make private to this file when project converted to TS)
function load_config() {
    // since this file loads before emsdk we don't have environment vars yet, so we define them locally
    const ENVIRONMENT_IS_NODE = typeof process === "object";
    const ENVIRONMENT_IS_WEB = typeof window === "object";

    // In some cases there may be no Module object (such as in tests)
    // so we no-op during the callback
    const callback = typeof Module !== "undefined" ? Module['onConfigLoaded'] : (_) => {};

    if (ENVIRONMENT_IS_NODE){
        try {
            const config = JSON.parse(require("./mono-config.json"));
            callback(config);
        } catch(e) {
            callback({error: "Error loading mono-config.json file from current directory"});
        }
    } else if (ENVIRONMENT_IS_WEB){
        const xobj = new XMLHttpRequest();
        xobj.overrideMimeType("application/json");
        xobj.open("GET", "./mono-config.json", true);
        xobj.onreadystatechange = function() {
            if (xobj.readyState == XMLHttpRequest.DONE) {
                if (xobj.status === 0 || (xobj.status >= 200 && xobj.status < 400)) {
                    const config = JSON.parse(xobj.responseText);
                    callback(config);
                } else {
                    // error if the request to load the file was successful but loading failed
                    callback({error: "Error loading mono-config.json file from current directory"});
                }
            }
        };
        xobj.onerror = function() {
            // error if the request failed
            callback({error: "Error loading mono-config.json file from current directory"});
        }

        try {
            xobj.send();
        } catch(e) {
            // other kinds of errors
            callback({error: "Error loading mono-config.json file from current directory"});
        }
    } else { // shell or worker
        try {
            const config = JSON.parse(read("./mono-config.json")); // read is a v8 debugger command
            callback(config);
        } catch(e) {
            callback({error: "Error loading mono-config.json file from current directory"});
        }
    }
}
load_config();
