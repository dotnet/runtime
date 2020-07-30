
var Module = { 
	onRuntimeInitialized: function () {
		config.loaded_cb = function () {
			App.init ();
		};
		MONO.mono_load_runtime_and_bcl_args (config)
	},
};
