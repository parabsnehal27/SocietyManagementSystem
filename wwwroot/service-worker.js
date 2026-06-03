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
                url: data.url,
                entryId: data.entryId
            },
            actions: [
                {
                    action: "approve",
                    title: "Approve"
                },
                {
                    action: "reject",
                    title: "Reject"
                }
            ]
        })
    );
});
self.addEventListener("notificationclick", function (event) {
    event.notification.close();

    const entryId = event.notification.data.entryId;

    if (event.action === "approve") {
        event.waitUntil(
            clients.openWindow(`/Resident/ApproveVisitorFromNotification?entryId=${entryId}`)
        );
        return;
    }

    if (event.action === "reject") {
        event.waitUntil(
            clients.openWindow(`/Resident/RejectVisitorFromNotification?entryId=${entryId}`)
        );
        return;
    }

    event.waitUntil(
        clients.openWindow(event.notification.data.url)
    );
});