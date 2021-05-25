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
        }
        var xobj = new XMLHttpRequest();
        xobj.overrideMimeType("application/json");
        xobj.open('GET', './mono-config.json', true);
        xobj.onreadystatechange = function () {
            if (xobj.readyState == 4 && xobj.status == "200") {
                const config = JSON.parse(xobj.responseText);
                if (callback){
                    callback(config);
                }
            }
        };
        xobj.send();  
    },
}
