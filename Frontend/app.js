document.addEventListener("DOMContentLoaded", (event) => {
  const ws = new WebSocket("ws://yourserver.com/socket");
  const remoteScreen = document.getElementById("remote-screen");

  ws.onopen = function () {
    console.log("Connected to server");
  };

  ws.onmessage = function (event) {
    // Assuming the server sends base64 encoded images
    remoteScreen.src = "data:image/png;base64," + event.data;
  };

  ws.onclose = function () {
    console.log("Disconnected from server");
  };

  ws.onerror = function (error) {
    console.error("WebSocket Error: ", error);
  };

  document.addEventListener("keydown", function (event) {
    ws.send(JSON.stringify({ type: "keydown", key: event.key }));
  });

  document.addEventListener("mousemove", function (event) {
    const rect = remoteScreen.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;
    ws.send(JSON.stringify({ type: "mousemove", x: x, y: y }));
  });

  document.addEventListener("click", function (event) {
    const rect = remoteScreen.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;
    ws.send(JSON.stringify({ type: "click", x: x, y: y }));
  });
});
