let ws;
const canvas = document.getElementById("screen");
const ctx = canvas.getContext("2d");
let previousImage = null;

function connectWebSocket() {
  ws = new WebSocket("ws://localhost:8080/RemoteDesktop");

  ws.onopen = function () {
    console.log("Conectado al servidor WebSocket");
    requestCapture();
  };

  ws.onclose = function () {
    console.log("WebSocket connection closed");
    setTimeout(connectWebSocket, 1000); // Intentar reconectar después de 1 segundo
  };

  ws.onerror = function (error) {
    console.error("WebSocket error: ", error);
    ws.close();
  };

  ws.onmessage = function (event) {
    const blob = new Blob([event.data], { type: "image/png" });
    const url = URL.createObjectURL(blob);
    const img = new Image();

    img.onload = function () {
      if (previousImage) {
        applyXor(previousImage, img);
        console.log("aplicando xor");
      } else {
        ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
        previousImage = document.createElement("canvas");
        previousImage.width = canvas.width;
        previousImage.height = canvas.height;
        previousImage.getContext("2d").drawImage(img, 0, 0);
      }
      URL.revokeObjectURL(url); // Liberar el objeto URL
      requestCapture(); // Solicitar la siguiente captura
    };

    img.onerror = function () {
      console.error("Error loading image");
      URL.revokeObjectURL(url); // Liberar el objeto URL
      requestCapture(); // Intentar la siguiente captura
    };

    img.src = url;
  };
}

function requestCapture() {
  if (ws.readyState === WebSocket.OPEN) {
    ws.send("capture"); // Solicitar captura al servidor
  } else {
    console.error("WebSocket is not open. Ready state: " + ws.readyState);
  }
}

function applyXor(previousImage, xorImage) {
  const width = previousImage.width;
  const height = previousImage.height;
  const resultCanvas = document.createElement("canvas");
  resultCanvas.width = width;
  resultCanvas.height = height;
  const resultCtx = resultCanvas.getContext("2d");

  // Dibujar la imagen previa
  resultCtx.drawImage(previousImage, 0, 0);
  const previousData = resultCtx.getImageData(0, 0, width, height);

  // Dibujar la imagen XOR recibida
  resultCtx.drawImage(xorImage, 0, 0);
  const xorData = resultCtx.getImageData(0, 0, width, height);

  // Aplicar XOR pixel por pixel
  for (let i = 0; i < previousData.data.length; i += 4) {
    previousData.data[i] ^= xorData.data[i]; // R
    previousData.data[i + 1] ^= xorData.data[i + 1]; // G
    previousData.data[i + 2] ^= xorData.data[i + 2]; // B
    // Alpha channel remains unchanged
  }

  resultCtx.putImageData(previousData, 0, 0);

  // Actualizar el canvas principal y previousImage
  ctx.drawImage(resultCanvas, 0, 0, canvas.width, canvas.height);
  previousImage.getContext("2d").drawImage(resultCanvas, 0, 0);
}

// Capturar eventos del mouse y teclado para enviar al servidor
canvas.addEventListener("mousemove", function (event) {
  if (ws.readyState === WebSocket.OPEN) {
    const data = {
      type: "mousemove",
      x: event.offsetX,
      y: event.offsetY,
    };
    ws.send(JSON.stringify(data));
  }
});

canvas.addEventListener("click", function (event) {
  if (ws.readyState === WebSocket.OPEN) {
    const data = { type: "click", x: event.offsetX, y: event.offsetY };
    ws.send(JSON.stringify(data));
  }
});

document.addEventListener("keydown", function (event) {
  if (ws.readyState === WebSocket.OPEN) {
    const data = { type: "keydown", key: event.key };
    ws.send(JSON.stringify(data));
  }
});

// Iniciar la conexión WebSocket
connectWebSocket();
