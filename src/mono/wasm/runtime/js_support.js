// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Loads the config file
async function loadMonoConfig(configFile) {
  // In some cases there may be no Module object (such as in some tests)
  // so we no-op during the callback
  const callback = typeof Module !== "undefined" ? Module['onConfigLoaded'] : (_) => {};

  const ENVIRONMENT_IS_NODE = typeof process === "object";
  const ENVIRONMENT_IS_WEB = typeof window === "object";

  // NOTE: when we add nodejs make sure to include the nodejs fetch package
  if (ENVIRONMENT_IS_WEB || ENVIRONMENT_IS_NODE) {
    try {
      const configRaw = await fetch(configFile);
      const config = await configRaw.json();
      callback(config);
    } catch(e) {
      callback({error: e});
    }

  } else { // shell or worker
    try {
      const config = JSON.parse(read(configFile)); // read is a v8 debugger command
      callback(config);
    } catch(e) {
      callback({error: `Error loading ${configFile} file`});
    }
  }
}
