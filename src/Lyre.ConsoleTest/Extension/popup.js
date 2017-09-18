// Copyright (c) 2014 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

/**
 * Get the current URL.
 *
 * @param {function(string)} callback - called when the URL of the current tab
 *   is found.
 */
function getCurrentTabUrl(callback) {
    // Query filter to be passed to chrome.tabs.query - see
    // https://developer.chrome.com/extensions/tabs#method-query
    var queryInfo = {
        active: true,
        currentWindow: true
    };

    chrome.tabs.query(queryInfo, function (tabs) {
        // chrome.tabs.query invokes the callback with a list of tabs that match the
        // query. When the popup is opened, there is certainly a window and at least
        // one tab, so we can safely assume that |tabs| is a non-empty array.
        // A window can only have one active tab at a time, so the array consists of
        // exactly one tab.
        var tab = tabs[0];

        // A tab is a plain object that provides information about the tab.
        // See https://developer.chrome.com/extensions/tabs#type-Tab
        var url = tab.url;

        // tab.url is only available if the "activeTab" permission is declared.
        // If you want to see the URL of other tabs (e.g. after removing active:true
        // from |queryInfo|), then the "tabs" permission is required to see their
        // "url" properties.
        console.assert(typeof url === "string", "tab.url should be a string");

        callback(url);
    });
}

function renderStatus(statusText) {
    document.getElementById("status").textContent += statusText + "\r\n";
}

document.addEventListener("DOMContentLoaded", function () {
    getCurrentTabUrl(function (url) {
        chrome.runtime.onMessage.addListener(function (response) {
            if (response.value) {
                renderStatus("Time: " + response.dateTime);
                renderStatus("Message: " + response.value);
            }
        });

        chrome.runtime.sendMessage({
            value: {
                value: "Hello there from popup",
                dateTime: new Date().toJSON()
            }
        });
    });
});
