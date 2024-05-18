const displayMediaOptions = { video: { displaySurface: "window" }, audio: false };
var mediaRecorder,
    recordingData = [];
async function stopCapture() {
    var e = document.getElementById("videoElem");
    if (($("#blinkingText").show(), null === e.srcObject || void 0 === e.srcObject)) return;
    let t = e.srcObject.getTracks();
    null != t && void 0 != t && (t.forEach((e) => e.stop()), (e.srcObject = null), mediaRecorder.stop());
}
async function convertBlobToBase64(e) {
    return new Promise((t, r) => {
        let a = new FileReader();
        (a.onerror = r),
            (a.onload = () => {
                t(a.result);
            }),
            a.readAsDataURL(e);
    });
}
async function startCapture() {
    try {
        navigator.mediaDevices
            .getDisplayMedia(displayMediaOptions)
            .then((e) => {
                (document.getElementById("videoElem").srcObject = e),
                    ((mediaRecorder = new MediaRecorder(e, { mimeType: "video/webm;codecs=h264" })).ondataavailable = (e) => {
                        if (e.data && e.data.size > 0) {
                            recordingData.push(e.data);
                            let t = new Blob(recordingData, { type: "video/webm;codecs=h264" });
                            (document.getElementById("videoElem").src = URL.createObjectURL(t)),
                                convertBlobToBase64(t).then((e) => {
                                    console.log("the length is " + recordingData.length),
                                        //fetch("https://tp2function.azurewebsites.net/api/FxBlobReceiver", { method: "POST", body: e })
                                        fetch("http://localhost:7016/api/FxBlobReceiver", { method: "POST", body: e })
                                            .then((e) => e.arrayBuffer())
                                            .then((e) => {
                                                let t = new Blob([new Uint8Array(e)], { type: "image/gif" }),
                                                    r = document.createElement("a");
                                                (r.download = "download.gif"), (r.href = URL.createObjectURL(t)), document.body.appendChild(r), r.click(), document.body.removeChild(r), $("#blinkingText").hide();
                                            }),
                                        (recordingData = []);
                                });
                        }
                    }),
                    mediaRecorder.start();
            })
            .err((e) => {
                console.error(`Error: ${e}`);
            });
    } catch (e) { }
}
window.addEventListener("DOMContentLoaded", function () {
    document.getElementById("startRecording").addEventListener("click", startCapture), document.getElementById("stopRecording").addEventListener("click", stopCapture), $("#blinkingText").hide();
});
