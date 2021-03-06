var path = require("path"),
    Promise = require(path.join(__dirname, "lib", "npo.src.js"));
module.exports.deferred = function() {
    var o = {};
    return o.promise = new Promise(function(resolve, reject) {
        o.resolve = resolve, o.reject = reject
    }), o
}, module.exports.resolved = function(val) {
    return Promise.resolve(val)
}, module.exports.rejected = function(reason) {
    return Promise.reject(reason)
};