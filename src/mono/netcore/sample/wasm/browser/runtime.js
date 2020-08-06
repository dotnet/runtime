
var Module = { 
    onRuntimeInitialized: function () {
        config.loaded_cb = function () {
            App.init ();
        };
        config.fetch_file_cb = function (asset) {
            if (typeof window != 'undefined') {
                return fetch (asset, { credentials: 'same-origin' });
            } else {
                // The default mono_load_runtime_and_bcl defaults to using
                // fetch to load the assets.  It also provides a way to set a
                // fetch promise callback.
                // Here we wrap the file read in a promise and fake a fetch response
                // structure.
                return new Promise ((resolve, reject) => {
                    var bytes = null, error = null;
                    try {
                        bytes = read (asset, 'binary');
                    } catch (exc) {
                        error = exc;
                    }
                    var response = { ok: (bytes && !error), url: asset,
                        arrayBuffer: function () {
                            return new Promise ((resolve2, reject2) => {
                                if (error)
                                    reject2 (error);
                                else
                                    resolve2 (new Uint8Array (bytes));
                        }
                    )}
                    }
                    resolve (response);
                })
            }
        }

        MONO.mono_load_runtime_and_bcl_args (config);
    },
};
