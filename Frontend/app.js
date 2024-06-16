const ws = new WebSocket("ws://localhost:8080/RemoteDesktop");
const canvas = document.getElementById("screen");
const ctx = canvas.getContext("2d");
let previousImage = null;

ws.onopen = function () {
  console.log("Conectado al servidor WebSocket");
  requestCapture();
};

ws.onclose = function () {
  console.log("WebSocket connection closed");
};

ws.onmessage = function (event) {
  const blob = new Blob([event.data], { type: "image/png" });
  const url = URL.createObjectURL(blob);
  const img = new Image();

  img.onload = function () {
    if (previousImage) {
      const xorImage = createXorImage(previousImage, img);
      ctx.drawImage(xorImage, 0, 0, canvas.width, canvas.height);
      previousImage = xorImage;
    } else {
      ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
      previousImage = img;
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

function requestCapture() {
  ws.send("capture"); // Solicitar captura al servidor
}

function createXorImage(img1, img2) {
  const canvas = document.createElement("canvas");
  const ctx = canvas.getContext("2d");
  canvas.width = img1.width;
  canvas.height = img1.height;

  ctx.drawImage(img1, 0, 0);
  const img1Data = ctx.getImageData(0, 0, canvas.width, canvas.height);

  ctx.drawImage(img2, 0, 0);
  const img2Data = ctx.getImageData(0, 0, canvas.width, canvas.height);

  const xorData = ctx.createImageData(canvas.width, canvas.height);

  for (let i = 0; i < img1Data.data.length; i += 4) {
    xorData.data[i] = img1Data.data[i] ^ img2Data.data[i];
    xorData.data[i + 1] = img1Data.data[i + 1] ^ img2Data.data[i + 1];
    xorData.data[i + 2] = img1Data.data[i + 2] ^ img2Data.data[i + 2];
    xorData.data[i + 3] = 255; // set alpha channel to fully opaque
  }

  ctx.putImageData(xorData, 0, 0);
  const xorImg = new Image();
  xorImg.src = canvas.toDataURL();
  return xorImg;
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
