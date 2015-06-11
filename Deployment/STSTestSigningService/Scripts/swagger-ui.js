var urlSegments = location.href.substr(0, location.href.indexOf("/swagger")).split('/');
var apiUrl = "/" + urlSegments[urlSegments.length - 1];

var apis = window.swaggerUi.api.apisArray;
for (var i = 0; i < apis.length; i++) {
    var operations = apis[i].operationsArray;
    if (operations != null) {
        for (var j = 0; j < operations.length; j++) {
            var basePath = operations[j].basePath;
            operations[j].basePath = apiUrl;
            if (basePath !== "/")
                operations[j].basePath += basePath;
        }
    }
}