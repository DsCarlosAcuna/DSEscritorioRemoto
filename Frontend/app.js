const ws = new WebSocket("ws://localhost:8080/RemoteDesktop"); // Cambia localhost por la IP del servidor si es necesario
const canvas = document.getElementById("screen");
const ctx = canvas.getContext("2d");
let currentImage = null; // Mantener la imagen actual

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
      if (currentImage) {
        // Crear un canvas temporal para realizar la operación XOR
        const tempCanvas = document.createElement("canvas");
        const tempCtx = tempCanvas.getContext("2d");
        tempCanvas.width = canvas.width;
        tempCanvas.height = canvas.height;

        // Dibujar la imagen actual en el canvas temporal
        tempCtx.drawImage(currentImage, 0, 0);

        // Obtener los datos de imagen del canvas temporal y la nueva imagen
        const currentData = tempCtx.getImageData(0, 0, canvas.width, canvas.height);
        ctx.drawImage(img, 0, 0);
        const newData = ctx.getImageData(0, 0, canvas.width, canvas.height);

        // Aplicar la operación XOR
        let hasDifference = false;
        for (let i = 0; i < currentData.data.length; i += 4) {
          const r = currentData.data[i] ^ newData.data[i]; // Red channel
          const g = currentData.data[i + 1] ^ newData.data[i + 1]; // Green channel
          const b = currentData.data[i + 2] ^ newData.data[i + 2]; // Blue channel

          if (r !== 0 || g !== 0 || b !== 0) {
            hasDifference = true;
          }

          currentData.data[i] = r;
          currentData.data[i + 1] = g;
          currentData.data[i + 2] = b;
          currentData.data[i + 3] = 255; // Alpha channel
        }

        // Si hay diferencia, actualizar el canvas con los datos de imagen modificados
        if (hasDifference) {
          ctx.putImageData(currentData, 0, 0);
          currentImage.src = canvas.toDataURL();
        }
      } else {
        // Si no hay una imagen actual, simplemente dibujar la nueva imagen
        currentImage = new Image();
        currentImage.src = url;
        currentImage.onload = function () {
          ctx.drawImage(currentImage, 0, 0, canvas.width, canvas.height);
          URL.revokeObjectURL(url); // Liberar memoria
        };
      }

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