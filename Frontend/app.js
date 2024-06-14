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
      const newImageData = getImageDataFromImage(img);

      if (lastImageData) {
        applyXor(lastImageData, newImageData);
      } else {
        ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
      }

      lastImageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
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

function getImageDataFromImage(image) {
  const tempCanvas = document.createElement('canvas');
  const tempCtx = tempCanvas.getContext('2d');
  tempCanvas.width = canvas.width;
  tempCanvas.height = canvas.height;
  tempCtx.drawImage(image, 0, 0, canvas.width, canvas.height);
  return tempCtx.getImageData(0, 0, canvas.width, canvas.height);
}

function applyXor(prevImageData, newImageData) {
  const prevData = prevImageData.data;
  const newData = newImageData.data;

  for (let i = 0; i < prevData.length; i++) {
    newData[i] = prevData[i] ^ newData[i];
  }

  ctx.putImageData(newImageData, 0, 0);
}