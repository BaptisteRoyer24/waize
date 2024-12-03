import {changeRoutesToRed, findItinerary} from "./map.js";

document.addEventListener("DOMContentLoaded", () => {
    const popupContainer = document.querySelector(".popup-container");
    const popupMessage = document.querySelector(".popup-message");
    const popupClose = document.querySelector(".popup-close");
    let changedItinerary = false;

    popupContainer.style.display = "none";

    // Connect to ActiveMQ using Stomp.js
    const client = Stomp.client("ws://localhost:61614/stomp"); // WebSocket URL of ActiveMQ

    client.connect(
        "admin", // ActiveMQ username
        "admin", // ActiveMQ password
        () => {
            console.log("Connected to ActiveMQ");

            // Subscribe to the queue/topic
            client.subscribe("/queue/info", (message) => {
                if (message.body) {
                    console.log(message.body)
                    displayPopup(message.body);
                }
                if (message.body === "The itinerary has changed !") {
                    changeRoutesToRed();
                    changedItinerary = true;
                }
            });
        },
        (error) => {
            console.error("Error connecting to ActiveMQ:", error);
        }
    );

    // Function to display the popup with the message
    const displayPopup = (message) => {
        popupMessage.textContent = message;
        popupContainer.style.display = "flex"; // Show the popup
    };

    // Close popup on button click
    popupClose.addEventListener("click", () => {
        if (changedItinerary) {
            findItinerary()
            changedItinerary = false;
        }
        popupContainer.style.display = "none"; // Hide the popup
    });
});