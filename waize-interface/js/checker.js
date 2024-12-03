import {getDestinationInput, getOriginInput} from "./fields.js";

const checkItinerary = async (originLat, originLng, destLat, destLng) => {
    try {
        const response = await fetch(`http://localhost:5000/api/Routing/check-itinerary?originLat=${originLat}&originLng=${originLng}&destLat=${destLat}&destLng=${destLng}`, {
            method: 'GET'
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        console.log("Itinerary checked successfully");
    } catch (error) {
        console.error("Error checking itinerary:", error);
    }
};

// Call the checkItinerary function every 30 seconds
setInterval(() => {
    const originInput = getOriginInput();
    const destinationInput = getDestinationInput();

    if (!originInput || !destinationInput) {
        return;
    }

    if (isNaN(originInput.lat) || isNaN(originInput.lng) || isNaN(destinationInput.lat) || isNaN(destinationInput.lng)) {
        return;
    }

    checkItinerary(originInput.lat, originInput.lng, destinationInput.lat, destinationInput.lng);
}, 30000);