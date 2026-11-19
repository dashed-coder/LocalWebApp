if (location.origin === "https://appassets.example" && location.pathname === "/index.html") {
    history.replaceState(history.state, "", "/" + location.search + location.hash);
}
