// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


var divxPos = 0;
window.onload = function () {
    runCode();
};
function runCode() {
    var test = document.getElementById("testElement");
    test.style.left = divxPos++ + 'px';
    setTimeout(() => runCode(), 50);
}