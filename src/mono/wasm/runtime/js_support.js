// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// THIS FILE IS COPIED DIRECTLY INTO THE DOTNET.JS FILE WITHOUT BEING RUN OR COMPILED/OPTIMIZED
// IT I MEANT AS A SET OF JS TOOLS TO SUPPORT THE SDK

var JSSupportLib = {
    // Loads the config file located in the root of the project
    load_config: function (callback) {
        if (ENVIRONMENT_IS_NODE){
            const config = require('./mono-config.json');
            if (callback){
                callback(config);
            }
        } else {
            var xobj = new XMLHttpRequest();
            xobj.overrideMimeType("application/json");
            xobj.open('GET', './mono-config.json', true);
            xobj.onreadystatechange = function() {
                if (callback && xobj.readyState == XMLHttpRequest.DONE) {
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
                if (callback){ 
                    callback({error: "Error loading mono-config.json file from current directory"});
                }
            }

            try {
                xobj.send()
            } catch(e) {
                // other kinds of errors
                if (callback){ 
                    callback({error: "Error loading mono-config.json file from current directory"});
                }
            }
        }
    },
}
