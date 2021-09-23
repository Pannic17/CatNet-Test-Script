#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

using System;
using UnityEngine;
using UnityEngine.UI;
using Unity.Barracuda;


using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;

using CVRect = OpenCVForUnity.CoreModule.Rect;

[RequireComponent(typeof(WebCamTextureToMatHelper))]
public class Classification : MonoBehaviour
{

    public ResolutionPreset requestedResolution = ResolutionPreset._640x480;
    public FPSPreset requestedFPS = FPSPreset._30;
    public Toggle rotate90DegreeToggle;
    public Toggle flipVerticalToggle;
    public Toggle flipHorizontalToggle;

    public RawImage inputImage;
    public RawImage captureImage;

    // WebCam
    Texture2D webcam_img; // the texture for webcam display
    WebCamTextureToMatHelper webCamTextureToMatHelper;

    // OpenCV
    private string cat_cascade = "haarcascade_frontalcatface_extended.xml";
    Texture2D cat_img; // the texuture of captured, cropped, normalized cat face
    CVRect[] cat_box; // the outline of captured cat face
    CascadeClassifier cascade;

    // Barracuda
    public NNModel cat_model;
    Model model;
    IWorker worker;
    string[] labels = { "Bicolor", "Calico", "Colorpoint", "Mix", "Orange", "Solid", "Tabby" };
    int count;


    // Use this for initialization
    void Start()
    {
        webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();
        int width, height;
        Dimensions(requestedResolution, out width, out height);
        webCamTextureToMatHelper.requestedWidth = width;
        webCamTextureToMatHelper.requestedHeight = height;
        webCamTextureToMatHelper.requestedFPS = (int)requestedFPS;
        webCamTextureToMatHelper.Initialize();



        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
        {
            RectTransform rt = inputImage.GetComponent<RectTransform>();
            //rt.sizeDelta = new Vector2(720, 1280);
            //rt.localScale = new Vector3(1.6f, 1.6f, 1f);
            rt.sizeDelta = new Vector2(480, 640);
            rt.localScale = new Vector3(2.75f, 2.75f, 1f);
        }

        //////////////////////////////////////////
        /// Cascade initiation
        //////////////////////////////////////////
        cascade = new CascadeClassifier();
        cascade.load(Utils.getFilePath(cat_cascade));
        if (cascade.empty())
        {
            Debug.LogError("Cannot load cascade");
        }
        else
        {
            Debug.Log("Successfuly loaded cascade");
        }


        //////////////////////////////////////////
        /// Barracuda initiation
        //////////////////////////////////////////
        ///
        model = ModelLoader.Load(cat_model);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
        if (worker == null)
        {
            Debug.LogError("Cannot load model");
        }
        else
        {
            Debug.Log("Successfully loaded model");
        }
        count = 0;
    }



    public void OnWebCamTextureToMatHelperInitialized()
    {
        Debug.Log("OnWebCamTextureToMatHelperInitialized");

        Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

        webcam_img = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);

        Utils.fastMatToTexture2D(webCamTextureMat, webcam_img);

        gameObject.GetComponent<Renderer>().material.mainTexture = webcam_img;

        gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);
        Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

        float width = webCamTextureMat.width();
        float height = webCamTextureMat.height();

        float widthScale = (float)Screen.width / width;
        float heightScale = (float)Screen.height / height;
        if (widthScale < heightScale)
        {
            Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
        }
        else
        {
            Camera.main.orthographicSize = height / 2;
        }
    }
    public void OnWebCamTextureToMatHelperDisposed()
    {
        Debug.Log("OnWebCamTextureToMatHelperDisposed");

        if (webcam_img != null)
        {
            Texture2D.Destroy(webcam_img);
            webcam_img = null;
        }

        worker.Dispose();
    }
    public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
    {
        Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);

    }
    void OnDestroy()
    {
        webCamTextureToMatHelper.Dispose();
    }
    public void OnPlayButtonClick()
    {
        webCamTextureToMatHelper.Play();
    }
    public void OnPauseButtonClick()
    {
        webCamTextureToMatHelper.Pause();
    }
    public void OnStopButtonClick()
    {
        webCamTextureToMatHelper.Stop();
    }
    public void OnChangeCameraButtonClick()
    {
        webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.IsFrontFacing();
    }
    public void OnRequestedResolutionDropdownValueChanged(int result)
    {
        if ((int)requestedResolution != result)
        {
            requestedResolution = (ResolutionPreset)result;

            int width, height;
            Dimensions(requestedResolution, out width, out height);

            webCamTextureToMatHelper.Initialize(width, height);
        }
    }
    public void OnRequestedFPSDropdownValueChanged(int result)
    {
        string[] enumNames = Enum.GetNames(typeof(FPSPreset));
        int value = (int)System.Enum.Parse(typeof(FPSPreset), enumNames[result], true);

        if ((int)requestedFPS != value)
        {
            requestedFPS = (FPSPreset)value;

            webCamTextureToMatHelper.requestedFPS = (int)requestedFPS;
        }
    }
    public void OnRotate90DegreeToggleValueChanged()
    {
        if (rotate90DegreeToggle.isOn != webCamTextureToMatHelper.rotate90Degree)
        {
            webCamTextureToMatHelper.rotate90Degree = rotate90DegreeToggle.isOn;
        }
    }
    public void OnFlipVerticalToggleValueChanged()
    {
        if (flipVerticalToggle.isOn != webCamTextureToMatHelper.flipVertical)
        {
            webCamTextureToMatHelper.flipVertical = flipVerticalToggle.isOn;
        }


    }
    public void OnFlipHorizontalToggleValueChanged()
    {
        if (flipHorizontalToggle.isOn != webCamTextureToMatHelper.flipHorizontal)
        {
            webCamTextureToMatHelper.flipHorizontal = flipHorizontalToggle.isOn;
        }
    }
    public enum FPSPreset : int
    {
        _0 = 0,
        _1 = 1,
        _5 = 5,
        _10 = 10,
        _15 = 15,
        _30 = 30,
        _60 = 60,
    }
    public enum ResolutionPreset : byte
    {
        _50x50 = 0,
        _640x480,
        _1280x720,
        _1920x1080,
        _9999x9999,
    }
    private void Dimensions(ResolutionPreset preset, out int width, out int height)
    {
        switch (preset)
        {
            case ResolutionPreset._50x50:
                width = 50;
                height = 50;
                break;
            case ResolutionPreset._640x480:
                width = 640;
                height = 480;
                break;
            case ResolutionPreset._1280x720:
                width = 1280;
                height = 720;
                break;
            case ResolutionPreset._1920x1080:
                width = 1920;
                height = 1080;
                break;
            case ResolutionPreset._9999x9999:
                width = 9999;
                height = 9999;
                break;
            default:
                width = height = 0;
                break;
        }
    }




    ////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////    CUSTOM CODE  ///////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////

    void Update()
    {
        if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
        {

            Mat webcam = webCamTextureToMatHelper.GetMat();
            cat_box = CVProcessor.DetectCatFace(webcam, cascade);

            if (cat_box.Length > 0)
            {
                Debug.Log("Detected cat!");
                Imgproc.rectangle(webcam, cat_box[0].tl(), cat_box[0].br(), new Scalar(255, 0, 0, 255), 2);
            }

            // display with rectangle
            Utils.fastMatToTexture2D(webcam, webcam_img);
            inputImage.texture = webcam_img;
        }
    }


    public void capture()
    {
        // capture and crop the cat face
        Mat img = webCamTextureToMatHelper.GetMat();
        Mat original = img.clone();


        Debug.Log("Capturing...");

        if (cat_box.Length < 1)
        {
            Debug.LogWarning("Failed to capture");
        }
        else
        {
            Debug.Log("Captured Cat!");
            Debug.Log("x:" + cat_box[0].x + ", y:" + cat_box[0].y + ", s:" + cat_box[0].width);

            // crop the face out by the index 0 detected cat face and normalize it
            Mat cropped = CVProcessor.CropCatFace(cat_box, original, 0);
            // calculate main color via kmeans
            Color[] colors = CVProcessor.ColorKMEANS(cropped, 3, CVProcessor.HSV);
            // transform it to Texture2D
            Texture2D captured = new Texture2D(cropped.cols(), cropped.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(cropped, captured);

            cat_img = CVProcessor.Normalize2Tensor(captured);

            captureImage.texture = cat_img;
        }

    }

    public void classify()
    {
        count++;
        string result = CVProcessor.PredictNeuroNetwork(cat_img, worker, labels);
        // Processor.save2JPG(cat_img);
        Debug.Log(count+"######"+result);
    }

}


#endif