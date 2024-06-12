const ws = new WebSocket("ws://localhost:8080/RemoteDesktop"); // Cambia localhost por la IP del servidor si es necesario
const canvas = document.getElementById("screen");
const ctx = canvas.getContext("2d");

ws.onopen = function () {
  console.log("Conectado al servidor WebSocket");
  requestCapture();
  requestStatus(); // Solicitar el rendimiento del sistema al conectarse
};

ws.onclose = function () {
  console.log("WebSocket connection closed");
};

ws.onmessage = function (event) {
  if (typeof event.data === "string") {
    console.log(event.data); // Mostrar el rendimiento del sistema en la consola
  } else {
    const blob = new Blob([event.data], { type: "image/png" });
    const url = URL.createObjectURL(blob);
    const img = new Image();
    img.onload = function () {
      ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
      URL.revokeObjectURL(url); // Liberar memoria
      requestCapture(); // Solicitar la siguiente captura
    };
    img.src = url;
  }
};

function requestCapture() {
  ws.send("capture");
}

function requestStatus() {
  ws.send("status");
}

// Capturar eventos del mouse y teclado para enviar al servidor
canvas.addEventListener("mousemove", function (event) {
  const data = {
    type: "mousemove",
    x: event.offsetX,
    y: event.offsetY,
  };
  ws.send(JSON.stringify(data));
});

canvas.addEventListener("click", function (event) {
  const data = { type: "click", x: event.offsetX, y: event.offsetY };
  ws.send(JSON.stringify(data));
});

document.addEventListener("keydown", function (event) {
  const data = { type: "keydown", key: event.key };
  ws.send(JSON.stringify(data));
});

// Solicitar el rendimiento del sistema cada 5 segundos
setInterval(requestStatus, 5000);
