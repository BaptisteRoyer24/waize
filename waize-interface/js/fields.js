import { getDestinationMarker, getOriginMarker, map, findItinerary, updateMarker } from "./map.js";
let originInput = {}
let destinationInput = {}

export const getOriginInput = () => (originInput);
export const getDestinationInput = () => (destinationInput);

// Add map click event listener for selecting origin or destination
document.addEventListener("DOMContentLoaded", async () => {
    map.on('click', (e) => {
        const { lat, lng } = e.latlng;

        if (!inputs.origin.value) {
            setOrigin(lat, lng)
        } else if (!inputs.destination.value) {
            setDestination(lat, lng)
        }

        const originValue = inputs.origin.value.trim();
        const destinationValue = inputs.destination.value.trim();

        if (originValue && destinationValue) {
            findItinerary();
        }
    });

    const inputs = {
        origin: document.getElementById('origin-input'),
        destination: document.getElementById('destination-input'),
        originResults: document.getElementById('origin-autocomplete-results'),
        destinationResults: document.getElementById('destination-autocomplete-results')
    };

    const setupAutocomplete = (input, resultsContainer, isOrigin) => {
        const marker = isOrigin ? getOriginMarker() : getDestinationMarker();
        const iconUrl = isOrigin ? "./img/origin.png" : "./img/destination.png";

        const fetchSuggestionsWithDebounce = debounce(async () => {
            const query = input.value.trim();
            if (!query) {
                resultsContainer.innerHTML = ""; // Clear results if input is empty
                return;
            }

            try {
                const suggestions = await fetchAutocompleteSuggestions(query);
                displayAutocompleteResults(suggestions, resultsContainer);
            } catch (error) {
                console.error("Error fetching autocomplete results:", error);
            }
        }, 500); // Delay of 0.5 seconds

        input.addEventListener("input", fetchSuggestionsWithDebounce);

        handleAutocompleteSelection(input, resultsContainer, marker, iconUrl);

        document.addEventListener("click", (e) => {
            if (!resultsContainer.contains(e.target) && !resultsContainer.contains(e.target)) {
                resultsContainer.innerHTML = "";
            }
        });
    };

    const originButton = document.getElementById("origin-location-button");
    const destinationButton = document.getElementById("destination-location-button");

    const setOrigin = (lat, lng) => {
        inputs.origin.value = `${lat.toFixed(6)}, ${lng.toFixed(6)}`;
        updateMarker(getOriginMarker(), [lat, lng], "./img/origin.png");
        originInput = {displayName: "", lat: lat, lng: lng};
    }

    const setDestination = (lat, lng) => {
        inputs.destination.value = `${lat.toFixed(6)}, ${lng.toFixed(6)}`;
        updateMarker(getDestinationMarker(), [lat, lng], "./img/destination.png");
        destinationInput = {displayName: "", lat: lat, lng: lng};
    }

    function getUserLocation(buttonType) {
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(
                (position) => {
                    const lat = position.coords.latitude;
                    const lng = position.coords.longitude;
                    if (buttonType === "Origin") {
                        setOrigin(lat, lng)
                    } else if (buttonType === "Destination") {
                        setDestination(lat, lng)
                    }
                },
                (error) => {
                    console.error("Error getting location:", error.message);
                }
            );
        } else {
            console.error("Geolocation is not supported by this browser.");
        }
    }

    originButton.addEventListener("click", () => getUserLocation("Origin"));
    destinationButton.addEventListener("click", () => getUserLocation("Destination"));

    // Initialize autocomplete for origin and destination
    setupAutocomplete(inputs.origin, inputs.originResults, true);
    setupAutocomplete(inputs.destination, inputs.destinationResults, false);
})

// Function to fetch autocomplete suggestions
const fetchAutocompleteSuggestions = async (query) => {
    const url = `http://localhost:5117/api/Routing/autocomplete?input=${query}`;
    const response = await fetch(url);
    if (!response.ok) {
        throw new Error("Failed to fetch autocomplete results.");
    }
    return response.json();
};

// Function to display autocomplete results
const displayAutocompleteResults = (results, container) => {
    container.innerHTML = ""; // Clear previous results
    results.forEach(result => {
        const item = document.createElement("div");
        item.classList.add("autocomplete-item");
        item.textContent = result.displayName;
        item.dataset.lat = result.coordinates.latitude;
        item.dataset.lng = result.coordinates.longitude;
        container.appendChild(item);
    });
};

// Function to handle autocomplete selection
const handleAutocompleteSelection = (input, resultsContainer, marker, iconUrl) => {
    resultsContainer.addEventListener("click", (event) => {
        if (event.target.classList.contains("autocomplete-item")) {
            const { lat, lng } = event.target.dataset;

            if (input.id === "origin-input") {
                originInput.lat = lat;
                originInput.lng = lng;
            }

            if (input.id === "destination-input") {
                destinationInput.lat = lat;
                destinationInput.lng = lng;
            }

            // Set input value and update the marker
            input.value = event.target.textContent;
            updateMarker(marker, [parseFloat(lat), parseFloat(lng)], iconUrl);

            // Clear the autocomplete results
            resultsContainer.innerHTML = "";

            // Trigger itinerary search if both inputs are filled
            if (originInput.lat && originInput.lng && destinationInput.lat && destinationInput.lng) {
                findItinerary();
            }
        }
    });
};

const debounce = (func, delay) => {
    let timeout;
    return (...args) => {
        clearTimeout(timeout); // Clear the previous timeout
        timeout = setTimeout(() => func(...args), delay); // Set a new timeout
    };
};