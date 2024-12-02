import { updateDirectionsUI } from "./directions.js"
import {getDestinationInput, getOriginInput} from "./fields.js";

export let map = L.map('map').setView([45.762, 4.835], 15);
let originMarker;
let destinationMarker;
let pickupStationMarker;
let dropOffStationMarker;
let routeLayers = [];

export const getOriginMarker = () => (originMarker);
export const getDestinationMarker = () => (destinationMarker);

document.addEventListener("DOMContentLoaded", async () => {

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);
});

export const updateMarker = (marker, coordinates, iconUrl = null) => {
    if (marker) {
        // Update the existing marker position
        marker.setLatLng(coordinates);

        // Update the icon if a new icon URL is provided
        if (iconUrl) {
            const newIcon = L.icon({
                iconUrl: iconUrl,
                iconSize: [30, 30], // Adjust size as needed
                iconAnchor: [15, 30], // Anchor point of the icon
            });
            marker.setIcon(newIcon);
        }
    } else {
        // Create a new marker with or without a custom icon
        const options = { draggable: false };
        if (iconUrl) {
            options.icon = L.icon({
                iconUrl: iconUrl,
                iconSize: [30, 30], // Adjust size as needed
                iconAnchor: [15, 30], // Anchor point of the icon
            });
        }
        marker = L.marker(coordinates, options).addTo(map);
    }
    return marker;
};

export const findItinerary = async () => {
    const originInput = getOriginInput();
    const destinationInput = getDestinationInput();

    if (!originInput || !destinationInput) {
        return;
    }

    if (isNaN(originInput.lat) || isNaN(originInput.lng) || isNaN(destinationInput.lat) || isNaN(destinationInput.lng)) {
        alert("Invalid coordinates! Please use the format: latitude, longitude");
        return;
    }

    originMarker = updateMarker(originMarker, [originInput.lat, originInput.lng], "./img/origin.png");
    destinationMarker = updateMarker(destinationMarker, [destinationInput.lat, destinationInput.lng], "./img/destination.png");

    try {
        const response = await fetch(`http://localhost:5117/api/Routing/directions?originLat=${originInput.lat}&originLng=${originInput.lng}&destLat=${destinationInput.lat}&destLng=${destinationInput.lng}`);
        if (!response.ok) {
            throw new Error("Error calling the API");
        }

        const routeDetails = await response.json();
        console.log(routeDetails);

        routeLayers.forEach(layer => map.removeLayer(layer));
        routeLayers = [];

        const sections = [
            routeDetails.walkingToStation,
            routeDetails.bikingBetweenStations,
            routeDetails.walkingToDestination
        ].filter(section => section !== null);

        sections.forEach(section => {
            const color = section.mode === "walking" ? "green" : "blue";
            const dashArray = section.mode === "walking" ? "10, 10" : null;
            const polyline = L.polyline(
                section.coordinates.map(coord => [coord.latitude, coord.longitude]),
                { color: color, weight: 5, dashArray: dashArray }
            ).addTo(map);

            routeLayers.push(polyline);
        });

        if (routeLayers.length > 0) {
            const allBounds = L.featureGroup(routeLayers).getBounds();
            map.fitBounds(allBounds);
        }

        if (routeDetails.pickupStation) {
            pickupStationMarker = updateMarker(
                pickupStationMarker,
                [routeDetails.pickupStation.coordinate.latitude, routeDetails.pickupStation.coordinate.longitude],
                "./img/location.png"
            );
        }

        if (routeDetails.dropOffStation) {
            dropOffStationMarker = updateMarker(
                dropOffStationMarker,
                [routeDetails.dropOffStation.coordinate.latitude, routeDetails.dropOffStation.coordinate.longitude],
                "./img/location.png"
            );
        }

        updateDirectionsUI(sections);
    } catch (error) {
        console.error("Error fetching directions:", error);
        alert("Failed to fetch directions. Please try again.");
    }
};