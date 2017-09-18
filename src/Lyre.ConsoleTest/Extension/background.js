
var context = {};

var tabs = [];

var hostName = "com.lyre.consoletest";
var port = null;

var isLoggingEnabled = false;

function connect() {
    port = chrome.runtime.connectNative(hostName);

    port.onMessage.addListener(function (message) {
        // Message from native messaging
        
        if (isLoggingEnabled)
            console.log("Response from native messaging: " + JSON.stringify(message));
        
        chrome.runtime.sendMessage(message);

        if (!tabs.length) {
            return;
        }

        tabs.forEach(function (tab) {
            try {
                chrome.tabs.sendMessage(tab, message);
            } catch (e) {
                console.error(e);
            }
        });
    });

    port.onDisconnect.addListener(function () {
        console.warn("Disconnected from native messaging");
        port = null;
        setTimeout(connect, 5000);
    });
}

if (!port) {
    connect();
}

port.postMessage({value:"Connected", dateTime: new Date().toJSON()});

chrome.runtime.onMessage.addListener(function (message, sender) {

    // Capture the tab so we can respond to it
    if (sender.tab && tabs.indexOf(sender.tab.id) === -1) {
        tabs.push(sender.tab.id);
    }

    if (message.value) {

        if (isLoggingEnabled)
            console.log("Sending to native messaging: " + JSON.stringify(message.value));

        if (!port) {
            connect();
        }

        port.postMessage(message.value);
    }
});
