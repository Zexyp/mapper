using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

using System.Windows.Media.Media3D;

using MapMess;

namespace MapTileViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CameraController cameraController;

        public MainWindow()
        {
            InitializeComponent();

            cameraController = new CameraController(MainCamera);

            Model3DGroup modelGroup = new Model3DGroup();
            GeometryModel3D geometryModel = new GeometryModel3D();
            MeshGeometry3D mesh = new MeshGeometry3D();
            ModelVisual3D modelVisual = new ModelVisual3D();

            AmbientLight ambientLight = new AmbientLight();
            ambientLight.Color = Color.FromRgb(255, 255, 255);
            modelGroup.Children.Add(ambientLight);

            BinaryReader binaryReader = new BinaryReader(File.Open("bila.bin", FileMode.Open));
            //MapDownloader.GetMap(21,570398,354669)
            MapReader mapReader = new MapReader(binaryReader);
            mapReader.ReadBinary();
            mapReader.ReadHeader();
            MapMesh mapMesh = mapReader.ReadMesh();

            binaryReader.Close();

            int x = 0;
            int y = 0;
            int z = 0;

            for (int i = 0; i < mapMesh.vertices.Length - 3; i += 3, x = mapMesh.vertices[i], y = mapMesh.vertices[i+1], z = mapMesh.vertices[i+2])
            {
                mesh.Positions.Add(new Point3D((double)x / 65536, (double)y / 65536, (double)z / 65536));
            }
            
            for (int i = 3; i < mapMesh.indices.Length; i++)
            {
                mesh.TriangleIndices.Add(mapMesh.indices[i]);
            }
            
            for (int i = 0; i < mapMesh.internalUVs.Length - 2; i += 2, x = mapMesh.internalUVs[i], y = mapMesh.internalUVs[i + 1])
            {
                mesh.TextureCoordinates.Add(new Point((double)x / 65536, (double)y / 65536));
            }

            //for (int i = 0; i < Mapper.n.indices.Length; i++)
            //{
            //    x = Mapper.n.vertices[Mapper.n.indices[i]*3];
            //    y = Mapper.n.vertices[Mapper.n.indices[i]*3+1];
            //    z = Mapper.n.vertices[Mapper.n.indices[i]*3+2];
            //    int uvx = Mapper.n.internalUVs[Mapper.n.indices[i]*2];
            //    int uvy = Mapper.n.internalUVs[Mapper.n.indices[i]*2+1];
            //    mesh.Positions.Add(new Point3D((double)x / 65536, (double)y / 65536, (double)z / 65536));
            //    mesh.TextureCoordinates.Add(new Point((double)uvx / 65536, (double)uvy / 65536));
            //}

            double maxSize = Math.Max(mapMesh.bboxMax[0] - mapMesh.bboxMin[0], Math.Max(mapMesh.bboxMax[1] - mapMesh.bboxMin[1], mapMesh.bboxMax[2] - mapMesh.bboxMin[2]));
            double xs = mapMesh.bboxMax[0] - mapMesh.bboxMin[0];
            double ys = mapMesh.bboxMax[1] - mapMesh.bboxMin[1];
            double zs = mapMesh.bboxMax[2] - mapMesh.bboxMin[2];
            Vector3D fitScale = new Vector3D((xs / maxSize), (ys / maxSize), (zs / maxSize));

            Transform3DGroup transformGroup = new Transform3DGroup();
            transformGroup.Children.Add(new ScaleTransform3D(fitScale));
            Quaternion quat = new Quaternion(new Vector3D(1, 0, 0), 15) * new Quaternion(new Vector3D(0, 1, 0), -40);
            transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(quat.Axis, quat.Angle)));
            geometryModel.Transform = transformGroup;

            //mesh.Positions.Add(new Point3D(-0.5, -0.5, 0.5));

            //mesh.TriangleIndices.Add(0);

            //mesh.Normals.Add(new Vector3D(0, 0, -1));

            //mesh.TextureCoordinates.Add(new Point(1, 0));

            //MapDownloader.GetConfig();

            //Brush brush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            ImageBrush brush = new ImageBrush();
            brush.ImageSource = new BitmapImage(new Uri("bila.jpg", UriKind.Relative));
            DiffuseMaterial material = new DiffuseMaterial(brush);

            geometryModel.Material = material;
            geometryModel.BackMaterial = material;

            //RotateTransform3D myRotateTransform3D = new RotateTransform3D();
            //AxisAngleRotation3D myAxisAngleRotation3d = new AxisAngleRotation3D();
            //myAxisAngleRotation3d.Axis = new Vector3D(0, 3, 0);
            //myAxisAngleRotation3d.Angle = 40;
            //myRotateTransform3D.Rotation = myAxisAngleRotation3d;
            //geometryModel.Transform = myRotateTransform3D;

            geometryModel.Geometry = mesh;
            modelGroup.Children.Add(geometryModel);
            modelVisual.Content = modelGroup;
            Viewport.Children.Add(modelVisual);
        }

        //bool rotating = false;
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lastMousePos = e.GetPosition(this);
            //if(e.MiddleButton == MouseButtonState.Pressed)
            //{
            //    rotating = true;
            //}
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //if (e.MiddleButton == MouseButtonState.Released)
            //{
            //    rotating = false;
            //}
        }

        Point lastMousePos;
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            InfoLabel.Content = 
                "center: " + cameraController.center.ToString() +
                "\ncamera: " + cameraController.camera.Position.ToString();
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                Point delta = lastMousePos - (Vector)e.GetPosition(this);
                lastMousePos = e.GetPosition(this);
                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    cameraController.Move(new Vector3D(-delta.X / this.ActualHeight * 0.5, -delta.Y / this.ActualHeight * 0.5, 0));
                }
                else
                {
                    cameraController.Rotate(delta.X / this.ActualWidth * 360, delta.Y / this.ActualHeight * 360);
                }
            }
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            cameraController.Zoom((double)e.Delta / 500);
        }
    }

    class CameraController
    {
        public PerspectiveCamera camera;
        public Point3D center = new Point3D(0, 0, 0);
        Vector3D offset = new Vector3D(0, 0, 2);
        Quaternion rotation;
        double yaw = 0;
        double pitch = 0;
        double zoom = 2;

        public CameraController(PerspectiveCamera camera)
        {
            this.camera = camera;
        }

        public void Zoom(double amount)
        {
            zoom += -amount;
            if (zoom < 0) zoom = 0;
            offset = new Vector3D(0, 0, zoom);
            Update();
            //camera.Position += camera.LookDirection * amount;
        }

        public void Move(Vector3D movement)
        {
            Vector3D move = Vector3D.CrossProduct(center - camera.Position, Vector3D.CrossProduct(camera.UpDirection, camera.LookDirection)) * movement.Y * zoom + Vector3D.CrossProduct(camera.UpDirection, camera.LookDirection) * movement.X * zoom;
            center += move;
            Update();
            //camera.Position += move;
            //new TranslateTransform3D(camera.UpDirection * movement.Y + Vector3D.CrossProduct(camera.UpDirection, camera.LookDirection) * movement.X);
            //camera.LookDirection = center - camera.Position;
        }

        public void Rotate(double yaw, double pitch)
        {
            this.yaw += yaw;
            this.pitch += pitch;
            if (this.pitch > 179) this.pitch = 179;
            if (this.pitch < 1) this.pitch = 1;
            Quaternion quatYaw = new Quaternion(new Vector3D(0, 0, 1), this.yaw);
            Quaternion quatPitch = new Quaternion(new Vector3D(1, 0, 0), this.pitch);
            rotation = quatYaw * quatPitch;
            Update();
            //camera.Transform = new RotateTransform3D(new AxisAngleRotation3D(rotation.Axis, rotation.Angle), center);
            //this.rotation *= rotation;
        }

        void Update()
        {
            camera.Position = center + new RotateTransform3D(new AxisAngleRotation3D(rotation.Axis, rotation.Angle), center).Transform(offset);
            camera.LookDirection = center - camera.Position;
            camera.UpDirection = new Vector3D(0, 0, 1);
        }
    }
}
