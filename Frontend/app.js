const ws = new WebSocket("ws://localhost:8080/RemoteDesktop");
const canvas = document.getElementById("screen");
const ctx = canvas.getContext("2d");

let lastImageData = null;

ws.onopen = function () {
  console.log("Conectado al servidor WebSocket");
  requestCapture();
};

ws.onclose = function () {
  console.log("WebSocket connection closed");
};

ws.onmessage = function (event) {
  if (typeof event.data === "string") {
    console.log(event.data);
  } else {
    const blob = new Blob([event.data], { type: "image/png" });
    const url = URL.createObjectURL(blob);
    const img = new Image();
    img.onload = function () {
      if (lastImageData) {
        // Dibujar solo los cambios
        drawChanges(lastImageData, img);
      } else {
        // Dibujar la imagen completa si es la primera vez
        ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
      }
      lastImageData = img;
      URL.revokeObjectURL(url); // Liberar el objeto URL
      requestCapture(); // Solicitar la siguiente captura
    };
    img.src = url;
  }
};

function requestCapture() {
  ws.send("capture"); // Solicitar captura al servidor
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

function drawChanges(prevImage, newImage) {
  const prevCanvas = document.createElement('canvas');
  const prevCtx = prevCanvas.getContext('2d');
  prevCanvas.width = canvas.width;
  prevCanvas.height = canvas.height;
  
  prevCtx.drawImage(prevImage, 0, 0);

  const diffCanvas = document.createElement('canvas');
  const diffCtx = diffCanvas.getContext('2d');
  diffCanvas.width = canvas.width;
  diffCanvas.height = canvas.height;

  diffCtx.clearRect(0, 0, canvas.width, canvas.height);
  diffCtx.globalCompositeOperation = 'source-in';
  diffCtx.drawImage(newImage, 0, 0);
  diffCtx.globalCompositeOperation = 'source-over';
  diffCtx.drawImage(prevCanvas, 0, 0);

  ctx.clearRect(0, 0, canvas.width, canvas.height);
  ctx.drawImage(diffCanvas, 0, 0);
}