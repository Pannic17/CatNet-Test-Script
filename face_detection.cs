#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using OpenCVForUnity.ObjdetectModule;
using CVRect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// WebCamTextureToMatHelper Example
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class face_detection : MonoBehaviour
    {

        public ResolutionPreset requestedResolution = ResolutionPreset._640x480;
        public FPSPreset requestedFPS = FPSPreset._30;
        public Toggle rotate90DegreeToggle;
        public Toggle flipVerticalToggle;
        public Toggle flipHorizontalToggle;
        Texture2D texture;
        Texture2D cat_img;
        WebCamTextureToMatHelper webCamTextureToMatHelper;
        CVRect[] cat_box;

        public RawImage inputImage;
        public RawImage captureImage;

        string cat_cascade = "haarcascade_frontalcatface_extended.xml";
        Mat webcam_capture;
        MatOfRect detected_cat_face;
        CascadeClassifier cascade;

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
            ///cascade
            //////////////////////////////////////////
            cascade = new CascadeClassifier();
            cascade.load(Utils.getFilePath(cat_cascade));
            if (cascade.empty())
            {
                Debug.LogError("Cannot load cascade");
            }
            else
            {
                Debug.Log("Successful Loaded cascade");
            }
            
        }



        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
            
            Utils.fastMatToTexture2D(webCamTextureMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

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

            // initialized the webcam capture to opencv
            webcam_capture = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC1);
            detected_cat_face = new MatOfRect();
            
            

        }

        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
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
                cat_box = detect_cat_face(webcam);

                if (cat_box.Length > 0)
                {
                    Debug.Log("Detected cat!");
                    Imgproc.rectangle(webcam, cat_box[0].tl(), cat_box[0].br(), new Scalar(255, 0, 0, 255), 2);
                }

                // display with rectangle
                Utils.fastMatToTexture2D(webcam, texture);
                inputImage.texture = texture;
            }
        }

        public CVRect[] detect_cat_face(Mat webcam)
        {
            // detect cat face using opencv cascade
            Imgproc.cvtColor(webcam, webcam_capture, Imgproc.COLOR_RGBA2GRAY);
            cascade.detectMultiScale(webcam_capture, detected_cat_face, 1.02, 5);
            cat_box = detected_cat_face.toArray();

            return cat_box;
        }


        public void capture()
        {
            Mat img = webCamTextureToMatHelper.GetMat();
            Mat original = img.clone();
            Mat crop_face = new Mat();


            //////////////////////////////////////////
            // convert pic to gray
            // Imgproc.cvtColor(capture_img, cat_gray, Imgproc.COLOR_RGBA2GRAY);
            // detect
            // cascade.detectMultiScale(cat_gray, cat_face, 1.05, 5);
            // CVRect[] cat_box = cat_face.toArray();
            //////////////////////////////////////////

            Debug.Log("Capturing...");

            if (cat_box.Length < 1)
            {
                Debug.Log("Failed to capture");
            }
            else
            {
                Debug.Log("Captured Cat!");
                Debug.Log("x:" + cat_box[0].x + ", y:" + cat_box[0].y + ", s:" + cat_box[0].width);

                crop_face = crop_and_enlarge(cat_box, original);

                // display
                Texture2D current = new Texture2D(crop_face.cols(), crop_face.rows(), TextureFormat.RGBA32, false);
                Utils.matToTexture2D(crop_face, current);
                cat_img = normalize_to_tensor(current);
                
                captureImage.texture = cat_img;
            }

        }
        public Mat crop_and_enlarge(CVRect[] area, Mat original)
        {
            // crop the selected area of original image and enlarge it by 120%
            Mat result = new Mat();
            double delta = Math.Ceiling((double)area[0].width * 0.1);
            CVRect enlarged_area = new CVRect((int)(area[0].x - delta),
                                              (int)(area[0].y - delta * 2),
                                              (int)(area[0].width * 1.2),
                                              (int)(area[0].height * 1.2));
            result = new Mat(original, enlarged_area);
            return result;
        }

        public Texture2D normalize_to_tensor(Texture2D original)
        {
            // resize the originally captured cat face to normalized 224*224 for tensor
            Texture2D normalized = new Texture2D(224, 224);

            original.filterMode = FilterMode.Point;
            RenderTexture rt = RenderTexture.GetTemporary(224, 224);
            rt.filterMode = FilterMode.Point;
            RenderTexture.active = rt;
            Graphics.Blit(original, rt);
            
            normalized.ReadPixels(new UnityEngine.Rect(0, 0, 224, 224), 0, 0);
            normalized.Apply();
            RenderTexture.active = null;
            return normalized;
        }

    }
}

#endif