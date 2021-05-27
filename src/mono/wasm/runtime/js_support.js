// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// THIS FILE IS COPIED DIRECTLY INTO THE DOTNET.JS FILE WITHOUT BEING RUN OR COMPILED/OPTIMIZED
// IT I MEANT AS A SET OF JS TOOLS TO SUPPORT THE SDK

// Loads the config file located in the root of the project
// Not meant to be used outside of this class (TODO make private to this file when project converted to TS)
function load_config() {
    // since this file loads before emsdk we don't have environment vars yet, so we define them locally
    var ENVIRONMENT_IS_NODE = typeof process === 'object';
    var ENVIRONMENT_IS_WEB = typeof window === 'object';
    // var ENVIRONMENT_IS_WORKER = typeof importScripts === 'function';
    // var ENVIRONMENT_IS_SHELL = !ENVIRONMENT_IS_WEB && !ENVIRONMENT_IS_NODE && !ENVIRONMENT_IS_WORKER;

    callback = Module['onConfigLoaded'];

    if (ENVIRONMENT_IS_NODE){
        try {
            var config = JSON.parse(require('./mono-config.json'));
            callback(config);
        } catch(e) {
            callback({error: "Error loading mono-config.json file from current directory"});
        }
    } else if (ENVIRONMENT_IS_WEB){
        var xobj = new XMLHttpRequest();
        xobj.overrideMimeType("application/json");
        xobj.open('GET', './mono-config.json', true);
        xobj.onreadystatechange = function() {
            if (xobj.readyState == XMLHttpRequest.DONE) {
                if (xobj.status === 0 || (xobj.status >= 200 && xobj.status < 400)) {
                    var config = JSON.parse(xobj.responseText);
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
    } else {
        try {
            var config = JSON.parse(load("./mono-config.json"));
            callback(config);
        } catch(e) {
            callback({error: "Error loading mono-config.json file from current directory"});
        }
    }
}
load_config();
