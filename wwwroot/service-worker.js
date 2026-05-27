self.addEventListener("install", function (event) {
    console.log("Service Worker Installed");
});

self.addEventListener("activate", function (event) {
    console.log("Service Worker Activated");
});

self.addEventListener("push", function (event) {
    const data = event.data.json();

    event.waitUntil(
        self.registration.showNotification(data.title, {
            body: data.body,
            icon: "/icons/icon-192.png",
            badge: "/icons/icon-192.png",
            data: {
                url: data.url
            }
        })
    );
});

self.addEventListener("notificationclick", function (event) {
    event.notification.close();

    event.waitUntil(
        clients.openWindow(event.notification.data.url)
    );
});