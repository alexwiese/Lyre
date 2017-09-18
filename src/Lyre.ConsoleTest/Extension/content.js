var isLoggingEnabled = false;

document.addEventListener("sendCommand",
    function (data) {
        chrome.runtime.sendMessage({ command: data.detail });
    });

chrome.extension.onMessage.addListener(function (response) {
    if (response) {
        if (isLoggingEnabled)
            console.log("Response from native messaging: " + JSON.stringify(response));
    }

    var event = new CustomEvent("receiveCommand", { detail: response });
    document.dispatchEvent(event);
});