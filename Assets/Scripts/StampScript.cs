using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCvSharp;
using OpenCvSharp.Demo;
using System;

public class StampScript : MonoBehaviour
{

    //Canvas which involves UI parts
    public GameObject canvas;
    //Viwer of image
    public RawImage preview;
    //Region of screen shot
    UnityEngine.Rect capRect;
    //Texture of screen shot image
    Texture2D capTexture;
    //Mat:Format of image for OpenCV
    //bgraMat is for color image with alpha channel.
    //binMat is for binarized image.
    Mat bgraMat, binMat;
    //14colors. Please cut & paste from color.txt
    byte[,] colors = { { 255, 255, 255 },{ 18, 0, 230 },
    { 0, 152, 243 }, { 0, 241, 255 }, { 31, 195, 143 },
    { 68, 153, 0 }, { 150, 158, 0 }, { 233, 160, 0 },
    { 183, 104, 0 }, { 136, 32, 29 }, { 131, 7, 146 },
    { 127, 0, 228 }, { 79, 0, 229 }, { 0, 0, 0 } };
    //index of color(colNo=0~13)
    int colorNo = 3;

    //Template object of textured quad.
    public GameObject original;

    //List for holding generated stamps
    List<GameObject> stampList = new List<GameObject>();


    // Start is called before the first frame update
    void Start()
    {
        int w = Screen.width;
        int h = Screen.height;
        //Definition of capture region as (0,0) to (w,h)
        capRect = new UnityEngine.Rect(0, 0, w, h);
        //Creating texture image of the size of capRect
        capTexture =
            new Texture2D(w, h, TextureFormat.RGBA32, false);
        //Applying capTexture as texture of preview area.
        preview.material.mainTexture = capTexture;
    }

    //Function to putting stamp object.
    public void PutObject()
    {
        //Getteing camera.
        Camera cam = Camera.main;
        //Convert left-bottom of screen into 3D space(z=0.6m)
        Vector3 v1 =
        cam.ViewportToWorldPoint(new Vector3(0, 0, 0.6f));
        //Convert right-upper of screen into 3D space(z=0.6m)
        Vector3 v2 =
        cam.ViewportToWorldPoint(new Vector3(1, 1, 0.6f));
        //Convert left-upper of screen into 3D space(z=0.6m)
        Vector3 v3 =
        cam.ViewportToWorldPoint(new Vector3(0, 1, 0.6f));
        //Calculate physical size of stamp.
        float w = Vector3.Distance(v2, v3);
        float h = Vector3.Distance(v1, v3);

        GameObject stamp = GameObject.Instantiate(original);
        //Set position/rotation/size of stamp relative to camera.
        stamp.transform.parent = cam.transform;
        stamp.transform.localPosition = new Vector3(0, 0, 0.6f);
        stamp.transform.localRotation = Quaternion.identity;
        stamp.transform.localScale = new Vector3(w, h, 1);
        //Creating texture to apply the object instantiated above.
        Texture2D stampTexture =
        new Texture2D(capTexture.width, capTexture.height);
        //Setting color and applying texture.
        SetColor(stampTexture);
        stamp.GetComponent<Renderer>().material.mainTexture
        = stampTexture;
        //Detach stamp object from cameara.
        stamp.transform.parent = null;
        //Stamp is memorized and deleted by following code.
        stampList.Add(stamp);
        if (stampList.Count == 10)
        {
            DestroyImmediate(stampList[0].
            GetComponent<Renderer>().material.mainTexture);
            DestroyImmediate(stampList[0]);
            stampList.RemoveAt(0);
        }
        preview.enabled = false;
    }


    IEnumerator ImageProcessing()
    {
        canvas.SetActive(false);//Making UIs invisible
        //Releasing Memories allocated for two Mats
        if (bgraMat != null) { bgraMat.Release(); }
        if (binMat != null) { binMat.Release(); }
        yield return new WaitForEndOfFrame();
        CreateImages();
        SetColor(capTexture);//Setting color to capTexture
        canvas.SetActive(true);//Making UIs visible
        //Show preview area
        preview.enabled = true;
    }

    public void ChangeColor()
    {
        colorNo++;
        colorNo %= colors.Length / 3;
        SetColor(capTexture);
    }


    void SetColor(Texture2D texture)
    {
        if (bgraMat == null || binMat == null) { return; }
        unsafe
        {
            //Get pointer of pixel array of 2 Mats.
            byte* bgraPtr = bgraMat.DataPointer;
            byte* binPtr = binMat.DataPointer;
            //Calculate number of pixels of the image.
            int pixelCount = binMat.Width * binMat.Height;
            //Make white pixels to transparent
            for (int i = 0; i < pixelCount; i++)
            {
                //Address of blue color of i-th pixel
                int bgraPos = i * 4;
                //If i-th pixel of binPtr is 255(white).
                if (binPtr[i] == 255)
                {
                    bgraPtr[bgraPos + 3] = 0;
                }
                //If i-th pixel of binPtr is 0(black).
                else
                {
                    bgraPtr[bgraPos] = colors[colorNo, 0]; //B
                    bgraPtr[bgraPos + 1] = colors[colorNo, 1]; //G
                    bgraPtr[bgraPos + 2] = colors[colorNo, 2]; //R
                    bgraPtr[bgraPos + 3] = 255;
                }
            }
        }
        OpenCvSharp.Unity.MatToTexture(bgraMat, texture);
    }

    void CreateImages()
    {
        capTexture.ReadPixels(capRect, 0, 0);//Starting capture
        capTexture.Apply();//Apply captured image.
        //Conversion Texure2D to Mat
        bgraMat = OpenCvSharp.Unity.TextureToMat(capTexture);
        //Conversion Color Image to Gray Scale Image
        binMat = bgraMat.CvtColor(ColorConversionCodes.BGRA2GRAY);
        //Binarization of image with Otsu’s method.
        binMat = binMat.Threshold(100, 255, ThresholdTypes.Otsu);
        //Conversion Gray Scale to BGRA to change its color later.
        bgraMat = binMat.CvtColor(ColorConversionCodes.GRAY2BGRA);
    }

    public void StartCV()
    {
        StartCoroutine(ImageProcessing());//Calling coroutine 
    }
}