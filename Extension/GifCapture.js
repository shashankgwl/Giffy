
const displayMediaOptions = {
    video: {
        displaySurface: "window",
    },
    audio: false,
};

var mediaRecorder;
var recordingData = [];

window.addEventListener('DOMContentLoaded', function () {
    //your code here
    document.getElementById("startRecording").addEventListener("click", startCapture);
    document.getElementById("stopRecording").addEventListener("click", stopCapture);
});

async function stopCapture() {
    var vElem = document.getElementById("videoElem");
    let tracks = vElem.srcObject.getTracks();

    tracks.forEach((track) => track.stop());
    vElem.srcObject = null;
    mediaRecorder.stop();
}

async function convertBlobToBase64(blob) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onerror = reject;
        reader.onload = () => {
            resolve(reader.result);
        };
        reader.readAsDataURL(blob);
    });
}
/*
let url = window.URL.createObjectURL(blob);
let a = document.createElement('a');
a.style.display = 'none';
a.href = url;
a.download = 'test.gif';
document.body.appendChild(a);
a.click();
*/

async function startCapture() {
    try {
        navigator.mediaDevices.getDisplayMedia(displayMediaOptions).then(stream => {
            var vElem = document.getElementById("videoElem");
            vElem.srcObject = stream
            let options = { mimeType: 'video/webm;codecs=vp9.0' };
            //let options = { mimeType: 'video/mp4' };
            mediaRecorder = new MediaRecorder(stream, options)

            mediaRecorder.ondataavailable = event => {
                if (event.data && event.data.size > 0) {
                    recordingData.push(event.data);

                    let blob = new Blob(recordingData, { type: 'video/webm;codecs=vp9.0' });
                    convertBlobToBase64(blob).then(base64 => {

                        //let blob = new Blob(recordingData, { type: 'video/mp4' });
                        console.log("the length is " + recordingData.length);

                        //fetch("http://localhost:7016/api/FxBlobReceiver", {
                        fetch("https://portalwebtest.azurewebsites.net/api/FxBlobReceiver", {
                            method: "POST",
                            body: base64
                        }).then(response => response.arrayBuffer()).then(buffer => {

                            let blob = new Blob([new Uint8Array(buffer)], { type: "image/gif" });

                            let link = document.createElement('a');

                            // Set the download attribute of the link
                            link.download = 'download.gif';

                            // Create an object URL from the blob
                            link.href = URL.createObjectURL(blob);

                            // Append the link to the body
                            document.body.appendChild(link);

                            // Programmatically click the link to start the download
                            link.click();

                            // Remove the link from the body
                            document.body.removeChild(link);
                        })


                        recordingData = [];

                    });

                }
            };

            mediaRecorder.start();

        }).err(err => {
            console.error(`Error: ${err}`);
        })

    } catch (e) {

    }
}


