var AppStart = {
  Construct: function() {
    this._frame = document.createElement('iframe');
    document.body.appendChild(this._frame);
  },

  WaitForPageShow: async function() {
    let promise;
    let promiseResolve;
    this._frame.src = 'appstart-frame.html';
    promise = new Promise(resolve => { promiseResolve = resolve; })
    window.resolveAppStartEvent = function(event) { promiseResolve(); }
    await promise;
  },

  WaitForReached: async function() {
    let promise;
    let promiseResolve;
    this._frame.src = 'appstart-frame.html';
    promise = new Promise(resolve => { promiseResolve = resolve; })
    window.resolveAppStartEvent = function(event) {
      if (event == "reached")
        promiseResolve();
    }
    await promise;
  },

  RemoveFrame: function () {
    document.body.removeChild(this._frame);
  }
};
