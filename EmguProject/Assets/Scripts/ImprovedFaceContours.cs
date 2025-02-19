﻿
using Emgu.CV.CvEnum;
using UnityEngine;
using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Collections.Generic;
using Emgu.CV.Util;
using System.Runtime.InteropServices;

namespace FaceRecognition
{
    public class ImprovedFaceContours : MonoBehaviour
    {
        // Webcam.
        private VideoCapture webcamTexture;
        // Webcam image that is copied to a texure2D - Is there a faster method?
        private Texture2D copyTexture;
        // image which is used in OpenCV manipulation.
        Mat img = new Mat();
        // The end result which is displayed on GUITexture.
        private Texture2D resultTexture;
        // List of available webcam devices.
        private WebCamDevice[] devices;
        // Cascade used for detecting faces.
        private CascadeClassifier faceCascade = new CascadeClassifier("Assets\\Resources\\haarcascade_frontalface_default.xml");

        private VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();

        private int[,] hierarchy;

        public ComputeShader shader;


        // Use this for initialization
        void Start()
        {
            // If there is a available webcam.
            webcamTexture = new VideoCapture(0);
            webcamTexture.Start();
        }


        // Update is called once per frame
        void Update()
        {
            webcamTexture.Read(img);
            CvInvoke.CvtColor(img, img, ColorConversion.Bgr2Rgb);
            // Use statement - improves performance.
            using (UMat gray = new UMat())
            {
                // Convert image to gray scale.
                CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);
                // Equalise the lighting.
                CvInvoke.EqualizeHist(gray, gray);
                //CvInvoke.CvtColor(img, img, ColorConversion.Bgr2Gray);

                // Rectanlges which highlight where the face is in the image.
                Rectangle[] faces = null;

                // Detect faces in image.
                faces = faceCascade.DetectMultiScale(gray, 1.15, 5);
                foreach (Rectangle face in faces)
                {
                    using (Mat faceGray = new Mat(img, face))
                    {
                        // Draw a green rectangle around the found area.
                        CvInvoke.Rectangle(img, face, new MCvScalar(0, 255, 0));
                        // Convert ROI to gray-scale.                     
                        CvInvoke.CvtColor(faceGray, faceGray, ColorConversion.Bgr2Gray);
                        // Improves detection of edges.
                        CvInvoke.Blur(faceGray, faceGray, new Size(3, 3), new Point(0, 0));

                        // Convert image to canny to detect edges.
                        //CvInvoke.Canny(faceGray, faceGray, 30, 128, 3, false);
                        //CvInvoke.Sobel(faceGray, faceGray, DepthType.Default, 1, 0, 3);
                        CvInvoke.Threshold(faceGray, faceGray, 100, 255, ThresholdType.BinaryInv);
                        // Hierarchy order of contours. 
                        //hierarchy = CvInvoke.FindContourTree(faceGray, contours, ChainApproxMethod.ChainApproxSimple);
                        // Find the contours in the ROI area.
                        CvInvoke.FindContours(faceGray, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple, face.Location);

                        for (int i = 0; i < contours.Size; ++i)
                            CvInvoke.DrawContours(img, contours, i, new MCvScalar(255, 255, 255));
                    }
                }
            }

            // Update the result texture.
            resultTexture = TextureConvert.InputArrayToTexture2D(img, FlipType.Vertical);
            //RunShader();

            


           // MatToTexture();
            GetComponent<GUITexture>().texture = resultTexture;
            Size s = img.Size;
            GetComponent<GUITexture>().pixelInset = new Rect(s.Width / 2, -s.Height / 2, s.Width, s.Height);
        }


        void OnDestroy()
        {
            webcamTexture.Stop();
        }

        void MatToTexture()
        {

            byte[] bytePixels = new byte[img.Width*img.Height];
            img.CopyTo(bytePixels);
            Color32[] pixels = new Color32[img.Width * img.Height];
            pixels = FromByteArray<Color32>(bytePixels);   
            resultTexture = new Texture2D(webcamTexture.Width, webcamTexture.Height);
            resultTexture.SetPixels32(pixels);
            resultTexture.Apply();

        }
        private T[] FromByteArray<T>(byte[] source) where T : struct
        {
            T[] destination = new T[source.Length / Marshal.SizeOf(typeof(T))];
            GCHandle handle = GCHandle.Alloc(destination, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                Marshal.Copy(source, 0, pointer, source.Length);
                return destination;
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
        }

        void RunShader()
        {
            byte[] data = img.GetData();
            Color32[] output = new Color32[data.Length];
            resultTexture = new Texture2D(img.Width, img.Height);
            ComputeBuffer buffer = new ComputeBuffer(data.Length, 8);
            int kernel = shader.FindKernel("CSMain");
            shader.SetBuffer(kernel, "dataBuffer", buffer);
            shader.Dispatch(kernel, data.Length, 1, 1);
            buffer.GetData(output);
            Debug.Log(output.Length);
            Debug.Log(data[0] + "|" + output[0]);
            resultTexture.SetPixels32(output);
            resultTexture.Apply();
            //buffer.Release();
        }
    }
}