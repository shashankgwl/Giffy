
const displayMediaOptions = {
    video: {
        displaySurface: "window",
    },
    audio: false,
};

var mediaRecorder;
var recordingData = [];


window.addEventListener('DOMContentLoaded', function () {
    document.getElementById("startRecording").addEventListener("click", startCapture);
    document.getElementById("stopRecording").addEventListener("click", stopCapture);
    $('#blinkingText').hide();

});



async function stopCapture() {
    var vElem = document.getElementById("videoElem");
    $('#blinkingText').show();

    if (vElem.srcObject === null || vElem.srcObject === undefined) {
        return
    }

    let tracks = vElem.srcObject.getTracks();
    if (tracks == null || tracks == undefined) {
        return
    }


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


async function startCapture() {
    try {
        navigator.mediaDevices.getDisplayMedia(displayMediaOptions).then(stream => {
            var vElem = document.getElementById("videoElem");
            vElem.srcObject = stream
            let options = { mimeType: 'video/webm;codecs=h264' };
            //let options = { mimeType: 'video/mp4' };
            mediaRecorder = new MediaRecorder(stream, options)

            mediaRecorder.ondataavailable = event => {
                if (event.data && event.data.size > 0) {
                    recordingData.push(event.data);

                    let blob = new Blob(recordingData, { type: 'video/webm;codecs=h264' });
                    document.getElementById("videoElem").src = URL.createObjectURL(blob)
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
                            $('#blinkingText').hide();

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


