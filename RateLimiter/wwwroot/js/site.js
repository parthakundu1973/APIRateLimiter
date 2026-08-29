const apiKeyInput = document.getElementById("apiKey");
const burstCountInput = document.getElementById("burstCount");

const sendButton = document.getElementById("sendButton");
const burstButton = document.getElementById("burstButton");

const allowedCountElement = document.getElementById("allowedCount");
const limitedCountElement = document.getElementById("limitedCount");
const limitInfoElement = document.getElementById("limitInfo");
const historyElement = document.getElementById("history");

let requestNumber = 0;
let allowedCount = 0;
let limitedCount = 0;

async function sendRequest() {
    const apiKey = apiKeyInput.value.trim();

    if (!apiKey) {
        alert("Please enter an API key.");
        return;
    }

    const start = performance.now();
    const happenedAt = new Date();

    let response;

    try {
        response = await fetch("/api/process", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "X-API-Key": apiKey
            },
            body: JSON.stringify({
                event_id: "evt_ui",
                type: "property.updated",
                occurred_at: new Date().toISOString(),
                payload: {}
            })
        });

        const duration = Math.round(performance.now() - start);

        addResult(
            response.status,
            happenedAt,
            duration
        );

        if (response.status === 200) {
            allowedCount++;
        } else if (response.status === 429) {
            limitedCount++;
        }

        updateCounts();

        await updateLimitInfo(apiKey);
    }
    catch (error) {
        const duration = Math.round(performance.now() - start);

        addResult(
            "ERROR",
            happenedAt,
            duration
        );
    }
}

async function sendBurst() {
    const count = Number.parseInt(
        burstCountInput.value,
        10
    );

    if (!Number.isInteger(count) || count < 1) {
        alert("Burst count must be at least 1.");
        return;
    }

    for (let i = 0; i < count; i++) {
        await sendRequest();
    }
}

async function updateLimitInfo(apiKey) {
    const response = await fetch(
        `/api/limits?apiKey=${encodeURIComponent(apiKey)}`
    );

    if (!response.ok) {
        return;
    }

    const data = await response.json();

    limitInfoElement.textContent =
        `${data.limit} requests per window | ` +
        `${data.remaining} remaining`;
}

function addResult(status, happenedAt, duration) {
    requestNumber++;

    const row = document.createElement("tr");

    const numberCell = document.createElement("td");
    numberCell.textContent = requestNumber;

    const statusCell = document.createElement("td");
    statusCell.textContent = status;

    const timeCell = document.createElement("td");
    timeCell.textContent =
        happenedAt.toLocaleTimeString();

    const durationCell = document.createElement("td");
    durationCell.textContent = `${duration} ms`;

    row.appendChild(numberCell);
    row.appendChild(statusCell);
    row.appendChild(timeCell);
    row.appendChild(durationCell);

    historyElement.prepend(row);
}

function updateCounts() {
    allowedCountElement.textContent = allowedCount;
    limitedCountElement.textContent = limitedCount;
}

sendButton.addEventListener("click", sendRequest);
burstButton.addEventListener("click", sendBurst);

updateLimitInfo(apiKeyInput.value.trim());
