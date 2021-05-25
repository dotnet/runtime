// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var JSSupportLib = {
    // $JS__postset: 'JS.export_functions (Module);',
    // $JS: {
        load_config: function (callback) {
            if (ENVIRONMENT_IS_NODE){
                const config = require('./mono-config.json');
                callback(config);
            }
            var xobj = new XMLHttpRequest();
            xobj.overrideMimeType("application/json");
            xobj.open('GET', './mono-config.json', true);
            xobj.onreadystatechange = function () {
                if (xobj.readyState == 4 && xobj.status == "200") {
                    const config = JSON.parse(xobj.responseText);
                    callback(config);
                }
            };
            xobj.send();  
        },

    //     export_functions: function (module) {
	// 		module ["load_config"] = JS.load_config.bind(JS);
            
    //     }
    // },
}

// autoAddDeps(JSSupportLib, '$JS')
// mergeInto(LibraryManager.library, JSSupportLib) // TODO FOR SOME REASON THE LOAD_CONFIG FUNCTION IS NOT BEING ADDED TO DOTNET.JS. THIS IS NEEDED SO THAT I CAN CALL LOAD_CONFIG() IN THE SAMPLES
