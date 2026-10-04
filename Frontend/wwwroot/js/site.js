"use strict";

document.documentElement.classList.add("js");

function setNavigationState(button, navigation, isOpen) {
    navigation.classList.toggle("open", isOpen);
    button.setAttribute("aria-expanded", String(isOpen));
    button.setAttribute("aria-label", isOpen ? "Close navigation" : "Open navigation");
}

function togglePassword(button) {
    const input = document.getElementById(button.dataset.showPassword);
    const shouldShow = input.type === "password";
    const label = document.querySelector(`label[for="${input.id}"]`).textContent.replace("*", "").trim().toLowerCase();

    input.type = shouldShow ? "text" : "password";
    button.textContent = shouldShow ? "Hide" : "Show";
    button.setAttribute("aria-label", `${shouldShow ? "Hide" : "Show"} ${label}`);
    button.setAttribute("aria-pressed", String(shouldShow));
}

const navigationButton = document.querySelector("[data-menu]");
const navigation = document.getElementById("main-navigation");

navigationButton.addEventListener("click", () => {
    setNavigationState(navigationButton, navigation, navigationButton.getAttribute("aria-expanded") !== "true");
});

document.addEventListener("keydown", event => {
    if (event.key !== "Escape" || !navigation.classList.contains("open")) return;

    setNavigationState(navigationButton, navigation, false);
    navigationButton.focus();
});

for (const button of document.querySelectorAll("[data-show-password]")) {
    button.addEventListener("click", () => togglePassword(button));
}

// Give immediate feedback in the browser; the C# form model checks the same rule on the server.
for (const confirmation of document.querySelectorAll("[data-match-password]")) {
    const password = document.getElementById(confirmation.dataset.matchPassword);
    const checkMatch = () => confirmation.setCustomValidity(
        confirmation.value && confirmation.value !== password.value ? "The passwords do not match." : ""
    );
    confirmation.addEventListener("input", checkMatch);
    password.addEventListener("input", checkMatch);
    checkMatch();
}

for (const input of document.querySelectorAll(".input-validation-error")) {
    input.setAttribute("aria-invalid", "true");
}

const validationSummary = document.querySelector(".validation-summary-errors");
validationSummary?.focus();
