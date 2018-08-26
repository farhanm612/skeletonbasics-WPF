
namespace Kinect.SkeletonBasics
{
    using System;
    using System.Windows.Forms;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    public partial class MainWindow : Window
    {
        private const float RenderWidth = 640.0f;
        private const float RenderHeight = 480.0f;
        private const double JointThickness = 5;
        private const double BodyCenterThickness = 10;
        private const double ClipBoundsThickness = 10;

        private readonly Brush centerPointBrush = Brushes.Blue;
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));        
        private readonly Brush inferredJointBrush = Brushes.Yellow;
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);       
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        private string floorPlane;
        private string dirPath;
        private int filename;
        private List<Skeleton> testFramesList = new List<Skeleton>();
        private string testString = "";
        private bool isRecording = false;

        private KinectSensor sensor;

        private DrawingGroup drawingGroup;

        private DrawingImage imageSource;

        public MainWindow()
        {
            InitializeComponent();
            //t.Start();
            //SendToServer();
        }
        
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            this.drawingGroup = new DrawingGroup();
            this.imageSource = new DrawingImage(this.drawingGroup);
            Image.Source = this.imageSource;

            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {

                this.sensor.SkeletonStream.Enable();
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
                
            }
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            
            
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {   
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
                else {
                    testFramesList.Clear();
                    //testString = "";
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            string A = e.OpenSkeletonFrame().FloorClipPlane.Item1.ToString();
                            string B = e.OpenSkeletonFrame().FloorClipPlane.Item2.ToString();
                            string C = e.OpenSkeletonFrame().FloorClipPlane.Item3.ToString();
                            string D = e.OpenSkeletonFrame().FloorClipPlane.Item4.ToString();
                            floorPlane = "Floor;" + A + ";" + B + ";" + C + ";" + D + ";";
                            if (test_dock.Visibility == Visibility.Hidden)
                            {   
                                testFramesList.Add(skel);
                                FramesForTesting();

                            }
                            else if (isRecording)
                            { WriteToFile(skel,floorPlane); }

                            this.DrawBonesAndJoints(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        private void FramesForTesting()
        {
            
            //JointCollection joint;
            if (testFramesList.Count==120)
            {
                foreach (Skeleton skel in testFramesList)
                {
                    AppendTestString(skel);
                   // WriteToFile(skel);
                }
                testFramesList.Clear();
                SendToServer();
                testString = "";
//               System.Threading.Thread.Sleep(3000);

            }
            return;
        }

        private void AppendTestString(Skeleton skel)
        {
            JointCollection joint = skel.Joints;
            WriteJointToString( joint[JointType.Head]);
            WriteJointToString( joint[JointType.ShoulderCenter]);
            WriteJointToString( joint[JointType.ShoulderRight]);
            WriteJointToString( joint[JointType.ShoulderLeft]);
            WriteJointToString( joint[JointType.ElbowRight]);
            WriteJointToString( joint[JointType.ElbowLeft]);
            WriteJointToString( joint[JointType.WristRight]);
            WriteJointToString( joint[JointType.WristLeft]);
            WriteJointToString( joint[JointType.HandRight]);
            WriteJointToString( joint[JointType.HandLeft]);
            WriteJointToString( joint[JointType.Spine]);
            WriteJointToString( joint[JointType.HipCenter]);
            WriteJointToString( joint[JointType.HipRight]);
            WriteJointToString( joint[JointType.HipLeft]);
            WriteJointToString( joint[JointType.KneeRight]);
            WriteJointToString( joint[JointType.KneeLeft]);
            WriteJointToString( joint[JointType.AnkleRight]);
            WriteJointToString( joint[JointType.AnkleLeft]);
            WriteJointToString( joint[JointType.FootRight]);
            WriteJointToString( joint[JointType.FootLeft]);
        }

        private void WriteJointToString(Joint joint)
        {
            testString += joint.JointType.ToString() + ";" +
                          joint.Position.X.ToString() + ";" +
                          joint.Position.Y.ToString() + ";" + 
                          joint.Position.Z.ToString() + ";";
        }

        private void SendToServer()
        {
            //string line;
            //string text = "";
            //string text = File.ReadAllText(@"C:\Users\Farhan\Desktop\test\x.txt");
            //System.IO.StreamReader file = new System.IO.StreamReader(@"C:\Users\Farhan\Desktop\test\x.txt");
            //while ((line = file.ReadLine()) != null)
           // {
            //    text+=line;
            //}

            //file.Close();
            //Console.WriteLine(text);
            //Console.WriteLine(testString);

            var content = new MultipartFormDataContent();
            //content.Add(text, "value");
            string add = "http://192.168.43.227:5000";
            var client = new HttpClient { BaseAddress = new Uri(add) };
            try
            {
                testString = floorPlane + testString;
                Console.WriteLine(testString);
                var response = client.PostAsync(add, new StringContent(testString)).Result;
                Console.WriteLine(response);
            }
            catch
            {
                return;
            }
            //var response = client.PostAsync("http://pingfyp.herokuapp.com/api/person", content).Result;
            //var result = await client.PostAsync("pingfyp.herokuapp.com/api/person", content);
            //string resultContent = await result.Content.ReadAsStringAsync();
            
            //}
            //return;
        }

        

        private void WriteToFile(Skeleton skel, string floorPlaneEquation)
        {
            string filePath;
            filePath = dirPath + "\\" + filename.ToString() + ".txt";
            JointCollection joint = skel.Joints;
            if (!File.Exists(filePath))
            {
                using (StreamWriter sw = File.CreateText(filePath))
                {
                    sw.WriteLine(floorPlaneEquation);
                    WriteJointToFile(sw, joint[JointType.Head]);
                    WriteJointToFile(sw, joint[JointType.ShoulderCenter]);
                    WriteJointToFile(sw, joint[JointType.ShoulderRight]);
                    WriteJointToFile(sw, joint[JointType.ShoulderLeft]);
                    WriteJointToFile(sw, joint[JointType.ElbowRight]);
                    WriteJointToFile(sw, joint[JointType.ElbowLeft]);
                    WriteJointToFile(sw, joint[JointType.WristRight]);
                    WriteJointToFile(sw, joint[JointType.WristLeft]);
                    WriteJointToFile(sw, joint[JointType.HandRight]);
                    WriteJointToFile(sw, joint[JointType.HandLeft]);
                    WriteJointToFile(sw, joint[JointType.Spine]);
                    WriteJointToFile(sw, joint[JointType.HipCenter]);
                    WriteJointToFile(sw, joint[JointType.HipRight]);
                    WriteJointToFile(sw, joint[JointType.HipLeft]);
                    WriteJointToFile(sw, joint[JointType.KneeRight]);
                    WriteJointToFile(sw, joint[JointType.KneeLeft]);
                    WriteJointToFile(sw, joint[JointType.AnkleRight]);
                    WriteJointToFile(sw, joint[JointType.AnkleLeft]);
                    WriteJointToFile(sw, joint[JointType.FootRight]);
                    WriteJointToFile(sw, joint[JointType.FootLeft]);

                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filePath))
                {
                    WriteJointToFile(sw, joint[JointType.Head]);
                    WriteJointToFile(sw, joint[JointType.ShoulderCenter]);
                    WriteJointToFile(sw, joint[JointType.ShoulderRight]);
                    WriteJointToFile(sw, joint[JointType.ShoulderLeft]);
                    WriteJointToFile(sw, joint[JointType.ElbowRight]);
                    WriteJointToFile(sw, joint[JointType.ElbowLeft]);
                    WriteJointToFile(sw, joint[JointType.WristRight]);
                    WriteJointToFile(sw, joint[JointType.WristLeft]);
                    WriteJointToFile(sw, joint[JointType.HandRight]);
                    WriteJointToFile(sw, joint[JointType.HandLeft]);
                    WriteJointToFile(sw, joint[JointType.Spine]);
                    WriteJointToFile(sw, joint[JointType.HipCenter]);
                    WriteJointToFile(sw, joint[JointType.HipRight]);
                    WriteJointToFile(sw, joint[JointType.HipLeft]);
                    WriteJointToFile(sw, joint[JointType.KneeRight]);
                    WriteJointToFile(sw, joint[JointType.KneeLeft]);
                    WriteJointToFile(sw, joint[JointType.AnkleRight]);
                    WriteJointToFile(sw, joint[JointType.AnkleLeft]);
                    WriteJointToFile(sw, joint[JointType.FootRight]);
                    WriteJointToFile(sw, joint[JointType.FootLeft]);
                }
            }

        }

        private void WriteJointToFile(StreamWriter sw, Joint joint)
        {
            sw.WriteLine(joint.JointType.ToString() + ";" +
                         joint.Position.X.ToString() + ";" +
                         joint.Position.Y.ToString() + ";" +
                         joint.Position.Z.ToString() + ";");
        }

        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);
 
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;                    
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;                    
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];
            
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            isRecording = true;
            dirPath = "C:\\Users\\Farhan\\Dropbox\\data";
            dirPath = dirPath+ "\\" + PersonName.Text;
                if (!Directory.Exists(dirPath))
                {
                Directory.CreateDirectory(dirPath);
                }
            filename = Directory.GetFiles(dirPath, "*").Length;
            filename++;            
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            isRecording = false;
        }

        private void Training_Click(object sender, RoutedEventArgs e)
        {
            this.test_dock.Visibility = Visibility.Visible;
        }

        private void Testing_Click(object sender, RoutedEventArgs e)
        {
            this.test_dock.Visibility = Visibility.Hidden;
            
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
           string dirpath = "C:\\Users\\Farhan\\PycharmProjects\\Code\\data";
            dirpath = dirpath + "\\" + PersonName.Text;
            string[] files = Directory.GetFiles(dirpath);
            string key = PersonName.Text;
            string wasif = "" ; 
            foreach (string filename in files)
            {
                if(filename == "1.txt")
                {
                    wasif = File.ReadAllText(dirPath+"\\"+filename);
                }
                else
                {
                    String[] lines = File.ReadAllLines(Path.Combine(dirpath , filename));
                    for (int i=1; i < lines.GetLength(0); i++)
                    {
                        wasif = wasif + lines[i];
                    }
                }
            }
            
        }
    }
}